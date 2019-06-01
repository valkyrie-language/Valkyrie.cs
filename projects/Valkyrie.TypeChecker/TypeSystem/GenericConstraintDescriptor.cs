namespace Valkyrie.TypeChecker.TypeSystem;

/// <summary>
/// 泛型约束描述，表示类型参数上的单个约束
/// </summary>
public sealed class GenericConstraintDescriptor
{
    /// <summary>约束种类</summary>
    public ConstraintKind Kind { get; }

    /// <summary>约束目标类型名（Trait 约束时为 trait 名称）</summary>
    public string? TargetTypeName { get; }

    /// <summary>约束目标类型（解析后的 ValkyrieType，Trait 约束时有效）</summary>
    public ValkyrieType? TargetType { get; }

    public GenericConstraintDescriptor(ConstraintKind kind, string? targetTypeName = null, ValkyrieType? targetType = null)
    {
        Kind = kind;
        TargetTypeName = targetTypeName;
        TargetType = targetType;
    }

    /// <summary>
    /// 从约束类型名推断约束种类
    /// </summary>
    public static GenericConstraintDescriptor FromName(string name, ValkyrieType? resolvedType = null)
    {
        return name switch
        {
            "class" => new GenericConstraintDescriptor(ConstraintKind.Class),
            "struct" => new GenericConstraintDescriptor(ConstraintKind.Struct),
            "new" or "new()" => new GenericConstraintDescriptor(ConstraintKind.New),
            "enum" => new GenericConstraintDescriptor(ConstraintKind.Enum),
            "Numeric" => new GenericConstraintDescriptor(ConstraintKind.Numeric, name, resolvedType),
            "Integer" => new GenericConstraintDescriptor(ConstraintKind.Integer, name, resolvedType),
            "Float" => new GenericConstraintDescriptor(ConstraintKind.Float, name, resolvedType),
            "nullable" => new GenericConstraintDescriptor(ConstraintKind.Nullable),
            _ => new GenericConstraintDescriptor(ConstraintKind.Trait, name, resolvedType)
        };
    }

    public override string ToString()
    {
        return Kind switch
        {
            ConstraintKind.Trait => TargetTypeName ?? "trait",
            ConstraintKind.Class => "class",
            ConstraintKind.Struct => "struct",
            ConstraintKind.New => "new()",
            ConstraintKind.Enum => "enum",
            ConstraintKind.Numeric => "Numeric",
            ConstraintKind.Integer => "Integer",
            ConstraintKind.Float => "Float",
            ConstraintKind.Nullable => "nullable",
            _ => Kind.ToString()
        };
    }
}
