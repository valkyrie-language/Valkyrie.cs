using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;

namespace Valkyrie.TypeChecker;

/// <summary>
/// 泛型约束求解器
/// 负责解析约束声明、验证类型参数是否满足约束、报告约束违反
/// 支持 Trait 约束、class/struct/new()/enum/numeric/integer/float 约束
/// </summary>
public sealed partial class TypeChecker
{
    #region 约束求解

    /// <summary>
    /// 解析泛型约束声明为约束描述符列表
    /// 将 AST 中的 GenericConstraint 转换为结构化的 GenericConstraintDescriptor
    /// </summary>
    private List<GenericConstraintDescriptor> ResolveConstraints(
        IReadOnlyList<GenericConstraint> genericConstraints,
        IReadOnlyList<TypeParameter> typeParameters)
    {
        var descriptors = new List<GenericConstraintDescriptor>();

        foreach (var constraint in genericConstraints)
        {
            var tp = typeParameters.FirstOrDefault(t => t.Name == constraint.ParameterName);
            if (tp is null)
            {
                continue;
            }

            foreach (var constraintType in constraint.ConstraintTypes)
            {
                var resolvedType = ResolveTypeAnnotation(constraintType);
                var descriptor = GenericConstraintDescriptor.FromName(constraintType.Name, resolvedType);
                descriptors.Add(descriptor);
            }
        }

        return descriptors;
    }

    /// <summary>
    /// 增强的泛型约束检查：在泛型函数调用时验证推导出的类型参数是否满足所有约束
    /// 支持 Trait 实现、class/struct/new()/enum/numeric 等约束种类
    /// </summary>
    private void CheckGenericConstraintsEnhanced(
        IReadOnlyList<TypeParameter> typeParameters,
        IReadOnlyList<GenericConstraint> genericConstraints,
        TermCallExpression call)
    {
        var descriptors = ResolveConstraints(genericConstraints, typeParameters);

        foreach (var tp in typeParameters)
        {
            if (!_typeSubstitutions.TryGetValue(tp.Name, out var resolvedType))
            {
                continue;
            }

            var constraintsForParam = descriptors.Where(d =>
                genericConstraints.Any(gc =>
                    gc.ParameterName == tp.Name &&
                    gc.ConstraintTypes.Any(ct => ct.Name == d.TargetTypeName || d.Kind != ConstraintKind.Trait)))
                .ToList();

            var paramConstraints = GetConstraintsForParameter(tp.Name, genericConstraints, descriptors);

            foreach (var constraint in paramConstraints)
            {
                if (!resolvedType.SatisfiesConstraint(constraint))
                {
                    var constraintDesc = constraint.ToString();
                    AddError("VALK2042",
                        $"类型参数 '{tp.Name}' 不满足约束 '{constraintDesc}'，" +
                        $"推导类型为 '{resolvedType}'",
                        call.Span, $"确保类型参数满足 where {tp.Name} : {constraintDesc} 约束");
                }
            }
        }
    }

    /// <summary>
    /// 获取指定类型参数的所有约束描述符
    /// </summary>
    private List<GenericConstraintDescriptor> GetConstraintsForParameter(
        string paramName,
        IReadOnlyList<GenericConstraint> genericConstraints,
        List<GenericConstraintDescriptor> allDescriptors)
    {
        var result = new List<GenericConstraintDescriptor>();

        foreach (var gc in genericConstraints)
        {
            if (gc.ParameterName != paramName)
            {
                continue;
            }

            foreach (var ct in gc.ConstraintTypes)
            {
                var descriptor = allDescriptors.FirstOrDefault(d =>
                    d.TargetTypeName == ct.Name ||
                    (d.Kind == ConstraintKind.Class && ct.Name == "class") ||
                    (d.Kind == ConstraintKind.Struct && ct.Name == "struct") ||
                    (d.Kind == ConstraintKind.New && (ct.Name == "new" || ct.Name == "new()")) ||
                    (d.Kind == ConstraintKind.Enum && ct.Name == "enum") ||
                    (d.Kind == ConstraintKind.Numeric && ct.Name == "Numeric") ||
                    (d.Kind == ConstraintKind.Integer && ct.Name == "Integer") ||
                    (d.Kind == ConstraintKind.Float && ct.Name == "Float"));

                if (descriptor is not null)
                {
                    result.Add(descriptor);
                }
                else
                {
                    var resolvedType = ResolveTypeAnnotation(ct);
                    result.Add(GenericConstraintDescriptor.FromName(ct.Name, resolvedType));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 增强的泛型约束声明检查
    /// 验证约束引用的类型参数存在、约束类型已定义、约束种类合法
    /// </summary>
    private void CheckTypeParameterConstraintsEnhanced(
        IReadOnlyList<TypeParameter> typeParameters,
        IReadOnlyList<GenericConstraint> genericConstraints)
    {
        foreach (var constraint in genericConstraints)
        {
            var tp = typeParameters.FirstOrDefault(t => t.Name == constraint.ParameterName);
            if (tp is null)
            {
                AddWarning("VALK2043",
                    $"约束引用了未声明的类型参数 '{constraint.ParameterName}'",
                    constraint.Span);
                continue;
            }

            foreach (var ct in constraint.ConstraintTypes)
            {
                if (IsBuiltinConstraintKind(ct.Name))
                {
                    continue;
                }

                var resolved = ResolveTypeAnnotation(ct);
                if (resolved.IsError || resolved.Kind == TypeKind.Unknown)
                {
                    AddWarning("VALK2044",
                        $"约束类型 '{ct.Name}' 未定义，类型参数 '{constraint.ParameterName}' 的约束无法验证",
                        ct.Span);
                }
                else if (resolved.Kind != TypeKind.Trait && resolved.Kind != TypeKind.Class)
                {
                    AddWarning("VALK2081",
                        $"约束类型 '{ct.Name}' 不是 trait 或接口，类型参数 '{constraint.ParameterName}' 的约束可能无效",
                        ct.Span, "约束类型应为 trait、接口或内置约束种类");
                }
            }
        }

        CheckConstraintCombinationValidity(typeParameters, genericConstraints);
    }

    /// <summary>
    /// 检查约束组合的合法性
    /// 例如：class 和 struct 约束互斥、struct 和 new() 约束冗余等
    /// </summary>
    private void CheckConstraintCombinationValidity(
        IReadOnlyList<TypeParameter> typeParameters,
        IReadOnlyList<GenericConstraint> genericConstraints)
    {
        foreach (var tp in typeParameters)
        {
            var constraintsForParam = genericConstraints
                .Where(gc => gc.ParameterName == tp.Name)
                .SelectMany(gc => gc.ConstraintTypes)
                .Select(ct => ct.Name)
                .ToList();

            if (constraintsForParam.Contains("class") && constraintsForParam.Contains("struct"))
            {
                AddError("VALK2082",
                        $"类型参数 '{tp.Name}' 同时具有 'class' 和 'struct' 约束，二者互斥",
                        default(TextSpan), "移除其中一个约束");
            }

            if (constraintsForParam.Contains("struct") && constraintsForParam.Contains("new()"))
            {
                AddWarning("VALK2083",
                    $"类型参数 '{tp.Name}' 的 'new()' 约束在 'struct' 约束下冗余",
                    default(TextSpan), "struct 约束已隐含 new()");
            }

            if (constraintsForParam.Contains("enum") && constraintsForParam.Contains("struct"))
            {
                AddWarning("VALK2084",
                    $"类型参数 '{tp.Name}' 的 'struct' 约束在 'enum' 约束下冗余",
                    default(TextSpan), "enum 约束已隐含值类型");
            }
        }
    }

    /// <summary>
    /// 判断约束名是否为内置约束种类
    /// </summary>
    private static bool IsBuiltinConstraintKind(string name)
    {
        return name is "class" or "struct" or "new" or "new()" or "enum"
            or "Numeric" or "Integer" or "Float" or "nullable";
    }

    #endregion

    #region Trait 注册与验证

    /// <summary>
    /// 从 ValkyrieType 的字符串约束信息检查泛型约束
    /// 用于泛型函数调用时，从 funcType 的 GenericConstraintNames 解析约束
    /// </summary>
    private void CheckGenericConstraintsFromFuncType(ValkyrieType funcType, TermCallExpression call)
    {
        if (funcType.GenericConstraintNames.Count == 0)
        {
            return;
        }

        foreach (var tpName in funcType.GenericTypeParams)
        {
            if (!_typeSubstitutions.TryGetValue(tpName, out var resolvedType))
            {
                continue;
            }

            foreach (var constraintStr in funcType.GenericConstraintNames)
            {
                var colonIdx = constraintStr.IndexOf(':');
                if (colonIdx < 0)
                {
                    continue;
                }

                var paramName = constraintStr.Substring(0, colonIdx);
                if (paramName != tpName)
                {
                    continue;
                }

                var constraintTypeStr = constraintStr.Substring(colonIdx + 1);
                var constraintTypeNames = constraintTypeStr.Split(',');

                foreach (var ctn in constraintTypeNames)
                {
                    var trimmedName = ctn.Trim();
                    if (string.IsNullOrEmpty(trimmedName))
                    {
                        continue;
                    }

                    var descriptor = _typeRegistry.ContainsKey(trimmedName)
                        ? new GenericConstraintDescriptor(ConstraintKind.Trait, trimmedName, _typeRegistry[trimmedName])
                        : GenericConstraintDescriptor.FromName(trimmedName);

                    if (descriptor.Kind == ConstraintKind.Trait)
                    {
                        if (_typeRegistry.TryGetValue(trimmedName, out var traitType))
                        {
                            descriptor = GenericConstraintDescriptor.FromName(trimmedName, traitType);
                        }
                    }

                    if (!resolvedType.SatisfiesConstraint(descriptor))
                    {
                        AddError("VALK2042",
                            $"类型参数 '{tpName}' 不满足约束 '{descriptor}'，" +
                            $"推导类型为 '{resolvedType}'",
                            call.Span, $"确保类型参数满足 where {tpName} : {descriptor} 约束");
                    }
                }
            }
        }
    }

    #endregion

    #region Trait 注册与验证

    /// <summary>
    /// 注册 trait 声明到类型注册表
    /// </summary>
    private void RegisterTraitDecl(string name, ValkyrieType traitType)
    {
        _typeRegistry[name] = traitType;

        var symbol = new Symbol(name, traitType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2085",
                $"Trait '{name}' 已在当前作用域中定义",
                default, "重命名 trait 以避免重复定义");
        }
    }

    /// <summary>
    /// 为类型注册 trait 实现
    /// 在类型声明时调用，记录该类型实现了哪些 trait
    /// </summary>
    private void RegisterTraitImplementation(string typeName, string traitName)
    {
        if (_typeRegistry.TryGetValue(typeName, out var existingType))
        {
            var traits = new List<string>(existingType.ImplementedTraits) { traitName };
            var updatedType = new ValkyrieType(
                existingType.Kind, existingType.Name, existingType.GenericArgs,
                existingType.Parameters, existingType.ReturnType, existingType.IsMutable,
                existingType.ConstraintTypeNames, existingType.GenericTypeParams,
                existingType.GenericConstraintNames, existingType.Effects,
                implementedTraits: traits, narrowedFrom: existingType.NarrowedFrom);

            _typeRegistry[typeName] = updatedType;
        }
    }

    /// <summary>
    /// 验证类型是否实现了指定的 trait
    /// 用于泛型约束求解和类型测试
    /// </summary>
    private bool VerifyTraitImplementation(ValkyrieType type, string traitName)
    {
        if (type.IsError || type.IsAuto)
        {
            return true;
        }

        if (type.ImplementsTrait(traitName))
        {
            return true;
        }

        if (_typeRegistry.TryGetValue(traitName, out var traitType) && traitType.IsTrait)
        {
            return type.IsAssignableFrom(traitType);
        }

        return false;
    }

    #endregion
}
