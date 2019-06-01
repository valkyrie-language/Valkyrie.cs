using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;

namespace Valkyrie.Tests.ParserTests;

public class ValkyrieEcsRegressionTests : ValkyrieParserTestBase
{
    private readonly ValkyrieLanguage _language = ValkyrieLanguage.Standard;

    private ProgramRoot Parse(string source)
    {
        return ParseWithTimeout(source, _language);
    }

    #region Component 回归测试

    [Fact]
    public void Parse_ComponentWithMultipleFields_ShouldReturnAllFields()
    {
        var result = Parse("component Position { x: f32 y: f32 z: f32 }");

        var decl = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Equal("Position", decl.Name);
        Assert.Equal(3, decl.Fields.Count);
    }

    [Fact]
    public void Parse_ComponentWithDefaultValues_ShouldReturnFieldWithInitializer()
    {
        var result = Parse("component Health { hp: int = 100 max_hp: int = 100 }");

        var decl = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Equal(2, decl.Fields.Count);
        Assert.NotNull(decl.Fields[0].DefaultValue);
        Assert.NotNull(decl.Fields[1].DefaultValue);
    }

    [Fact]
    public void Parse_ComponentWithAttribute_ShouldCaptureAttribute()
    {
        var result = Parse("[Serializable]\ncomponent Tag { value: string }");

        var decl = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Single(decl.Attributes);
        Assert.Equal("Serializable", decl.Attributes[0].Name);
    }

    [Fact]
    public void Parse_ComponentWithModifiers_ShouldCaptureModifiers()
    {
        var result = Parse("public component PlayerData { score: int }");

        var decl = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Single(decl.Modifiers);
        Assert.Equal("public", decl.Modifiers[0]);
    }

    [Fact]
    public void Parse_GenericComponent_ShouldParseSuccessfully()
    {
        var result = Parse("component Container<T> { value: T }");

        Assert.NotNull(result);
    }

    #endregion

    #region System 回归测试

    [Fact]
    public void Parse_SystemWithQueryAny_ShouldReturnSystemDecl()
    {
        var result = Parse("system Collision { query = Query.any(Collider) }");

        var decl = Assert.IsType<SystemDeclaration>(result.Declarations[0]);
        Assert.Equal("Collision", decl.Name);
        Assert.Single(decl.Queries);
    }

    [Fact]
    public void Parse_SystemWithQueryNone_ShouldReturnSystemDecl()
    {
        var result = Parse("system Cleanup { query = Query.none(Alive) }");

        var decl = Assert.IsType<SystemDeclaration>(result.Declarations[0]);
        Assert.Equal("Cleanup", decl.Name);
        Assert.Single(decl.Queries);
    }

    [Fact]
    public void Parse_SystemWithMultipleQueries_ShouldReturnAllQueries()
    {
        var source = @"
            system RenderSystem {
                query_positions = Query.all(Position)
                query_sprites = Query.all(Sprite)
            }";
        var result = Parse(source);

        var decl = Assert.IsType<SystemDeclaration>(result.Declarations[0]);
        Assert.Equal("RenderSystem", decl.Name);
        Assert.Equal(2, decl.Queries.Count);
    }

    [Fact]
    public void Parse_SystemWithLifecycleMethod_ShouldReturnMethod()
    {
        var source = @"
            system Movement {
                query = Query.all(Position, Velocity)
                update(dt: f64) { }
            }";
        var result = Parse(source);

        var decl = Assert.IsType<SystemDeclaration>(result.Declarations[0]);
        Assert.Single(decl.Methods);
        Assert.Equal("update", decl.Methods[0].Name);
    }

    [Fact]
    public void Parse_SystemWithChainedQuery_ShouldReturnFilters()
    {
        var result = Parse("system Filtered { query = Query.all(Position).Query.any(Active) }");

        var decl = Assert.IsType<SystemDeclaration>(result.Declarations[0]);
        Assert.Single(decl.Queries);
    }

    #endregion

    #region Widget 回归测试

    [Fact]
    public void Parse_WidgetWithPropertiesAndRender_ShouldReturnWidgetDecl()
    {
        var source = @"
            widget Counter {
                count: int
                render() { }
            }";
        var result = Parse(source);

        var decl = Assert.IsType<WidgetDecl>(result.Declarations[0]);
        Assert.Equal("Counter", decl.Name);
        Assert.Single(decl.Properties);
        Assert.NotNull(decl.RenderMethod);
    }

    [Fact]
    public void Parse_WidgetWithOnlyProperties_ShouldHaveNoRenderMethod()
    {
        var result = Parse("widget Label { text: string }");

        var decl = Assert.IsType<WidgetDecl>(result.Declarations[0]);
        Assert.Equal("Label", decl.Name);
        Assert.Null(decl.RenderMethod);
    }

    #endregion

    #region Import 回归测试

    [Fact]
    public void Parse_ImportWithAlias_ShouldCaptureAlias()
    {
        var result = Parse("import core.math as m");

        var decl = Assert.IsType<UsingDeclaration>(result.Declarations[0]);
        Assert.Equal("core.math", decl.ModulePath);
        Assert.Equal("m", decl.Alias);
    }

    [Fact]
    public void Parse_ImportDeepPath_ShouldCaptureFullPath()
    {
        var result = Parse("import game.ecs.components");

        var decl = Assert.IsType<UsingDeclaration>(result.Declarations[0]);
        Assert.Equal("game.ecs.components", decl.ModulePath);
    }

    #endregion

    #region Variable 回归测试

    [Fact]
    public void Parse_MutableVariable_ShouldCaptureMutFlag()
    {
        var result = Parse("let mut counter: int = 0");

        var decl = Assert.IsType<LetDeclaration>(result.Declarations[0]);
        Assert.Equal("counter", decl.Name);
        Assert.True(decl.IsMutable);
    }

    [Fact]
    public void Parse_ImmutableVariable_ShouldNotBeMutable()
    {
        var result = Parse("let pi: f64 = 3.14159");

        var decl = Assert.IsType<LetDeclaration>(result.Declarations[0]);
        Assert.Equal("pi", decl.Name);
        Assert.False(decl.IsMutable);
    }

    [Fact]
    public void Parse_VariableWithoutType_ShouldHaveNullType()
    {
        var result = Parse("let x = 42");

        var decl = Assert.IsType<LetDeclaration>(result.Declarations[0]);
        Assert.Equal("x", decl.Name);
        Assert.Null(decl.VarType);
        Assert.NotNull(decl.Initializer);
    }

    #endregion
}
