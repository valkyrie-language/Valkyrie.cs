using System.Text;
using Valkyrie.CLI.Compiler;

namespace Valkyrie.CLI.Repl;

/// <summary>
/// VCC 交互式 REPL
/// 支持表达式求值、多行输入和会话状态维护
/// </summary>
public sealed class VccRepl
{
    private readonly Valkyrie.Runtime.ValkyrieRuntime _runtime;
    private readonly StringBuilder _sessionSource;
    private readonly VccCompiler _compiler;
    private bool _isRunning;

    /// <summary>
    /// 初始化 VCC REPL
    /// </summary>
    public VccRepl()
    {
        _compiler = new VccCompiler();
        _runtime = _compiler.Runtime;
        _sessionSource = new StringBuilder();
        _isRunning = false;
    }

    /// <summary>
    /// 启动 REPL 交互循环
    /// </summary>
    public void Run()
    {
        _isRunning = true;

        Console.WriteLine("VCC REPL — Valkyrie Compiler Collection 交互式环境");
        Console.WriteLine("输入表达式进行求值，输入 exit 或 quit 退出");
        Console.WriteLine();

        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            while (_isRunning)
            {
                Console.Write("vcc> ");
                var input = ReadMultiLineInput();

                if (input is null)
                {
                    continue;
                }

                var trimmed = input.Trim();

                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (IsExitCommand(trimmed))
                {
                    break;
                }

                EvaluateInput(trimmed);
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }

    #region 输入处理

    /// <summary>
    /// 读取多行输入，当大括号未匹配时自动续行
    /// </summary>
    /// <returns>完整输入内容，EOF 时返回 null</returns>
    private static string? ReadMultiLineInput()
    {
        var buffer = new StringBuilder();
        var braceDepth = 0;
        var isFirstLine = true;

        while (true)
        {
            var line = Console.ReadLine();

            if (line is null)
            {
                if (buffer.Length > 0)
                {
                    return buffer.ToString();
                }

                return null;
            }

            buffer.AppendLine(line);

            foreach (var ch in line)
            {
                if (ch == '{')
                {
                    braceDepth++;
                }
                else if (ch == '}')
                {
                    braceDepth--;
                }
            }

            if (braceDepth <= 0 && !isFirstLine)
            {
                break;
            }

            if (braceDepth > 0)
            {
                Console.Write("  ... ");
                isFirstLine = false;
            }
            else
            {
                break;
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    /// 判断是否为退出命令
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>是否为退出命令</returns>
    private static bool IsExitCommand(string input)
    {
        return string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)
               || string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 处理 Ctrl+C 中断信号
    /// </summary>
    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _isRunning = false;
        Console.WriteLine();
    }

    #endregion

    #region 表达式求值

    /// <summary>
    /// 求值用户输入
    /// 将输入包装为 micro 函数，编译并在 NyarVM 中执行
    /// </summary>
    /// <param name="input">用户输入的表达式或语句</param>
    private void EvaluateInput(string input)
    {
        try
        {
            var wrappedSource = WrapAsMicroFunction(input);
            var moduleName = $"repl_{Guid.NewGuid():N}";

            var compileResult = _runtime.Compile(wrappedSource, moduleName);

            if (compileResult.HasErrors)
            {
                foreach (var error in _runtime.Diagnostics.Errors)
                {
                    Console.WriteLine($"  错误：{error.Message}");
                }

                _runtime.Diagnostics.Clear();
                return;
            }

            var moduleAdapter = new Nyar.Assembler.CodeGenModuleAdapter(compileResult.GeneratedUnit);
            _runtime.VM.Load(moduleAdapter);

            var result = _runtime.Run(moduleName, "__repl_eval", Array.Empty<Nyar.Value>());

            if (result.Type != Nyar.ValueType.Null)
            {
                Console.WriteLine($"  {FormatValue(result)}");
            }

            _sessionSource.AppendLine(input);
        }
        catch (Nyar.VM.NyarRuntimeException ex)
        {
            Console.WriteLine($"  运行时错误：{ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  编译错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 将用户输入包装为 micro 函数以便求值
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>包装后的完整源代码</returns>
    private static string WrapAsMicroFunction(string input)
    {
        return $"micro __repl_eval() {{ {input} }}";
    }

    /// <summary>
    /// 格式化输出值
    /// </summary>
    /// <param name="value">Nyar 值</param>
    /// <returns>格式化后的字符串</returns>
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

    #endregion
}
