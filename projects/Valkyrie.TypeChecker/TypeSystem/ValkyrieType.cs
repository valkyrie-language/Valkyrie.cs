namespace Valkyrie.TypeChecker.TypeSystem;

/// <summary>
/// Valkyrie 类型系统的核心类型表示
/// 支持原始类型、泛型、函数、联合/交集、Trait、类型变量等
/// 提供类型兼容性判断、类型替换、约束验证和类型窄化
/// </summary>
public sealed class ValkyrieType
{
    public TypeKind Kind { get; }
    public string Name { get; }
    public IReadOnlyList<ValkyrieType> GenericArgs { get; }
    public IReadOnlyList<ParameterType>? Parameters { get; }
    public ValkyrieType? ReturnType { get; }
    public bool IsMutable { get; }
    public IReadOnlyList<string> ConstraintTypeNames { get; }
    public IReadOnlyList<string> GenericTypeParams { get; }
    public IReadOnlyList<string> GenericConstraintNames { get; }
    public IReadOnlyList<EffectKind> Effects { get; }

    /// <summary>
    /// 该类型实现的 trait 名称列表（用于约束求解）
    /// </summary>
    public IReadOnlyList<string> ImplementedTraits { get; }

    /// <summary>
    /// 类型窄化信息：在类型测试后窄化到的目标类型
    /// </summary>
    public ValkyrieType? NarrowedFrom { get; }

    public ValkyrieType(TypeKind kind, string name,
        IReadOnlyList<ValkyrieType>? genericArgs = null,
        IReadOnlyList<ParameterType>? parameters = null,
        ValkyrieType? returnType = null,
        bool isMutable = false,
        IReadOnlyList<string>? constraintTypeNames = null,
        IReadOnlyList<string>? genericTypeParams = null,
        IReadOnlyList<string>? genericConstraintNames = null,
        IReadOnlyList<EffectKind>? effects = null,
        IReadOnlyList<string>? implementedTraits = null,
        ValkyrieType? narrowedFrom = null)
    {
        Kind = kind;
        Name = name;
        GenericArgs = genericArgs ?? [];
        Parameters = parameters;
        ReturnType = returnType;
        IsMutable = isMutable;
        ConstraintTypeNames = constraintTypeNames ?? [];
        GenericTypeParams = genericTypeParams ?? [];
        GenericConstraintNames = genericConstraintNames ?? [];
        Effects = effects ?? [];
        ImplementedTraits = implementedTraits ?? [];
        NarrowedFrom = narrowedFrom;
    }

    #region 类型属性

    public bool IsNumeric => Kind == TypeKind.Primitive && Name is "i8" or "i16" or "i32" or "i64"
        or "u8" or "u16" or "u32" or "u64"
        or "f32" or "f64";

    public bool IsInteger => Kind == TypeKind.Primitive && Name is "i8" or "i16" or "i32" or "i64"
        or "u8" or "u16" or "u32" or "u64";

    public bool IsFloat => Kind == TypeKind.Primitive && Name is "f32" or "f64";

    public bool IsBool => Kind == TypeKind.Primitive && Name is "bool";

    public bool IsString => Kind == TypeKind.Primitive && Name is "string";

    public bool IsUnit => Name is "unit" or "none";

    public bool IsError => Kind == TypeKind.Error;

    public bool IsAuto => Kind == TypeKind.Unknown && Name == "auto";

    public bool IsTrait => Kind == TypeKind.Trait;

    public bool IsTuple => Kind == TypeKind.Tuple;

    /// <summary>
    /// 是否为引用类型（class、string、array、map、nullable、trait）
    /// </summary>
    public bool IsReferenceType => Kind is TypeKind.Class or TypeKind.Array or TypeKind.Map
        or TypeKind.Nullable or TypeKind.Trait
        || (Kind == TypeKind.Primitive && Name == "string");

    /// <summary>
    /// 是否为值类型（primitive（非 string）、struct、enum、tuple）
    /// </summary>
    public bool IsValueType => Kind is TypeKind.Struct or TypeKind.Enum or TypeKind.Tuple
        || (Kind == TypeKind.Primitive && !IsString);

    /// <summary>
    /// 是否支持默认构造（有 new() 能力）
    /// </summary>
    public bool HasDefaultConstructor => Kind is TypeKind.Primitive or TypeKind.Struct or TypeKind.Enum
        or TypeKind.Class or TypeKind.Tuple
        && !IsString;

    /// <summary>
    /// 是否满足数值约束（所有数值类型）
    /// </summary>
    public bool SatisfiesNumericConstraint => IsNumeric;

    /// <summary>
    /// 是否满足整数约束
    /// </summary>
    public bool SatisfiesIntegerConstraint => IsInteger;

    /// <summary>
    /// 是否满足浮点约束
    /// </summary>
    public bool SatisfiesFloatConstraint => IsFloat;

    #endregion

    #region 类型兼容性

    /// <summary>
    /// 判断当前类型是否可以从 other 类型赋值
    /// 支持 Error 兼容、Nullable 兼容、名称+泛型参数匹配、隐式数值转换、Trait 实现
    /// </summary>
    public bool IsAssignableFrom(ValkyrieType other)
    {
        if (IsError || other.IsError)
        {
            return true;
        }

        if (IsAuto)
        {
            return true;
        }

        if (Kind == TypeKind.Nullable && other.Name == "null")
        {
            return true;
        }

        if (Kind == TypeKind.Nullable && GenericArgs.Count > 0)
        {
            return GenericArgs[0].IsAssignableFrom(other);
        }

        if (IsTrait && other.ImplementsTrait(Name))
        {
            return true;
        }

        if (Name == other.Name && GenericArgs.Count == other.GenericArgs.Count)
        {
            for (int i = 0; i < GenericArgs.Count; i++)
            {
                if (!GenericArgs[i].IsAssignableFrom(other.GenericArgs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        if (IsNumeric && other.IsNumeric)
        {
            return IsImplicitNumericConversion(other, this);
        }

        if (Kind == TypeKind.Union && GenericArgs.Count > 0)
        {
            foreach (var member in GenericArgs)
            {
                if (member.IsAssignableFrom(other))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断当前类型是否实现了指定名称的 trait
    /// </summary>
    public bool ImplementsTrait(string traitName)
    {
        foreach (var impl in ImplementedTraits)
        {
            if (string.Equals(impl, traitName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (IsNumeric && (traitName == "Numeric" || traitName == "Addable" || traitName == "Comparable"))
        {
            return true;
        }

        if (IsInteger && (traitName == "Integer" || traitName == "Numeric" || traitName == "Bitwise"))
        {
            return true;
        }

        if (IsFloat && (traitName == "Float" || traitName == "Numeric"))
        {
            return true;
        }

        if (IsBool && traitName == "Comparable")
        {
            return true;
        }

        if (IsString && (traitName == "Comparable" || traitName == "Addable" || traitName == "Iterable"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 验证类型是否满足指定约束
    /// </summary>
    public bool SatisfiesConstraint(GenericConstraintDescriptor constraint)
    {
        if (IsError || IsAuto)
        {
            return true;
        }

        return constraint.Kind switch
        {
            ConstraintKind.Class => IsReferenceType,
            ConstraintKind.Struct => IsValueType && !IsNumeric,
            ConstraintKind.New => HasDefaultConstructor,
            ConstraintKind.Enum => Kind == TypeKind.Enum,
            ConstraintKind.Numeric => SatisfiesNumericConstraint,
            ConstraintKind.Integer => SatisfiesIntegerConstraint,
            ConstraintKind.Float => SatisfiesFloatConstraint,
            ConstraintKind.Nullable => Kind == TypeKind.Nullable,
            ConstraintKind.Trait => ImplementsTrait(constraint.TargetTypeName ?? ""),
            _ => true
        };
    }

    #endregion

    #region 类型窄化

    /// <summary>
    /// 创建窄化类型：在类型测试成功后，将类型窄化为更具体的类型
    /// 例如：if (x is i32) 之后 x 的类型从 auto 窄化为 i32
    /// </summary>
    public ValkyrieType NarrowTo(ValkyrieType targetType)
    {
        if (Kind == TypeKind.Nullable && GenericArgs.Count > 0 && targetType.IsAssignableFrom(GenericArgs[0]))
        {
            return new ValkyrieType(GenericArgs[0].Kind, GenericArgs[0].Name,
                GenericArgs[0].GenericArgs, GenericArgs[0].Parameters, GenericArgs[0].ReturnType,
                GenericArgs[0].IsMutable, GenericArgs[0].ConstraintTypeNames,
                GenericArgs[0].GenericTypeParams, GenericArgs[0].GenericConstraintNames,
                GenericArgs[0].Effects, GenericArgs[0].ImplementedTraits,
                narrowedFrom: this);
        }

        if (Kind == TypeKind.Union && GenericArgs.Count > 0)
        {
            foreach (var member in GenericArgs)
            {
                if (targetType.IsAssignableFrom(member) || member.IsAssignableFrom(targetType))
                {
                    return new ValkyrieType(targetType.Kind, targetType.Name,
                        targetType.GenericArgs, targetType.Parameters, targetType.ReturnType,
                        targetType.IsMutable, targetType.ConstraintTypeNames,
                        targetType.GenericTypeParams, targetType.GenericConstraintNames,
                        targetType.Effects, targetType.ImplementedTraits,
                        narrowedFrom: this);
                }
            }
        }

        if (IsAuto || Kind == TypeKind.Unknown)
        {
            return new ValkyrieType(targetType.Kind, targetType.Name,
                targetType.GenericArgs, targetType.Parameters, targetType.ReturnType,
                targetType.IsMutable, targetType.ConstraintTypeNames,
                targetType.GenericTypeParams, targetType.GenericConstraintNames,
                targetType.Effects, targetType.ImplementedTraits,
                narrowedFrom: this);
        }

        return this;
    }

    /// <summary>
    /// 判断类型测试是否可能成功（窄化是否有意义）
    /// </summary>
    public bool CanNarrowTo(ValkyrieType targetType)
    {
        if (IsError || targetType.IsError)
        {
            return false;
        }

        if (Kind == TypeKind.Nullable && GenericArgs.Count > 0)
        {
            return targetType.IsAssignableFrom(GenericArgs[0]) || GenericArgs[0].IsAssignableFrom(targetType);
        }

        if (Kind == TypeKind.Union && GenericArgs.Count > 0)
        {
            foreach (var member in GenericArgs)
            {
                if (targetType.IsAssignableFrom(member) || member.IsAssignableFrom(targetType))
                {
                    return true;
                }
            }

            return false;
        }

        if (IsAuto || Kind == TypeKind.Unknown)
        {
            return true;
        }

        return targetType.IsAssignableFrom(this) && !targetType.Equals(this);
    }

    #endregion

    #region 格式化与相等

    public override string ToString()
    {
        if (GenericArgs.Count > 0)
        {
            return $"{Name}<{string.Join(", ", GenericArgs)}>";
        }

        return Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is ValkyrieType other && Name == other.Name
                                         && GenericArgs.SequenceEqual(other.GenericArgs);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, GenericArgs.Count);
    }

    #endregion

    #region 数值转换

    private static bool IsImplicitNumericConversion(ValkyrieType from, ValkyrieType to)
    {
        int fromRank = GetNumericRank(from.Name);
        int toRank = GetNumericRank(to.Name);

        if (fromRank < 0 || toRank < 0)
        {
            return false;
        }

        if (from.Name.StartsWith('u') && to.Name.StartsWith('i'))
        {
            return false;
        }

        return toRank > fromRank;
    }

    public static int GetNumericRank(string name)
    {
        return name switch
        {
            "i8" => 0,
            "u8" => 1,
            "i16" => 2,
            "u16" => 3,
            "i32" => 4,
            "u32" => 5,
            "f32" => 6,
            "i64" => 7,
            "u64" => 8,
            "f64" => 9,
            _ => -1
        };
    }

    #endregion

    #region 预定义类型

    public static ValkyrieType I8 => new(TypeKind.Primitive, "i8");
    public static ValkyrieType I16 => new(TypeKind.Primitive, "i16");
    public static ValkyrieType I32 => new(TypeKind.Primitive, "i32");
    public static ValkyrieType I64 => new(TypeKind.Primitive, "i64");
    public static ValkyrieType U8 => new(TypeKind.Primitive, "u8");
    public static ValkyrieType U16 => new(TypeKind.Primitive, "u16");
    public static ValkyrieType U32 => new(TypeKind.Primitive, "u32");
    public static ValkyrieType U64 => new(TypeKind.Primitive, "u64");
    public static ValkyrieType F32 => new(TypeKind.Primitive, "f32");
    public static ValkyrieType F64 => new(TypeKind.Primitive, "f64");
    public static ValkyrieType Bool => new(TypeKind.Primitive, "bool");
    public static ValkyrieType String => new(TypeKind.Primitive, "string");
    public static ValkyrieType Unit => new(TypeKind.Primitive, "unit");
    public static ValkyrieType Null => new(TypeKind.Primitive, "null");
    public static ValkyrieType Auto => new(TypeKind.Unknown, "auto");
    public static ValkyrieType Error => new(TypeKind.Error, "<error>");

    /// <summary>
    /// 创建带 trait 实现列表的类型
    /// </summary>
    public static ValkyrieType WithTraits(TypeKind kind, string name,
        IReadOnlyList<string> implementedTraits,
        IReadOnlyList<ValkyrieType>? genericArgs = null)
    {
        return new ValkyrieType(kind, name, genericArgs, implementedTraits: implementedTraits);
    }

    /// <summary>
    /// 创建 Trait 类型
    /// </summary>
    public static ValkyrieType TraitType(string name,
        IReadOnlyList<ValkyrieType>? genericArgs = null,
        IReadOnlyList<ParameterType>? parameters = null,
        ValkyrieType? returnType = null)
    {
        return new ValkyrieType(TypeKind.Trait, name, genericArgs, parameters, returnType);
    }

    /// <summary>
    /// 创建元组类型
    /// </summary>
    public static ValkyrieType Tuple(IReadOnlyList<ValkyrieType> elements)
    {
        var name = $"({string.Join(", ", elements)})";
        return new ValkyrieType(TypeKind.Tuple, name, elements);
    }

    public static ValkyrieType List(ValkyrieType element) =>
        new(TypeKind.Array, "list", [element]);

    public static ValkyrieType Map(ValkyrieType key, ValkyrieType value) =>
        new(TypeKind.Map, "map", [key, value]);

    public static ValkyrieType Nullable(ValkyrieType inner) =>
        new(TypeKind.Nullable, $"{inner.Name}?", [inner]);

    public static ValkyrieType TypeVariable(string name,
        IReadOnlyList<string>? constraintTypeNames = null) =>
        new(TypeKind.TypeVariable, name, constraintTypeNames: constraintTypeNames);

    public static ValkyrieType Vec2 => new(TypeKind.Primitive, "vec2");
    public static ValkyrieType Vec3 => new(TypeKind.Primitive, "vec3");
    public static ValkyrieType Vec4 => new(TypeKind.Primitive, "vec4");
    public static ValkyrieType IVec2 => new(TypeKind.Primitive, "ivec2");
    public static ValkyrieType IVec3 => new(TypeKind.Primitive, "ivec3");
    public static ValkyrieType IVec4 => new(TypeKind.Primitive, "ivec4");
    public static ValkyrieType UVec2 => new(TypeKind.Primitive, "uvec2");
    public static ValkyrieType UVec3 => new(TypeKind.Primitive, "uvec3");
    public static ValkyrieType UVec4 => new(TypeKind.Primitive, "uvec4");
    public static ValkyrieType Mat3 => new(TypeKind.Primitive, "mat3");
    public static ValkyrieType Mat4 => new(TypeKind.Primitive, "mat4");

    public bool IsVector => Name is "vec2" or "vec3" or "vec4"
        or "ivec2" or "ivec3" or "ivec4"
        or "uvec2" or "uvec3" or "uvec4";

    public bool IsMatrix => Name is "mat3" or "mat4";

    public bool IsShaderType => IsVector || IsMatrix || IsNumeric;

    public bool IsTypeVariable => Kind == TypeKind.TypeVariable;

    /// <summary>
    /// 是否已确定具体类型（非类型变量）
    /// </summary>
    public bool IsConcrete => !IsTypeVariable && Kind != TypeKind.Unknown && Kind != TypeKind.Error;

    public bool IsPure => Effects.Contains(EffectKind.Pure);

    public bool IsAsync => Effects.Contains(EffectKind.Async);

    public bool IsIo => Effects.Contains(EffectKind.Io);

    #endregion

    #region 类型替换

    /// <summary>
    /// 使用类型替换字典替换类型变量
    /// </summary>
    /// <returns>替换后的新类型，若无变化则返回自身</returns>
    public ValkyrieType Substitute(IReadOnlyDictionary<string, ValkyrieType> substitutions)
    {
        if (IsTypeVariable && substitutions.TryGetValue(Name, out var concrete))
        {
            return concrete;
        }

        if (GenericArgs.Count > 0)
        {
            var substituted = GenericArgs.Select(a => a.Substitute(substitutions)).ToList();
            if (substituted.SequenceEqual(GenericArgs))
            {
                return this;
            }

            return new ValkyrieType(Kind, Name, substituted, Parameters, ReturnType, IsMutable,
                ConstraintTypeNames, GenericTypeParams, GenericConstraintNames, Effects,
                ImplementedTraits, NarrowedFrom);
        }

        if (Parameters is not null)
        {
            var substParams = Parameters.Select(p =>
                new ParameterType(p.Name, p.Type.Substitute(substitutions), p.IsMutable)).ToList();
            var substReturn = ReturnType?.Substitute(substitutions);
            return new ValkyrieType(Kind, Name, null, substParams, substReturn, IsMutable,
                ConstraintTypeNames, GenericTypeParams, GenericConstraintNames, Effects,
                ImplementedTraits, NarrowedFrom);
        }

        return this;
    }

    #endregion
}
