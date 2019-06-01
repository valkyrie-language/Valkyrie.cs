using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.Formatter;

namespace Valkyrie.Tests.FormatterTests;

public class FormatterTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;

    public FormatterTests()
    {
        _lexer = new ValkyrieLexer(_diagnostics);
        _parser = new ValkyrieParser(ValkyrieLanguage.Standard, _diagnostics);
    }

    private CompilationUnit ParseSource(string source)
    {
        var tokens = _lexer.Tokenize(source);
        var result = _parser.Parse(tokens);
        return Assert.IsType<CompilationUnit>(result);
    }

    #region 变量声明格式化

    [Fact]
    public void VariableDecl_ShouldFormatCorrectly()
    {
        var source = """
                     let x: i32 = 42;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("let x: i32 = 42;", result.FormattedText);
    }

    [Fact]
    public void MutableVariableDecl_ShouldFormatCorrectly()
    {
        var source = """
                     let mut counter: i32 = 0;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("let mut counter: i32 = 0;", result.FormattedText);
    }

    #endregion

    #region 组件声明格式化

    [Fact]
    public void ComponentDecl_ShouldFormatCorrectly()
    {
        var source = """
                     component Position {
                         x: f32;
                         y: f32;
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("component Position", result.FormattedText);
        Assert.Contains("x: f32;", result.FormattedText);
        Assert.Contains("y: f32;", result.FormattedText);
    }

    #endregion

    #region 函数声明格式化

    [Fact]
    public void FunctionDecl_ShouldFormatCorrectly()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 {
                         return a + b
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("micro add(a: i32, b: i32): i32", result.FormattedText);
        Assert.Contains("return a + b;", result.FormattedText);
    }

    #endregion

    #region 导入格式化

    [Fact]
    public void ImportDecl_ShouldFormatCorrectly()
    {
        var source = """
                     import Gnosis.ECS;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("import Gnosis.ECS;", result.FormattedText);
    }

    [Fact]
    public void ImportDecl_WithAlias_ShouldFormatCorrectly()
    {
        var source = """
                     import Gnosis.ECS as ecs;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("import Gnosis.ECS as ecs;", result.FormattedText);
    }

    #endregion

    #region 控制流格式化

    [Fact]
    public void IfStmt_ShouldFormatCorrectly()
    {
        var source = """
                     micro check(x: i32): bool {
                         if x > 0 {
                             return true
                         } else {
                             return false
                         }
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("if", result.FormattedText);
        Assert.Contains("else", result.FormattedText);
    }

    [Fact]
    public void LoopStmt_ShouldFormatCorrectly()
    {
        var source = """
                     micro iterate(items: list) {
                         loop item in items {
                             process(item)
                         }
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("loop item in items", result.FormattedText);
    }

    #endregion

    #region 配置测试

    [Fact]
    public void CompactConfig_ShouldUseSmallerIndent()
    {
        var source = """
                     component Position {
                         x: f32;
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter(FormatterConfig.Compact);
        var result = formatter.Format(ast);

        Assert.Contains("  x: f32;", result.FormattedText);
    }

    [Fact]
    public void DefaultConfig_ShouldUse4SpaceIndent()
    {
        var source = """
                     component Position {
                         x: f32;
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter(FormatterConfig.Default);
        var result = formatter.Format(ast);

        Assert.Contains("    x: f32;", result.FormattedText);
    }

    #endregion

    #region 表达式格式化

    [Fact]
    public void BinaryExpr_ShouldFormatWithSpaces()
    {
        var source = """
                     let result: i32 = 1 + 2;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("1 + 2", result.FormattedText);
    }

    [Fact]
    public void MemberAccessExpr_ShouldFormatWithDot()
    {
        var source = """
                     let val: f32 = entity.position.x;
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("entity.position.x", result.FormattedText);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void MultipleDeclarations_ShouldFormatWithBlankLines()
    {
        var source = """
                     import Gnosis.ECS;

                     component Position {
                         x: f32;
                         y: f32;
                     }

                     micro update(pos: Position, dt: f32) {
                     }
                     """;
        var ast = ParseSource(source);
        var formatter = new CodeFormatter();
        var result = formatter.Format(ast);

        Assert.Contains("import Gnosis.ECS;", result.FormattedText);
        Assert.Contains("component Position", result.FormattedText);
        Assert.Contains("micro update", result.FormattedText);
    }

    #endregion
}