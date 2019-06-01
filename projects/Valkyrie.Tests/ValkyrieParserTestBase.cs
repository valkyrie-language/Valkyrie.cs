using Oak.Parsing;
using Oak.Testing;

namespace Valkyrie.Tests;

public abstract class ValkyrieParserTestBase : TestBase
{
    protected virtual int ParseTimeoutMs => 5000;

    protected ProgramRoot ParseWithTimeout(string source, ValkyrieLanguage? language = null, DiagnosticSink? diagnostics = null)
    {
        var lang = language ?? ValkyrieLanguage.Standard;
        var lexer = new ValkyrieLexer(lang, diagnostics);
        var tokens = ExecuteWithTimeout(() => lexer.Tokenize(source), "Valkyrie 词法分析器");
        var parser = new ValkyrieParser(lang, diagnostics);
        return ExecuteWithTimeout(() =>
        {
            var result = parser.Parse(tokens);
            return Assert.IsType<ProgramRoot>(result);
        }, "Valkyrie 语法分析器");
    }

    protected static void AssertParseResultNotNull(ProgramRoot result)
    {
        Assert.NotNull(result);
    }

    protected static void AssertNoErrors(DiagnosticSink diagnostics)
    {
        Assert.False(diagnostics.HasErrors, $"语法分析产生意外的错误：\n{diagnostics.FormatAll()}");
    }

    protected static void AssertErrorCount(DiagnosticSink diagnostics, int expectedCount)
    {
        Assert.Equal(expectedCount, diagnostics.Errors.Count);
    }
}
