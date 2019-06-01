namespace Valkyrie.TypeChecker.TypeSystem;

/// <summary>
/// 类型种类枚举，定义所有可能的类型分类
/// </summary>
public enum TypeKind
{
    /// <summary>原始类型（i8~i64, u8~u64, f32, f64, bool, string, unit）</summary>
    Primitive,
    /// <summary>类型变量（泛型推断用）</summary>
    TypeVariable,
    /// <summary>泛型类型（用户自定义泛型）</summary>
    Generic,
    /// <summary>函数类型</summary>
    Function,
    /// <summary>ECS 组件</summary>
    Component,
    /// <summary>ECS 系统</summary>
    System,
    /// <summary>Widget</summary>
    Widget,
    /// <summary>Plugin</summary>
    Plugin,
    /// <summary>结构体</summary>
    Struct,
    /// <summary>类</summary>
    Class,
    /// <summary>枚举</summary>
    Enum,
    /// <summary>联合类型（代数数据类型）</summary>
    Union,
    /// <summary>交集类型</summary>
    Intersection,
    /// <summary>数组/list</summary>
    Array,
    /// <summary>映射</summary>
    Map,
    /// <summary>可空类型</summary>
    Nullable,
    /// <summary>着色器</summary>
    Shader,
    /// <summary>Trait（特征/接口）</summary>
    Trait,
    /// <summary>元组类型</summary>
    Tuple,
    /// <summary>错误类型</summary>
    Error,
    /// <summary>未知类型（含 auto）</summary>
    Unknown
}