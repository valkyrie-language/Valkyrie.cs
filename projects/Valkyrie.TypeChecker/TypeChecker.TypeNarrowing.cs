using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;

namespace Valkyrie.TypeChecker;

/// <summary>
/// 类型窄化与类型测试支持
/// 在 if/match 条件中的类型测试后，窄化变量类型
/// 支持 is 表达式推断、as 表达式推断、显式转型检查
/// </summary>
public sealed partial class TypeChecker
{
    #region 类型窄化

    /// <summary>
    /// 类型窄化上下文：记录在条件分支中窄化的变量
    /// </summary>
    private readonly Dictionary<string, ValkyrieType> _narrowedTypes = new(StringComparer.Ordinal);

    /// <summary>
    /// 在 if 语句的条件分支中应用类型窄化
    /// 如果条件包含类型测试（如 x is i32），则在 then 块中窄化 x 的类型
    /// </summary>
    private void ApplyTypeNarrowingInIf(IfStatement ifStmt)
    {
        var savedNarrowed = new Dictionary<string, ValkyrieType>(_narrowedTypes, StringComparer.Ordinal);

        var narrowingInfo = AnalyzeConditionForNarrowing(ifStmt.Condition);
        foreach (var (varName, narrowedType) in narrowingInfo.PositiveNarrowing)
        {
            _narrowedTypes[varName] = narrowedType;
        }

        CheckBlockStmt(ifStmt.ThenBlock);

        _narrowedTypes.Clear();
        foreach (var kv in savedNarrowed)
        {
            _narrowedTypes[kv.Key] = kv.Value;
        }

        if (ifStmt.ElseBlock is not null)
        {
            var savedNarrowed2 = new Dictionary<string, ValkyrieType>(_narrowedTypes, StringComparer.Ordinal);

            foreach (var (varName, narrowedType) in narrowingInfo.NegativeNarrowing)
            {
                _narrowedTypes[varName] = narrowedType;
            }

            if (ifStmt.ElseBlock is BlockStmt elseBlock)
            {
                CheckBlockStmt(elseBlock);
            }
            else if (ifStmt.ElseBlock is IfStatement elseIf)
            {
                CheckIfStmt(elseIf);
            }

            _narrowedTypes.Clear();
            foreach (var kv in savedNarrowed2)
            {
                _narrowedTypes[kv.Key] = kv.Value;
            }
        }
    }

    /// <summary>
    /// 条件窄化分析结果
    /// </summary>
    private sealed class NarrowingInfo
    {
        /// <summary>条件为真时的窄化映射</summary>
        public Dictionary<string, ValkyrieType> PositiveNarrowing { get; } = new(StringComparer.Ordinal);

        /// <summary>条件为假时的窄化映射</summary>
        public Dictionary<string, ValkyrieType> NegativeNarrowing { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// 分析条件表达式中的类型窄化机会
    /// 识别 x is T、x != null、x == null 等模式
    /// </summary>
    private NarrowingInfo AnalyzeConditionForNarrowing(AstNode condition)
    {
        var info = new NarrowingInfo();

        switch (condition)
        {
            case BinaryExpr binary:
                AnalyzeBinaryConditionForNarrowing(binary, info);
                break;

            case TermUnaryExpression { Operator: "!", IsPrefix: true } unary:
                var innerInfo = AnalyzeConditionForNarrowing(unary.Operand);
                foreach (var kv in innerInfo.PositiveNarrowing)
                {
                    info.NegativeNarrowing[kv.Key] = kv.Value;
                }

                foreach (var kv in innerInfo.NegativeNarrowing)
                {
                    info.PositiveNarrowing[kv.Key] = kv.Value;
                }
                break;

            default:
                break;
        }

        return info;
    }

    /// <summary>
    /// 分析二元条件表达式中的类型窄化
    /// </summary>
    private void AnalyzeBinaryConditionForNarrowing(BinaryExpr binary, NarrowingInfo info)
    {
        if (binary.Operator == "&&")
        {
            var leftInfo = AnalyzeConditionForNarrowing(binary.Left);
            var rightInfo = AnalyzeConditionForNarrowing(binary.Right);

            foreach (var kv in leftInfo.PositiveNarrowing)
            {
                info.PositiveNarrowing[kv.Key] = kv.Value;
            }

            foreach (var kv in rightInfo.PositiveNarrowing)
            {
                info.PositiveNarrowing[kv.Key] = kv.Value;
            }
        }
        else if (binary.Operator == "||")
        {
            var leftInfo = AnalyzeConditionForNarrowing(binary.Left);
            var rightInfo = AnalyzeConditionForNarrowing(binary.Right);

            foreach (var kv in leftInfo.NegativeNarrowing)
            {
                info.NegativeNarrowing[kv.Key] = kv.Value;
            }

            foreach (var kv in rightInfo.NegativeNarrowing)
            {
                info.NegativeNarrowing[kv.Key] = kv.Value;
            }
        }
        else if (binary.Operator == "!=")
        {
            AnalyzeNullComparisonForNarrowing(binary, isNullCheck: false, info);
        }
        else if (binary.Operator == "==")
        {
            AnalyzeNullComparisonForNarrowing(binary, isNullCheck: true, info);
        }
    }

    /// <summary>
    /// 分析 null 比较中的类型窄化
    /// x != null → 正向窄化：去掉 nullable
    /// x == null → 反向窄化：确认 nullable
    /// </summary>
    private void AnalyzeNullComparisonForNarrowing(BinaryExpr binary, bool isNullCheck, NarrowingInfo info)
    {
        if (binary.Right is LiteralExpr { LiteralKind: LiteralType.Null })
        {
            var varName = GetVariableName(binary.Left);
            if (varName is not null)
            {
                var varType = ResolveVariableType(varName);
                if (varType.Kind == TypeKind.Nullable && varType.GenericArgs.Count > 0)
                {
                    var innerType = varType.GenericArgs[0];
                    if (isNullCheck)
                    {
                        info.NegativeNarrowing[varName] = innerType;
                    }
                    else
                    {
                        info.PositiveNarrowing[varName] = innerType;
                    }
                }
            }
        }
        else if (binary.Left is LiteralExpr { LiteralKind: LiteralType.Null })
        {
            var varName = GetVariableName(binary.Right);
            if (varName is not null)
            {
                var varType = ResolveVariableType(varName);
                if (varType.Kind == TypeKind.Nullable && varType.GenericArgs.Count > 0)
                {
                    var innerType = varType.GenericArgs[0];
                    if (isNullCheck)
                    {
                        info.NegativeNarrowing[varName] = innerType;
                    }
                    else
                    {
                        info.PositiveNarrowing[varName] = innerType;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取表达式中的变量名（仅支持简单标识符）
    /// </summary>
    private static string? GetVariableName(AstNode expr)
    {
        return expr switch
        {
            IdentifierNode ident => ident.Name,
            _ => null
        };
    }

    /// <summary>
    /// 解析变量的当前类型（考虑窄化）
    /// </summary>
    private ValkyrieType ResolveVariableType(string varName)
    {
        if (_narrowedTypes.TryGetValue(varName, out var narrowedType))
        {
            return narrowedType;
        }

        var symbol = _currentScope.Resolve(varName);
        return symbol?.Type ?? ValkyrieType.Error;
    }

    #endregion

    #region 类型测试表达式推断

    /// <summary>
    /// 推断 is 表达式的类型
    /// x is T 返回 bool，同时在条件上下文中窄化 x 的类型为 T
    /// </summary>
    private ValkyrieType InferIsType(BinaryExpr binary)
    {
        if (binary.Operator != "is")
        {
            return ValkyrieType.Error;
        }

        var leftType = InferType(binary.Left);
        var rightType = ResolveTypeFromIsOperand(binary.Right);

        if (leftType.IsError || rightType.IsError)
        {
            return ValkyrieType.Bool;
        }

        if (!leftType.CanNarrowTo(rightType))
        {
            AddWarning("VALK2088",
                $"类型测试 '{leftType} is {rightType}' 永远为 false，因为 '{leftType}' 不可能为 '{rightType}'",
                binary.Span, "检查类型测试是否正确");
        }

        return ValkyrieType.Bool;
    }

    /// <summary>
    /// 推断 as 表达式的类型
    /// x as T 返回 T?（可空类型），如果转换失败返回 null
    /// </summary>
    private ValkyrieType InferAsType(BinaryExpr binary)
    {
        if (binary.Operator != "as")
        {
            return ValkyrieType.Error;
        }

        var leftType = InferType(binary.Left);
        var rightType = ResolveTypeFromIsOperand(binary.Right);

        if (leftType.IsError || rightType.IsError)
        {
            return rightType;
        }

        if (!leftType.CanNarrowTo(rightType) && !rightType.IsAssignableFrom(leftType))
        {
            AddWarning("VALK2089",
                $"类型转换 '{leftType} as {rightType}' 永远返回 null",
                binary.Span, "检查类型转换是否正确，或使用显式类型转换");
        }

        return MakeNullable(rightType);
    }

    /// <summary>
    /// 从 is/as 操作的右侧操作数解析类型
    /// 支持标识符和类型注解两种形式
    /// </summary>
    private ValkyrieType ResolveTypeFromIsOperand(AstNode operand)
    {
        if (operand is IdentifierNode ident)
        {
            if (IsPrimitiveTypeName(ident.Name))
            {
                return ResolvePrimitiveType(ident.Name);
            }

            if (_typeRegistry.TryGetValue(ident.Name, out var registeredType))
            {
                return registeredType;
            }

            AddError("VALK2090",
                $"类型测试中引用了未定义的类型 '{ident.Name}'",
                ident.Span, "确保类型已定义");
            return ValkyrieType.Error;
        }

        return InferType(operand);
    }

    /// <summary>
    /// 检查显式类型转换的合法性
    /// 支持数值类型之间的转换、nullable 解包、引用类型向下转型
    /// </summary>
    private ValkyrieType CheckExplicitCast(ValkyrieType sourceType, ValkyrieType targetType, TextSpan span)
    {
        if (sourceType.IsError || targetType.IsError)
        {
            return targetType;
        }

        if (targetType.IsAssignableFrom(sourceType))
        {
            return targetType;
        }

        if (sourceType.IsNumeric && targetType.IsNumeric)
        {
            return targetType;
        }

        if (sourceType.Kind == TypeKind.Nullable && targetType.IsAssignableFrom(sourceType.GenericArgs[0]))
        {
            return targetType;
        }

        if (sourceType.Kind == TypeKind.Union)
        {
            foreach (var member in sourceType.GenericArgs)
            {
                if (targetType.IsAssignableFrom(member) || member.IsAssignableFrom(targetType))
                {
                    return targetType;
                }
            }
        }

        AddError("VALK2091",
            $"不允许从 '{sourceType}' 到 '{targetType}' 的显式类型转换",
            span, "确保类型之间有合法的转换路径");

        return targetType;
    }

    #endregion
}
