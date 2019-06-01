using Nyar.Assembler;
using Nyar.Optimizer;
using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class ModuleDceTests
{
    [Fact]
    public void Run_RemovesUnreachableFunctions()
    {
        var unit = new CompilationUnit("test");

        var mainFunc = new NyarFunction("main", "i32");
        unit.AddFunction(mainFunc);
        unit.AddExport(new ModuleExport("main", "function"));

        var deadFunc = new NyarFunction("dead_code", "i32");
        unit.AddFunction(deadFunc);

        Assert.Equal(2, unit.Functions.Count);

        ModuleDce.Run(unit);

        Assert.Single(unit.Functions);
        Assert.Equal("main", unit.Functions[0].Name);
    }

    [Fact]
    public void Run_KeepsReachableFunctions()
    {
        var unit = new CompilationUnit("test");

        var mainFunc = new NyarFunction("main", "i32");
        mainFunc.AddInstruction(new Instruction(0, Operand.CreateFunctionRef("helper")));
        unit.AddFunction(mainFunc);
        unit.AddExport(new ModuleExport("main", "function"));

        var helperFunc = new NyarFunction("helper", "i32");
        unit.AddFunction(helperFunc);

        ModuleDce.Run(unit);

        Assert.Equal(2, unit.Functions.Count);
    }

    [Fact]
    public void Run_NoExports_KeepsNothing()
    {
        var unit = new CompilationUnit("test");

        var func = new NyarFunction("orphan", "i32");
        unit.AddFunction(func);

        ModuleDce.Run(unit);

        Assert.Empty(unit.Functions);
    }

    [Fact]
    public void Run_RemovesUnusedImports()
    {
        var unit = new CompilationUnit("test");

        var mainFunc = new NyarFunction("main", "i32");
        mainFunc.AddInstruction(new Instruction(0, Operand.CreateFunctionRef("ext_func")));
        unit.AddFunction(mainFunc);
        unit.AddExport(new ModuleExport("main", "function"));

        unit.AddImport(new ModuleImport("env", "unused_func", "function"));
        unit.AddImport(new ModuleImport("env", "ext_func", "function"));

        ModuleDce.Run(unit);

        Assert.Single(unit.Imports);
        Assert.Equal("ext_func", unit.Imports[0].Name);
    }
}