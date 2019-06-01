using System.Collections.Immutable;
using Nyar;
using Nyar.IR.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.Types;
using Oak.Valkyrie.AST;
using Valkyrie.Interpreter.Converter;

using AstTypeAnnotation = Oak.Valkyrie.AST.TypeAnnotation;

namespace Valkyrie.Tests;

public class VarDeclAndShaderConverterTests
{
    [Fact]
    public void AstToIkunConverter_VarDecl_Immutable_ShouldConvert()
    {
        var converter = new AstToIkunConverter();

        var decl = new VariableDecl
        {
            Name = "x",
            VarType = new AstTypeAnnotation { Name = "i32" },
            Initializer = new LiteralExpr { LiteralKind = LiteralType.Number, Value = 42 },
            IsMutable = false
        };

        var ast = new CompilationUnit { Declarations = [decl], FilePath = "var_decl_test" };
        var result = converter.Convert(ast, "var_decl_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }

    [Fact]
    public void AstToIkunConverter_VarDecl_Mutable_ShouldConvert()
    {
        var converter = new AstToIkunConverter();

        var decl = new VariableDecl
        {
            Name = "x",
            VarType = new AstTypeAnnotation { Name = "i32" },
            Initializer = new LiteralExpr { LiteralKind = LiteralType.Number, Value = 42 },
            IsMutable = true
        };

        var ast = new CompilationUnit { Declarations = [decl], FilePath = "var_decl_mutable_test" };
        var result = converter.Convert(ast, "var_decl_mutable_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }

    [Fact]
    public void ShaderAstToIkunConverter_ShaderWithUniform_ShouldConvert()
    {
        var converter = new ShaderAstToIkunConverter();

        var uniform = new UniformDecl
        {
            Name = "tex",
            UniformType = new AstTypeAnnotation { Name = "Texture2D" }
        };
        var vertexStage = new VertexShaderDecl();
        var shader = new ShaderDecl { Name = "MyShader", Stages = [vertexStage] };

        var ast = new CompilationUnit { Declarations = [shader, uniform], FilePath = "shader_uniform_test" };
        var result = converter.Convert(ast, "shader_uniform_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }

    [Fact]
    public void ShaderAstToIkunConverter_ShaderWithVarying_ShouldConvert()
    {
        var converter = new ShaderAstToIkunConverter();

        var varying = new VaryingDecl
        {
            Name = "uv",
            VaryingType = new AstTypeAnnotation { Name = "vec2" }
        };
        var vertexStage = new VertexShaderDecl();
        var fragmentStage = new FragmentShaderDecl();
        var shader = new ShaderDecl { Name = "MyShader", Stages = [vertexStage, fragmentStage] };

        var ast = new CompilationUnit { Declarations = [shader, varying], FilePath = "shader_varying_test" };
        var result = converter.Convert(ast, "shader_varying_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }

    [Fact]
    public void ShaderAstToIkunConverter_ComputeShader_ShouldConvert()
    {
        var converter = new ShaderAstToIkunConverter();

        var computeStage = new ComputeShaderDecl();
        var shader = new ShaderDecl { Name = "ComputeShader", Stages = [computeStage] };

        var ast = new CompilationUnit { Declarations = [shader], FilePath = "compute_shader_test" };
        var result = converter.Convert(ast, "compute_shader_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }
}
