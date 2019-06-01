using Oak.Syntax;
using Oak.Valkyrie.AST;

namespace Valkyrie.TypeChecker;

/// <summary>
/// Match 穷举性检查增强
/// 支持缺失分支报告、更精确的穷举分析、Nullable 穷举检查
/// </summary>
public sealed partial class TypeChecker
{
    #region 穷举性检查增强

    /// <summary>
    /// 穷举性检查结果
    /// </summary>
    private sealed class ExhaustivenessResult
    {
        public bool IsExhaustive { get; set; }
        public List<string> MissingArms { get; set; } = [];
        public List<int> RedundantArmIndices { get; set; } = [];
    }

    /// <summary>
    /// 增强的穷举性检查入口
    /// 返回详细的检查结果，包含缺失分支信息
    /// </summary>
    private ExhaustivenessResult CheckExhaustiveness(ValkyrieType subjectType, IReadOnlyList<MatchArm> arms)
    {
        var result = new ExhaustivenessResult();

        if (arms.Count == 0)
        {
            result.IsExhaustive = false;
            result.MissingArms.Add($"类型 '{subjectType}' 的所有可能值");
            return result;
        }

        if (arms.Any(a => a.Pattern is WildcardPattern))
        {
            result.IsExhaustive = true;
            return result;
        }

        var coveredSet = new HashSet<string>(StringComparer.Ordinal);
        var allVariants = GetAllVariants(subjectType);

        foreach (var arm in arms)
        {
            var covered = GetPatternCoveredTypes(arm, subjectType);
            foreach (var c in covered)
            {
                coveredSet.Add(c);
            }
        }

        var missingVariants = allVariants.Where(v => !coveredSet.Contains(v)).ToList();
        result.IsExhaustive = missingVariants.Count == 0;
        result.MissingArms.AddRange(missingVariants);

        DetectRedundantArms(subjectType, arms, result);

        return result;
    }

    /// <summary>
    /// 获取类型的所有可能变体
    /// 用于穷举性分析
    /// </summary>
    private List<string> GetAllVariants(ValkyrieType subjectType)
    {
        var variants = new List<string>();

        if (subjectType.Kind == TypeKind.Union && subjectType.GenericArgs.Count > 0)
        {
            foreach (var member in subjectType.GenericArgs)
            {
                variants.Add(member.Name);
            }
        }
        else if (subjectType.IsBool)
        {
            variants.Add("true");
            variants.Add("false");
        }
        else if (subjectType.Kind == TypeKind.Enum)
        {
            if (_typeRegistry.TryGetValue(subjectType.Name, out var enumType) && enumType.GenericArgs.Count > 0)
            {
                foreach (var variant in enumType.GenericArgs)
                {
                    variants.Add(variant.Name);
                }
            }
        }
        else if (subjectType.Kind == TypeKind.Nullable && subjectType.GenericArgs.Count > 0)
        {
            variants.Add(subjectType.GenericArgs[0].Name);
            variants.Add("null");
        }

        return variants;
    }

    /// <summary>
    /// 增强的 pattern 覆盖类型获取
    /// 返回一个 pattern 覆盖的所有类型名称
    /// </summary>
    private List<string> GetPatternCoveredTypes(MatchArm arm, ValkyrieType subjectType)
    {
        var covered = new List<string>();

        switch (arm.Pattern)
        {
            case WildcardPattern:
                covered.AddRange(GetAllVariants(subjectType));
                if (covered.Count == 0)
                {
                    covered.Add("wildcard");
                }
                break;

            case ConstantPattern cp:
                if (cp.Value is LiteralExpr { LiteralKind: LiteralType.Boolean, Value: "true" })
                {
                    covered.Add("true");
                }
                else if (cp.Value is LiteralExpr { LiteralKind: LiteralType.Boolean, Value: "false" })
                {
                    covered.Add("false");
                }
                else if (cp.Value is LiteralExpr { LiteralKind: LiteralType.Null })
                {
                    covered.Add("null");
                }
                else
                {
                    covered.Add($"const:{cp.Value}");
                }
                break;

            case DeclarationPattern dp:
                if (dp.TypeAnnotation is not null)
                {
                    var resolvedType = ResolveTypeAnnotation(dp.TypeAnnotation);
                    covered.Add(resolvedType.Name);
                }
                else
                {
                    covered.AddRange(GetAllVariants(subjectType));
                    if (covered.Count == 0)
                    {
                        covered.Add(subjectType.Name);
                    }
                }
                break;

            case TypePattern tp:
                covered.Add(tp.TypeAnnotation.Name);
                break;

            default:
                covered.Add(subjectType.Name);
                break;
        }

        return covered;
    }

    /// <summary>
    /// 检测冗余的 match arm
    /// 被前面 arm 完全覆盖的 arm 是冗余的
    /// </summary>
    private void DetectRedundantArms(ValkyrieType subjectType, IReadOnlyList<MatchArm> arms, ExhaustivenessResult result)
    {
        var cumulativeCovered = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < arms.Count; i++)
        {
            var arm = arms[i];

            if (arm.Pattern is WildcardPattern)
            {
                for (var j = i + 1; j < arms.Count; j++)
                {
                    result.RedundantArmIndices.Add(j);
                }

                break;
            }

            var armCovered = GetPatternCoveredTypes(arm, subjectType);
            var isRedundant = true;

            foreach (var c in armCovered)
            {
                if (!cumulativeCovered.Contains(c))
                {
                    isRedundant = false;
                    break;
                }
            }

            if (isRedundant && armCovered.Count > 0)
            {
                result.RedundantArmIndices.Add(i);
            }

            foreach (var c in armCovered)
            {
                cumulativeCovered.Add(c);
            }
        }
    }

    /// <summary>
    /// 增强的 Match 语句检查入口
    /// 替代原有的 CheckMatchStmt，提供更详细的诊断
    /// </summary>
    private void CheckMatchStmtEnhanced(MatchStmt matchStmt)
    {
        var subjectType = InferType(matchStmt.Expression);

        var exhaustiveness = CheckExhaustiveness(subjectType, matchStmt.Arms);

        if (!exhaustiveness.IsExhaustive && !subjectType.IsError)
        {
            var missingDesc = exhaustiveness.MissingArms.Count > 5
                ? string.Join(", ", exhaustiveness.MissingArms.Take(5)) + "..."
                : string.Join(", ", exhaustiveness.MissingArms);

            AddWarning("VALK2051",
                $"match 语句未穷举所有情况：类型 '{subjectType}' 缺少 {exhaustiveness.MissingArms.Count} 个分支（{missingDesc}）",
                matchStmt.Span, $"添加缺失的 case 分支或使用 case _ 作为默认分支");
        }

        foreach (var idx in exhaustiveness.RedundantArmIndices)
        {
            if (idx < matchStmt.Arms.Count)
            {
                AddWarning("VALK2054",
                    $"match arm 是冗余的，已被前面的 arm 覆盖",
                    matchStmt.Arms[idx].Pattern.Span);
            }
        }

        foreach (var arm in matchStmt.Arms)
        {
            var patternType = InferPatternType(arm.Pattern, subjectType);
            CheckPatternBinder(arm.Pattern, patternType, matchStmt.Expression);
            CheckPatternTypeCompatibility(arm.Pattern, patternType, subjectType);
            CheckBlockStmt(arm.Body);
        }
    }

    /// <summary>
    /// 检查 pattern 类型与 subject 类型的兼容性
    /// </summary>
    private void CheckPatternTypeCompatibility(AstNode pattern, ValkyrieType patternType, ValkyrieType subjectType)
    {
        if (patternType.IsError || subjectType.IsError || patternType.IsAuto || subjectType.IsAuto)
        {
            return;
        }

        if (pattern is TypePattern tp)
        {
            var resolvedPatternType = ResolveTypeAnnotation(tp.TypeAnnotation);
            if (!subjectType.IsAssignableFrom(resolvedPatternType) && !resolvedPatternType.IsAssignableFrom(subjectType))
            {
                AddError("VALK2086",
                    $"pattern 类型 '{resolvedPatternType}' 与 match subject 类型 '{subjectType}' 不兼容",
                    pattern.Span, "确保 pattern 类型是 subject 类型的子类型或成员");
            }
        }
        else if (pattern is DeclarationPattern dp && dp.TypeAnnotation is not null)
        {
            var resolvedPatternType = ResolveTypeAnnotation(dp.TypeAnnotation);
            if (!subjectType.IsAssignableFrom(resolvedPatternType) && !resolvedPatternType.IsAssignableFrom(subjectType))
            {
                AddError("VALK2086",
                    $"pattern 类型 '{resolvedPatternType}' 与 match subject 类型 '{subjectType}' 不兼容",
                    pattern.Span, "确保 pattern 类型是 subject 类型的子类型或成员");
            }
        }
        else if (pattern is ConstantPattern cp)
        {
            var constType = InferType(cp.Value);
            if (!subjectType.IsAssignableFrom(constType) && !constType.IsAssignableFrom(subjectType))
            {
                AddWarning("VALK2087",
                    $"常量 pattern 类型 '{constType}' 与 match subject 类型 '{subjectType}' 不匹配",
                    pattern.Span, "确保常量 pattern 的类型与 subject 类型一致");
            }
        }
    }

    #endregion
}
