using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Oak.Von;
using Xunit.Abstractions;

namespace Valkyrie.Tests.StdTests;

/// <summary>
/// 标准库解析验证测试
/// 递归读取 examples 中所有 .v 和 .von 文件
/// .v 文件用 Valkyrie parser 解析，.von 文件用 Gon parser 解析
/// 打印所有报错
/// </summary>
public class StdParseTests
{
    private readonly ITestOutputHelper _output;

    private static readonly string ExamplesRoot = Path.GetFullPath(
        Path.Combine("..", "..", "..", "..", "..", "examples"));

    public StdParseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 获取 examples 目录下所有 .v 文件路径
    /// </summary>
    public static IEnumerable<object[]> GetVFiles()
    {
        if (!Directory.Exists(ExamplesRoot))
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(ExamplesRoot, "*.v", SearchOption.AllDirectories))
        {
            yield return new object[] { file };
        }
    }

    /// <summary>
    /// 获取 examples 目录下所有 .von 文件路径
    /// </summary>
    public static IEnumerable<object[]> GetVonFiles()
    {
        if (!Directory.Exists(ExamplesRoot))
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(ExamplesRoot, "*.von", SearchOption.AllDirectories))
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(GetVFiles))]
    public void VFile_ShouldParseWithoutErrors(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var diagnostics = new DiagnosticSink();
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Standard, diagnostics);
        var tokens = lexer.Tokenize(content);
        var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
        parser.Parse(tokens);

        if (diagnostics.HasErrors)
        {
            foreach (var error in diagnostics.Errors)
            {
                _output.WriteLine($"[V 解析错误] {filePath}: {error}");
            }
        }

        Assert.False(diagnostics.HasErrors,
            $"{filePath} 存在 {diagnostics.Errors.Count} 个解析错误，已打印到输出");
    }

    [Theory]
    [MemberData(nameof(GetVonFiles))]
    public void VonFile_ShouldParseWithoutErrors(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var diagnostics = new DiagnosticSink();
        var parser = new GonParser(diagnostics);
        parser.Parse(content);

        if (diagnostics.HasErrors)
        {
            foreach (var error in diagnostics.Errors)
            {
                _output.WriteLine($"[VON 解析错误] {filePath}: {error}");
            }
        }

        Assert.False(diagnostics.HasErrors,
            $"{filePath} 存在 {diagnostics.Errors.Count} 个解析错误，已打印到输出");
    }
}
