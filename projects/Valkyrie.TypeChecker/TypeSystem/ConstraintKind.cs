namespace Valkyrie.TypeChecker.TypeSystem;

/// <summary>
/// 泛型约束种类，定义类型参数可施加的约束类型
/// </summary>
public enum ConstraintKind
{
    /// <summary>Trait 约束：where T : TraitName</summary>
    Trait,
    /// <summary>类约束：where T : class</summary>
    Class,
    /// <summary>结构体约束：where T : struct</summary>
    Struct,
    /// <summary>构造函数约束：where T : new()</summary>
    New,
    /// <summary>枚举约束：where T : enum</summary>
    Enum,
    /// <summary>数值约束：where T : Numeric</summary>
    Numeric,
    /// <summary>整数约束：where T : Integer</summary>
    Integer,
    /// <summary>浮点约束：where T : Float</summary>
    Float,
    /// <summary>可空约束：where T : nullable</summary>
    Nullable
}
