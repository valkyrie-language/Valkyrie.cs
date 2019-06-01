using ClrBackend = Nyar.Assembler.Clr.ClrBackend;
using JvmBackend = Nyar.Assembler.JVM.JvmBackend;
using NativeBackend = Nyar.Assembler.Native.NativeBackend;
using WasmBackend = Nyar.Assembler.Wasm.WasmBackend;
using ValkyrieCompiler = Valkyrie.Compiler.ValkyrieCompiler;

namespace Valkyrie.CLI.Compiler;

/// <summary>
/// VCC 编译器，编译阶段直接调用 Valkyrie.Compiler，运行阶段调用 Valkyrie.Runtime。
/// </summary>
public sealed class VccCompiler
{
    private static readonly string[] SupportedTargets =
        ["nyar", "wasm", "wasip1", "wasip2", "clr", "jvm", "native"];

    private readonly Valkyrie.Runtime.ValkyrieRuntime _runtime;
    private readonly ValkyrieCompiler _compiler;
    private readonly Valkyrie.Compiler.Targets.TargetArtifactEmitter _artifactEmitter;
    private readonly Valkyrie.Compiler.Targets.CanonicalTripleRegistry _canonicalTripleRegistry;

    public Valkyrie.Runtime.ValkyrieRuntime Runtime => _runtime;

    public VccCompiler()
    {
        _runtime = new Valkyrie.Runtime.ValkyrieRuntime();
        _compiler = new ValkyrieCompiler();
        _artifactEmitter = new Valkyrie.Compiler.Targets.TargetArtifactEmitter();
        _canonicalTripleRegistry = new Valkyrie.Compiler.Targets.CanonicalTripleRegistry();

        _artifactEmitter.RegisterBackend(new WasmBackend());
        _artifactEmitter.RegisterBackend(new JvmBackend());
        _artifactEmitter.RegisterBackend(new ClrBackend());
        _artifactEmitter.RegisterBackend(new NativeBackend());
    }

    #region 公开编译 API

    public VccBuildResult Compile(string file, string target, string outputDir, bool verbose)
    {
        if (!File.Exists(file))
        {
            return new VccBuildResult
            {
                Success = false,
                Error = $"源文件不存在 '{file}'"
            };
        }

        if (!TryResolveTarget(target, out var targetInfo))
        {
            return new VccBuildResult
            {
                Success = false,
                Error = $"不支持的编译目标 '{target}'，可选：{string.Join(" / ", SupportedTargets)}"
            };
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var moduleName = Path.GetFileNameWithoutExtension(file);

        if (verbose)
        {
            Console.WriteLine($"  编译：{Path.GetFileName(file)} -> {target}");
        }

        var source = File.ReadAllText(file);
        try
        {
            var artifactSet = CompileToArtifacts(source, moduleName, file, targetInfo);
            return WriteOutputFiles(artifactSet, outputDir, verbose);
        }
        catch (InvalidOperationException ex)
        {
            return new VccBuildResult
            {
                Success = false,
                Error = $"编译失败：{ex.Message}"
            };
        }
    }

    public Nyar.Value CompileAndRun(string file, string target, bool verbose)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($"源文件不存在 '{file}'");
        }

        if (!TryResolveTarget(target, out var targetInfo))
        {
            throw new ArgumentException($"不支持的编译目标 '{target}'，可选：{string.Join(" / ", SupportedTargets)}");
        }

        if (targetInfo.Target.Arch != Nyar.Types.Arch.NyarVm)
        {
            throw new InvalidOperationException("CompileAndRun 仅支持 nyar 目标，请使用 vcc build 生成其他目标产物。");
        }

        var moduleName = Path.GetFileNameWithoutExtension(file);
        if (verbose)
        {
            Console.WriteLine($"  编译并运行：{Path.GetFileName(file)}");
        }

        var source = File.ReadAllText(file);
        var lir = CompileToLir(source, moduleName, file, targetInfo.CanonicalTriple);
        _runtime.LoadModule(lir.Module);
        var runResult = _runtime.Run(moduleName, "main", Array.Empty<Nyar.Value>());

        if (verbose && runResult.Type != Nyar.ValueType.Null)
        {
            Console.WriteLine($"  结果：{FormatValue(runResult)}");
        }

        return runResult;
    }

    public Nyar.Value RunNyarFile(string file, bool verbose)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($".nyar 文件不存在 '{file}'");
        }

        var bytecode = File.ReadAllBytes(file);
        var moduleName = Path.GetFileNameWithoutExtension(file);

        if (verbose)
        {
            Console.WriteLine($"  加载模块：{moduleName}（{bytecode.Length} 字节）");
        }

        _runtime.LoadBytecode(bytecode);
        var result = _runtime.Run(moduleName, "main", Array.Empty<Nyar.Value>());

        if (verbose && result.Type != Nyar.ValueType.Null)
        {
            Console.WriteLine($"  结果：{FormatValue(result)}");
        }

        return result;
    }

    #endregion

    #region 编译主链

    private Valkyrie.Compiler.Pipeline.ArtifactSet CompileToArtifacts(
        string source,
        string moduleName,
        string filePath,
        TargetInfo targetInfo)
    {
        var lir = CompileToLir(source, moduleName, filePath, targetInfo.CanonicalTriple);
        return _artifactEmitter.Emit(lir.Module, targetInfo.Target);
    }

    private Valkyrie.Compiler.Lir.LirModule CompileToLir(
        string source,
        string moduleName,
        string filePath,
        string canonicalTriple)
    {
        var plan = new Valkyrie.Compiler.Pipeline.BuildPlan(moduleName, canonicalTriple, filePath);
        var targetContract = _canonicalTripleRegistry.Resolve(canonicalTriple);

        _compiler.Diagnostics.Clear();
        var tokens = _compiler.Lex(source);
        if (_compiler.Diagnostics.HasErrors)
        {
            throw new InvalidOperationException("词法分析失败。");
        }

        var ast = _compiler.Parse(tokens);
        if (_compiler.Diagnostics.HasErrors)
        {
            throw new InvalidOperationException("语法分析失败。");
        }

        var semantics = _compiler.Analyze(ast, plan);
        if (semantics.HasErrors)
        {
            var message = string.Join("; ", semantics.Diagnostics.Select(diagnostic => diagnostic.Message));
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "语义分析失败。" : message);
        }

        var hir = _compiler.BuildHir(ast, semantics, plan);
        var mir = _compiler.BuildMir(hir, plan, targetContract);
        return _compiler.BuildLir(mir, plan);
    }

    #endregion

    #region 辅助方法

    private static bool TryResolveTarget(string target, out TargetInfo targetInfo)
    {
        targetInfo = default;
        if (!Nyar.Types.TargetTriple.TryParse(target, out var triple))
        {
            return false;
        }

        targetInfo = new TargetInfo(triple.ToCompilationTarget(), triple.ToString());
        return true;
    }

    private static VccBuildResult WriteOutputFiles(
        Valkyrie.Compiler.Pipeline.ArtifactSet artifactSet,
        string outputDir,
        bool verbose)
    {
        var artifacts = EnumerateArtifacts(artifactSet).ToList();
        var fileNames = new List<string>(artifacts.Count);
        var mainArtifact = string.Empty;

        foreach (var artifact in artifacts)
        {
            var filePath = Path.Combine(outputDir, artifact.Name);
            File.WriteAllBytes(filePath, artifact.Content);
            fileNames.Add(artifact.Name);

            if (verbose)
            {
                Console.WriteLine($"  生成：{artifact.Name}（{artifact.Content.Length} 字节）");
            }
        }

        if (fileNames.Count > 0)
        {
            mainArtifact = Path.Combine(outputDir, fileNames[0]);
        }

        return new VccBuildResult
        {
            Success = true,
            OutputDirectory = outputDir,
            OutputFiles = fileNames,
            MainArtifact = mainArtifact
        };
    }

    private static IEnumerable<Valkyrie.Compiler.Pipeline.CompilerArtifact> EnumerateArtifacts(
        Valkyrie.Compiler.Pipeline.ArtifactSet artifactSet)
    {
        yield return artifactSet.PrimaryArtifact;

        foreach (var artifact in artifactSet.SidecarArtifacts)
        {
            yield return artifact;
        }

        foreach (var artifact in artifactSet.DebugArtifacts)
        {
            yield return artifact;
        }
    }

    private static string FormatValue(Nyar.Value value)
    {
        return value.Type switch
        {
            Nyar.ValueType.Int => value.Int.ToString(),
            Nyar.ValueType.Long => value.Long.ToString(),
            Nyar.ValueType.Double => value.Double.ToString(),
            Nyar.ValueType.Bool => value.Bool.ToString(),
            Nyar.ValueType.String => value.String?.ToString() ?? "null",
            Nyar.ValueType.Null => "null",
            _ => $"<{value.Type}>"
        };
    }

    private readonly record struct TargetInfo(Nyar.Types.CompilationTarget Target, string CanonicalTriple);

    #endregion
}
