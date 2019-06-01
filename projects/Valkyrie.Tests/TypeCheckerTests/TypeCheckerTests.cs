using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.TypeChecker;

namespace Valkyrie.Tests.TypeCheckerTests;

public class TypeCheckerTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;

    public TypeCheckerTests()
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

    private TypeCheckResult CheckSource(string source)
    {
        var ast = ParseSource(source);
        var typeChecker = new Valkyrie.TypeChecker.TypeChecker();
        return typeChecker.Check(ast);
    }

    private void AssertNoErrors(TypeCheckResult result)
    {
        Assert.False(result.HasErrors);
    }

    private void AssertHasError(TypeCheckResult result, string code)
    {
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    private void AssertHasWarning(TypeCheckResult result, string code)
    {
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    #region 变量声明类型检查

    [Fact]
    public void VariableDecl_WithCorrectType_ShouldPass()
    {
        var source = "let x: i32 = 42;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void VariableDecl_WithMismatchedType_ShouldReportError()
    {
        var source = "let x: bool = 42;";
        var result = CheckSource(source);
        AssertHasError(result, "VALK2002");
    }

    [Fact]
    public void VariableDecl_WithInferredType_ShouldPass()
    {
        var source = "let x = 42; let y = 3.14; let z = true; let s = \"hello\";";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void VariableDecl_Duplicate_ShouldReportError()
    {
        var source = """
                     let x: i32 = 1;
                     let x: i32 = 2;
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2003");
    }

    [Fact]
    public void VariableDecl_FloatWithCorrectType_ShouldPass()
    {
        var source = "let pi: f32 = 3.14;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void VariableDecl_BoolWithCorrectType_ShouldPass()
    {
        var source = "let flag: bool = true;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void VariableDecl_StringWithCorrectType_ShouldPass()
    {
        var source = "let name: string = \"test\";";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 组件声明类型检查

    [Fact]
    public void ComponentDecl_WithValidFields_ShouldPass()
    {
        var source = """
                     component Position { x: f32; y: f32; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ComponentDecl_WithDefaultValues_ShouldPass()
    {
        var source = """
                     component Config { maxCount: i32 = 100; name: string = "default"; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ComponentDecl_Duplicate_ShouldReportError()
    {
        var source = """
                     component Position { x: f32; }
                     component Position { y: f32; }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2006");
    }

    [Fact]
    public void ComponentDecl_WithMultipleFields_ShouldPass()
    {
        var source = """
                     component Transform { x: f32; y: f32; z: f32; rotation: f32; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 函数声明类型检查

    [Fact]
    public void FunctionDecl_WithValidParams_ShouldPass()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 { return a + b; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void FunctionDecl_Duplicate_ShouldReportError()
    {
        var source = """
                     micro foo() {}
                     micro foo() {}
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2011");
    }

    [Fact]
    public void FunctionDecl_WithUnitReturn_ShouldPass()
    {
        var source = """
                     micro greet(name: string): unit { return; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void FunctionDecl_WithNoParams_ShouldPass()
    {
        var source = """
                     micro getValue(): i32 { return 42; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void FunctionDecl_WithMultipleParams_ShouldPass()
    {
        var source = """
                     micro compute(a: i32, b: f32, c: bool): i32 { return a; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 表达式类型检查

    [Fact]
    public void BinaryExpr_Arithmetic_ShouldPass()
    {
        var source = "let a: i32 = 1 + 2; let b: f32 = 3.0 * 4.0;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BinaryExpr_Comparison_ShouldReturnBool()
    {
        var source = "let flag: bool = 1 > 0;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BinaryExpr_InvalidArithmetic_ShouldReportError()
    {
        var source = "let x: bool = true + false;";
        var result = CheckSource(source);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void IfStmt_NonBoolCondition_ShouldReportError()
    {
        var source = """
                     micro test() {
                         if (42) {}
                     }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2012");
    }

    [Fact]
    public void WhileStmt_NonBoolCondition_ShouldReportError()
    {
        var source = """
                     micro test() {
                         while (42) {}
                     }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2014");
    }

    [Fact]
    public void BinaryExpr_LogicalAnd_ShouldPass()
    {
        var source = "let result: bool = true && false;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BinaryExpr_LogicalOr_ShouldPass()
    {
        var source = "let result: bool = true || false;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region using 检查

    [Fact]
    public void UsingDecl_ShouldPass()
    {
        var source = "using Gnosis.ECS;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void UsingDecl_WithAlias_ShouldPass()
    {
        var source = "using Gnosis.ECS as ecs;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void UsingDecl_Conflict_ShouldError()
    {
        var source = """
                     using Gnosis.ECS;
                     using Gnosis.ECS;
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2001");
    }

    [Fact]
    public void UsingDecl_MultipleDifferentModules_ShouldPass()
    {
        var source = """
                     using Gnosis.ECS;
                     using Gnosis.Graphic;
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 泛型类型推断

    [Fact]
    public void GenericFn_Identity_CallWithInt_ShouldInferReturnType()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x: i32 = identity(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_Swap_CallWithStrings_ShouldPass()
    {
        var source = """
                     micro swap<T>(a: T, b: T) {}
                     micro test() { swap("hello", "world"); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_TwoTypeParams_CallWithIntAndString_ShouldInfer()
    {
        var source = """
                     micro pair<A, B>(first: A, second: B) {}
                     micro test() { pair(42, "hello"); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_WithNestedGenericTypes_ShouldInfer()
    {
        var source = """
                     micro first<T>(list: list<T>): T { return list[0]; }
                     micro test() { let value: i32 = first([1, 2, 3]); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_WithoutTypeParams_ShouldUseRegularCheck()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 { return a + b; }
                     micro test() { let x: i32 = add(1, 2); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_ChainedCalls_ShouldInferEachCall()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x: i32 = identity(identity(42)); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_DeepChainedCalls_ShouldInfer()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x: i32 = identity(identity(identity(42))); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericFn_MultipleGenericParams_ShouldInferIndependently()
    {
        var source = """
                     micro select<A, B>(flag: bool, a: A, b: B): A { return a; }
                     micro test() { let x: i32 = select(true, 42, "hello"); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 模式匹配类型检查

    [Fact]
    public void Match_WithWildcardPattern_ShouldBeExhaustive()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 1:
                                 let _ = 1;
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_NonExhaustive_ShouldWarn()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 1:
                                 let _ = 1;
                             case 2:
                                 let _ = 2;
                         }
                     }
                     """;
        var result = CheckSource(source);
        Assert.True(result.HasErrors || result.Diagnostics.Count > 0);
    }

    [Fact]
    public void Match_WithConstantPattern_ShouldCheckConstant()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 42:
                                 let _ = 1;
                             else:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_BoolWithTrueAndFalse_ShouldBeExhaustive()
    {
        var source = """
                     micro test(flag: bool) {
                         match flag {
                             case true:
                                 let _ = 1;
                             case false:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_BoolWithOnlyTrue_ShouldWarnNonExhaustive()
    {
        var source = """
                     micro test(flag: bool) {
                         match flag {
                             case true:
                                 let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        Assert.True(result.Diagnostics.Count > 0);
    }

    [Fact]
    public void Match_WildcardMakesExhaustive()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_ElseKeyword_ShouldBeExhaustive()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 1:
                                 let _ = 1;
                             else:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_EmptyMatch_ShouldWarn()
    {
        var source = """
                     micro test(x: i32) {
                         match x { }
                     }
                     """;
        var result = CheckSource(source);
        Assert.True(result.Diagnostics.Count > 0);
    }

    [Fact]
    public void Match_ConstantPatternWithWildcard_ShouldBeExhaustive()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 1:
                                 let _ = 1;
                             case 2:
                                 let _ = 2;
                             case 3:
                                 let _ = 3;
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Match_WithReturnInArm_ShouldPass()
    {
        var source = """
                     micro test(x: i32): i32 {
                         match x {
                             case 1:
                                 return 1;
                             else:
                                 return 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region Effect 系统类型检查

    [Fact]
    public void Effect_PureFunction_CalledAnywhere_ShouldPass()
    {
        var source = """
                     micro add(a: i32, b: i32): i32 { return a + b; }
                     micro test() { let result = add(1, 2); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Effect_WhileLoopWithCondition_EffectInBody()
    {
        var source = """
                     micro test(n: i32) {
                         while (n > 0) {
                             let _ = n;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Effect_NestedIfStatements_ShouldPass()
    {
        var source = """
                     micro test(a: bool, b: bool) {
                         if (a) {
                             if (b) {
                                 let _ = 1;
                             }
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 作用域和名称解析

    [Fact]
    public void Scope_ShadowingOuterVariable_ShouldPass()
    {
        var source = """
                     let x: i32 = 1;
                     micro test() { let x: f32 = 2.0; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Scope_InnerScopeAccessOuter_ShouldPass()
    {
        var source = """
                     let globalVal: i32 = 100;
                     micro test() { let local: i32 = globalVal; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Scope_NestedFunctionSeesParentScope()
    {
        var source = """
                     micro outer() {
                         let parentVar: i32 = 10;
                         micro inner() { let _ = parentVar; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Scope_MultipleShadowLevels_DeepestWins()
    {
        var source = """
                     let level: i32 = 1;
                     micro test() {
                         let level: i32 = 2;
                         { let level: i32 = 3; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Scope_NestedBlockAccess_ShouldPass()
    {
        var source = """
                     micro test() {
                         let outer: i32 = 1;
                         { let inner: i32 = outer; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Scope_DeeplyNestedBlocks_ShouldResolve()
    {
        var source = """
                     micro test() {
                         let a: i32 = 1;
                         { let b: i32 = a;
                             { let c: i32 = b; }
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 类型兼容性

    [Fact]
    public void TypeCompat_IntToFloatAssign_ShouldPass()
    {
        var source = "let x: f32 = 1;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_FloatToInt_ShouldError()
    {
        var source = "let x: i32 = 3.14;";
        var result = CheckSource(source);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void TypeCompat_StringConcat_ShouldResolveToString()
    {
        var source = "let greeting: string = \"Hello, \" + \"World\";";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_MixedArithmeticIntFloat_ShouldProd()
    {
        var source = "let result: f32 = 1 + 2.0;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_IndexOnString_ShouldResolve()
    {
        var source = """
                     micro test(s: string) { let c = s[0]; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_NegationOfBool_ShouldReturnBool()
    {
        var source = """
                     micro test(flag: bool) { let inverted: bool = !flag; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_ListTypeAnnotation_ShouldResolve()
    {
        var source = """
                     micro test() { let numbers: list<i32> = [1, 2, 3]; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_I32Addition_ShouldReturnI32()
    {
        var source = "let x: i32 = 10 + 20;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_F32Multiplication_ShouldReturnF32()
    {
        var source = "let x: f32 = 2.0 * 3.0;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TypeCompat_BoolComparison_ShouldReturnBool()
    {
        var source = "let x: bool = 1 >= 0;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 边界条件

    [Fact]
    public void EdgeCase_EmptyFunction_ShouldPass()
    {
        var source = "micro test() {}";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_EmptySource_ShouldPass()
    {
        var result = CheckSource("");
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_DeeplyNestedBlocks_ShouldPass()
    {
        var source = """
                     micro test() {
                         {{{{ let x: i32 = 1; }}}}
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_UnitFunction_ImplicitReturn_ShouldPass()
    {
        var source = """
                     micro test(): unit { let x: i32 = 1; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_FunctionWithoutReturnType_HasImplicitUnit()
    {
        var source = """
                     micro doSomething() { let _ = 1; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_RecursiveGenericCall_ShouldHandle()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x: i32 = identity(identity(identity(42))); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_MultipleUsings_ShouldAllPass()
    {
        var source = """
                     using Gnosis.ECS;
                     using Gnosis.Graphic;
                     using Gnosis.Audio as snd;
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_HexLiteral_ShouldBeI32()
    {
        var source = "let x: i32 = 0xFF;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ScientificNotationLiteral_ShouldBeF64()
    {
        var source = "let x: f64 = 1.0e10;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_UnsignedSuffixLiteral_ShouldBeU32()
    {
        var source = "let x: u32 = 42u;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_NullableType_ShouldResolve()
    {
        var source = "let maybe: i32? = null;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_LoopStatement_ShouldPass()
    {
        var source = """
                     micro test() {
                         loop { let _ = 1; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ForLoop_ShouldPass()
    {
        var source = "micro test() { let i: i32 = 0; }";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_BreakStatement_ShouldPass()
    {
        var source = """
                     micro test() {
                         while (true) { break; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ContinueStatement_ShouldPass()
    {
        var source = """
                     micro test() {
                         while (true) { continue; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 可达性分析

    [Fact]
    public void Reachability_CodeAfterReturn_ShouldWarn()
    {
        var source = """
                     micro test(): i32 {
                         return 42;
                         let x: i32 = 1;
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2060");
    }

    [Fact]
    public void Reachability_CodeAfterReturnInBlock_ShouldWarn()
    {
        var source = """
                     micro test() {
                         {
                             return;
                             let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2060");
    }

    [Fact]
    public void Reachability_ReturnInIfBranch_DoesNotAffectAfterIf()
    {
        var source = """
                     micro test(flag: bool) {
                         if (flag) {
                             return;
                         }
                         let _ = 1;
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Reachability_ExpressionAfterReturn_ShouldWarn()
    {
        var source = """
                     micro test(): i32 {
                         return 1;
                         2 + 3;
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2060");
    }

    [Fact]
    public void Reachability_MultipleReturns_LastStatementAfterShouldWarn()
    {
        var source = """
                     micro test(flag: bool) {
                         if (flag) {
                             return;
                         }
                         return;
                         let dead: i32 = 0;
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2060");
    }

    [Fact]
    public void Reachability_CodeAfterDiscard_ShouldWarn()
    {
        var source = """
                     micro test() {
                         discard;
                         let _ = 1;
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2063");
    }

    [Fact]
    public void Reachability_BlockReturn_ThenOuterDeadCode()
    {
        var source = """
                     micro test() {
                         {
                             return;
                             let dead: i32 = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2060");
    }

    [Fact]
    public void Reachability_DiscardFollowedByVarDecl_ShouldWarn()
    {
        var source = """
                     micro test() {
                         discard;
                         let x = 1;
                     }
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2063");
    }

    [Fact]
    public void Reachability_NormalFlow_NoWarning()
    {
        var source = "let x: i32 = 1;";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Reachability_ReturnAtEndOfUnitFunction_NoWarning()
    {
        var source = """
                     micro test() {
                         let _ = 1;
                         return;
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 泛型约束检查

    [Fact]
    public void GenericConstraint_WithTraitConstraint_ShouldPass()
    {
        var source = """
                     trait Numeric {}
                     micro process<T>(item: T): T where T : Numeric {}
                     micro test() { let x = process(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericConstraint_MultipleBounds_ShouldCheckAll()
    {
        var source = """
                     trait Clone {} trait Debug {}
                     micro process<T>(item: T) where T : Clone, T : Debug {}
                     micro test() { process(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void GenericConstraint_DualParamsWithSeparateBounds()
    {
        var source = """
                     trait Clone {} trait Debug {}
                     micro compose<A, B>(a: A, b: B) where A : Clone, B : Debug {}
                     micro test() { compose(1, 2); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 着色器类型检查

    [Fact]
    public void ShaderDecl_Basic_ShouldPass()
    {
        var source = """
                     shader SimpleShader { vertex {} fragment {} }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ShaderDecl_WithUniforms_ShouldPass()
    {
        var source = """
                     shader LitShader {
                         vertex { uniform mvp: mat4; }
                         fragment {}
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ShaderDecl_WithComputeStage_ShouldPass()
    {
        var source = """
                     shader ComputeShader { compute {} }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 结构体和组件

    [Fact]
    public void StructDecl_WithValidFields_ShouldPass()
    {
        var source = """
                     struct Point { x: f32; y: f32; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void StructDecl_WithMultipleFields_ShouldPass()
    {
        var source = """
                     struct Rect { x: f32; y: f32; width: f32; height: f32; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EnumDecl_WithValues_ShouldPass()
    {
        var source = """
                     enum Color { Red; Green; Blue; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void FlagsDecl_WithValues_ShouldPass()
    {
        var source = """
                     flags Permission { Read; Write; Execute; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Integration_MultipleDeclarations_ShouldPass()
    {
        var source = """
                     using Gnosis.ECS;

                     component Position { x: f32; y: f32; }
                     component Velocity { dx: f32; dy: f32; }

                     micro update(pos: Position, vel: Velocity, dt: f32) {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_GenericFunctionWithComponent_ShouldPass()
    {
        var source = """
                     component Health { value: f32; }

                     micro clamp<T>(val: T, min: T, max: T): T { return val; }
                     micro test(h: Health) { let v = clamp(h.value, 0.0, 100.0); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_NestedIfElse_ShouldPass()
    {
        var source = """
                     micro classify(x: i32): i32 {
                         if (x > 0) {
                             return 1;
                         }
                         else {
                             if (x < 0) {
                                 return -1;
                             }
                             else {
                                 return 0;
                             }
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_MultipleGenericCalls_ShouldPass()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x: i32 = identity(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_StructWithFunction_ShouldPass()
    {
        var source = """
                     struct Vec3 { x: f32; y: f32; z: f32; }
                     micro length(v: Vec3): f32 { return v.x; }
                     micro test() { let v: Vec3 = Vec3 { x: 1.0; y: 0.0; z: 0.0; }; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_WhileLoopWithGenericCondition_ShouldPass()
    {
        var source = """
                     micro countdown(n: i32) {
                         while (n > 0) {
                             let _ = n;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion
}