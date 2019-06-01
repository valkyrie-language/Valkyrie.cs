using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.Lexer;

namespace Valkyrie.Tests.LexerTests;

public class ValkyrieLexerTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;

    public ValkyrieLexerTests()
    {
        _lexer = new ValkyrieLexer(_diagnostics);
    }

    private IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        return _lexer.Tokenize(source);
    }

    [Fact]
    public void EmptySource_ShouldReturnOnlyEof()
    {
        var tokens = Tokenize("");
        Assert.Single(tokens);
    }

    [Fact]
    public void IntegerNumber_ShouldBeRecognized()
    {
        var tokens = Tokenize("42");
        Assert.True(tokens.Count >= 1);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void FloatNumber_ShouldBeRecognized()
    {
        var tokens = Tokenize("3.14");
        Assert.True(tokens.Count >= 1);
        Assert.Equal("3.14", tokens[0].Text);
    }

    [Fact]
    public void NumberSuffix_ShouldBeRecognized()
    {
        var tokens = Tokenize("1.0f 42i 100u");
        Assert.True(tokens.Count >= 3);
        Assert.Equal("1.0f", tokens[0].Text);
        Assert.Equal("42i", tokens[1].Text);
    }

    [Fact]
    public void DoubleQuotedString_ShouldBeRecognized()
    {
        var tokens = Tokenize("\"hello world\"");
        Assert.True(tokens.Count >= 1);
    }

    [Fact]
    public void ArithmeticOperators_ShouldBeRecognized()
    {
        var tokens = Tokenize("+ - * / %");
        Assert.True(tokens.Count >= 5);
    }

    [Fact]
    public void ComparisonOperators_ShouldBeRecognized()
    {
        var tokens = Tokenize("== != < > <= >=");
        Assert.True(tokens.Count >= 6);
    }

    [Fact]
    public void Delimiters_ShouldBeRecognized()
    {
        var tokens = Tokenize("( ) { } , ; .");
        Assert.True(tokens.Count >= 7);
    }

    [Fact]
    public void ComponentDeclaration_ShouldTokenize()
    {
        var source = """
                     component Position {
                         x: f32;
                         y: f32;
                     }
                     """;
        var tokens = Tokenize(source);
        Assert.True(tokens.Count > 1);
        Assert.Equal("component", tokens[0].Text);
    }

    [Fact]
    public void FunctionDeclaration_ShouldTokenize()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 {
                         return a + b
                     }
                     """;
        var tokens = Tokenize(source);
        Assert.True(tokens.Count > 1);
        Assert.Equal("micro", tokens[0].Text);
    }
}