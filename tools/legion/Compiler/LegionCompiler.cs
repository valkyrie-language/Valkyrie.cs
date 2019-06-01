using AsmCompilationUnit = Nyar.Assembler.CompilationUnit;
using ClrBackend = Nyar.Assembler.Clr.ClrBackend;
using MicroDeclaration = Oak.Valkyrie.AST.Declaration.MicroDeclaration;
using JvmBackend = Nyar.Assembler.JVM.JvmBackend;
using NativeBackend = Nyar.Assembler.Native.NativeBackend;
using WasmBackend = Nyar.Assembler.Wasm.WasmBackend;
using Iris.CLI;
using Legion.Package;
using Nyar;
using Nyar.Types;
using Valkyrie.Compiler;
using Valkyrie.Compiler.Pipeline;
using Valkyrie.Compiler.Targets;
using Nyar.Assembler;
using Legion.CLI.Target;
using Nyar.VM.Bytecode;

namespace Legion.CLI.Compiler;

/// <summary>
/// Legion 构建编译器
/// 统一走 Valkyrie.Compiler（AST/HIR/MIR/LIR + target emitter）
/// 每个 [main] 函数生成独立产物，基于依赖分析裁剪未使用代码
/// 默认链接 std 库及对应平台的 std.adaptor
/// </summary>
public sealed class LegionCompiler
{
    private static readonly string[] SupportedTargets = ["nyar", "gnosis", "wasm", "wasip1", "wasip2", "clr", "jvm", "native"];

    private static readonly HashSet<CgOpcode> CallOpcodes =
    [
        CgOpcode.Call,
        CgOpcode.CallStatic
    ];

    private readonly TargetArtifactEmitter _artifactEmitter;
    private readonly ValkyrieCompiler _compiler;
    private readonly CanonicalTripleRegistry _canonicalTripleRegistry;
    private readonly TargetTripleResolver _targetResolver = new();

    public LegionCompiler()
    {
        _artifactEmitter = new TargetArtifactEmitter();
        _compiler = new ValkyrieCompiler();
        _canonicalTripleRegistry = new CanonicalTripleRegistry();
        _artifactEmitter.RegisterBackend(new WasmBackend());
        _artifactEmitter.RegisterBackend(new JvmBackend());
        _artifactEmitter.RegisterBackend(new ClrBackend());
        _artifactEmitter.RegisterBackend(new NativeBackend());
    }

    #region 公开编译 API

    public LegionBuildResult Build(string projectDir, string outputDir, string target, bool verbose)
    {
        return Build(projectDir, outputDir, target, verbose, null);
    }

    public LegionBuildResult Build(string projectDir, string outputDir, string target, bool verbose, BuildTarget? buildTargetOptions)
    {
        var triple = _targetResolver.Resolve(target);
        if (triple == null)
        {
            return new LegionBuildResult { Success = false, Error = $"不支持的目标三元组 '{target}'" };
        }

        var moduleName = Path.GetFileName(projectDir);

        if (verbose)
        {
            Console.WriteLine($"[Legion] 开始构建项目: {moduleName}");
            Console.WriteLine($"[Legion] 目标: {triple}");
        }

        var sourceCollectResult = CollectBuildSources(projectDir, triple, verbose);
        if (!sourceCollectResult.Success)
        {
            return new LegionBuildResult { Success = false, Error = sourceCollectResult.Error };
        }

        var vFiles = sourceCollectResult.Files;
        var mainFunctions = ScanMainFunctions(vFiles);

        if (mainFunctions.Count == 0)
        {
            return new LegionBuildResult { Success = false, Error = "未找到入口函数（需标记为 [main]）" };
        }

        var fullUnit = CompileAllVFiles(vFiles, moduleName, triple, verbose, projectDir);
        if (fullUnit == null)
        {
            return new LegionBuildResult { Success = false, Error = "源文件编译失败" };
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var generatedFiles = new List<string>();
        
        foreach (var mainFunc in mainFunctions)
        {
            if (verbose)
            {
                Console.WriteLine($"[Legion] 为入口点生成代码: {mainFunc}");
            }

            var prunedUnit = PruneUnusedCode(fullUnit, mainFunc);
            
            // 为每个入口点使用唯一的名称，避免互相覆盖
            var entryUnit = new AsmCompilationUnit(mainFunc);
            foreach (var func in prunedUnit.Functions) entryUnit.AddFunction(func);
            foreach (var imp in prunedUnit.Imports) entryUnit.AddImport(imp);
            foreach (var exp in prunedUnit.Exports) entryUnit.AddExport(exp);

            var options = new CompilationOptions
            {
                Target = triple,
                GenerateWat = buildTargetOptions?.Wat ?? false,
                GenerateSourceMap = buildTargetOptions?.SourceMap ?? false,
                GenerateTypeScriptDecls = buildTargetOptions?.TypeScript ?? false,
                GenerateMsil = buildTargetOptions?.Msil ?? false
            };

            List<CompilerArtifact> artifacts;
            try
            {
                artifacts = BuildArtifacts(entryUnit, triple, options, mainFunc);
            }
            catch (Exception ex)
            {
                return new LegionBuildResult { Success = false, Error = ex.Message };
            }

            foreach (var artifact in artifacts)
            {
                var filePath = Path.Combine(outputDir, artifact.Name);
                File.WriteAllBytes(filePath, artifact.Content);
                generatedFiles.Add(artifact.Name);
                if (verbose)
                {
                    Console.WriteLine($"[Legion] 已生成产物: {filePath}");
                }
            }
        }

        return new LegionBuildResult
        {
            Success = true,
            OutputDirectory = outputDir,
            OutputFiles = generatedFiles,
            Error = string.Empty
        };
    }

    public LegionBuildResult BuildIncremental(string projectDir, string outputDir, string target, bool verbose)
    {
        return new LegionBuildResult { Success = false, Error = "增量构建暂不可用" };
    }

    public void Clean(string projectDir)
    {
        var distDir = Path.Combine(projectDir, "dist");
        if (Directory.Exists(distDir))
        {
            Directory.Delete(distDir, true);
        }
    }

    public byte[] CompileToNyar(string source, string moduleName)
    {
        var target = _targetResolver.Resolve("nyar")!;
        var unit = CompileSourceToCgModule(source, moduleName, target, moduleName, verbose: false);
        if (unit == null)
        {
            return [];
        }

        try
        {
            var artifactSet = _artifactEmitter.Emit(unit, target, new CompilationOptions { Target = target });
            return artifactSet.PrimaryArtifact.Content;
        }
        catch
        {
            return [];
        }
    }

    #endregion

    #region 私有分析辅助

    private SourceCollectResult CollectBuildSources(string projectDir, CompilationTarget target, bool verbose)
    {
        var files = new List<string>();

        var projectSourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(projectSourceDir))
        {
            return SourceCollectResult.Fail($"源码目录不存在 '{projectSourceDir}'");
        }

        files.AddRange(Directory.GetFiles(projectSourceDir, "*.v", SearchOption.AllDirectories));

        var examplesRootDir = Path.GetDirectoryName(projectDir);
        if (string.IsNullOrEmpty(examplesRootDir))
        {
            return SourceCollectResult.Ok(files);
        }

        var stdProjectDir = Path.Combine(examplesRootDir, "std");
        var stdSourceDir = Path.Combine(stdProjectDir, "source");
        if (!Directory.Exists(stdSourceDir))
        {
            return SourceCollectResult.Ok(files);
        }

        var stdManifest = LegionManifest.Load(stdProjectDir);
        files.AddRange(Directory.GetFiles(stdSourceDir, "*.v", SearchOption.AllDirectories));

        var adaptorPackageName = ResolveStdAdaptorPackageName(target);
        if (adaptorPackageName is null)
        {
            return SourceCollectResult.Ok(files);
        }

        if (!stdManifest.Dependencies.ContainsKey(adaptorPackageName))
        {
            return SourceCollectResult.Fail($"`std` 未声明目标适配器依赖：{adaptorPackageName}");
        }

        var adaptorProjectDir = Path.Combine(examplesRootDir, adaptorPackageName);
        var adaptorSourceDir = Path.Combine(adaptorProjectDir, "source");
        if (!Directory.Exists(adaptorSourceDir))
        {
            return SourceCollectResult.Fail($"适配器源码目录不存在：'{adaptorSourceDir}'");
        }

        files.AddRange(Directory.GetFiles(adaptorSourceDir, "*.v", SearchOption.AllDirectories));

        if (verbose)
        {
            Console.WriteLine($"[Legion] 已链接标准库: {stdProjectDir}");
            Console.WriteLine($"[Legion] 已链接标准适配器: {adaptorPackageName}");
        }

        return SourceCollectResult.Ok(files.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string? ResolveStdAdaptorPackageName(CompilationTarget target)
    {
        return target.Arch switch
        {
            Arch.NyarVm => "std.adaptor.nyar",
            Arch.Clr => "std.adaptor.dotnet",
            Arch.Jvm => "std.adaptor.jvm",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP1 => "std.adaptor.wasip1",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP2 => "std.adaptor.wasip2",
            Arch.Wasm32 or Arch.Wasm64 => "std.adaptor.wasm",
            Arch.Native when target.OS == OS.Windows => "std.adaptor.windows",
            Arch.Native when target.OS == OS.Linux => "std.adaptor.linux",
            Arch.Native when target.OS == OS.macOS => "std.adaptor.macos",
            _ => null
        };
    }

    private List<string> ScanMainFunctions(string[] vFiles)
    {
        var mainFuncs = new List<string>();
        foreach (var file in vFiles)
        {
            var source = File.ReadAllText(file);
            _compiler.Diagnostics.Clear();
            var tokens = _compiler.Lex(source);
            var ast = _compiler.Parse(tokens);
            if (_compiler.Diagnostics.HasErrors)
            {
                continue;
            }

            foreach (var decl in ast.Declarations)
            {
                if (decl is MicroDeclaration func && func.Attributes.Any(a => a.Name == "main"))
                {
                    mainFuncs.Add(func.Name);
                }
            }
        }

        return mainFuncs;
    }

    private static AsmCompilationUnit PruneUnusedCode(AsmCompilationUnit fullUnit, string entryFunc)
    {
        var prunedUnit = new AsmCompilationUnit(fullUnit.Name);
        var visited = new HashSet<string>();
        var queue = new Queue<string>();

        var funcMap = fullUnit.Functions.ToDictionary(f => f.Name, f => f);

        if (!funcMap.ContainsKey(entryFunc))
        {
            return prunedUnit;
        }

        queue.Enqueue(entryFunc);
        visited.Add(entryFunc);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!funcMap.TryGetValue(current, out var func))
            {
                continue;
            }

            foreach (var callee in GetCallees(func))
            {
                if (!visited.Contains(callee))
                {
                    visited.Add(callee);
                    queue.Enqueue(callee);
                }
            }
        }

        foreach (var funcName in visited)
        {
            if (funcMap.TryGetValue(funcName, out var func))
            {
                prunedUnit.AddFunction(func);
            }
        }

        foreach (var import in fullUnit.Imports)
        {
            prunedUnit.AddImport(import);
        }

        foreach (var export in fullUnit.Exports)
        {
            if (visited.Contains(export.Name))
            {
                var funcIndex = prunedUnit.Functions.FindIndex(f => f.Name == export.Name);
                if (funcIndex >= 0)
                {
                    prunedUnit.AddExport(new CgModuleExport(export.Name, export.Kind, funcIndex));
                }
            }
        }

        return prunedUnit;
    }

    private static IEnumerable<string> GetCallees(CgFunction func)
    {
        foreach (var instr in func.Instructions)
        {
            if (!CallOpcodes.Contains(instr.Opcode))
            {
                continue;
            }

            foreach (var operand in instr.Operands)
            {
                if (operand is CgOperand.FuncRef funcRef && !string.IsNullOrEmpty(funcRef.Name))
                {
                    yield return funcRef.Name;
                }
            }
        }
    }

    #endregion

    #region 共享编译管线

    private AsmCompilationUnit? CompileAllVFiles(
        string[] vFiles,
        string moduleName,
        CompilationTarget target,
        bool verbose,
        string projectDir)
    {
        var combinedUnit = new AsmCompilationUnit(moduleName);

        foreach (var vFile in vFiles)
        {
            if (verbose)
            {
                Console.WriteLine($"[Legion] 编译源文件: {vFile}");
            }

            var source = File.ReadAllText(vFile);
            var unit = CompileSourceToCgModule(source, moduleName, target, vFile, verbose);
            if (unit == null)
            {
                if (IsStdSourceFile(projectDir, vFile))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"[Legion] 标准库文件暂不可编译，已跳过: {vFile}");
                    }

                    continue;
                }

                if (verbose)
                {
                    Console.WriteLine($"[Legion] 文件编译失败: {vFile}");
                }

                return null;
            }

            MergeUnits(combinedUnit, unit);
        }

        return combinedUnit;
    }

    private static bool IsStdSourceFile(string projectDir, string filePath)
    {
        var examplesRootDir = Path.GetDirectoryName(projectDir);
        if (string.IsNullOrEmpty(examplesRootDir))
        {
            return false;
        }

        var stdSourceDir = Path.Combine(examplesRootDir, "std", "source");
        var normalizedStd = Path.GetFullPath(stdSourceDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedFile = Path.GetFullPath(filePath);
        return normalizedFile.StartsWith(normalizedStd, StringComparison.OrdinalIgnoreCase);
    }

    private AsmCompilationUnit? CompileSourceToCgModule(
        string source,
        string moduleName,
        CompilationTarget target,
        string filePath,
        bool verbose)
    {
        var canonicalTriple = ResolveCanonicalTriple(target);
        if (string.IsNullOrWhiteSpace(canonicalTriple))
        {
            return null;
        }

        var plan = new Valkyrie.Compiler.Pipeline.BuildPlan(moduleName, canonicalTriple, filePath);

        _compiler.Diagnostics.Clear();
        var tokens = _compiler.Lex(source);
        if (_compiler.Diagnostics.HasErrors)
        {
            PrintCompilerErrors(filePath, verbose);
            return null;
        }

        var ast = _compiler.Parse(tokens);
        if (_compiler.Diagnostics.HasErrors)
        {
            PrintCompilerErrors(filePath, verbose);
            return null;
        }

        var semantics = _compiler.Analyze(ast, plan);
        if (semantics.HasErrors)
        {
            PrintCompilerErrors(filePath, verbose);
            return null;
        }

        TargetContract targetContract;
        try
        {
            targetContract = _canonicalTripleRegistry.Resolve(canonicalTriple);
        }
        catch (NotSupportedException)
        {
            return null;
        }

        var hir = _compiler.BuildHir(ast, semantics, plan);
        var mir = _compiler.BuildMir(hir, plan, targetContract);
        var lir = _compiler.BuildLir(mir, plan);
        return CloneToCompilationUnit(moduleName, lir.Module);
    }

    private static AsmCompilationUnit CloneToCompilationUnit(string moduleName, CgModule source)
    {
        var unit = new AsmCompilationUnit(moduleName);
        foreach (var function in source.Functions)
        {
            unit.AddFunction(function);
        }

        foreach (var import in source.Imports)
        {
            unit.AddImport(import);
        }

        foreach (var export in source.Exports)
        {
            unit.AddExport(export);
        }

        return unit;
    }

    private static string? ResolveCanonicalTriple(CompilationTarget target)
    {
        return target.Arch switch
        {
            Arch.NyarVm => "nyarvm-standard",
            Arch.Jvm => "jvm-openjdk-linux",
            Arch.Clr => "clr-microsoft-windows",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP1 => "wasm32-unknown-wasi-wasip1",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP2 => "wasm32-unknown-wasi-wasip2",
            Arch.Wasm32 or Arch.Wasm64 => "wasm32-unknown-browser",
            _ => null
        };
    }

    private void PrintCompilerErrors(string filePath, bool verbose)
    {
        if (!verbose)
        {
            return;
        }

        foreach (var message in _compiler.Diagnostics.Messages)
        {
            Console.WriteLine($"[Legion] 编译诊断 {filePath}: {message.Level} {message.Code} {message.Message}");
        }
    }

    private static void MergeUnits(AsmCompilationUnit target, CgModule source)
    {
        foreach (var func in source.Functions)
        {
            if (target.Functions.All(f => f.Name != func.Name))
            {
                target.AddFunction(func);
            }
        }

        foreach (var import in source.Imports)
        {
            if (target.Imports.All(i => i.ModuleName != import.ModuleName))
            {
                target.AddImport(import);
            }
        }

        foreach (var export in source.Exports)
        {
            if (target.Exports.All(e => e.Name != export.Name))
            {
                target.AddExport(export);
            }
        }
    }

    private static IEnumerable<CompilerArtifact> EnumerateArtifacts(ArtifactSet artifactSet)
    {
        yield return artifactSet.PrimaryArtifact;

        foreach (var sidecar in artifactSet.SidecarArtifacts)
        {
            yield return sidecar;
        }

        foreach (var debug in artifactSet.DebugArtifacts)
        {
            yield return debug;
        }
    }

    private List<CompilerArtifact> BuildArtifacts(
        AsmCompilationUnit entryUnit,
        CompilationTarget target,
        CompilationOptions options,
        string mainFunc)
    {
        if (target.Arch == Arch.NyarVm)
        {
            // `nyar` 目标当前没有独立后端，直接复用运行时序列化路径输出 `.nyar` 二进制。
            var runtimeModule = CompilationUnitSerializer.Serialize(entryUnit);
            var bytecode = NyarModuleConverter.Encode(runtimeModule);
            return [new CompilerArtifact($"{mainFunc}.nyar", bytecode, "application/x-nyar")];
        }

        var artifactSet = _artifactEmitter.Emit(entryUnit, target, options);
        return EnumerateArtifacts(artifactSet).ToList();
    }

    private sealed class SourceCollectResult
    {
        private SourceCollectResult(bool success, string[] files, string? error)
        {
            Success = success;
            Files = files;
            Error = error;
        }

        public bool Success { get; }
        public string[] Files { get; }
        public string? Error { get; }

        public static SourceCollectResult Ok(List<string> files)
        {
            return new SourceCollectResult(true, files.ToArray(), null);
        }

        public static SourceCollectResult Fail(string error)
        {
            return new SourceCollectResult(false, [], error);
        }
    }

    #endregion
}
