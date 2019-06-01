using Nyar;
using Nyar.Assembler.NyarVM;
using Nyar.IR.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Valkyrie.Interpreter.Converter;

namespace Valkyrie.Tests.E2ETests;

public class ValkyrieE2ETests
{
    [Fact]
    public void BytecodeGenerator_ConstantModule_ShouldGenerate()
    {
        var generator = new BytecodeGenerator("test");
        var egraph = new EGraph<IKun>(null, null);

        var constId = egraph.Add(new IKun.Constant(42));
        var retId = egraph.Add(new IKun.Return(constId));
        var lambdaId = egraph.Add(IKunBuilder.Lambda(
            System.Collections.Immutable.ImmutableArray<string>.Empty, retId));
        var exportId = egraph.Add(IKunBuilder.Export("main", lambdaId));
        var moduleId = egraph.Add(IKunBuilder.Module("test",
            System.Collections.Immutable.ImmutableArray.Create(exportId)));

        var extractor = new Extractor(egraph, new TestCostModel());
        var tree = extractor.Extract(moduleId);

        var result = generator.Generate(tree);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Single(result.Functions);
        Assert.Equal("main", result.Functions[0].Name);
    }

    [Fact]
    public void BytecodeGenerator_ArithmeticModule_ShouldGenerate()
    {
        var generator = new BytecodeGenerator("arith");
        var egraph = new EGraph<IKun>(null, null);

        var leftId = egraph.Add(new IKun.Constant(10));
        var rightId = egraph.Add(new IKun.Constant(20));
        var addId = egraph.Add(IKunBuilder.BinaryOp("+", leftId, rightId));
        var retId = egraph.Add(new IKun.Return(addId));
        var lambdaId = egraph.Add(IKunBuilder.Lambda(
            System.Collections.Immutable.ImmutableArray<string>.Empty, retId));
        var exportId = egraph.Add(IKunBuilder.Export("add", lambdaId));
        var moduleId = egraph.Add(IKunBuilder.Module("arith",
            System.Collections.Immutable.ImmutableArray.Create(exportId)));

        var extractor = new Extractor(egraph, new TestCostModel());
        var tree = extractor.Extract(moduleId);

        var result = generator.Generate(tree);

        Assert.NotNull(result);
        Assert.Single(result.Functions);
        Assert.Equal("add", result.Functions[0].Name);
        Assert.True(result.Functions[0].Instructions.Count > 0);
    }

    [Fact]
    public void BytecodeGenerator_FunctionWithParams_ShouldGenerate()
    {
        var generator = new BytecodeGenerator("params");
        var egraph = new EGraph<IKun>(null, null);

        var symX = egraph.Add(IKunBuilder.Symbol("x"));
        var symY = egraph.Add(IKunBuilder.Symbol("y"));
        var addId = egraph.Add(IKunBuilder.BinaryOp("+", symX, symY));
        var retId = egraph.Add(new IKun.Return(addId));
        var lambdaId = egraph.Add(IKunBuilder.Lambda(
            System.Collections.Immutable.ImmutableArray.Create("x", "y"), retId));
        var exportId = egraph.Add(IKunBuilder.Export("add_params", lambdaId));
        var moduleId = egraph.Add(IKunBuilder.Module("params",
            System.Collections.Immutable.ImmutableArray.Create(exportId)));

        var extractor = new Extractor(egraph, new TestCostModel());
        var tree = extractor.Extract(moduleId);

        var result = generator.Generate(tree);

        Assert.NotNull(result);
        Assert.Single(result.Functions);
        Assert.Equal(2, result.Functions[0].Parameters.Count);
    }

    [Fact]
    public void BytecodeGenerator_IfElse_ShouldGenerate()
    {
        var generator = new BytecodeGenerator("ifelse");
        var egraph = new EGraph<IKun>(null, null);

        var condId = egraph.Add(new IKun.BooleanConstant(true));
        var thenId = egraph.Add(new IKun.Constant(1));
        var elseId = egraph.Add(new IKun.Constant(2));
        var choiceId = egraph.Add(IKunBuilder.Choice(condId, thenId, elseId));
        var retId = egraph.Add(new IKun.Return(choiceId));
        var lambdaId = egraph.Add(IKunBuilder.Lambda(
            System.Collections.Immutable.ImmutableArray<string>.Empty, retId));
        var exportId = egraph.Add(IKunBuilder.Export("choose", lambdaId));
        var moduleId = egraph.Add(IKunBuilder.Module("ifelse",
            System.Collections.Immutable.ImmutableArray.Create(exportId)));

        var extractor = new Extractor(egraph, new TestCostModel());
        var tree = extractor.Extract(moduleId);

        var result = generator.Generate(tree);

        Assert.NotNull(result);
        Assert.Single(result.Functions);
    }

    [Fact]
    public void BytecodeGenerator_VariableDecl_ShouldGenerate()
    {
        var generator = new BytecodeGenerator("vars");
        var egraph = new EGraph<IKun>(null, null);

        var keyId = egraph.Add(IKunBuilder.Symbol("x"));
        var valId = egraph.Add(new IKun.Constant(42));
        var stateId = egraph.Add(IKunBuilder.StateUpdate(keyId, valId));
        var retId = egraph.Add(new IKun.Return(keyId));
        var seqId = egraph.Add(IKunBuilder.Seq(
            System.Collections.Immutable.ImmutableArray.Create(stateId, retId)));
        var lambdaId = egraph.Add(IKunBuilder.Lambda(
            System.Collections.Immutable.ImmutableArray<string>.Empty, seqId));
        var exportId = egraph.Add(IKunBuilder.Export("test_var", lambdaId));
        var moduleId = egraph.Add(IKunBuilder.Module("vars",
            System.Collections.Immutable.ImmutableArray.Create(exportId)));

        var extractor = new Extractor(egraph, new TestCostModel());
        var tree = extractor.Extract(moduleId);

        var result = generator.Generate(tree);

        Assert.NotNull(result);
        Assert.Single(result.Functions);
    }

    [Fact]
    public void AstToIkunConverter_ShaderDecl_ShouldConvert()
    {
        var converter = new AstToIkunConverter();

        var shader = new Oak.Valkyrie.AST.ShaderDecl
        {
            Name = "MyShader"
        };

        var ast = new Oak.Valkyrie.AST.CompilationUnit
        {
            Declarations = [shader],
            FilePath = "shader_test"
        };

        var result = converter.Convert(ast, "shader_test");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }

    [Fact]
    public void ShaderAstToIkunConverter_ShaderDecl_ShouldConvertWithGpuContext()
    {
        var converter = new ShaderAstToIkunConverter();

        var shader = new Oak.Valkyrie.AST.ShaderDecl
        {
            Name = "TestShader"
        };

        var ast = new Oak.Valkyrie.AST.CompilationUnit
        {
            Declarations = [shader],
            FilePath = "shader_module"
        };

        var result = converter.Convert(ast, "shader_module");

        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
    }
}