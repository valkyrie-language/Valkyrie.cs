using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;

namespace Valkyrie.Linter.Rules;

public partial class CodeQualityRules
{
    private static IEnumerable<AstNode> GetChildren(AstNode node)
    {
        return node switch
        {
            CompilationUnit cu => cu.Declarations,
            BlockStmt block => block.Statements,
            IfStatement ifStmt => ifStmt.ElseBlock is not null
                ? [ifStmt.Condition, ifStmt.ThenBlock, ifStmt.ElseBlock]
                : [ifStmt.Condition, ifStmt.ThenBlock],
            WhileStatement whileStmt => [whileStmt.Condition, whileStmt.Body],
            LoopStmt loopStmt => loopStmt.Iterable is not null
                ? [loopStmt.Iterable, loopStmt.Body]
                : [loopStmt.Body],
            BinaryExpr binary => [binary.Left, binary.Right],
            TermCallExpression call => call.Arguments.Cast<AstNode>().Prepend(call.Callee),
            OrdinalIndexExpression ordinalIndex => ordinalIndex.Indices.Prepend(ordinalIndex.Target),
            OffsetIndexExpression offsetIndex => offsetIndex.Indices.Prepend(offsetIndex.Target),
            TermIndexExpression index => [index.Target, index.Index],
            MemberAccessExpr member => [member.Target],
            AssignmentExpr assign => [assign.Target, assign.Value],
            TermUnaryExpression unary => [unary.Operand],
            LambdaExpr lambda => [lambda.Body],
            MatchStmt match => [match.Expression],
            _ => []
        };
    }
}
