using Valkyrie.CLI.Compiler;
using Valkyrie.CLI.Repl;
using Iris.CLI;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;

namespace Valkyrie.CLI;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            var repl = new VccRepl();
            repl.Run();
            return 0;
        }

        return IrisApp.Run(args, registry =>
        {
            RegisterCompileCommand(registry);
            RegisterRunCommand(registry);
            RegisterTestCommand(registry);
            RegisterReplCommand(registry);
            RegisterFormatCommand(registry);
            RegisterCheckCommand(registry);
        });
    }

    private static void RegisterCompileCommand(CommandRegistryBuilder registry)
    {
        registry.Add("compile", (string file, string target = "nyar", string? output = null, bool verbose = false) =>
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
                return 1;
            }

            var outputDir = output ?? Path.Combine(Path.GetDirectoryName(file) ?? ".", "dist");
            var compiler = new VccCompiler();
            var result = compiler.Compile(file, target, outputDir, verbose);

            if (!result.Success)
            {
                Console.Error.WriteLine($"编译失败：{result.Error}");
                return 1;
            }

            Console.WriteLine($"编译完成 → {result.OutputDirectory}");

            if (verbose)
            {
                foreach (var outputFile in result.OutputFiles)
                {
                    Console.WriteLine($"  产出：{outputFile}");
                }
            }

            return 0;
        });
    }

    private static void RegisterRunCommand(CommandRegistryBuilder registry)
    {
        registry.Add("run", (string file, string target = "nyar", bool nyar = false, bool verbose = false) =>
        {
            var compiler = new VccCompiler();

            if (nyar || file.EndsWith(".nyar", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var result = compiler.RunNyarFile(file, verbose);
                    if (result.Type != Nyar.ValueType.Null)
                    {
                        Console.WriteLine(FormatNyarValue(result));
                    }

                    return 0;
                }
                catch (FileNotFoundException ex)
                {
                    Console.Error.WriteLine($"错误：{ex.Message}");
                    return 1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"运行时错误：{ex.Message}");
                    return 1;
                }
            }

            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
                return 1;
            }

            try
            {
                var result = compiler.CompileAndRun(file, target, verbose);
                if (result.Type != Nyar.ValueType.Null)
                {
                    Console.WriteLine(FormatNyarValue(result));
                }

                return 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"错误：{ex.Message}");
                return 1;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"错误：{ex.Message}");
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"编译失败：{ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"运行时错误：{ex.Message}");
                return 1;
            }
        });
    }

    private static void RegisterTestCommand(CommandRegistryBuilder registry)
    {
        registry.Add("test", (string file, string target = "nyar", bool verbose = false) =>
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
                return 1;
            }

            var compiler = new VccCompiler();

            try
            {
                var result = compiler.CompileAndRun(file, target, verbose);
                Console.WriteLine(verbose && result.Type != Nyar.ValueType.Null
                    ? $"测试结果：{FormatNyarValue(result)}"
                    : "测试完成");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"测试失败：{ex.Message}");
                return 1;
            }
        });
    }

    private static void RegisterReplCommand(CommandRegistryBuilder registry)
    {
        registry.Add("repl", () =>
        {
            var repl = new VccRepl();
            repl.Run();
            return 0;
        });
    }

    private static void RegisterFormatCommand(CommandRegistryBuilder registry)
    {
        registry.Add("format", (string? path = null, bool check = false, bool verbose = false) =>
        {
            Console.Error.WriteLine("format 命令暂不可用：Valkyrie.Formatter 正在迁移到新版 AST。");
            return 2;
        });
    }

    private static void RegisterCheckCommand(CommandRegistryBuilder registry)
    {
        registry.Add("check", (string? path = null, bool verbose = false) =>
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(ValkyrieLanguage.Standard, diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var typeChecker = new Valkyrie.TypeChecker.TypeChecker();

            var totalFiles = 0;
            var totalErrors = 0;
            var totalWarnings = 0;

            var targetPath = path ?? ".";
            var vFiles = Directory.Exists(targetPath)
                ? Directory.GetFiles(targetPath, "*.v", SearchOption.AllDirectories)
                : File.Exists(targetPath) && targetPath.EndsWith(".v")
                    ? [targetPath]
                    : [];

            foreach (var vFile in vFiles)
            {
                totalFiles++;
                var fileName = Path.GetFileName(vFile);

                try
                {
                    var source = File.ReadAllText(vFile);
                    var tokens = lexer.Tokenize(source);
                    var ast = (CompilationUnit)parser.Parse(tokens);

                    if (diagnostics.Errors.Count > 0)
                    {
                        totalErrors += diagnostics.Errors.Count;
                        foreach (var error in diagnostics.Errors)
                        {
                            Console.WriteLine($"  {fileName}: 语法错误 - {error.Message}");
                        }

                        diagnostics.Clear();
                        continue;
                    }

                    var typeResult = typeChecker.Check(ast, vFile);
                    if (typeResult.HasErrors || typeResult.HasWarnings)
                    {
                        foreach (var diag in typeResult.Diagnostics)
                        {
                            var location = diag.Line > 0 ? $"{fileName}({diag.Line}:{diag.Column}): " : $"{fileName}: ";
                            Console.WriteLine($"  {location}{diag.Severity.ToString().ToLower()} {diag.Code}: {diag.Message}");
                        }

                        totalErrors += typeResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
                        totalWarnings += typeResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
                    }
                    else if (verbose)
                    {
                        Console.WriteLine($"  {fileName}: 类型检查通过");
                    }

                    diagnostics.Clear();
                }
                catch (IOException ex)
                {
                    totalErrors++;
                    Console.WriteLine($"  {fileName}: IO 错误 - {ex.Message}");
                }
            }

            Console.WriteLine($"  共 {totalFiles} 个文件，{totalErrors} 个错误，{totalWarnings} 个警告");
            return totalErrors > 0 ? 1 : 0;
        });
    }

    #region 辅助方法

    private static string FormatNyarValue(Nyar.Value value)
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

    #endregion
}
