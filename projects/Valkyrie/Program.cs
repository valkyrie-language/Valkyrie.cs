using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dejavu.Report;
using Iris.CLI;
using Iris.Commanding;
using Iris.Middleware;
using Iris.Repl;
using Nyar.Assembler;
using Nyar.Assembler.Native;
using Nyar.Types;
using Valkyrie.Interpreter;
using Valkyrie.Formatter;

namespace VCC;

class Program
{
    static int Main(string[] args)
    {
        IrisApp.WithName("valkyrie")
            .WithDescription("Valkyrie 编译器管线 — GGScript/GGShader 语言前端")
            .WithVersion("2.0.0");

        IrisApp.UseMiddleware<ErrorHandlingMiddleware>();
        IrisApp.UseMiddleware<TimingMiddleware>();

        var runtime = new ValkyrieRuntime();

        return IrisApp.Run(args, registry =>
        {
            #region build 命令

            registry.Add("build", cmd => cmd
                .WithDescription("编译源代码到指定目标平台")
                .AddArgument<string>("source", a => a
                    .WithRequired()
                    .WithDescription("源文件路径"))
                .AddOption<string>("output", o => o
                    .WithShortName('o')
                    .WithDescription("输出文件路径"))
                .AddOption<string>("target", o => o
                    .WithShortName('t')
                    .WithDefault("nyar")
                    .WithDescription("目标平台：nyar|wasm|jvm|clr|native"))
                .AddOption<string>("module", o => o
                    .WithShortName('m')
                    .WithDefault("main")
                    .WithDescription("模块名称"))
                .AddOption<int>("optimize", o => o
                    .WithShortName('O')
                    .WithDefault(0)
                    .WithRange(0, 3)
                    .WithDescription("优化等级 0-3"))
                .AddOption<bool>("verbose", o => o
                    .WithShortName('v')
                    .WithDescription("详细输出"))
                .WithHandler((string source, string output, string target, string module, int optimize, bool verbose) =>
                {
                    if (!File.Exists(source))
                    {
                        Console.Error.WriteLine($"错误：源文件不存在 '{source}'");
                        return 1;
                    }

                    var src = File.ReadAllText(source);
                    var sw = verbose ? Stopwatch.StartNew() : null;

                    switch (target)
                    {
                        case "nyar":
                            return BuildNyar(runtime, src, module, output, verbose, sw);

                        case "wasm":
                            return BuildWasm(runtime, src, module, output, verbose, sw);

                        case "native":
                            return BuildNative(runtime, src, module, output, verbose, sw);

                        default:
                            return BuildViaTarget(runtime, src, module, target, output, verbose, sw);
                    }
                }));

            #endregion

            #region check 命令

            registry.Add("check", cmd => cmd
                .WithDescription("仅做词法/语法/类型检查，不生成代码")
                .AddArgument<string>("source", a => a
                    .WithRequired()
                    .WithDescription("源文件路径"))
                .AddOption<bool>("verbose", o => o
                    .WithShortName('v')
                    .WithDescription("详细输出"))
                .WithHandler((string source, bool verbose) =>
                {
                    if (!File.Exists(source))
                    {
                        Console.Error.WriteLine($"错误：源文件不存在 '{source}'");
                        return 1;
                    }

                    var src = File.ReadAllText(source);
                    var sw = verbose ? Stopwatch.StartNew() : null;

                    var tokens = runtime.Lex(src);
                    if (runtime.Diagnostics.Errors.Count > 0)
                    {
                        PrintDiagnostics(runtime.Diagnostics);
                        return 1;
                    }
                    if (verbose) Console.WriteLine($"词法分析完成：{tokens.Count} 个 token");

                    var ast = runtime.Parse(tokens);
                    if (runtime.Diagnostics.Errors.Count > 0)
                    {
                        PrintDiagnostics(runtime.Diagnostics);
                        return 1;
                    }
                    if (verbose) Console.WriteLine("语法分析完成");

                    var typeCheckResult = runtime.CheckTypes(ast, source);
                    if (typeCheckResult.HasErrors)
                    {
                        foreach (var diag in typeCheckResult.Diagnostics)
                        {
                            var prefix = diag.Severity == Valkyrie.TypeChecker.DiagnosticSeverity.Error ? "错误" : "警告";
                            Console.Error.WriteLine($"{prefix} [{diag.Code}] {diag.FilePath}:{diag.Line}:{diag.Column} {diag.Message}");
                            if (diag.Suggestion is not null)
                            {
                                Console.Error.WriteLine($"  建议：{diag.Suggestion}");
                            }
                        }
                        return 1;
                    }
                    if (verbose)
                    {
                        var warnings = 0;
                        foreach (var diag in typeCheckResult.Diagnostics)
                        {
                            if (diag.Severity == Valkyrie.TypeChecker.DiagnosticSeverity.Warning)
                            {
                                warnings++;
                                Console.WriteLine($"警告 [{diag.Code}] {diag.FilePath}:{diag.Line}:{diag.Column} {diag.Message}");
                            }
                        }
                        Console.WriteLine($"类型检查完成：{warnings} 个警告");
                    }

                    sw?.Stop();
                    if (verbose) Console.WriteLine($"检查耗时：{sw?.ElapsedMilliseconds}ms");

                    Console.WriteLine("检查通过");
                    return 0;
                }));

            #endregion

            #region run 命令

            registry.Add("run", cmd => cmd
                .WithDescription("编译并立即执行")
                .AddArgument<string>("source", a => a
                    .WithRequired()
                    .WithDescription("源文件路径"))
                .AddOption<string>("module", o => o
                    .WithShortName('m')
                    .WithDefault("main")
                    .WithDescription("模块名称"))
                .AddOption<string>("function", o => o
                    .WithShortName('f')
                    .WithDefault("main")
                    .WithDescription("入口函数名称"))
                .AddOption<bool>("verbose", o => o
                    .WithShortName('v')
                    .WithDescription("详细输出"))
                .WithHandler((string source, string module, string function, bool verbose) =>
                {
                    if (!File.Exists(source))
                    {
                        Console.Error.WriteLine($"错误：源文件不存在 '{source}'");
                        return 1;
                    }

                    var src = File.ReadAllText(source);

                    var result = runtime.CompileAndLoad(src, module);

                    if (result.HasErrors)
                    {
                        PrintDiagnostics(runtime.Diagnostics);
                        return 1;
                    }

                    if (verbose) Console.WriteLine($"编译完成，执行 {module}::{function}");

                    try
                    {
                        var retVal = runtime.Run(module, function);
                        if (verbose) Console.WriteLine($"返回值：{retVal}");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"运行时错误：{ex.Message}");
                        return 2;
                    }
                }));

            #endregion

            #region fmt 命令

            registry.Add("fmt", cmd => cmd
                .WithDescription("格式化源代码")
                .AddArgument<string>("source", a => a
                    .WithRequired()
                    .WithDescription("源文件路径"))
                .AddOption<bool>("write", o => o
                    .WithShortName('w')
                    .WithDescription("直接写入文件（默认输出到 stdout）"))
                .AddOption<bool>("check", o => o
                    .WithShortName('c')
                    .WithDescription("检查格式是否正确（不修改文件）"))
                .WithHandler((string source, bool write, bool check) =>
                {
                    if (!File.Exists(source))
                    {
                        Console.Error.WriteLine($"错误：源文件不存在 '{source}'");
                        return 1;
                    }

                    var src = File.ReadAllText(source);

                    var tokens = runtime.Lex(src);
                    var ast = runtime.Parse(tokens);

                    var formatter = new CodeFormatter();
                    var formatted = formatter.Format(ast).FormattedText;

                    if (check)
                    {
                        return src == formatted ? 0 : 1;
                    }

                    if (write)
                    {
                        File.WriteAllText(source, formatted);
                        Console.WriteLine($"已格式化：{source}");
                        return 0;
                    }

                    Console.Write(formatted);
                    return 0;
                }));

            #endregion

            #region repl 命令

            registry.Add("repl", cmd => cmd
                .WithDescription("交互式 REPL")
                .WithHandler(() =>
                {
                    var commandRegistry = new CommandRegistry();

                    var router = new ReplCommandRouter(commandRegistry);
                    var pipeline = new MiddlewarePipeline();
                    pipeline.Use<TimingMiddleware>();

                    var readLine = new DefaultReadLineHandler();
                    var output = ConsoleOutputWriter.Instance;

                    var engine = new ReplEngine(readLine, output, router, commandRegistry, pipeline)
                    {
                        PrimaryPrompt = "valkyrie> ",
                        ContinuationPrompt = "....  "
                    };

                    var app = new ReplApplication("valkyrie", engine);

                    Console.WriteLine("Valkyrie REPL v2.0.0");
                    Console.WriteLine("输入 .help 查看可用命令，.exit 退出");

                    app.RunAsync().GetAwaiter().GetResult();
                    return 0;
                }));

            #endregion

            #region compile 命令（向后兼容）

            registry.Add("compile", cmd => cmd
                .WithDescription("编译源代码（build 的别名）")
                .AddArgument<string>("file", a => a
                    .WithRequired()
                    .WithDescription("源文件路径"))
                .AddOption<string>("output", o => o
                    .WithShortName('o')
                    .WithDescription("输出文件路径"))
                .WithHandler((string file, string output) =>
                {
                    if (!File.Exists(file))
                    {
                        Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
                        return 1;
                    }

                    var src = File.ReadAllText(file);
                    var r = runtime.Compile(src);

                    if (r.HasErrors)
                    {
                        PrintDiagnostics(runtime.Diagnostics);
                        return 1;
                    }

                    if (!string.IsNullOrEmpty(output))
                    {
                        var unit = r.GeneratedUnit;
                        var adapter = new Nyar.Assembler.NyarVM.CodeGenModuleAdapter(unit);
                        var bytecode = adapter.ToBytecode();
                        File.WriteAllBytes(output, bytecode);
                        Console.WriteLine($"已输出：{output} ({bytecode.Length} bytes)");
                    }
                    else
                    {
                        Console.WriteLine("OK");
                    }

                    return 0;
                }));

            #endregion
        });
    }

    #region 构建辅助方法

    private static int BuildNyar(ValkyrieRuntime runtime, string src, string module, string output, bool verbose, Stopwatch sw)
    {
        var result = runtime.Compile(src, module);

        if (result.HasErrors)
        {
            PrintDiagnostics(runtime.Diagnostics);
            return 1;
        }

        var unit = result.GeneratedUnit;
        var adapter = new Nyar.Assembler.NyarVM.CodeGenModuleAdapter(unit);
        var bytecode = adapter.ToBytecode();

        var outPath = string.IsNullOrEmpty(output)
            ? $"{module}.nyar"
            : output;

        File.WriteAllBytes(outPath, bytecode);
        sw?.Stop();

        if (verbose)
        {
            Console.WriteLine($"模块：{module}");
            Console.WriteLine($"输出：{outPath} ({bytecode.Length} bytes)");
            Console.WriteLine($"耗时：{sw?.ElapsedMilliseconds}ms");
        }
        else
        {
            Console.WriteLine(outPath);
        }

        return 0;
    }

    private static int BuildWasm(ValkyrieRuntime runtime, string src, string module, string output, bool verbose, Stopwatch sw)
    {
        var result = runtime.CompileToWasm(src, module);

        if (result.Diagnostics?.Errors.Count > 0 || result.WasmBytes is null)
        {
            Console.Error.WriteLine("编译失败");
            return 1;
        }

        var outPath = string.IsNullOrEmpty(output)
            ? $"{module}.wasm"
            : output;

        File.WriteAllBytes(outPath, result.WasmBytes);
        sw?.Stop();

        if (verbose)
        {
            Console.WriteLine($"模块：{module}");
            Console.WriteLine($"输出：{outPath} ({result.WasmBytes.Length} bytes)");

            if (result.JsImports is not null && result.JsImports.Count > 0)
            {
                Console.WriteLine($"JS 导入：{string.Join(", ", result.JsImports)}");
            }

            Console.WriteLine($"耗时：{sw?.ElapsedMilliseconds}ms");
        }
        else
        {
            Console.WriteLine(outPath);
        }

        return 0;
    }

    private static int BuildNative(ValkyrieRuntime runtime, string src, string module, string output, bool verbose, Stopwatch sw)
    {
        var compileResult = runtime.Compile(src, module);

        if (compileResult.HasErrors)
        {
            PrintDiagnostics(runtime.Diagnostics);
            return 1;
        }

        var target = new CompilationTarget { Arch = Arch.X86_64 };
        var opts = new CompilationOptions
        {
            OptimizationLevel = OptimizationLevel.Basic,
            Target = target,
            AdditionalOptions = new Dictionary<string, object>
            {
                ["OutputFormat"] = "pe"
            }
        };

        var backend = new NativeBackend();
        var files = backend.Compile(compileResult.GeneratedUnit, opts);

        foreach (var fo in files.Files)
        {
            var outPath = string.IsNullOrEmpty(output)
                ? Path.Combine(Directory.GetCurrentDirectory(), fo.Name)
                : output;

            File.WriteAllBytes(outPath, fo.Content);
            if (verbose)
            {
                Console.WriteLine($"输出：{outPath} ({fo.Content.Length} bytes)");
            }
            else
            {
                Console.WriteLine(outPath);
            }
        }

        sw?.Stop();
        if (verbose) Console.WriteLine($"耗时：{sw?.ElapsedMilliseconds}ms");

        return 0;
    }

    private static int BuildViaTarget(ValkyrieRuntime runtime, string src, string module, string target, string output, bool verbose, Stopwatch sw)
    {
        var compilationTarget = new CompilationTarget
        {
            Platform = target switch
            {
                "jvm" => "jvm",
                "clr" => "clr",
                _ => target
            }
        };

        var result = runtime.CompileToTarget(src, module, compilationTarget);

        if (!result.Success)
        {
            Console.Error.WriteLine($"编译失败：目标平台 '{target}' 不可用");
            return 1;
        }

        foreach (var file in result.OutputFiles)
        {
            var outPath = string.IsNullOrEmpty(output)
                ? file.Name
                : output;

            File.WriteAllBytes(outPath, file.Content);
            if (verbose)
            {
                Console.WriteLine($"输出：{outPath} ({file.Content.Length} bytes)");
            }
            else
            {
                Console.WriteLine(outPath);
            }
        }

        sw?.Stop();
        if (verbose) Console.WriteLine($"耗时：{sw?.ElapsedMilliseconds}ms");

        return 0;
    }

    #endregion

    #region 诊断输出

    private static void PrintDiagnostics(DiagnosticSink diagnostics)
    {
        foreach (var error in diagnostics.Errors)
        {
            Console.Error.WriteLine($"错误 [{error.Code}] {error.FilePath}:{error.Location.Start} {error.Message}");
        }

        foreach (var warning in diagnostics.Warnings)
        {
            Console.Error.WriteLine($"警告 [{warning.Code}] {warning.FilePath}:{warning.Location.Start} {warning.Message}");
        }
    }

    #endregion
}