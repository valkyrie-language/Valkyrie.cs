using Nyar;
using Nyar.Assembler;
using Nyar.Assembler.NyarVM;
using Nyar.Assembler.Wasm;
using Nyar.IR.EGraph;
using Nyar.IR.Extractor;
using Nyar.Types;
using Oak.Valkyrie;
using Nyar.VM;
using Nyar.VM.Bytecode;
using Nyar.Avatar;
using Nyar.Optimizer;
using Oak.Diagnostics;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.Interpreter.Converter;

using AstCompilationUnit = Oak.Valkyrie.AST.CompilationUnit;
using AsmCompilationUnit = Nyar.Assembler.CompilationUnit;

namespace Valkyrie.Tests.E2ETests;

public class NativeTargetTests
{
    #region 辅助方法

    private static AsmCompilationUnit CompileToUnit(string source, string moduleName)
    {
        var diagnostics = new DiagnosticSink();
        var lexer = new ValkyrieLexer(diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);

        var tokens = lexer.Tokenize(source);
        var ast = (AstCompilationUnit)parser.Parse(tokens);

        Assert.Empty(diagnostics.Errors);

        var converter = new AstToIkunConverter();
        var conversion = converter.Convert(ast, moduleName);

        var loweringPass = new LoweringPass(new CoreDialect().LoweringRules);
        loweringPass.Run(conversion.EGraph);

        var extractor = new Extractor(conversion.EGraph, new TestCostModel2());
        var tree = extractor.Extract(conversion.ModuleId);

        var generator = new BytecodeGenerator(moduleName);
        return generator.Generate(tree);
    }

    private static NyarVM CompileAndLoad(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var module = CompilationUnitSerializer.Serialize(unit);

        var vm = new NyarVM();
        vm.Load(module);
        return vm;
    }

    private static byte[] CompileToWasm(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var moduleData = WasmBackend.ConvertModule(unit);
        return Acorn.Wasm.Encode.WasmEncoder.EncodeModule(moduleData);
    }

    private static GeneratedFiles CompileToClr(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var backend = new ClrBackend();
        var options = new CompilationOptions
        {
            Target = new CompilationTarget { Arch = Arch.Clr, Abi = ABI.CLR, Api = API.NET },
            OptimizationLevel = OptimizationLevel.Basic
        };
        return backend.Compile(unit, options);
    }

    private static GeneratedFiles CompileToJvm(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var backend = new JvmBackend();
        var options = new CompilationOptions
        {
            Target = new CompilationTarget { Arch = Arch.Jvm, Abi = ABI.JVM, Api = API.Java },
            OptimizationLevel = OptimizationLevel.Basic
        };
        return backend.Compile(unit, options);
    }

    private static GeneratedFiles CompileToNativePe(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var backend = new NativeBackend();
        var options = new CompilationOptions
        {
            Target = new CompilationTarget { Arch = Arch.X86_64 },
            OptimizationLevel = OptimizationLevel.Basic
        };
        options.AdditionalOptions["OutputFormat"] = "pe";
        return backend.Compile(unit, options);
    }

    private static GeneratedFiles CompileToNativeElf(string source, string moduleName)
    {
        var unit = CompileToUnit(source, moduleName);
        var backend = new NativeBackend();
        var options = new CompilationOptions
        {
            Target = new CompilationTarget { Arch = Arch.X86_64 },
            OptimizationLevel = OptimizationLevel.Basic
        };
        options.AdditionalOptions["OutputFormat"] = "elf";
        return backend.Compile(unit, options);
    }

    #endregion

    #region NyarVM 目标测试 — 基础运算

    [Fact]
    public void NyarTarget_Constant_Return_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro main(): i32 { return 42 }", "const_test");
        var result = vm.Run("const_test", "main");

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(42, result.Int);
    }

    [Fact]
    public void NyarTarget_Arithmetic_Add_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro add(a: i32, b: i32): i32 { return a + b }", "add_test");
        var result = vm.Run("add_test", "add", Value.FromInt(3), Value.FromInt(5));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(8, result.Int);
    }

    [Fact]
    public void NyarTarget_Subtraction_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro sub(a: i32, b: i32): i32 { return a - b }", "sub_test");
        var result = vm.Run("sub_test", "sub", Value.FromInt(10), Value.FromInt(3));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(7, result.Int);
    }

    [Fact]
    public void NyarTarget_Multiplication_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro mul(a: i32, b: i32): i32 { return a * b }", "mul_test");
        var result = vm.Run("mul_test", "mul", Value.FromInt(4), Value.FromInt(7));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(28, result.Int);
    }

    [Fact]
    public void NyarTarget_Division_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro div(a: i32, b: i32): i32 { return a / b }", "div_test");
        var result = vm.Run("div_test", "div", Value.FromInt(20), Value.FromInt(4));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(5, result.Int);
    }

    [Fact]
    public void NyarTarget_Modulo_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro mod(a: i32, b: i32): i32 { return a % b }", "mod_test");
        var result = vm.Run("mod_test", "mod", Value.FromInt(17), Value.FromInt(5));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(2, result.Int);
    }

    [Fact]
    public void NyarTarget_Negation_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro negate(a: i32): i32 { return -a }", "neg_test");
        var result = vm.Run("neg_test", "negate", Value.FromInt(5));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(-5, result.Int);
    }

    #endregion

    #region NyarVM 目标测试 — 比较与逻辑

    [Fact]
    public void NyarTarget_Comparison_GreaterThan_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro is_greater(a: i32, b: i32): i32 { if a > b { return 1 } else { return 0 } }", "cmp_gt_test");
        var resultTrue = vm.Run("cmp_gt_test", "is_greater", Value.FromInt(10), Value.FromInt(3));
        Assert.Equal(1, resultTrue.Int);

        var resultFalse = vm.Run("cmp_gt_test", "is_greater", Value.FromInt(3), Value.FromInt(10));
        Assert.Equal(0, resultFalse.Int);
    }

    [Fact]
    public void NyarTarget_Comparison_LessThan_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro is_less(a: i32, b: i32): i32 { if a < b { return 1 } else { return 0 } }", "cmp_lt_test");
        var resultTrue = vm.Run("cmp_lt_test", "is_less", Value.FromInt(3), Value.FromInt(10));
        Assert.Equal(1, resultTrue.Int);

        var resultFalse = vm.Run("cmp_lt_test", "is_less", Value.FromInt(10), Value.FromInt(3));
        Assert.Equal(0, resultFalse.Int);
    }

    [Fact]
    public void NyarTarget_Equality_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro equals(a: i32, b: i32): i32 { if a == b { return 1 } else { return 0 } }", "eq_test");
        var resultTrue = vm.Run("eq_test", "equals", Value.FromInt(5), Value.FromInt(5));
        Assert.Equal(1, resultTrue.Int);

        var resultFalse = vm.Run("eq_test", "equals", Value.FromInt(5), Value.FromInt(3));
        Assert.Equal(0, resultFalse.Int);
    }

    [Fact]
    public void NyarTarget_NotEqual_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro not_eq(a: i32, b: i32): i32 { if a != b { return 1 } else { return 0 } }", "neq_test");
        var resultTrue = vm.Run("neq_test", "not_eq", Value.FromInt(5), Value.FromInt(3));
        Assert.Equal(1, resultTrue.Int);

        var resultFalse = vm.Run("neq_test", "not_eq", Value.FromInt(5), Value.FromInt(5));
        Assert.Equal(0, resultFalse.Int);
    }

    [Fact]
    public void NyarTarget_GreaterOrEqual_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro is_ge(a: i32, b: i32): i32 { if a >= b { return 1 } else { return 0 } }", "ge_test");
        var resultEqual = vm.Run("ge_test", "is_ge", Value.FromInt(5), Value.FromInt(5));
        Assert.Equal(1, resultEqual.Int);

        var resultGreater = vm.Run("ge_test", "is_ge", Value.FromInt(6), Value.FromInt(5));
        Assert.Equal(1, resultGreater.Int);

        var resultLess = vm.Run("ge_test", "is_ge", Value.FromInt(4), Value.FromInt(5));
        Assert.Equal(0, resultLess.Int);
    }

    [Fact]
    public void NyarTarget_LessOrEqual_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro is_le(a: i32, b: i32): i32 { if a <= b { return 1 } else { return 0 } }", "le_test");
        var resultEqual = vm.Run("le_test", "is_le", Value.FromInt(5), Value.FromInt(5));
        Assert.Equal(1, resultEqual.Int);

        var resultLess = vm.Run("le_test", "is_le", Value.FromInt(4), Value.FromInt(5));
        Assert.Equal(1, resultLess.Int);

        var resultGreater = vm.Run("le_test", "is_le", Value.FromInt(6), Value.FromInt(5));
        Assert.Equal(0, resultGreater.Int);
    }

    #endregion

    #region NyarVM 目标测试 — 变量与控制流

    [Fact]
    public void NyarTarget_VariableDecl_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro compute(): i32 { let x: i32 = 10 return x }", "var_test");
        var result = vm.Run("var_test", "compute");

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(10, result.Int);
    }

    [Fact]
    public void NyarTarget_NestedArithmetic_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro calc(a: i32, b: i32, c: i32): i32 { return a * b + c }", "nested_test");
        var result = vm.Run("nested_test", "calc", Value.FromInt(2), Value.FromInt(3), Value.FromInt(4));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(10, result.Int);
    }

    [Fact]
    public void NyarTarget_ComplexArithmetic_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro calc(a: i32, b: i32): i32 { return (a + b) * (a - b) }", "complex_test");
        var result = vm.Run("complex_test", "calc", Value.FromInt(5), Value.FromInt(3));

        Assert.Equal(Nyar.ValueType.Int, result.Type);
        Assert.Equal(16, result.Int);
    }

    [Fact]
    public void NyarTarget_IfElse_WithVariable_ShouldCompileAndRun()
    {
        var source = "micro abs(x: i32): i32 { if x < 0 { return -x } else { return x } }";
        var vm = CompileAndLoad(source, "abs_test");

        var posResult = vm.Run("abs_test", "abs", Value.FromInt(5));
        Assert.Equal(5, posResult.Int);

        var negResult = vm.Run("abs_test", "abs", Value.FromInt(-3));
        Assert.Equal(3, negResult.Int);

        var zeroResult = vm.Run("abs_test", "abs", Value.FromInt(0));
        Assert.Equal(0, zeroResult.Int);
    }

    [Fact]
    public void NyarTarget_NestedIfElse_ShouldCompileAndRun()
    {
        var source = "micro classify(x: i32): i32 { if x > 0 { return 1 } else { if x < 0 { return -1 } else { return 0 } } }";
        var vm = CompileAndLoad(source, "classify_test");

        Assert.Equal(1, vm.Run("classify_test", "classify", Value.FromInt(5)).Int);
        Assert.Equal(-1, vm.Run("classify_test", "classify", Value.FromInt(-3)).Int);
        Assert.Equal(0, vm.Run("classify_test", "classify", Value.FromInt(0)).Int);
    }

    [Fact]
    public void NyarTarget_VariableWithExpression_ShouldCompileAndRun()
    {
        var source = "micro compute(a: i32, b: i32): i32 { let sum: i32 = a + b return sum * 2 }";
        var vm = CompileAndLoad(source, "varexpr_test");

        var result = vm.Run("varexpr_test", "compute", Value.FromInt(3), Value.FromInt(4));
        Assert.Equal(14, result.Int);
    }

    [Fact]
    public void NyarTarget_MultipleVariables_ShouldCompileAndRun()
    {
        var source = "micro compute(a: i32, b: i32): i32 { let x: i32 = a + b let y: i32 = a - b return x * y }";
        var vm = CompileAndLoad(source, "multivar_test");

        var result = vm.Run("multivar_test", "compute", Value.FromInt(5), Value.FromInt(3));
        Assert.Equal(16, result.Int);
    }

    #endregion

    #region NyarVM 目标测试 — 多函数调用

    [Fact]
    public void NyarTarget_MultipleFunctions_ShouldCompileAndRun()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro main(): i32 { return add(10, 20) }
";
        var vm = CompileAndLoad(source, "multi_func_test");
        var result = vm.Run("multi_func_test", "main");

        Assert.Equal(30, result.Int);
    }

    [Fact]
    public void NyarTarget_FunctionCallWithArgs_ShouldCompileAndRun()
    {
        var source = @"
micro double(x: i32): i32 { return x * 2 }
micro main(a: i32): i32 { return double(a) }
";
        var vm = CompileAndLoad(source, "call_args_test");
        var result = vm.Run("call_args_test", "main", Value.FromInt(7));

        Assert.Equal(14, result.Int);
    }

    [Fact]
    public void NyarTarget_ChainedFunctionCalls_ShouldCompileAndRun()
    {
        var source = @"
micro square(x: i32): i32 { return x * x }
micro add(a: i32, b: i32): i32 { return a + b }
micro main(): i32 { return add(square(3), square(4)) }
";
        var vm = CompileAndLoad(source, "chain_test");
        var result = vm.Run("chain_test", "main");

        Assert.Equal(25, result.Int);
    }

    [Fact]
    public void NyarTarget_RecursiveFactorial_ShouldCompileAndRun()
    {
        var source = @"
micro fact(n: i32): i32 { if n <= 1 { return 1 } else { return n * fact(n - 1) } }
";
        var vm = CompileAndLoad(source, "fact_test");

        Assert.Equal(1, vm.Run("fact_test", "fact", Value.FromInt(1)).Int);
        Assert.Equal(1, vm.Run("fact_test", "fact", Value.FromInt(0)).Int);
        Assert.Equal(2, vm.Run("fact_test", "fact", Value.FromInt(2)).Int);
        Assert.Equal(6, vm.Run("fact_test", "fact", Value.FromInt(3)).Int);
        Assert.Equal(120, vm.Run("fact_test", "fact", Value.FromInt(5)).Int);
    }

    [Fact]
    public void NyarTarget_Fibonacci_ShouldCompileAndRun()
    {
        var source = @"
micro fib(n: i32): i32 { if n <= 1 { return n } else { return fib(n - 1) + fib(n - 2) } }
";
        var vm = CompileAndLoad(source, "fib_test");

        Assert.Equal(0, vm.Run("fib_test", "fib", Value.FromInt(0)).Int);
        Assert.Equal(1, vm.Run("fib_test", "fib", Value.FromInt(1)).Int);
        Assert.Equal(1, vm.Run("fib_test", "fib", Value.FromInt(2)).Int);
        Assert.Equal(2, vm.Run("fib_test", "fib", Value.FromInt(3)).Int);
        Assert.Equal(3, vm.Run("fib_test", "fib", Value.FromInt(4)).Int);
        Assert.Equal(5, vm.Run("fib_test", "fib", Value.FromInt(5)).Int);
        Assert.Equal(8, vm.Run("fib_test", "fib", Value.FromInt(6)).Int);
    }

    #endregion

    #region NyarVM 目标测试 — 字节码往返（编码→解码→执行）

    [Fact]
    public void NyarTarget_Roundtrip_EncodeDecodeExecute()
    {
        var source = "micro add(a: i32, b: i32): i32 { return a + b }";
        var unit = CompileToUnit(source, "roundtrip_test");

        var module = CompilationUnitSerializer.Serialize(unit);

        var vm = new NyarVM();
        vm.Load(module);

        var result = vm.Run("roundtrip_test", "add", Value.FromInt(10), Value.FromInt(20));
        Assert.Equal(30, result.Int);
    }

    [Fact]
    public void NyarTarget_Roundtrip_ComplexFunction()
    {
        var source = @"
micro max(a: i32, b: i32): i32 { if a > b { return a } else { return b } }
micro main(x: i32, y: i32): i32 { return max(x, y) * 2 }
";
        var unit = CompileToUnit(source, "roundtrip_complex");

        var module = CompilationUnitSerializer.Serialize(unit);

        var vm = new NyarVM();
        vm.Load(module);

        Assert.Equal(20, vm.Run("roundtrip_complex", "main", Value.FromInt(10), Value.FromInt(5)).Int);
        Assert.Equal(14, vm.Run("roundtrip_complex", "main", Value.FromInt(3), Value.FromInt(7)).Int);
    }

    [Fact]
    public void NyarTarget_Roundtrip_ModuleMetadata()
    {
        var source = "micro main(): i32 { return 42 }";
        var unit = CompileToUnit(source, "meta_test");

        var module = CompilationUnitSerializer.Serialize(unit);

        Assert.Equal("meta_test", module.Name);
        Assert.NotEmpty(module.Constants);
        Assert.NotEmpty(module.Functions);
        Assert.NotNull(module.RawBytecode);
        Assert.NotEmpty(module.RawBytecode);
    }

    [Fact]
    public void NyarTarget_LoadBytecode_ViaNyarVM()
    {
        var source = "micro compute(a: i32, b: i32): i32 { return a * b + a }";
        var unit = CompileToUnit(source, "load_test");

        var module = CompilationUnitSerializer.Serialize(unit);

        var vm = new NyarVM();
        vm.Load(module);

        Assert.True(vm.HasModule("load_test"));

        var result = vm.Run("load_test", "compute", Value.FromInt(3), Value.FromInt(5));
        Assert.Equal(18, result.Int);
    }

    #endregion

    #region NyarVM 目标测试 — 边界值

    [Fact]
    public void NyarTarget_ZeroValues_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro zero(): i32 { return 0 }", "zero_test");
        var result = vm.Run("zero_test", "zero");
        Assert.Equal(0, result.Int);
    }

    [Fact]
    public void NyarTarget_NegativeResult_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro neg(a: i32, b: i32): i32 { return a - b }", "neg_res_test");
        var result = vm.Run("neg_res_test", "neg", Value.FromInt(3), Value.FromInt(10));
        Assert.Equal(-7, result.Int);
    }

    [Fact]
    public void NyarTarget_LargeArithmetic_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro large(a: i32, b: i32): i32 { return a * b }", "large_test");
        var result = vm.Run("large_test", "large", Value.FromInt(1000), Value.FromInt(2000));
        Assert.Equal(2000000, result.Int);
    }

    [Fact]
    public void NyarTarget_NoArgFunction_ShouldCompileAndRun()
    {
        var vm = CompileAndLoad("micro fortytwo(): i32 { return 42 }", "noarg_test");
        var result = vm.Run("noarg_test", "fortytwo");
        Assert.Equal(42, result.Int);
    }

    #endregion

    #region WASM 目标测试

    [Fact]
    public void WasmTarget_Constant_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro main(): i32 { return 42 }", "wasm_const");

        Assert.NotEmpty(wasmBytes);
        Assert.True(wasmBytes.Length > 8);

        Assert.Equal(0x00, wasmBytes[0]);
        Assert.Equal(0x61, wasmBytes[1]);
        Assert.Equal(0x73, wasmBytes[2]);
        Assert.Equal(0x6D, wasmBytes[3]);
    }

    [Fact]
    public void WasmTarget_Arithmetic_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro add(a: i32, b: i32): i32 { return a + b }", "wasm_add");

        Assert.NotEmpty(wasmBytes);
        Assert.True(wasmBytes.Length > 8);
    }

    [Fact]
    public void WasmTarget_IfElse_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro max(a: i32, b: i32): i32 { if a > b { return a } else { return b } }", "wasm_max");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_VariableDecl_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro compute(): i32 { let x: i32 = 10 return x }", "wasm_var");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_Subtraction_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro sub(a: i32, b: i32): i32 { return a - b }", "wasm_sub");

        Assert.NotEmpty(wasmBytes);
        Assert.Equal(0x00, wasmBytes[0]);
        Assert.Equal(0x61, wasmBytes[1]);
        Assert.Equal(0x73, wasmBytes[2]);
        Assert.Equal(0x6D, wasmBytes[3]);
    }

    [Fact]
    public void WasmTarget_Multiplication_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro mul(a: i32, b: i32): i32 { return a * b }", "wasm_mul");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_Comparison_ShouldCompile()
    {
        var wasmBytes = CompileToWasm("micro is_gt(a: i32, b: i32): i32 { if a > b { return 1 } else { return 0 } }", "wasm_cmp");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_MultipleFunctions_ShouldCompile()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro sub(a: i32, b: i32): i32 { return a - b }
";
        var wasmBytes = CompileToWasm(source, "wasm_multi");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_NestedIfElse_ShouldCompile()
    {
        var source = "micro classify(x: i32): i32 { if x > 0 { return 1 } else { if x < 0 { return -1 } else { return 0 } } }";
        var wasmBytes = CompileToWasm(source, "wasm_nested");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_ComplexExpression_ShouldCompile()
    {
        var source = "micro calc(a: i32, b: i32): i32 { return (a + b) * (a - b) }";
        var wasmBytes = CompileToWasm(source, "wasm_complex");

        Assert.NotEmpty(wasmBytes);
    }

    [Fact]
    public void WasmTarget_Version_ShouldBe1()
    {
        var wasmBytes = CompileToWasm("micro main(): i32 { return 1 }", "wasm_ver");

        Assert.True(wasmBytes.Length >= 8);

        Assert.Equal(0x01, wasmBytes[4]);
        Assert.Equal(0x00, wasmBytes[5]);
        Assert.Equal(0x00, wasmBytes[6]);
        Assert.Equal(0x00, wasmBytes[7]);
    }

    #endregion

    #region CLR 目标测试

    [Fact]
    public void ClrTarget_Constant_ShouldCompile()
    {
        var result = CompileToClr("micro main(): i32 { return 42 }", "clr_const");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.NotEmpty(file.Content);
            Assert.True(file.Content.Length >= 2);

            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);
        }
    }

    [Fact]
    public void ClrTarget_Arithmetic_ShouldCompile()
    {
        var result = CompileToClr("micro add(a: i32, b: i32): i32 { return a + b }", "clr_add");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_IfElse_ShouldCompile()
    {
        var result = CompileToClr("micro max(a: i32, b: i32): i32 { if a > b { return a } else { return b } }", "clr_max");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_MultipleFunctions_ShouldCompile()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro sub(a: i32, b: i32): i32 { return a - b }
micro main(): i32 { return add(10, 20) }
";
        var result = CompileToClr(source, "clr_multi");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_Subtraction_ShouldCompile()
    {
        var result = CompileToClr("micro sub(a: i32, b: i32): i32 { return a - b }", "clr_sub");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);
        }
    }

    [Fact]
    public void ClrTarget_Multiplication_ShouldCompile()
    {
        var result = CompileToClr("micro mul(a: i32, b: i32): i32 { return a * b }", "clr_mul");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_Comparison_ShouldCompile()
    {
        var result = CompileToClr("micro is_gt(a: i32, b: i32): i32 { if a > b { return 1 } else { return 0 } }", "clr_cmp");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_VariableDecl_ShouldCompile()
    {
        var result = CompileToClr("micro compute(): i32 { let x: i32 = 10 return x }", "clr_var");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_NestedIfElse_ShouldCompile()
    {
        var source = "micro classify(x: i32): i32 { if x > 0 { return 1 } else { if x < 0 { return -1 } else { return 0 } } }";
        var result = CompileToClr(source, "clr_nested");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void ClrTarget_ComplexExpression_ShouldCompile()
    {
        var source = "micro calc(a: i32, b: i32): i32 { return (a + b) * (a - b) }";
        var result = CompileToClr(source, "clr_complex");

        Assert.NotEmpty(result.Files);
    }

    #endregion

    #region JVM 目标测试

    [Fact]
    public void JvmTarget_Constant_ShouldCompile()
    {
        var result = CompileToJvm("micro main(): i32 { return 42 }", "JvmConst");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.NotEmpty(file.Content);
            Assert.True(file.Content.Length >= 4);

            Assert.Equal(0xCA, file.Content[0]);
            Assert.Equal(0xFE, file.Content[1]);
            Assert.Equal(0xBA, file.Content[2]);
            Assert.Equal(0xBE, file.Content[3]);
        }
    }

    [Fact]
    public void JvmTarget_Arithmetic_ShouldCompile()
    {
        var result = CompileToJvm("micro add(a: i32, b: i32): i32 { return a + b }", "JvmAdd");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_IfElse_ShouldCompile()
    {
        var result = CompileToJvm("micro max(a: i32, b: i32): i32 { if a > b { return a } else { return b } }", "JvmMax");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_VariableDecl_ShouldCompile()
    {
        var result = CompileToJvm("micro compute(): i32 { let x: i32 = 10 return x }", "JvmVar");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_MultipleFunctions_ShouldCompile()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro mul(a: i32, b: i32): i32 { return a * b }
";
        var result = CompileToJvm(source, "JvmMulti");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_Subtraction_ShouldCompile()
    {
        var result = CompileToJvm("micro sub(a: i32, b: i32): i32 { return a - b }", "JvmSub");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.Equal(0xCA, file.Content[0]);
            Assert.Equal(0xFE, file.Content[1]);
            Assert.Equal(0xBA, file.Content[2]);
            Assert.Equal(0xBE, file.Content[3]);
        }
    }

    [Fact]
    public void JvmTarget_Multiplication_ShouldCompile()
    {
        var result = CompileToJvm("micro mul(a: i32, b: i32): i32 { return a * b }", "JvmMul");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_Comparison_ShouldCompile()
    {
        var result = CompileToJvm("micro is_gt(a: i32, b: i32): i32 { if a > b { return 1 } else { return 0 } }", "JvmCmp");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_NestedIfElse_ShouldCompile()
    {
        var source = "micro classify(x: i32): i32 { if x > 0 { return 1 } else { if x < 0 { return -1 } else { return 0 } } }";
        var result = CompileToJvm(source, "JvmNested");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_ComplexExpression_ShouldCompile()
    {
        var source = "micro calc(a: i32, b: i32): i32 { return (a + b) * (a - b) }";
        var result = CompileToJvm(source, "JvmComplex");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void JvmTarget_ClassFile_Version_ShouldBeValid()
    {
        var result = CompileToJvm("micro main(): i32 { return 1 }", "JvmVer");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.True(file.Content.Length >= 8);

            var minorVersion = BitConverter.ToUInt16(file.Content, 6);
            var majorVersion = BitConverter.ToUInt16(file.Content, 4);

            Assert.True(majorVersion >= 45);
            Assert.True(majorVersion <= 65);
        }
    }

    #endregion

    #region Native PE 目标测试

    [Fact]
    public void NativePeTarget_Constant_ShouldCompile()
    {
        var result = CompileToNativePe("micro main(): i32 { return 42 }", "native_pe_const");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.NotEmpty(file.Content);
            Assert.True(file.Content.Length >= 2);

            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);
        }
    }

    [Fact]
    public void NativePeTarget_Arithmetic_ShouldCompile()
    {
        var result = CompileToNativePe("micro add(a: i32, b: i32): i32 { return a + b }", "native_pe_add");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);
        }
    }

    [Fact]
    public void NativePeTarget_IfElse_ShouldCompile()
    {
        var result = CompileToNativePe("micro max(a: i32, b: i32): i32 { if a > b { return a } else { return b } }", "native_pe_max");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void NativePeTarget_VariableDecl_ShouldCompile()
    {
        var result = CompileToNativePe("micro compute(): i32 { let x: i32 = 10 return x }", "native_pe_var");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void NativePeTarget_MultipleFunctions_ShouldCompile()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro main(): i32 { return add(10, 20) }
";
        var result = CompileToNativePe(source, "native_pe_multi");

        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void NativePeTarget_HasDosHeader()
    {
        var result = CompileToNativePe("micro main(): i32 { return 42 }", "native_pe_header");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.True(file.Content.Length >= 64);
            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);

            Assert.Equal(0x50, file.Content[60]);
            Assert.Equal(0x45, file.Content[61]);
        }
    }

    [Fact]
    public void NativePeTarget_PrintCall_ShouldCompile()
    {
        var source = @"
micro main(): i32 {
    print(""hello"")
    return 0
}
";
        var result = CompileToNativePe(source, "native_pe_print");

        Assert.NotEmpty(result.Files);
    }

    #endregion

    #region Native ELF 目标测试

    [Fact]
    public void NativeElfTarget_Constant_ShouldCompile()
    {
        var result = CompileToNativeElf("micro main(): i32 { return 42 }", "native_elf_const");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.NotEmpty(file.Content);
            Assert.True(file.Content.Length >= 4);

            Assert.Equal(0x7F, file.Content[0]);
            Assert.Equal(0x45, file.Content[1]);
            Assert.Equal(0x4C, file.Content[2]);
            Assert.Equal(0x46, file.Content[3]);
        }
    }

    [Fact]
    public void NativeElfTarget_Arithmetic_ShouldCompile()
    {
        var result = CompileToNativeElf("micro add(a: i32, b: i32): i32 { return a + b }", "native_elf_add");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.Equal(0x7F, file.Content[0]);
            Assert.Equal(0x45, file.Content[1]);
            Assert.Equal(0x4C, file.Content[2]);
            Assert.Equal(0x46, file.Content[3]);
        }
    }

    [Fact]
    public void NativeElfTarget_HasElfHeader()
    {
        var result = CompileToNativeElf("micro main(): i32 { return 42 }", "native_elf_header");

        Assert.NotEmpty(result.Files);

        foreach (var file in result.Files)
        {
            Assert.True(file.Content.Length >= 64);

            Assert.Equal(0x7F, file.Content[0]);
            Assert.Equal(0x45, file.Content[1]);
            Assert.Equal(0x4C, file.Content[2]);
            Assert.Equal(0x46, file.Content[3]);

            var elfClass = file.Content[4];
            Assert.True(elfClass == 1 || elfClass == 2);
        }
    }

    [Fact]
    public void NativeElfTarget_PrintCall_ShouldCompile()
    {
        var source = @"
micro main(): i32 {
    print(""hello"")
    return 0
}
";
        var result = CompileToNativeElf(source, "native_elf_print");

        Assert.NotEmpty(result.Files);
    }

    #endregion

    #region 跨目标一致性测试

    [Fact]
    public void CrossTarget_SameSource_AllTargetsShouldCompile()
    {
        var source = "micro add(a: i32, b: i32): i32 { return a + b }";
        var unit = CompileToUnit(source, "cross_add");

        var nyarModuleData = NyarVmBackend.ConvertModule(unit);
        var nyarBytes = new Acorn.Nyar.Encode.NyarEncoder().Encode(nyarModuleData);
        Assert.NotEmpty(nyarBytes);

        var wasmModuleData = WasmBackend.ConvertModule(unit);
        var wasmBytes = Acorn.Wasm.Encode.WasmEncoder.EncodeModule(wasmModuleData);
        Assert.NotEmpty(wasmBytes);

        var clrResult = CompileToClr("micro add(a: i32, b: i32): i32 { return a + b }", "cross_add");
        Assert.NotEmpty(clrResult.Files);

        var jvmResult = CompileToJvm("micro add(a: i32, b: i32): i32 { return a + b }", "CrossAdd");
        Assert.NotEmpty(jvmResult.Files);

        var nativePeResult = CompileToNativePe("micro add(a: i32, b: i32): i32 { return a + b }", "cross_add");
        Assert.NotEmpty(nativePeResult.Files);
    }

    [Fact]
    public void CrossTarget_ComplexExpression_AllTargetsShouldCompile()
    {
        var source = "micro calc(a: i32, b: i32, c: i32): i32 { if a > b { return a + c } else { return b - c } }";
        var vm = CompileAndLoad(source, "cross_complex");

        var r1 = vm.Run("cross_complex", "calc", Value.FromInt(10), Value.FromInt(5), Value.FromInt(3));
        Assert.Equal(13, r1.Int);

        var r2 = vm.Run("cross_complex", "calc", Value.FromInt(3), Value.FromInt(10), Value.FromInt(2));
        Assert.Equal(8, r2.Int);

        var wasmBytes = CompileToWasm(source, "cross_complex_w");
        Assert.NotEmpty(wasmBytes);

        var clrResult = CompileToClr(source, "cross_complex_c");
        Assert.NotEmpty(clrResult.Files);

        var jvmResult = CompileToJvm(source, "CrossComplex");
        Assert.NotEmpty(jvmResult.Files);

        var nativePeResult = CompileToNativePe(source, "cross_complex_pe");
        Assert.NotEmpty(nativePeResult.Files);
    }

    [Fact]
    public void CrossTarget_AllBinaryFormats_HaveCorrectMagic()
    {
        var source = "micro main(): i32 { return 42 }";
        var unit = CompileToUnit(source, "magic_test");

        var nyarModuleData = NyarVmBackend.ConvertModule(unit);
        var nyarBytes = new Acorn.Nyar.Encode.NyarEncoder().Encode(nyarModuleData);

        Assert.True(nyarBytes.Length >= 4);
        Assert.Equal(0x4E, nyarBytes[0]);
        Assert.Equal(0x59, nyarBytes[1]);
        Assert.Equal(0x41, nyarBytes[2]);
        Assert.Equal(0x52, nyarBytes[3]);

        var wasmModuleData = WasmBackend.ConvertModule(unit);
        var wasmBytes = Acorn.Wasm.Encode.WasmEncoder.EncodeModule(wasmModuleData);

        Assert.True(wasmBytes.Length >= 4);
        Assert.Equal(0x00, wasmBytes[0]);
        Assert.Equal(0x61, wasmBytes[1]);
        Assert.Equal(0x73, wasmBytes[2]);
        Assert.Equal(0x6D, wasmBytes[3]);

        var clrResult = CompileToClr(source, "magic_test_c");
        foreach (var file in clrResult.Files)
        {
            Assert.Equal(0x4D, file.Content[0]);
            Assert.Equal(0x5A, file.Content[1]);
        }

        var jvmResult = CompileToJvm(source, "MagicTest");
        foreach (var file in jvmResult.Files)
        {
            Assert.Equal(0xCA, file.Content[0]);
            Assert.Equal(0xFE, file.Content[1]);
            Assert.Equal(0xBA, file.Content[2]);
            Assert.Equal(0xBE, file.Content[3]);
        }
    }

    [Fact]
    public void CrossTarget_NyarVMExecution_MatchesExpectedResults()
    {
        var source = @"
micro add(a: i32, b: i32): i32 { return a + b }
micro mul(a: i32, b: i32): i32 { return a * b }
micro compute(x: i32, y: i32): i32 { return add(mul(x, x), mul(y, y)) }
";
        var vm = CompileAndLoad(source, "cross_exec");

        var result = vm.Run("cross_exec", "compute", Value.FromInt(3), Value.FromInt(4));
        Assert.Equal(25, result.Int);

        var result2 = vm.Run("cross_exec", "compute", Value.FromInt(5), Value.FromInt(12));
        Assert.Equal(169, result2.Int);
    }

    [Fact]
    public void CrossTarget_NestedControlFlow_AllTargetsShouldCompile()
    {
        var source = @"
micro classify(a: i32, b: i32): i32 {
    if a > b {
        if a > 100 { return 3 }
        else { return 2 }
    } else {
        if b > 100 { return 1 }
        else { return 0 }
    }
}
";
        var vm = CompileAndLoad(source, "cross_ctrl");

        Assert.Equal(3, vm.Run("cross_ctrl", "classify", Value.FromInt(200), Value.FromInt(5)).Int);
        Assert.Equal(2, vm.Run("cross_ctrl", "classify", Value.FromInt(50), Value.FromInt(5)).Int);
        Assert.Equal(1, vm.Run("cross_ctrl", "classify", Value.FromInt(5), Value.FromInt(200)).Int);
        Assert.Equal(0, vm.Run("cross_ctrl", "classify", Value.FromInt(5), Value.FromInt(50)).Int);

        var wasmBytes = CompileToWasm(source, "cross_ctrl_w");
        Assert.NotEmpty(wasmBytes);

        var clrResult = CompileToClr(source, "cross_ctrl_c");
        Assert.NotEmpty(clrResult.Files);

        var jvmResult = CompileToJvm(source, "CrossCtrl");
        Assert.NotEmpty(jvmResult.Files);
    }

    #endregion

    #region ICodeGenBackend 接口测试

    [Fact]
    public void Backend_NyarVm_ImplementsICodeGenBackend()
    {
        ICodeGenBackend backend = new NyarVmBackend();
        Assert.Equal("NyarVM", backend.Name);
        Assert.Contains(Arch.NyarVm, backend.SupportedArchs);
    }

    [Fact]
    public void Backend_Wasm_ImplementsICodeGenBackend()
    {
        ICodeGenBackend backend = new WasmBackend();
        Assert.Equal("WASM", backend.Name);
        Assert.Contains(Arch.Wasm32, backend.SupportedArchs);
    }

    [Fact]
    public void Backend_Clr_ImplementsICodeGenBackend()
    {
        ICodeGenBackend backend = new ClrBackend();
        Assert.Equal("CLR", backend.Name);
        Assert.Contains(Arch.Clr, backend.SupportedArchs);
    }

    [Fact]
    public void Backend_Jvm_ImplementsICodeGenBackend()
    {
        ICodeGenBackend backend = new JvmBackend();
        Assert.Equal("JVM", backend.Name);
        Assert.Contains(Arch.Jvm, backend.SupportedArchs);
    }

    [Fact]
    public void Backend_AllBackends_CanCompileSameSource()
    {
        var source = "micro main(): i32 { return 42 }";
        var unit = CompileToUnit(source, "backend_test");

        ICodeGenBackend[] backends = [new NyarVmBackend(), new WasmBackend(), new ClrBackend(), new JvmBackend(), new NativeBackend()];

        foreach (var backend in backends)
        {
            var options = new CompilationOptions
            {
                Target = new CompilationTarget { Arch = backend.SupportedArchs[0] },
                OptimizationLevel = OptimizationLevel.Basic
            };

            var result = backend.Compile(unit, options);
            Assert.NotEmpty(result.Files);
        }
    }

    #endregion
}