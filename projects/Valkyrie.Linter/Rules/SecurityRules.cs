using Oak.Valkyrie.AST.Term;

namespace Valkyrie.Linter.Rules;

public partial class SecurityRules
{
    private static bool HasPotentialOutOfBounds(AstNode node)
    {
        return node switch
        {
            TermIndexExpression indexExpr => HasPotentialOutOfBounds(indexExpr.Target, indexExpr.Index),
            OrdinalIndexExpression ordinalIndex => ordinalIndex.Indices.Any(IsUnsafeIndex) || HasPotentialOutOfBounds(ordinalIndex.Target),
            OffsetIndexExpression offsetIndex => offsetIndex.Indices.Any(IsUnsafeIndex) || HasPotentialOutOfBounds(offsetIndex.Target),
            _ => false
        };
    }

    private static bool HasPotentialOutOfBounds(AstNode target, AstNode index)
    {
        return IsUnsafeIndex(index) || HasPotentialOutOfBounds(target);
    }

    private static bool IsUnsafeIndex(AstNode index)
    {
        return index is IdentifierNode or BinaryExpr;
    }
}
