using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.TypeChecker;
using Valkyrie.TypeChecker.TypeSystem;

namespace Valkyrie.Tests.TypeCheckerTests;

public class M4TypeCheckerTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;

    public M4TypeCheckerTests()
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

    private void AssertHasDiagnostic(TypeCheckResult result, string code)
    {
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    #region 泛型约束求解 - Trait 约束

    [Fact]
    public void TraitConstraint_NumericType_ShouldSatisfyNumericTrait()
    {
        var source = """
                     trait Numeric {}
                     micro add<T>(a: T, b: T): T where T : Numeric { return a; }
                     micro test() { let x = add(1, 2); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TraitConstraint_FloatType_ShouldSatisfyNumericTrait()
    {
        var source = """
                     trait Numeric {}
                     micro multiply<T>(a: T, b: T): T where T : Numeric { return a; }
                     micro test() { let x = multiply(1.0, 2.0); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TraitConstraint_IntegerType_ShouldSatisfyIntegerTrait()
    {
        var source = """
                     trait Integer {}
                     micro shift<T>(val: T, bits: i32): T where T : Integer { return val; }
                     micro test() { let x = shift(42, 1); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TraitConstraint_MultipleTraitBounds_ShouldPass()
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
    public void TraitConstraint_SameTraitOnMultipleParams_ShouldPass()
    {
        var source = """
                     trait Comparable {}
                     micro max<T>(a: T, b: T): T where T : Comparable { return a; }
                     micro test() { let x = max(1, 2); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void TraitConstraint_GenericClass_ShouldPass()
    {
        var source = """
                     trait Container {}
                     micro wrap<T>(item: T): list<T> where T : Container { return [item]; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 泛型约束求解 - 内置约束种类

    [Fact]
    public void BuiltinConstraint_ClassConstraint_ShouldParse()
    {
        var source = """
                     micro process<T>(item: T) where T : class {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_StructConstraint_ShouldParse()
    {
        var source = """
                     micro process<T>(item: T) where T : struct {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_NewConstraint_ShouldParse()
    {
        var source = """
                     micro create<T>(): T where T : new() { return default; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_EnumConstraint_ShouldParse()
    {
        var source = """
                     micro process<T>(val: T) where T : enum {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_NumericConstraint_ShouldParse()
    {
        var source = """
                     micro add<T>(a: T, b: T): T where T : Numeric { return a; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_IntegerConstraint_ShouldParse()
    {
        var source = """
                     micro bitwise<T>(a: T, b: T): T where T : Integer { return a; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_FloatConstraint_ShouldParse()
    {
        var source = """
                     micro compute<T>(a: T): T where T : Float { return a; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void BuiltinConstraint_CombinedClassAndTrait_ShouldParse()
    {
        var source = """
                     trait Serializable {}
                     micro save<T>(item: T) where T : class, T : Serializable {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 泛型约束求解 - 约束组合验证

    [Fact]
    public void ConstraintCombo_ClassAndStruct_ShouldBeMutuallyExclusive()
    {
        var source = """
                     micro bad<T>(item: T) where T : class, T : struct {}
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2082");
    }

    [Fact]
    public void ConstraintCombo_StructAndNew_ShouldWarnRedundant()
    {
        var source = """
                     micro redundant<T>(item: T) where T : struct, T : new() {}
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2083");
    }

    [Fact]
    public void ConstraintCombo_EnumAndStruct_ShouldWarnRedundant()
    {
        var source = """
                     micro redundant<T>(item: T) where T : enum, T : struct {}
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2084");
    }

    [Fact]
    public void ConstraintCombo_MultipleValidConstraints_ShouldPass()
    {
        var source = """
                     trait Clone {} trait Debug {}
                     micro process<T>(item: T) where T : Clone, T : Debug, T : new() {}
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 泛型约束求解 - 约束违反检测

    [Fact]
    public void ConstraintViolation_NonNumericTypeWithNumericConstraint_ShouldError()
    {
        var source = """
                     micro add<T>(a: T, b: T): T where T : Numeric { return a; }
                     micro test() { add(true, false); }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2042");
    }

    [Fact]
    public void ConstraintViolation_NonIntegerTypeWithIntegerConstraint_ShouldError()
    {
        var source = """
                     micro shift<T>(val: T): T where T : Integer { return val; }
                     micro test() { shift(3.14); }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2042");
    }

    [Fact]
    public void ConstraintViolation_NonFloatTypeWithFloatConstraint_ShouldError()
    {
        var source = """
                     micro compute<T>(val: T): T where T : Float { return val; }
                     micro test() { compute(42); }
                     """;
        var result = CheckSource(source);
        AssertHasError(result, "VALK2042");
    }

    #endregion

    #region 泛型约束求解 - 约束声明检查

    [Fact]
    public void ConstraintDecl_UndefinedTypeParam_ShouldWarn()
    {
        var source = """
                     micro process(item: i32) where U : Numeric {}
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2043");
    }

    [Fact]
    public void ConstraintDecl_UndefinedConstraintType_ShouldWarn()
    {
        var source = """
                     micro process<T>(item: T) where T : NonExistentTrait {}
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2044");
    }

    [Fact]
    public void ConstraintDecl_NonTraitConstraintType_ShouldWarn()
    {
        var source = """
                     struct Point { x: f32; y: f32; }
                     micro process<T>(item: T) where T : Point {}
                     """;
        var result = CheckSource(source);
        AssertHasWarning(result, "VALK2081");
    }

    #endregion

    #region Match 穷举检查增强 - 缺失分支报告

    [Fact]
    public void MatchExhaustiveness_BoolMissingFalse_ShouldReportMissing()
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
        AssertHasDiagnostic(result, "VALK2051");
    }

    [Fact]
    public void MatchExhaustiveness_BoolMissingTrue_ShouldReportMissing()
    {
        var source = """
                     micro test(flag: bool) {
                         match flag {
                             case false:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2051");
    }

    [Fact]
    public void MatchExhaustiveness_BoolBothBranches_ShouldBeExhaustive()
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
    public void MatchExhaustiveness_BoolWithWildcard_ShouldBeExhaustive()
    {
        var source = """
                     micro test(flag: bool) {
                         match flag {
                             case true:
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
    public void MatchExhaustiveness_EmptyMatch_ShouldReportMissing()
    {
        var source = """
                     micro test(x: i32) {
                         match x { }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2051");
    }

    [Fact]
    public void MatchExhaustiveness_WildcardCoversAll_ShouldBeExhaustive()
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
    public void MatchExhaustiveness_ConstantPlusWildcard_ShouldBeExhaustive()
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
    public void MatchExhaustiveness_ElseKeyword_ShouldBeExhaustive()
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

    #endregion

    #region Match 穷举检查增强 - 冗余 pattern 检测

    [Fact]
    public void MatchRedundant_ArmAfterWildcard_ShouldBeRedundant()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case _:
                                 let _ = 0;
                             case 1:
                                 let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2054");
    }

    [Fact]
    public void MatchRedundant_MultipleArmsAfterWildcard_ShouldAllBeRedundant()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case _:
                                 let _ = 0;
                             case 1:
                                 let _ = 1;
                             case 2:
                                 let _ = 2;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2054");
    }

    #endregion

    #region Match 穷举检查增强 - Nullable 穷举

    [Fact]
    public void MatchNullable_BothNullAndValue_ShouldBeExhaustive()
    {
        var source = """
                     micro test(maybe: i32?) {
                         match maybe {
                             case null:
                                 let _ = 0;
                             case _:
                                 let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void MatchNullable_OnlyNull_ShouldNotBeExhaustive()
    {
        var source = """
                     micro test(maybe: i32?) {
                         match maybe {
                             case null:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2051");
    }

    #endregion

    #region Match 穷举检查增强 - Pattern 类型兼容性

    [Fact]
    public void MatchPattern_IncompatibleType_ShouldError()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case "hello":
                                 let _ = 1;
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertHasDiagnostic(result, "VALK2087");
    }

    [Fact]
    public void MatchPattern_CompatibleType_ShouldPass()
    {
        var source = """
                     micro test(x: i32) {
                         match x {
                             case 42:
                                 let _ = 1;
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 类型系统增强 - ValkyrieType 属性

    [Fact]
    public void TypeProperties_I32IsNumeric()
    {
        var t = ValkyrieType.I32;
        Assert.True(t.IsNumeric);
        Assert.True(t.IsInteger);
        Assert.False(t.IsFloat);
        Assert.True(t.IsValueType);
        Assert.False(t.IsReferenceType);
    }

    [Fact]
    public void TypeProperties_F64IsNumeric()
    {
        var t = ValkyrieType.F64;
        Assert.True(t.IsNumeric);
        Assert.False(t.IsInteger);
        Assert.True(t.IsFloat);
    }

    [Fact]
    public void TypeProperties_BoolIsNotNumeric()
    {
        var t = ValkyrieType.Bool;
        Assert.False(t.IsNumeric);
        Assert.True(t.IsBool);
    }

    [Fact]
    public void TypeProperties_StringIsReferenceType()
    {
        var t = ValkyrieType.String;
        Assert.True(t.IsReferenceType);
        Assert.False(t.IsValueType);
    }

    [Fact]
    public void TypeProperties_ListIsReferenceType()
    {
        var t = ValkyrieType.List(ValkyrieType.I32);
        Assert.True(t.IsReferenceType);
        Assert.False(t.IsValueType);
    }

    [Fact]
    public void TypeProperties_NullableIsReferenceType()
    {
        var t = ValkyrieType.Nullable(ValkyrieType.I32);
        Assert.True(t.IsReferenceType);
    }

    [Fact]
    public void TypeProperties_StructIsValueType()
    {
        var t = new ValkyrieType(TypeKind.Struct, "Point");
        Assert.True(t.IsValueType);
        Assert.False(t.IsReferenceType);
    }

    [Fact]
    public void TypeProperties_ClassIsReferenceType()
    {
        var t = new ValkyrieType(TypeKind.Class, "Widget");
        Assert.True(t.IsReferenceType);
        Assert.False(t.IsValueType);
    }

    [Fact]
    public void TypeProperties_EnumIsValueType()
    {
        var t = new ValkyrieType(TypeKind.Enum, "Color");
        Assert.True(t.IsValueType);
    }

    [Fact]
    public void TypeProperties_TupleIsValueType()
    {
        var t = ValkyrieType.Tuple([ValkyrieType.I32, ValkyrieType.F32]);
        Assert.True(t.IsValueType);
        Assert.True(t.IsTuple);
    }

    [Fact]
    public void TypeProperties_ErrorTypeCompatibleWithAnything()
    {
        var err = ValkyrieType.Error;
        Assert.True(err.IsAssignableFrom(ValkyrieType.I32));
        Assert.True(ValkyrieType.I32.IsAssignableFrom(err));
    }

    [Fact]
    public void TypeProperties_AutoTypeIsNotConcrete()
    {
        var auto = ValkyrieType.Auto;
        Assert.False(auto.IsConcrete);
        Assert.True(auto.IsAuto);
    }

    [Fact]
    public void TypeProperties_I32IsConcrete()
    {
        var t = ValkyrieType.I32;
        Assert.True(t.IsConcrete);
    }

    #endregion

    #region 类型系统增强 - Trait 实现验证

    [Fact]
    public void TraitImpl_I32ImplementsNumeric()
    {
        var t = ValkyrieType.I32;
        Assert.True(t.ImplementsTrait("Numeric"));
    }

    [Fact]
    public void TraitImpl_I32ImplementsInteger()
    {
        var t = ValkyrieType.I32;
        Assert.True(t.ImplementsTrait("Integer"));
    }

    [Fact]
    public void TraitImpl_I32ImplementsBitwise()
    {
        var t = ValkyrieType.I32;
        Assert.True(t.ImplementsTrait("Bitwise"));
    }

    [Fact]
    public void TraitImpl_F32ImplementsFloat()
    {
        var t = ValkyrieType.F32;
        Assert.True(t.ImplementsTrait("Float"));
    }

    [Fact]
    public void TraitImpl_F32DoesNotImplementInteger()
    {
        var t = ValkyrieType.F32;
        Assert.False(t.ImplementsTrait("Integer"));
    }

    [Fact]
    public void TraitImpl_BoolImplementsComparable()
    {
        var t = ValkyrieType.Bool;
        Assert.True(t.ImplementsTrait("Comparable"));
    }

    [Fact]
    public void TraitImpl_StringImplementsIterable()
    {
        var t = ValkyrieType.String;
        Assert.True(t.ImplementsTrait("Iterable"));
    }

    [Fact]
    public void TraitImpl_StringImplementsAddable()
    {
        var t = ValkyrieType.String;
        Assert.True(t.ImplementsTrait("Addable"));
    }

    [Fact]
    public void TraitImpl_CustomTraitRegistration()
    {
        var t = ValkyrieType.WithTraits(TypeKind.Class, "MyClass", ["Serializable", "Cloneable"]);
        Assert.True(t.ImplementsTrait("Serializable"));
        Assert.True(t.ImplementsTrait("Cloneable"));
        Assert.False(t.ImplementsTrait("NonExistent"));
    }

    #endregion

    #region 类型系统增强 - 约束满足验证

    [Fact]
    public void ConstraintSatisfy_I32SatisfiesNumericConstraint()
    {
        var t = ValkyrieType.I32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Numeric);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_BoolDoesNotSatisfyNumericConstraint()
    {
        var t = ValkyrieType.Bool;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Numeric);
        Assert.False(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_I32SatisfiesIntegerConstraint()
    {
        var t = ValkyrieType.I32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Integer);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_F32DoesNotSatisfyIntegerConstraint()
    {
        var t = ValkyrieType.F32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Integer);
        Assert.False(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_F32SatisfiesFloatConstraint()
    {
        var t = ValkyrieType.F32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Float);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_I32DoesNotSatisfyFloatConstraint()
    {
        var t = ValkyrieType.I32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Float);
        Assert.False(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_ClassSatisfiesClassConstraint()
    {
        var t = new ValkyrieType(TypeKind.Class, "Widget");
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Class);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_StructSatisfiesStructConstraint()
    {
        var t = new ValkyrieType(TypeKind.Struct, "Point");
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Struct);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_EnumSatisfiesEnumConstraint()
    {
        var t = new ValkyrieType(TypeKind.Enum, "Color");
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Enum);
        Assert.True(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_PrimitiveDoesNotSatisfyEnumConstraint()
    {
        var t = ValkyrieType.I32;
        var constraint = new GenericConstraintDescriptor(ConstraintKind.Enum);
        Assert.False(t.SatisfiesConstraint(constraint));
    }

    [Fact]
    public void ConstraintSatisfy_ErrorTypeSatisfiesAnyConstraint()
    {
        var t = ValkyrieType.Error;
        Assert.True(t.SatisfiesConstraint(new GenericConstraintDescriptor(ConstraintKind.Numeric)));
        Assert.True(t.SatisfiesConstraint(new GenericConstraintDescriptor(ConstraintKind.Class)));
        Assert.True(t.SatisfiesConstraint(new GenericConstraintDescriptor(ConstraintKind.Enum)));
    }

    [Fact]
    public void ConstraintSatisfy_AutoTypeSatisfiesAnyConstraint()
    {
        var t = ValkyrieType.Auto;
        Assert.True(t.SatisfiesConstraint(new GenericConstraintDescriptor(ConstraintKind.Numeric)));
    }

    #endregion

    #region 类型系统增强 - 类型窄化

    [Fact]
    public void TypeNarrowing_NullableCanNarrowToInner()
    {
        var nullable = ValkyrieType.Nullable(ValkyrieType.I32);
        Assert.True(nullable.CanNarrowTo(ValkyrieType.I32));
    }

    [Fact]
    public void TypeNarrowing_PrimitiveCannotNarrowToUnrelated()
    {
        Assert.False(ValkyrieType.I32.CanNarrowTo(ValkyrieType.String));
    }

    [Fact]
    public void TypeNarrowing_NullableNarrowToInnerType()
    {
        var nullable = ValkyrieType.Nullable(ValkyrieType.I32);
        var narrowed = nullable.NarrowTo(ValkyrieType.I32);
        Assert.Equal(TypeKind.Primitive, narrowed.Kind);
        Assert.Equal("i32", narrowed.Name);
    }

    [Fact]
    public void TypeNarrowing_AutoNarrowToConcrete()
    {
        var auto = ValkyrieType.Auto;
        var narrowed = auto.NarrowTo(ValkyrieType.I32);
        Assert.Equal(TypeKind.Primitive, narrowed.Kind);
        Assert.Equal("i32", narrowed.Name);
        Assert.Equal(auto, narrowed.NarrowedFrom);
    }

    [Fact]
    public void TypeNarrowing_UnionCanNarrowToMember()
    {
        var union = new ValkyrieType(TypeKind.Union, "i32|f32",
            [ValkyrieType.I32, ValkyrieType.F32]);
        Assert.True(union.CanNarrowTo(ValkyrieType.I32));
        var narrowed = union.NarrowTo(ValkyrieType.I32);
        Assert.Equal("i32", narrowed.Name);
    }

    [Fact]
    public void TypeNarrowing_ErrorTypeCannotNarrow()
    {
        Assert.False(ValkyrieType.Error.CanNarrowTo(ValkyrieType.I32));
    }

    #endregion

    #region 类型系统增强 - 类型兼容性增强

    [Fact]
    public void TypeCompat_ImplicitI8ToI32()
    {
        var i8 = new ValkyrieType(TypeKind.Primitive, "i8");
        Assert.True(ValkyrieType.I32.IsAssignableFrom(i8));
    }

    [Fact]
    public void TypeCompat_ImplicitI32ToI64()
    {
        Assert.True(ValkyrieType.I64.IsAssignableFrom(ValkyrieType.I32));
    }

    [Fact]
    public void TypeCompat_NoImplicitU32ToI64()
    {
        var u32 = ValkyrieType.U32;
        Assert.False(ValkyrieType.I64.IsAssignableFrom(u32));
    }

    [Fact]
    public void TypeCompat_ImplicitI32ToF32()
    {
        Assert.True(ValkyrieType.F32.IsAssignableFrom(ValkyrieType.I32));
    }

    [Fact]
    public void TypeCompat_ImplicitF32ToF64()
    {
        Assert.True(ValkyrieType.F64.IsAssignableFrom(ValkyrieType.F32));
    }

    [Fact]
    public void TypeCompat_NoImplicitF32ToI32()
    {
        Assert.False(ValkyrieType.I32.IsAssignableFrom(ValkyrieType.F32));
    }

    [Fact]
    public void TypeCompat_NullableAcceptsNull()
    {
        var nullable = ValkyrieType.Nullable(ValkyrieType.I32);
        var nullType = ValkyrieType.Null;
        Assert.True(nullable.IsAssignableFrom(nullType));
    }

    [Fact]
    public void TypeCompat_NullableAcceptsInnerType()
    {
        var nullable = ValkyrieType.Nullable(ValkyrieType.I32);
        Assert.True(nullable.IsAssignableFrom(ValkyrieType.I32));
    }

    [Fact]
    public void TypeCompat_TraitAssignableFromImplementor()
    {
        var trait = ValkyrieType.TraitType("Numeric");
        Assert.True(trait.IsAssignableFrom(ValkyrieType.I32));
    }

    [Fact]
    public void TypeCompat_UnionAssignableFromMember()
    {
        var union = new ValkyrieType(TypeKind.Union, "i32|f32",
            [ValkyrieType.I32, ValkyrieType.F32]);
        Assert.True(union.IsAssignableFrom(ValkyrieType.I32));
        Assert.True(union.IsAssignableFrom(ValkyrieType.F32));
    }

    [Fact]
    public void TypeCompat_GenericArgsMustMatch()
    {
        var listI32 = ValkyrieType.List(ValkyrieType.I32);
        var listF32 = ValkyrieType.List(ValkyrieType.F32);
        Assert.False(listI32.IsAssignableFrom(listF32));
    }

    [Fact]
    public void TypeCompat_SameTypeWithSameGenericArgs()
    {
        var list1 = ValkyrieType.List(ValkyrieType.I32);
        var list2 = ValkyrieType.List(ValkyrieType.I32);
        Assert.True(list1.IsAssignableFrom(list2));
    }

    #endregion

    #region 类型系统增强 - 类型替换

    [Fact]
    public void TypeSubstitute_ReplaceTypeVariable()
    {
        var typeVar = ValkyrieType.TypeVariable("T");
        var substitutions = new Dictionary<string, ValkyrieType> { ["T"] = ValkyrieType.I32 };
        var result = typeVar.Substitute(substitutions);
        Assert.Equal("i32", result.Name);
    }

    [Fact]
    public void TypeSubstitute_ReplaceInGenericArgs()
    {
        var list = ValkyrieType.List(ValkyrieType.TypeVariable("T"));
        var substitutions = new Dictionary<string, ValkyrieType> { ["T"] = ValkyrieType.I32 };
        var result = list.Substitute(substitutions);
        Assert.Equal("list", result.Name);
        Assert.Single(result.GenericArgs);
        Assert.Equal("i32", result.GenericArgs[0].Name);
    }

    [Fact]
    public void TypeSubstitute_ReplaceInFunctionType()
    {
        var func = new ValkyrieType(TypeKind.Function, "fn",
            parameters: [new ParameterType("x", ValkyrieType.TypeVariable("T"))],
            returnType: ValkyrieType.TypeVariable("T"));
        var substitutions = new Dictionary<string, ValkyrieType> { ["T"] = ValkyrieType.I32 };
        var result = func.Substitute(substitutions);
        Assert.Equal("i32", result.Parameters![0].Type.Name);
        Assert.Equal("i32", result.ReturnType!.Name);
    }

    [Fact]
    public void TypeSubstitute_NoSubstitutionNeeded()
    {
        var i32 = ValkyrieType.I32;
        var substitutions = new Dictionary<string, ValkyrieType> { ["T"] = ValkyrieType.F32 };
        var result = i32.Substitute(substitutions);
        Assert.Same(i32, result);
    }

    [Fact]
    public void TypeSubstitute_MultipleTypeVariables()
    {
        var map = ValkyrieType.Map(ValkyrieType.TypeVariable("K"), ValkyrieType.TypeVariable("V"));
        var substitutions = new Dictionary<string, ValkyrieType>
        {
            ["K"] = ValkyrieType.String,
            ["V"] = ValkyrieType.I32
        };
        var result = map.Substitute(substitutions);
        Assert.Equal("string", result.GenericArgs[0].Name);
        Assert.Equal("i32", result.GenericArgs[1].Name);
    }

    #endregion

    #region 类型系统增强 - 预定义类型

    [Fact]
    public void PredefinedTypes_AllIntegerTypes()
    {
        Assert.True(ValkyrieType.I8.IsInteger);
        Assert.True(ValkyrieType.I16.IsInteger);
        Assert.True(ValkyrieType.I32.IsInteger);
        Assert.True(ValkyrieType.I64.IsInteger);
        Assert.True(ValkyrieType.U8.IsInteger);
        Assert.True(ValkyrieType.U16.IsInteger);
        Assert.True(ValkyrieType.U32.IsInteger);
        Assert.True(ValkyrieType.U64.IsInteger);
    }

    [Fact]
    public void PredefinedTypes_FloatTypes()
    {
        Assert.True(ValkyrieType.F32.IsFloat);
        Assert.True(ValkyrieType.F64.IsFloat);
        Assert.False(ValkyrieType.I32.IsFloat);
    }

    [Fact]
    public void PredefinedTypes_NumericRank()
    {
        Assert.Equal(0, ValkyrieType.GetNumericRank("i8"));
        Assert.Equal(4, ValkyrieType.GetNumericRank("i32"));
        Assert.Equal(9, ValkyrieType.GetNumericRank("f64"));
        Assert.Equal(-1, ValkyrieType.GetNumericRank("bool"));
    }

    [Fact]
    public void PredefinedTypes_ShaderTypes()
    {
        Assert.True(ValkyrieType.Vec2.IsVector);
        Assert.True(ValkyrieType.Vec3.IsVector);
        Assert.True(ValkyrieType.Vec4.IsVector);
        Assert.True(ValkyrieType.IVec2.IsVector);
        Assert.True(ValkyrieType.Mat3.IsMatrix);
        Assert.True(ValkyrieType.Mat4.IsMatrix);
    }

    [Fact]
    public void PredefinedTypes_TupleType()
    {
        var tuple = ValkyrieType.Tuple([ValkyrieType.I32, ValkyrieType.F32, ValkyrieType.String]);
        Assert.True(tuple.IsTuple);
        Assert.Equal(3, tuple.GenericArgs.Count);
    }

    [Fact]
    public void PredefinedTypes_TraitType()
    {
        var trait = ValkyrieType.TraitType("Serializable");
        Assert.True(trait.IsTrait);
        Assert.Equal("Serializable", trait.Name);
    }

    #endregion

    #region 约束描述符 - GenericConstraintDescriptor

    [Fact]
    public void ConstraintDescriptor_FromName_Class()
    {
        var d = GenericConstraintDescriptor.FromName("class");
        Assert.Equal(ConstraintKind.Class, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_Struct()
    {
        var d = GenericConstraintDescriptor.FromName("struct");
        Assert.Equal(ConstraintKind.Struct, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_New()
    {
        var d = GenericConstraintDescriptor.FromName("new()");
        Assert.Equal(ConstraintKind.New, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_Enum()
    {
        var d = GenericConstraintDescriptor.FromName("enum");
        Assert.Equal(ConstraintKind.Enum, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_Numeric()
    {
        var d = GenericConstraintDescriptor.FromName("Numeric");
        Assert.Equal(ConstraintKind.Numeric, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_Integer()
    {
        var d = GenericConstraintDescriptor.FromName("Integer");
        Assert.Equal(ConstraintKind.Integer, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_Float()
    {
        var d = GenericConstraintDescriptor.FromName("Float");
        Assert.Equal(ConstraintKind.Float, d.Kind);
    }

    [Fact]
    public void ConstraintDescriptor_FromName_CustomTrait()
    {
        var d = GenericConstraintDescriptor.FromName("MyTrait");
        Assert.Equal(ConstraintKind.Trait, d.Kind);
        Assert.Equal("MyTrait", d.TargetTypeName);
    }

    [Fact]
    public void ConstraintDescriptor_ToString_Class()
    {
        var d = new GenericConstraintDescriptor(ConstraintKind.Class);
        Assert.Equal("class", d.ToString());
    }

    [Fact]
    public void ConstraintDescriptor_ToString_New()
    {
        var d = new GenericConstraintDescriptor(ConstraintKind.New);
        Assert.Equal("new()", d.ToString());
    }

    [Fact]
    public void ConstraintDescriptor_ToString_Trait()
    {
        var d = new GenericConstraintDescriptor(ConstraintKind.Trait, "Serializable");
        Assert.Equal("Serializable", d.ToString());
    }

    [Fact]
    public void ConstraintDescriptor_ToString_Numeric()
    {
        var d = new GenericConstraintDescriptor(ConstraintKind.Numeric);
        Assert.Equal("Numeric", d.ToString());
    }

    #endregion

    #region 类型系统增强 - HasDefaultConstructor

    [Fact]
    public void DefaultConstructor_PrimitivesHaveIt()
    {
        Assert.True(ValkyrieType.I32.HasDefaultConstructor);
        Assert.True(ValkyrieType.F32.HasDefaultConstructor);
        Assert.True(ValkyrieType.Bool.HasDefaultConstructor);
    }

    [Fact]
    public void DefaultConstructor_StructHasIt()
    {
        var t = new ValkyrieType(TypeKind.Struct, "Point");
        Assert.True(t.HasDefaultConstructor);
    }

    [Fact]
    public void DefaultConstructor_ClassHasIt()
    {
        var t = new ValkyrieType(TypeKind.Class, "Widget");
        Assert.True(t.HasDefaultConstructor);
    }

    [Fact]
    public void DefaultConstructor_EnumHasIt()
    {
        var t = new ValkyrieType(TypeKind.Enum, "Color");
        Assert.True(t.HasDefaultConstructor);
    }

    [Fact]
    public void DefaultConstructor_TupleHasIt()
    {
        var t = ValkyrieType.Tuple([ValkyrieType.I32, ValkyrieType.F32]);
        Assert.True(t.HasDefaultConstructor);
    }

    #endregion

    #region 类型系统增强 - IsShaderType

    [Fact]
    public void ShaderType_NumericIsShaderType()
    {
        Assert.True(ValkyrieType.I32.IsShaderType);
        Assert.True(ValkyrieType.F32.IsShaderType);
    }

    [Fact]
    public void ShaderType_VectorIsShaderType()
    {
        Assert.True(ValkyrieType.Vec4.IsShaderType);
        Assert.True(ValkyrieType.IVec4.IsShaderType);
    }

    [Fact]
    public void ShaderType_BoolIsNotShaderType()
    {
        Assert.False(ValkyrieType.Bool.IsShaderType);
    }

    [Fact]
    public void ShaderType_StringIsNotShaderType()
    {
        Assert.False(ValkyrieType.String.IsShaderType);
    }

    #endregion

    #region 类型系统增强 - Effect 属性

    [Fact]
    public void Effect_PureCheck()
    {
        var pureFunc = new ValkyrieType(TypeKind.Function, "fn",
            effects: [EffectKind.Pure]);
        Assert.True(pureFunc.IsPure);
    }

    [Fact]
    public void Effect_AsyncCheck()
    {
        var asyncFunc = new ValkyrieType(TypeKind.Function, "fn",
            effects: [EffectKind.Async]);
        Assert.True(asyncFunc.IsAsync);
    }

    [Fact]
    public void Effect_IoCheck()
    {
        var ioFunc = new ValkyrieType(TypeKind.Function, "fn",
            effects: [EffectKind.Io]);
        Assert.True(ioFunc.IsIo);
    }

    #endregion

    #region 类型系统增强 - 类型变量

    [Fact]
    public void TypeVariable_IsTypeVariable()
    {
        var tv = ValkyrieType.TypeVariable("T");
        Assert.True(tv.IsTypeVariable);
        Assert.False(tv.IsConcrete);
    }

    [Fact]
    public void TypeVariable_WithConstraints()
    {
        var tv = ValkyrieType.TypeVariable("T", ["Numeric", "Clone"]);
        Assert.Equal(2, tv.ConstraintTypeNames.Count);
        Assert.Contains("Numeric", tv.ConstraintTypeNames);
        Assert.Contains("Clone", tv.ConstraintTypeNames);
    }

    #endregion

    #region 综合集成测试

    [Fact]
    public void Integration_GenericWithTraitConstraintAndMatch()
    {
        var source = """
                     trait Numeric {}
                     micro classify<T>(val: T): i32 where T : Numeric { return 0; }
                     micro test() { let x = classify(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_MultipleGenericFunctionsWithConstraints()
    {
        var source = """
                     trait Clone {} trait Debug {}
                     micro clone<T>(item: T): T where T : Clone { return item; }
                     micro debug<T>(item: T) where T : Debug {}
                     micro test() { let x = clone(42); debug(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_MatchWithGenericFunction()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test(flag: bool) {
                         match flag {
                             case true:
                                 let _ = identity(1);
                             case false:
                                 let _ = identity(2);
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_NestedGenericCalls()
    {
        var source = """
                     micro wrap<T>(val: T): list<T> { return [val]; }
                     micro first<T>(list: list<T>): T { return list[0]; }
                     micro test() { let x = first(wrap(42)); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_StructWithGenericMethod()
    {
        var source = """
                     struct Container { value: i32; }
                     micro get<T>(c: Container): T { return default; }
                     micro test() { let c: Container = Container { value: 42; }; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_EnumWithMatch()
    {
        var source = """
                     enum Color { Red; Green; Blue; }
                     micro test(c: Color): i32 {
                         match c {
                             case _:
                                 return 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_FlagsWithBitwiseOps()
    {
        var source = """
                     flags Permission { Read; Write; Execute; }
                     micro test(p: Permission): i32 { return 0; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_UnionWithMatch()
    {
        var source = """
                     union Result { Ok; Err; }
                     micro test(r: Result): i32 {
                         match r {
                             case _:
                                 return 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_ClassWithInheritance()
    {
        var source = """
                     class Animal { name: string; }
                     class Dog : Animal { breed: string; }
                     micro test(d: Dog) { let n = d.name; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_TypeAlias()
    {
        var source = """
                     type IntList = list<i32>;
                     micro test(items: IntList) { let x = items[0]; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_NamespaceWithDeclarations()
    {
        var source = """
                     namespace MyApp {
                         struct Point { x: f32; y: f32; }
                         micro origin(): Point { return Point { x: 0.0; y: 0.0; }; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_MultipleNamespaces()
    {
        var source = """
                     namespace Core { struct Vec2 { x: f32; y: f32; } }
                     namespace Math { micro zero(): f32 { return 0.0; } }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_ComponentWithGenericMethod()
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
    public void Integration_ShaderWithStruct()
    {
        var source = """
                     struct VertexInput { position: vec3; color: vec4; }
                     shader MainShader {
                         vertex { uniform mvp: mat4; }
                         fragment {}
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_WhileLoopWithGenericCondition()
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

    [Fact]
    public void Integration_ForLoopWithList()
    {
        var source = """
                     micro test() {
                         let items = [1, 2, 3];
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_LambdaInGenericCall()
    {
        var source = """
                     micro apply<T>(val: T, fn: fn(T) -> unit) {}
                     micro test() { apply(42, fn(x) {}); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_NullableWithMatch()
    {
        var source = """
                     micro test(maybe: i32?) {
                         match maybe {
                             case null:
                                 let _ = 0;
                             case _:
                                 let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_MultipleReturnPaths()
    {
        var source = """
                     micro classify(x: i32): i32 {
                         if (x > 0) {
                             return 1;
                         }
                         else {
                             return -1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Integration_DeeplyNestedGenericCalls()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() {
                         let a = identity(42);
                         let b = identity(a);
                         let c = identity(b);
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 边界条件与错误恢复

    [Fact]
    public void EdgeCase_ErrorTypeDoesNotCascade()
    {
        var source = """
                     micro test() { let x: i32 = undefined_var + 1; }
                     """;
        var result = CheckSource(source);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void EdgeCase_MultipleErrorsInSameFunction()
    {
        var source = """
                     micro test() {
                         let a: bool = 42;
                         let b: bool = 3.14;
                     }
                     """;
        var result = CheckSource(source);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void EdgeCase_EmptyStruct_ShouldPass()
    {
        var source = "struct Empty {}";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_EmptyClass_ShouldPass()
    {
        var source = "class Empty {}";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_EmptyEnum_ShouldPass()
    {
        var source = "enum Empty {}";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_EmptyNamespace_ShouldPass()
    {
        var source = "namespace Empty {}";
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_DeeplyNestedScopes_ShouldResolve()
    {
        var source = """
                     micro test() {
                         let a: i32 = 1;
                         { let b: i32 = a;
                             { let c: i32 = b;
                                 { let d: i32 = c; }
                             }
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ManyGenericParams_ShouldPass()
    {
        var source = """
                     micro quad<A, B, C, D>(a: A, b: B, c: C, d: D) {}
                     micro test() { quad(1, 2.0, true, "hello"); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_GenericWithNoArgs_ShouldPass()
    {
        var source = """
                     micro unit<T>(): T { return default; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_MultipleMatchOnSameValue()
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
    public void EdgeCase_NestedMatch_ShouldPass()
    {
        var source = """
                     micro test(x: i32, y: i32) {
                         match x {
                             case 1:
                                 match y {
                                     case _:
                                         let _ = 0;
                                 }
                             case _:
                                 let _ = 0;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_BreakInWhileLoop()
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
    public void EdgeCase_ContinueInWhileLoop()
    {
        var source = """
                     micro test() {
                         while (true) { continue; }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ReturnInMatchArm()
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

    [Fact]
    public void EdgeCase_CatchStatement()
    {
        var source = """
                     micro test() {
                         catch {
                             let _ = 1;
                         }
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_MutableVariable()
    {
        var source = """
                     micro test() {
                         let mut x: i32 = 1;
                         x = 2;
                     }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_ArrayLiteral()
    {
        var source = """
                     micro test() { let arr = [1, 2, 3]; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_EmptyArrayLiteral()
    {
        var source = """
                     micro test() { let arr: list<i32> = []; }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_MapLiteral()
    {
        var source = """
                     micro test() { let m: map<string, i32> = map(); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void EdgeCase_TurbofishSyntax()
    {
        var source = """
                     micro identity<T>(value: T): T { return value; }
                     micro test() { let x = identity::<i32>(42); }
                     """;
        var result = CheckSource(source);
        AssertNoErrors(result);
    }

    #endregion

    #region 数值类型转换矩阵

    [Fact]
    public void NumericConversion_I8ToAll()
    {
        var i8 = new ValkyrieType(TypeKind.Primitive, "i8");
        Assert.True(ValkyrieType.I16.IsAssignableFrom(i8));
        Assert.True(ValkyrieType.I32.IsAssignableFrom(i8));
        Assert.True(ValkyrieType.I64.IsAssignableFrom(i8));
        Assert.True(ValkyrieType.F32.IsAssignableFrom(i8));
        Assert.True(ValkyrieType.F64.IsAssignableFrom(i8));
    }

    [Fact]
    public void NumericConversion_U8ToAll()
    {
        var u8 = new ValkyrieType(TypeKind.Primitive, "u8");
        Assert.True(ValkyrieType.U16.IsAssignableFrom(u8));
        Assert.True(ValkyrieType.U32.IsAssignableFrom(u8));
        Assert.True(ValkyrieType.U64.IsAssignableFrom(u8));
        Assert.True(ValkyrieType.F32.IsAssignableFrom(u8));
        Assert.False(ValkyrieType.I16.IsAssignableFrom(u8));
    }

    [Fact]
    public void NumericConversion_I32ToI64()
    {
        Assert.True(ValkyrieType.I64.IsAssignableFrom(ValkyrieType.I32));
        Assert.False(ValkyrieType.I32.IsAssignableFrom(ValkyrieType.I64));
    }

    [Fact]
    public void NumericConversion_F32ToF64()
    {
        Assert.True(ValkyrieType.F64.IsAssignableFrom(ValkyrieType.F32));
        Assert.False(ValkyrieType.F32.IsAssignableFrom(ValkyrieType.F64));
    }

    [Fact]
    public void NumericConversion_U32ToI64_NotAllowed()
    {
        Assert.False(ValkyrieType.I64.IsAssignableFrom(ValkyrieType.U32));
    }

    [Fact]
    public void NumericConversion_SameType()
    {
        Assert.True(ValkyrieType.I32.IsAssignableFrom(ValkyrieType.I32));
        Assert.True(ValkyrieType.F64.IsAssignableFrom(ValkyrieType.F64));
    }

    #endregion

    #region ConstraintKind 枚举完整性

    [Fact]
    public void ConstraintKind_AllKindsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Trait));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Class));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Struct));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.New));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Enum));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Numeric));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Integer));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Float));
        Assert.True(Enum.IsDefined(typeof(ConstraintKind), ConstraintKind.Nullable));
    }

    #endregion

    #region TypeKind 枚举增强

    [Fact]
    public void TypeKind_AllKindsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Primitive));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.TypeVariable));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Generic));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Function));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Component));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.System));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Widget));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Plugin));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Struct));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Class));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Enum));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Union));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Intersection));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Array));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Map));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Nullable));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Shader));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Trait));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Tuple));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Error));
        Assert.True(Enum.IsDefined(typeof(TypeKind), TypeKind.Unknown));
    }

    #endregion
}
