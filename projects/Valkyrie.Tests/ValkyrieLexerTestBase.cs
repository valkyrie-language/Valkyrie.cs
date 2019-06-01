using Oak.Syntax;
using Oak.Testing;
using Oak.Valkyrie.Lexer;

namespace Valkyrie.Tests;

public abstract class ValkyrieLexerTestBase : TestBase
{
    protected virtual int TokenizeTimeoutMs => 5000;

    protected IReadOnlyList<GreenLeafNode> TokenizeWithTimeout(ValkyrieLexer lexer, string source)
    {
        return ExecuteWithTimeout(() => lexer.Tokenize(source), "Valkyrie 词法分析器");
    }

    protected static void AssertTokenCount(IReadOnlyList<GreenLeafNode> tokens, int expected)
    {
        Assert.Equal(expected, tokens.Count);
    }

    protected static void AssertTokenKind(IReadOnlyList<GreenLeafNode> tokens, int index, NodeKind expectedKind)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedKind, tokens[index].Kind);
    }

    protected static void AssertTokenKind(IReadOnlyList<GreenLeafNode> tokens, int index, string expectedKindName)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedKindName, KindToString(tokens[index].Kind));
    }

    protected static void AssertTokenValue(IReadOnlyList<GreenLeafNode> tokens, int index, string expectedValue)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedValue, tokens[index].Text);
    }

    protected static void AssertEndsWithEof(IReadOnlyList<GreenLeafNode> tokens)
    {
        Assert.NotEmpty(tokens);
        Assert.Equal(ValkyrieNodeKind.Eof, tokens[^1].Kind);
    }

    protected static void AssertErrorCount(DiagnosticSink diagnostics, int expectedCount)
    {
        Assert.Equal(expectedCount, diagnostics.Errors.Count);
    }

    private static string KindToString(NodeKind kind)
    {
        if (kind.IsKeyword()) return "Keyword";
        if (kind.IsLiteral()) return "Literal";
        if (kind == ValkyrieNodeKind.Identifier) return "Identifier";
        if (kind == ValkyrieNodeKind.Operator) return "Operator";
        if (kind == ValkyrieNodeKind.Punctuation) return "Punctuation";
        if (kind == ValkyrieNodeKind.Delimiter) return "Delimiter";
        if (kind == ValkyrieNodeKind.Colon) return "Colon";
        if (kind == ValkyrieNodeKind.DoubleColon) return "DoubleColon";
        if (kind == ValkyrieNodeKind.LeftParen) return "LeftParen";
        if (kind == ValkyrieNodeKind.RightParen) return "RightParen";
        if (kind == ValkyrieNodeKind.LeftBracket) return "LeftBracket";
        if (kind == ValkyrieNodeKind.RightBracket) return "RightBracket";
        if (kind == ValkyrieNodeKind.LeftBrace) return "LeftBrace";
        if (kind == ValkyrieNodeKind.RightBrace) return "RightBrace";
        if (kind == ValkyrieNodeKind.Semicolon) return "Semicolon";
        if (kind == ValkyrieNodeKind.Comma) return "Comma";
        if (kind == ValkyrieNodeKind.Attribute) return "Attribute";
        if (kind == ValkyrieNodeKind.MetaBlockStart) return "MetaBlockStart";
        if (kind == ValkyrieNodeKind.DocComment) return "DocComment";
        if (kind == ValkyrieNodeKind.Eof) return "Eof";
        if (kind == ValkyrieNodeKind.Meta) return "Meta";
        return kind.ToString();
    }
}
