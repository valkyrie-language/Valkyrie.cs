using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;

namespace Valkyrie.Tests.ParserTests;

public class ValkyrieParserTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;

    public ValkyrieParserTests()
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

    [Fact]
    public void EmptySource_ShouldReturnEmptyCompilationUnit()
    {
        var result = ParseSource("");
        Assert.NotNull(result);
        Assert.Empty(result.Declarations);
    }

    [Fact]
    public void ComponentDecl_ShouldParse()
    {
        var source = """
                     component Position {
                         x: f32;
                         y: f32;
                     }
                     """;
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var comp = Assert.IsType<ComponentDecl>(unit.Declarations[0]);
        Assert.Equal("Position", comp.Name);
        Assert.Equal(2, comp.Fields.Count);
    }

    [Fact]
    public void ComponentDecl_WithAttribute_ShouldParse()
    {
        var source = """
                     [Serializable]
                     component Tag {
                         value: bool;
                     }
                     """;
        var unit = ParseSource(source);
        var comp = Assert.IsType<ComponentDecl>(unit.Declarations[0]);
        Assert.Equal("Tag", comp.Name);
        Assert.Single(comp.Attributes);
    }

    [Fact]
    public void ComponentDecl_WithDefaultValue_ShouldParse()
    {
        var source = """
                     component Config {
                         maxCount: i32 = 100;
                         name: string = "default";
                     }
                     """;
        var unit = ParseSource(source);
        var comp = Assert.IsType<ComponentDecl>(unit.Declarations[0]);
        Assert.Equal(2, comp.Fields.Count);
        Assert.NotNull(comp.Fields[0].DefaultValue);
    }

    [Fact]
    public void SystemDecl_ShouldParse()
    {
        var source = """
                     system MovementSystem {
                         query all = Query.all(Position, Velocity);
                         
                         on_update(frame: Frame) {
                             loop entity in query.all {
                                 entity.position.x += entity.velocity.x * frame.dt
                             }
                         }
                     }
                     """;
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var sys = Assert.IsType<SystemDecl>(unit.Declarations[0]);
        Assert.Equal("MovementSystem", sys.Name);
    }

    [Fact]
    public void MicroDecl_ShouldParse()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 {
                         return a + b
                     }
                     """;
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("add", func.Name);
        Assert.Equal(2, func.Parameters.Count);
        Assert.NotNull(func.ReturnType);
        Assert.Equal("i32", func.ReturnType.Name);
    }

    [Fact]
    public void VariableDecl_ShouldParse()
    {
        var source = "let x: i32 = 42;";
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<VariableDecl>(unit.Declarations[0]);
        Assert.Equal("x", varDecl.Name);
        Assert.Equal("i32", varDecl.VarType?.Name);
        Assert.NotNull(varDecl.Initializer);
        Assert.False(varDecl.IsMutable);
    }

    [Fact]
    public void MutableVariableDecl_ShouldParse()
    {
        var source = "let mut counter: i32 = 0;";
        var unit = ParseSource(source);
        var varDecl = Assert.IsType<VariableDecl>(unit.Declarations[0]);
        Assert.True(varDecl.IsMutable);
    }

    [Fact]
    public void ImportDecl_ShouldParse()
    {
        var source = "import Gnosis.ECS;";
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var import = Assert.IsType<ImportDecl>(unit.Declarations[0]);
        Assert.Equal("Gnosis.ECS", import.ModulePath);
    }

    [Fact]
    public void ImportDecl_WithAlias_ShouldParse()
    {
        var source = "import Gnosis.ECS as ecs;";
        var unit = ParseSource(source);
        var import = Assert.IsType<ImportDecl>(unit.Declarations[0]);
        Assert.Equal("Gnosis.ECS", import.ModulePath);
        Assert.Equal("ecs", import.Alias);
    }

    [Fact]
    public void UsingDecl_ShouldParse()
    {
        var source = "using Gnosis.ECS;";
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var usingDecl = Assert.IsType<UsingDecl>(unit.Declarations[0]);
        Assert.Equal("Gnosis.ECS", usingDecl.ModulePath);
    }

    [Fact]
    public void UsingDecl_WithAlias_ShouldParse()
    {
        var source = "using Gnosis.ECS as ecs;";
        var unit = ParseSource(source);
        var usingDecl = Assert.IsType<UsingDecl>(unit.Declarations[0]);
        Assert.Equal("Gnosis.ECS", usingDecl.ModulePath);
        Assert.Equal("ecs", usingDecl.Alias);
    }

    [Fact]
    public void WidgetDecl_ShouldParse()
    {
        var source = """
                     widget Button {
                         label: string;
                         
                         render(frame: Frame) {
                             return null
                         }
                     }
                     """;
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        var widget = Assert.IsType<WidgetDecl>(unit.Declarations[0]);
        Assert.Equal("Button", widget.Name);
        Assert.Single(widget.Properties);
        Assert.NotNull(widget.RenderMethod);
    }

    [Fact]
    public void BinaryExpression_ShouldParse()
    {
        var source = "let result: i32 = 1 + 2 * 3;";
        var unit = ParseSource(source);
        var varDecl = Assert.IsType<VariableDecl>(unit.Declarations[0]);
        Assert.NotNull(varDecl.Initializer);
    }

    [Fact]
    public void MemberAccessExpression_ShouldParse()
    {
        var source = "let val: f32 = entity.position.x;";
        var unit = ParseSource(source);
        var varDecl = Assert.IsType<VariableDecl>(unit.Declarations[0]);
        Assert.NotNull(varDecl.Initializer);
    }

    [Fact]
    public void CallExpression_ShouldParse()
    {
        var source = "let result: i32 = add(1, 2);";
        var unit = ParseSource(source);
        var varDecl = Assert.IsType<VariableDecl>(unit.Declarations[0]);
        Assert.NotNull(varDecl.Initializer);
    }

    [Fact]
    public void IfStatement_ShouldParse()
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
        var unit = ParseSource(source);
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.NotNull(func.Body);
    }

    [Fact]
    public void LoopStatement_ShouldParse()
    {
        var source = """
                     micro iterate(items: list) {
                         loop item in items {
                             process(item)
                         }
                     }
                     """;
        var unit = ParseSource(source);
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.NotNull(func.Body);
    }

    [Fact]
    public void WhileStatement_ShouldParse()
    {
        var source = """
                     micro countdown(n: i32) {
                         while n > 0 {
                             n -= 1
                         }
                     }
                     """;
        var unit = ParseSource(source);
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.NotNull(func.Body);
    }

    [Fact]
    public void GenericType_ShouldParse()
    {
        var source = "let items: list<i32> = [];";
        var unit = ParseSource(source);
        Assert.NotNull(unit);
    }

    [Fact]
    public void NestedGenericType_ShouldParse()
    {
        var source = "let data: map<string, list<i32>> = {};";
        var unit = ParseSource(source);
        Assert.NotNull(unit);
    }

    [Fact]
    public void MultipleDeclarations_ShouldParse()
    {
        var source = """
                     using Gnosis.ECS;

                     component Position {
                         x: f32;
                         y: f32;
                     }

                     component Velocity {
                         dx: f32;
                         dy: f32;
                     }

                     micro update(pos: Position, vel: Velocity, dt: f32) {
                         pos.x += vel.dx * dt;
                         pos.y += vel.dy * dt
                     }
                     """;
        var unit = ParseSource(source);
        Assert.Equal(4, unit.Declarations.Count);
    }

    [Fact]
    public void PluginDeclaration_ShouldParse()
    {
        var source = """
                     plugin PhysicsEngine {
                         requires_arch = ["x86_64", "aarch64"];
                         provides_macros = ["PHYSICS_2D", "PHYSICS_3D"];
                         provides_capabilities = ["collision", "rigidbody"]
                     }
                     """;
        var unit = ParseSource(source);
        var plugin = Assert.IsType<PluginDecl>(unit.Declarations[0]);
        Assert.Equal("PhysicsEngine", plugin.Name);
        Assert.NotNull(plugin.RequiresArch);
        Assert.Equal(2, plugin.ProvidesMacros.Count);
        Assert.Equal(2, plugin.ProvidesCapabilities.Count);
    }
}