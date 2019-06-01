using AsmCompilationUnit = Nyar.Assembler.CompilationUnit;
using ClrBackend = Nyar.Assembler.Clr.ClrBackend;
using JvmBackend = Nyar.Assembler.JVM.JvmBackend;
using NativeBackend = Nyar.Assembler.Native.NativeBackend;
using WasmBackend = Nyar.Assembler.Wasm.WasmBackend;
using CompilationTarget = Nyar.Types.CompilationTarget;
using TargetTriple = Nyar.Types.TargetTriple;
using ValkyrieCompiler = Valkyrie.Compiler.ValkyrieCompiler;

namespace Asgard.CLI.Compiler;

/// <summary>
///     VOA 多目标构建器。
///     WASM 目标使用 VoaCompiler 的完整管线（含 AWSL 编译），
///     非 WASM 目标直接走 Valkyrie.Compiler + TargetArtifactEmitter。
/// </summary>
public sealed class VoaMultiTargetBuilder
{
    private static readonly string[] SupportedTargets =
        ["wasm", "wasip1", "wasip2", "clr", "jvm", "native", "nyar", "gnosis"];

    private readonly VoaCompiler _voaCompiler;
    private readonly ValkyrieCompiler _compiler;
    private readonly Valkyrie.Compiler.Targets.TargetArtifactEmitter _artifactEmitter;
    private readonly Valkyrie.Compiler.Targets.CanonicalTripleRegistry _canonicalTripleRegistry;

    /// <summary>
    ///     创建 VOA 多目标构建器实例
    /// </summary>
    public VoaMultiTargetBuilder()
    {
        _voaCompiler = new VoaCompiler();
        _compiler = new ValkyrieCompiler();
        _artifactEmitter = new Valkyrie.Compiler.Targets.TargetArtifactEmitter();
        _canonicalTripleRegistry = new Valkyrie.Compiler.Targets.CanonicalTripleRegistry();

        _artifactEmitter.RegisterBackend(new WasmBackend());
        _artifactEmitter.RegisterBackend(new JvmBackend());
        _artifactEmitter.RegisterBackend(new ClrBackend());
        _artifactEmitter.RegisterBackend(new NativeBackend());
    }

    /// <summary>
    ///     构建项目到指定目标平台。
    /// </summary>
    /// <param name="config">构建配置</param>
    /// <param name="projectDir">项目目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="target">目标平台标识</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <returns>构建结果</returns>
    public VoaBuildResult Build(VoaBuildConfig config, string projectDir, string outputDir, string target, bool verbose)
    {
        if (Array.IndexOf(SupportedTargets, target) < 0)
        {
            return new VoaBuildResult
            {
                Success = false,
                Error = $"不支持的编译目标 '{target}'，可选：{string.Join(" / ", SupportedTargets)}"
            };
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        return target.ToLowerInvariant() switch
        {
            "wasm" or "wasip1" or "wasip2" => BuildWasmTargets(projectDir, outputDir, target, verbose),
            _ => BuildNonWasmTarget(projectDir, outputDir, target, verbose)
        };
    }

    #region WASM 目标构建

    /// <summary>
    ///     构建 WASM 系列目标（wasm / wasip1 / wasip2），
    ///     使用 VoaCompiler 的完整管线，支持 .v �?.awsl 文件
    /// </summary>
    private VoaBuildResult BuildWasmTargets(string projectDir, string outputDir, string target, bool verbose)
    {
        if (verbose)
        {
            Console.WriteLine($"  使用 VOA WASM 管线构建目标：{target}");
        }

        var result = _voaCompiler.Build(projectDir, outputDir, target, verbose);

        if (!result.Success)
        {
            return result;
        }

        if (target != "wasm")
        {
            var compilationTarget = ParseTarget(target);
            if (compilationTarget is not null)
            {
                var sourceDir = Path.Combine(projectDir, "source");
                if (Directory.Exists(sourceDir))
                {
                    var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);
                    if (vFiles.Length > 0)
                    {
                        var moduleName = Path.GetFileName(projectDir);
                        foreach (var vFile in vFiles)
                        {
                            var source = File.ReadAllText(vFile);
                            if (!TryResolveTarget(target, out var targetInfo))
                            {
                                return new VoaBuildResult
                                {
                                    Success = false,
                                    Error = $"无法解析编译目标 '{target}'"
                                };
                            }

                            var artifactSet = CompileToArtifacts(source, moduleName, vFile, targetInfo);
                            WriteOutputFiles(artifactSet, outputDir, verbose);
                        }
                    }
                }
            }
        }

        return result;
    }

    #endregion

    #region 非 WASM 目标构建

    /// <summary>
    ///     构建非 WASM 目标（clr / jvm / native / nyar / gnosis），
    ///     编译流统一委托给 Valkyrie.Compiler。
    /// </summary>
    private VoaBuildResult BuildNonWasmTarget(string projectDir, string outputDir, string target, bool verbose)
    {
        var compilationTarget = ParseTarget(target);
        if (compilationTarget is null)
        {
            return new VoaBuildResult
            {
                Success = false,
                Error = $"无法解析编译目标 '{target}'"
            };
        }

        var sourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(sourceDir))
        {
            return new VoaBuildResult
            {
                Success = false,
                Error = $"源码目录不存在 '{sourceDir}'"
            };
        }

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);
        var awslFiles = Directory.GetFiles(sourceDir, "*.awsl", SearchOption.AllDirectories);

        if (verbose)
        {
            Console.WriteLine($"  找到 {vFiles.Length} 个 .v 文件，{awslFiles.Length} 个 .awsl 文件");
        }

        if (vFiles.Length == 0 && awslFiles.Length == 0)
        {
            return new VoaBuildResult
            {
                Success = false,
                Error = "未找到任何源码文件（.v 或 .awsl）"
            };
        }

        var moduleName = Path.GetFileName(projectDir);
        var allOutputFiles = new List<string>();

        foreach (var vFile in vFiles)
        {
            if (verbose)
            {
                Console.WriteLine($"  编译 .v 文件：{Path.GetFileName(vFile)}");
            }

            var source = File.ReadAllText(vFile);
            if (!TryResolveTarget(target, out var targetInfo))
            {
                return new VoaBuildResult
                {
                    Success = false,
                    Error = $"无法解析编译目标 '{target}'"
                };
            }

            try
            {
                var artifactSet = CompileToArtifacts(source, moduleName, vFile, targetInfo);
                var writtenFiles = WriteOutputFiles(artifactSet, outputDir, verbose);
                allOutputFiles.AddRange(writtenFiles);
            }
            catch (InvalidOperationException ex)
            {
                return new VoaBuildResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        if (awslFiles.Length > 0)
        {
            var awslResults = CompileAllAwslFiles(awslFiles, moduleName, verbose);
            var css = MergeCss(awslResults);
            if (css.Length > 0)
            {
                var cssPath = Path.Combine(outputDir, $"{moduleName}.css");
                File.WriteAllText(cssPath, css, System.Text.Encoding.UTF8);
                allOutputFiles.Add(cssPath);

                if (verbose)
                {
                    Console.WriteLine($"  写入 {cssPath}");
                }
            }
        }

        if (verbose)
        {
            Console.WriteLine($"VOA 构建完成，共生成 {allOutputFiles.Count} 个文件");
        }

        return new VoaBuildResult
        {
            Success = true,
            OutputDirectory = outputDir,
            OutputFiles = allOutputFiles
        };
    }

    #endregion

    #region AWSL 编译

    /// <summary>
    ///     编译所有 AWSL 文件。
    /// </summary>
    private List<AwslCompileResult> CompileAllAwslFiles(string[] awslFiles, string moduleName, bool verbose)
    {
        var results = new List<AwslCompileResult>();

        var componentNames = awslFiles
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var awslCompiler = new AwslReactiveCompiler();
        awslCompiler.RegisterComponentNames(componentNames);

        var widgetParser = new WidgetParser();

        foreach (var awslFile in awslFiles)
        {
            if (verbose)
            {
                Console.WriteLine($"  编译 .awsl 文件：{Path.GetFileName(awslFile)}");
            }

            var source = File.ReadAllText(awslFile);
            var parseResult = widgetParser.Parse(source, Path.GetFileName(awslFile));
            var compileResult = awslCompiler.Compile(parseResult, moduleName);
            results.Add(compileResult);
        }

        return results;
    }

    /// <summary>
    ///     合并所有 AWSL 组件生成的 CSS。
    /// </summary>
    private static string MergeCss(List<AwslCompileResult> awslResults)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("/* VOA 组件样式 - 自动生成 */");

        foreach (var r in awslResults)
        {
            if (!string.IsNullOrWhiteSpace(r.Css))
            {
                sb.AppendLine();
                sb.AppendLine($"/* {r.ComponentName} */");
                sb.Append(r.Css);

                if (!r.Css.EndsWith('\n'))
                {
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     将目标字符串解析为 CompilationTarget。
    /// </summary>
    private static CompilationTarget? ParseTarget(string target)
    {
        if (TargetTriple.TryParse(target, out var triple))
        {
            return triple.ToCompilationTarget();
        }

        return null;
    }

    /// <summary>
    ///     将编译输出文件写入磁盘。
    /// </summary>
    private static List<string> WriteOutputFiles(Valkyrie.Compiler.Pipeline.ArtifactSet artifactSet, string outputDir, bool verbose)
    {
        var writtenFiles = new List<string>();

        foreach (var file in EnumerateArtifacts(artifactSet))
        {
            var filePath = Path.Combine(outputDir, file.Name);
            File.WriteAllBytes(filePath, file.Content);
            writtenFiles.Add(filePath);

            if (verbose)
            {
                Console.WriteLine($"  生成：{file.Name}（{file.Content.Length} 字节）");
            }
        }

        return writtenFiles;
    }

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

    private static bool TryResolveTarget(string target, out TargetInfo targetInfo)
    {
        targetInfo = default;
        if (!TargetTriple.TryParse(target, out var triple))
        {
            return false;
        }

        targetInfo = new TargetInfo(triple.ToCompilationTarget(), triple.ToString());
        return true;
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

    private readonly record struct TargetInfo(CompilationTarget Target, string CanonicalTriple);

    #endregion
}
