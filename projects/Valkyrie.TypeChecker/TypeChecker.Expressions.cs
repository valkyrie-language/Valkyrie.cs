namespace Valkyrie.TypeChecker;

public partial class TypeChecker
{
    private ValkyrieType InferIndexType(TermIndexExpression index)
    {
        return InferOffsetIndexType(new OffsetIndexExpression
        {
            Target = index.Target,
            Indices = new[] { index.Index }
        });
    }

    private ValkyrieType InferOrdinalIndexType(OrdinalIndexExpression index)
    {
        var targetType = InferType(index.Target);
        var ordinalTypes = index.Indices.Select(InferType).ToArray();

        if (targetType.Kind == TypeKind.Array || targetType.Name == "list")
        {
            foreach (var ordinalType in ordinalTypes)
            {
                if (!ordinalType.IsError && !ordinalType.IsInteger)
                {
                    AddError("VALK2024", $"序数索引期望整数类型，实际为 '{ordinalType}'", index.Target.Span, "将序号转换为整数类型，例如 i32(ordinal)");
                    break;
                }
            }

            return targetType.GenericArgs.Count > 0 ? targetType.GenericArgs[0] : ValkyrieType.Auto;
        }

        if (targetType.Kind == TypeKind.Map)
        {
            if (targetType.GenericArgs.Count >= 2)
            {
                if (ordinalTypes.Length > 0)
                {
                    var ordinalType = ordinalTypes[0];
                    if (!ordinalType.IsError && !targetType.GenericArgs[0].IsAssignableFrom(ordinalType))
                    {
                        AddError("VALK2025",
                            $"Map 键类型不匹配：期望'{targetType.GenericArgs[0]}'，实际为 '{ordinalType}'",
                            index.Target.Span, "Map 序数访问的键类型必须与声明一致");
                    }
                }

                return targetType.GenericArgs[1];
            }
        }

        return ValkyrieType.Auto;
    }

    private ValkyrieType InferOffsetIndexType(OffsetIndexExpression index)
    {
        var targetType = InferType(index.Target);
        var offsetTypes = index.Indices.Select(InferType).ToArray();

        if (targetType.Kind == TypeKind.Array || targetType.Name == "list")
        {
            foreach (var offsetType in offsetTypes)
            {
                if (!offsetType.IsError && !offsetType.IsInteger)
                {
                    AddError("VALK2024", $"偏移索引期望整数类型，实际为 '{offsetType}'", index.Target.Span, "将偏移量转换为整数类型，例如 i32(offset)");
                    break;
                }
            }

            return targetType.GenericArgs.Count > 0 ? targetType.GenericArgs[0] : ValkyrieType.Auto;
        }

        if (targetType.Kind == TypeKind.Map)
        {
            if (targetType.GenericArgs.Count >= 2)
            {
                if (offsetTypes.Length > 0)
                {
                    var offsetType = offsetTypes[0];
                    if (!offsetType.IsError && !targetType.GenericArgs[0].IsAssignableFrom(offsetType))
                    {
                        AddError("VALK2025",
                            $"Map 键类型不匹配：期望'{targetType.GenericArgs[0]}'，实际为 '{offsetType}'",
                            index.Target.Span, "Map 偏移访问当前按键语义解释，建议改用序数访问或显式容器实现");
                    }
                }

                return targetType.GenericArgs[1];
            }
        }

        return ValkyrieType.Auto;
    }
}
