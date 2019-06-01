using System.Collections.Immutable;
using System.Globalization;
using Nyar.IR.EGraph;
using Nyar.IR.Intent;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Type;
using Valkyrie.Compiler.Hir;

namespace Valkyrie.Compiler.Mir;

/// <summary>
/// 将 HIR 降级为最小可运行的 MIR（EGraph + IKun）。
/// </summary>
public sealed class MirBuilder
{
    public MirModule Build(HirModule hir, string targetArchTag)
    {
        var graph = new EGraph<IKun>();
        var members = new List<Id>(hir.Functions.Count);

        foreach (var function in hir.Functions)
        {
            members.Add(BuildFunction(function, graph, targetArchTag));
        }

        var moduleId = graph.Add(IKunBuilder.Module(hir.Name, members.ToImmutableArray()));
        graph.Rebuild();
        return new MirModule(hir.Name, graph, moduleId);
    }

    private static Id BuildFunction(HirFunction function, EGraph<IKun> graph, string targetArchTag)
    {
        var bodyId = BuildBlock(function.Syntax.Body, graph, targetArchTag);
        var parameterNames = function.Parameters.Select(parameter => parameter.Name).ToImmutableArray();
        var lambdaId = graph.Add(IKunBuilder.Lambda(parameterNames, bodyId));
        return graph.Add(IKunBuilder.Export(function.Name, lambdaId));
    }

    private static Id BuildBlock(BlockStmt? block, EGraph<IKun> graph, string targetArchTag)
    {
        if (block is null)
        {
            return graph.Add(IKunBuilder.None());
        }

        var statementIds = block.Statements
            .Select(statement => BuildStatement(statement, graph, targetArchTag))
            .ToArray();

        return BuildSequence(statementIds, graph);
    }

    private static Id BuildSequence(IReadOnlyList<Id> ids, EGraph<IKun> graph)
    {
        if (ids.Count == 0)
        {
            return graph.Add(IKunBuilder.None());
        }

        if (ids.Count == 1)
        {
            return ids[0];
        }

        return graph.Add(IKunBuilder.Seq(ids.ToImmutableArray()));
    }

    private static Id BuildStatement(ValkyrieNode statement, EGraph<IKun> graph, string targetArchTag)
    {
        return statement switch
        {
            ReturnStatement ret => graph.Add(IKunBuilder.Return(BuildExpressionOrNone(ret.Value, graph, targetArchTag))),
            TermNode term => BuildExpressionOrNone(term.Expression, graph, targetArchTag),
            BlockStmt block => BuildBlock(block, graph, targetArchTag),
            LetDeclaration let => BuildLetDeclaration(let, graph, targetArchTag),
            MatchTemplate matchTemplate => BuildMatchTemplate(matchTemplate, graph, targetArchTag),
            _ => graph.Add(IKunBuilder.None())
        };
    }

    private static Id BuildExpressionOrNone(ValkyrieNode? node, EGraph<IKun> graph, string targetArchTag)
    {
        if (node is null)
        {
            return graph.Add(IKunBuilder.None());
        }

        return BuildExpression(node, graph, targetArchTag);
    }

    private static Id BuildExpression(ValkyrieNode expression, EGraph<IKun> graph, string targetArchTag)
    {
        return expression switch
        {
            TermAtomicLiteral literal => BuildLiteral(literal, graph),
            IdentifierNode ident => graph.Add(IKunBuilder.Symbol(ident.Name)),
            TermBinaryExpression binary => graph.Add(IKunBuilder.BinaryOp(
                binary.Operator,
                BuildExpression(binary.Left, graph, targetArchTag),
                BuildExpression(binary.Right, graph, targetArchTag))),
            TermUnaryExpression unary => graph.Add(IKunBuilder.UnaryOp(
                unary.Operator,
                BuildExpression(unary.Operand, graph, targetArchTag))),
            TermCallExpression call => graph.Add(IKunBuilder.Apply(
                BuildCallCalleeExpression(call.Callee, graph, targetArchTag),
                call.Arguments.Select(argument => BuildExpression(argument, graph, targetArchTag)).ToImmutableArray())),
            TermArrayLiteral array => graph.Add(IKunBuilder.ArrayLiteral(
                array.Elements.Select(element => BuildExpression(element, graph, targetArchTag)).ToImmutableArray())),
            AssignmentExpr assignment => BuildAssignment(assignment, graph, targetArchTag),
            TermNode term => BuildExpression(term.Expression, graph, targetArchTag),
            LetDeclaration let => BuildLetDeclaration(let, graph, targetArchTag),
            BlockStmt block => BuildBlock(block, graph, targetArchTag),
            ReturnStatement ret => graph.Add(IKunBuilder.Return(BuildExpressionOrNone(ret.Value, graph, targetArchTag))),
            _ => graph.Add(IKunBuilder.None())
        };
    }

    private static Id BuildCallCalleeExpression(ValkyrieNode callee, EGraph<IKun> graph, string targetArchTag)
    {
        if (TryResolveQualifiedName(callee, out var qualifiedName))
        {
            return graph.Add(IKunBuilder.Symbol(qualifiedName));
        }

        return BuildExpression(callee, graph, targetArchTag);
    }

    private static bool TryResolveQualifiedName(ValkyrieNode node, out string qualifiedName)
    {
        switch (node)
        {
            case IdentifierNode identifier when !string.IsNullOrWhiteSpace(identifier.Name):
                qualifiedName = identifier.Name;
                return true;
            case QualifiedPath path when !string.IsNullOrWhiteSpace(path.FullName):
                qualifiedName = path.FullName;
                return true;
            case TermDotExpression dot:
                if (TryResolveQualifiedName(dot.Target, out var targetName))
                {
                    qualifiedName = $"{targetName}.{dot.MemberName}";
                    return true;
                }

                break;
        }

        qualifiedName = string.Empty;
        return false;
    }

    private static Id BuildMatchTemplate(MatchTemplate template, EGraph<IKun> graph, string targetArchTag)
    {
        if (TrySelectMatchArm(template, targetArchTag, out var selectedBody))
        {
            var selectedIds = selectedBody.Select(node => BuildStatement(node, graph, targetArchTag)).ToArray();
            return BuildSequence(selectedIds, graph);
        }

        return graph.Add(IKunBuilder.None());
    }

    private static bool TrySelectMatchArm(MatchTemplate template, string targetArchTag, out IReadOnlyList<ValkyrieNode> body)
    {
        foreach (var arm in template.Arms)
        {
            if (string.Equals(arm.Condition.Name, targetArchTag, StringComparison.OrdinalIgnoreCase))
            {
                body = arm.Body;
                return true;
            }
        }

        if (template.DefaultBody is { Count: > 0 })
        {
            body = template.DefaultBody;
            return true;
        }

        body = Array.Empty<ValkyrieNode>();
        return false;
    }

    private static Id BuildAssignment(AssignmentExpr assignment, EGraph<IKun> graph, string targetArchTag)
    {
        var targetId = BuildExpression(assignment.Target, graph, targetArchTag);
        var valueId = BuildExpression(assignment.Value, graph, targetArchTag);

        if (assignment.Operator == "=")
        {
            return graph.Add(IKunBuilder.StateUpdate(targetId, valueId));
        }

        var op = assignment.Operator[..^1];
        var computedId = graph.Add(IKunBuilder.BinaryOp(op, targetId, valueId));
        return graph.Add(IKunBuilder.StateUpdate(targetId, computedId));
    }

    private static Id BuildLetDeclaration(LetDeclaration declaration, EGraph<IKun> graph, string targetArchTag)
    {
        Id? typeId = declaration.VarType is null
            ? null
            : graph.Add(IKunBuilder.TypeRef(declaration.VarType.Name, ImmutableArray<Id>.Empty));
        Id? valueId = declaration.Initializer is null
            ? null
            : BuildExpression(declaration.Initializer, graph, targetArchTag);

        return graph.Add(IKunBuilder.VarDecl(
            ImmutableArray<Id>.Empty,
            declaration.Modifiers.ToImmutableArray(),
            declaration.Name,
            typeId,
            valueId));
    }

    private static Id BuildLiteral(TermAtomicLiteral literal, EGraph<IKun> graph)
    {
        if (literal.LiteralKind == LiteralType.Number)
        {
            var text = literal.Value?.ToString() ?? "0";
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
            {
                return graph.Add(IKunBuilder.Constant(integerValue));
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                return graph.Add(IKunBuilder.FloatConstant(BitConverter.DoubleToUInt64Bits(floatValue)));
            }

            return graph.Add(IKunBuilder.Constant(0));
        }

        if (literal.LiteralKind == LiteralType.String)
        {
            return graph.Add(IKunBuilder.StringConstant(literal.Value?.ToString() ?? string.Empty));
        }

        if (literal.LiteralKind == LiteralType.Boolean)
        {
            var text = literal.Value?.ToString();
            var value = bool.TryParse(text, out var parsed) && parsed;
            return graph.Add(IKunBuilder.BooleanConstant(value));
        }

        return graph.Add(IKunBuilder.None());
    }
}
