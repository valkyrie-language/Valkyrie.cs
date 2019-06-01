using Oak.Valkyrie.AST.Term;

namespace Valkyrie.Formatter;

public partial class CodeFormatter
{
    private void FormatExpression(AstNode node)
    {
        switch (node)
        {
            case LiteralExpr lit:
                FormatLiteralExpr(lit);
                break;
            case IdentifierNode ident:
                Write(ident.Name);
                break;
            case BinaryExpr binary:
                FormatBinaryExpr(binary);
                break;
            case AssignmentExpr assign:
                FormatAssignmentExpr(assign);
                break;
            case MemberAccessExpr member:
                FormatMemberAccessExpr(member);
                break;
            case QualifiedPathExpr path:
                FormatQualifiedPathExpr(path);
                break;
            case TermCallExpression call:
                FormatCallExpr(call);
                break;
            case OrdinalIndexExpression ordinalIndex:
                FormatOrdinalIndexExpr(ordinalIndex);
                break;
            case OffsetIndexExpression offsetIndex:
                FormatOffsetIndexExpr(offsetIndex);
                break;
            case TermIndexExpression index:
                FormatIndexExpr(index);
                break;
            case TermUnaryExpression unary:
                FormatUnaryExpr(unary);
                break;
            case LambdaExpr lambda:
                FormatLambdaExpr(lambda);
                break;
            case QueryExpr query:
                FormatQueryExpr(query);
                break;
            case MetaBlock meta:
                Write("<% ");
                Write(meta.Content);
                Write(" %>");
                break;
            case SwizzleExpr swizzle:
                FormatSwizzleExpr(swizzle);
                break;
            default:
                break;
        }
    }

    private void FormatOrdinalIndexExpr(OrdinalIndexExpression index)
    {
        FormatIndexTarget(index.Target);
        Write("[");
        for (var i = 0; i < index.Indices.Count; i++)
        {
            if (i > 0)
            {
                Write(", ");
            }

            FormatExpression(index.Indices[i]);
        }
        Write("]");
    }

    private void FormatOffsetIndexExpr(OffsetIndexExpression index)
    {
        FormatIndexTarget(index.Target);
        Write("⁅");
        for (var i = 0; i < index.Indices.Count; i++)
        {
            if (i > 0)
            {
                Write(", ");
            }

            FormatExpression(index.Indices[i]);
        }
        Write("⁆");
    }

    private void FormatIndexExpr(TermIndexExpression index)
    {
        FormatIndexTarget(index.Target);
        Write("[");
        FormatExpression(index.Index);
        Write("]");
    }

    private void FormatIndexTarget(AstNode target)
    {
        switch (target)
        {
            case IdentifierNode ident:
                Write(ident.Name);
                break;
            default:
                FormatExpression(target);
                break;
        }
    }
}
