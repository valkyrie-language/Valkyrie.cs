namespace Asgard.CLI.Compiler;

/// <summary>
///     VOA 编译器 — Valkyrie 全栈框架的编译入口
///     消费 ValkyrieRuntime 的编译能力，注入 WebDialect，编排 VOA 特有的构建逻辑
///     不自建编译管线，所有语言编译能力来自 Valkyrie
/// </summary>
public sealed class VoaCompiler
{
    private readonly ValkyrieRuntime _runtime;
    private readonly WidgetParser _widgetParser;
    private readonly AwslReactiveCompiler _awslCompiler;
    private readonly WasmTargetBuilder _wasmBuilder;

    public VoaCompiler()
    {
        _runtime = new ValkyrieRuntime();
        _runtime.RegisterDialect(new WebDialect());

        _widgetParser = new WidgetParser();
        _awslCompiler = new AwslReactiveCompiler();
        _wasmBuilder = new WasmTargetBuilder();
    }

    public DiagnosticSink Diagnostics => _runtime.Diagnostics;

    #region 公开编译 API

    public VoaBuildResult Build(string projectDir, string outputDir, string target, bool verbose, bool pwa = false, bool ssr = false)
    {
        var sourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(sourceDir))
        {
            return new VoaBuildResult { Success = false, Error = $"源码目录不存在 '{sourceDir}'" };
        }

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);
        var awslFiles = Directory.GetFiles(sourceDir, "*.awsl", SearchOption.AllDirectories);

        if (verbose)
        {
            Console.WriteLine($"  找到 {vFiles.Length} 个 .v 文件，{awslFiles.Length} 个 .awsl 文件");
        }

        if (vFiles.Length == 0 && awslFiles.Length == 0)
        {
            return new VoaBuildResult { Success = false, Error = "未找到任何源码文件（.v 或 .awsl）" };
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var moduleName = Path.GetFileName(projectDir);
        var awslResults = CompileAllAwslFiles(awslFiles, moduleName, verbose);

        WasmCompilationResult? wasmResult = null;
        if (vFiles.Length > 0)
        {
            wasmResult = CompileAllVFilesToWasm(vFiles, moduleName, verbose);
            if (wasmResult is not null && !wasmResult.Success)
            {
                return new VoaBuildResult { Success = false, Error = "编译 .v 文件失败" };
            }
        }

        return target.ToLowerInvariant() switch
        {
            "wasm" => _wasmBuilder.Build(wasmResult, awslResults, moduleName, outputDir, verbose, pwa, ssr),
            _ => new VoaBuildResult { Success = false, Error = $"不支持的编译目标 '{target}'，当前仅支持：wasm" }
        };
    }

    public VoaBuildResult BuildWasm(string projectDir, string outputDir, bool verbose)
    {
        return Build(projectDir, outputDir, "wasm", verbose);
    }

    #endregion

    #region AWSL 编译管线

    private List<AwslCompileResult> CompileAllAwslFiles(string[] awslFiles, string moduleName, bool verbose)
    {
        var results = new List<AwslCompileResult>();

        var componentNames = awslFiles
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        _awslCompiler.RegisterComponentNames(componentNames);

        foreach (var awslFile in awslFiles)
        {
            if (verbose)
            {
                Console.WriteLine($"  编译 .awsl 文件：{Path.GetFileName(awslFile)}");
            }

            var source = File.ReadAllText(awslFile);
            var parseResult = _widgetParser.Parse(source, Path.GetFileName(awslFile));

            if (verbose)
            {
                Console.WriteLine($"    属性：{string.Join(", ", parseResult.Properties.Select(p => $"{p.Name}({p.DefaultValueKind})={p.DefaultValue}"))}");
                Console.WriteLine($"    方法：{string.Join(", ", parseResult.Methods.Select(m => m.Name))}");
            }

            var compileResult = _awslCompiler.Compile(parseResult, moduleName);
            results.Add(compileResult);
        }

        return results;
    }

    #endregion

    #region Valkyrie 编译管线

    /// <summary>
    ///     将所有 .v 文件编译到 WASM 目标，合并所有文件的桥接导入
    ///     委托 ValkyrieRuntime.CompileToWasm()：Oak 解码 → 前端转换 → WebDialect 优化 → 代码生成 → DCE → WASM 编码
    /// </summary>
    private WasmCompilationResult? CompileAllVFilesToWasm(string[] vFiles, string moduleName, bool verbose)
    {
        WasmCompilationResult? mergedResult = null;

        for (var i = 0; i < vFiles.Length; i++)
        {
            var vFile = vFiles[i];

            if (verbose)
            {
                Console.WriteLine($"  编译 .v 文件 ({i + 1}/{vFiles.Length})：{Path.GetFileName(vFile)}");
            }

            var source = File.ReadAllText(vFile);
            var result = _runtime.CompileToWasm(source, moduleName);

            if (!result.Success)
            {
                foreach (var error in result.Diagnostics.Errors)
                {
                    Console.WriteLine($"  错误：{error.Message}");
                }

                return result;
            }

            if (verbose && result.WasmBytes is not null)
            {
                Console.WriteLine($"    WASM：{result.WasmBytes.Length} 字节");
                if (result.JsImports is not null && result.JsImports.Count > 0)
                {
                    Console.WriteLine($"    JS 导入：{result.JsImports.Count} 个");
                }
            }

            if (mergedResult is null)
            {
                mergedResult = result;
            }
            else
            {
                if (result.WasmBytes is not null)
                {
                    mergedResult = new WasmCompilationResult
                    {
                        Success = true,
                        WasmBytes = result.WasmBytes,
                        JsImports = MergeJsImports(mergedResult.JsImports ?? [], result.JsImports ?? []),
                        Diagnostics = mergedResult.Diagnostics
                    };
                }
            }
        }

        return mergedResult;
    }

    private static List<JsImportInfo> MergeJsImports(List<JsImportInfo> existing, List<JsImportInfo> incoming)
    {
        var merged = new List<JsImportInfo>(existing);
        var existingKeys = new HashSet<string>(existing.Select(i => $"{i.ModuleName}.{i.FuncName}"));

        foreach (var import in incoming)
        {
            var key = $"{import.ModuleName}.{import.FuncName}";
            if (!existingKeys.Contains(key))
            {
                merged.Add(import);
                existingKeys.Add(key);
            }
        }

        return merged;
    }

    #endregion
}