using Nyar.Semantic;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Valkyrie.Analyzer;

/// <summary>
///     Valkyrie 类型检查桥接实现。
/// </summary>
public sealed class ValkyrieTypeCheckerImpl : Nyar.Semantic.TypeChecker
{
    private readonly Dictionary<int, IType> _expressionTypes;
    private readonly Valkyrie.TypeChecker.TypeChecker _inner;

    public ValkyrieTypeCheckerImpl()
    {
        _inner = new Valkyrie.TypeChecker.TypeChecker();
        Bridge = new ValkyrieSemanticBridge();
        _expressionTypes = new Dictionary<int, IType>();
    }

    public ValkyrieSemanticBridge Bridge { get; }

    public SemanticModel CheckCompilationUnit(CompilationUnit compilationUnit, string? filePath = null)
    {
        var checkedResult = _inner.Check(compilationUnit, filePath);
        return Bridge.BuildSemanticModel(checkedResult, compilationUnit, filePath ?? string.Empty);
    }

    public override IType InferType(ISymbol symbol)
    {
        if (symbol.Type is null)
        {
            return UnknownType.Instance;
        }

        return symbol.Type;
    }

    public override IType InferTypeOfExpression(object node)
    {
        if (node is not ValkyrieNode expression)
        {
            return UnknownType.Instance;
        }

        if (_expressionTypes.TryGetValue(expression.GetHashCode(), out var cached))
        {
            return cached;
        }

        var result = InferTypeCore(expression);
        _expressionTypes[expression.GetHashCode()] = result;
        return result;
    }

    public override bool CheckType(IType expected, IType actual, out SemanticDiagnostic? diagnostic)
    {
        if (expected.IsAssignableFrom(actual))
        {
            diagnostic = null;
            return true;
        }

        diagnostic = TypeMismatch(expected, actual, new TextSpan(0, 0));
        return false;
    }

    public override IReadOnlyList<SemanticDiagnostic> CheckAllTypes(SemanticModel model)
    {
        return [];
    }

    private IType InferTypeCore(ValkyrieNode node)
    {
        return node switch
        {
            TermAtomicLiteral literal => InferLiteralType(literal),
            IdentifierNode identifier => InferIdentifierType(identifier),
            TermBinaryExpression binary => InferBinaryType(binary),
            AssignmentExpr assignment => InferTypeOfExpression(assignment.Target),
            TermUnaryExpression unary => InferTypeOfExpression(unary.Operand),
            TermCallExpression call => InferCallType(call),
            TermOrdinalExpression ordinal => InferIndexType(ordinal.Target),
            TermOffsetExpression offset => InferIndexType(offset.Target),
            TermDotExpression dot => InferMemberAccessType(dot),
            QualifiedPath => UnknownType.Instance,
            QueryExpr => UnknownType.Instance,
            SwizzleExpr => UnknownType.Instance,
            LetDeclaration letDecl when letDecl.VarType is not null => Bridge.ConvertTypeAnnotation(letDecl.VarType),
            ComponentDeclaration component => new NamedType(component.Name, "component"),
            SystemDeclaration system => new NamedType(system.Name, "system"),
            MicroDeclaration micro => BuildFunctionType(micro),
            WidgetDecl widget => new NamedType(widget.Name, "widget"),
            _ => UnknownType.Instance
        };
    }

    private IType InferLiteralType(TermAtomicLiteral literal)
    {
        return literal.LiteralKind switch
        {
            LiteralType.Number => InferNumericLiteralType(literal.Value),
            LiteralType.String => new PrimitiveType("string"),
            LiteralType.Boolean => new PrimitiveType("bool"),
            LiteralType.Null => new NullableType(UnknownType.Instance),
            _ => UnknownType.Instance
        };
    }

    private static IType InferNumericLiteralType(object? value)
    {
        if (value is string text)
        {
            if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
            {
                return new PrimitiveType("f64");
            }

            return new PrimitiveType("i32");
        }

        return value switch
        {
            int => new PrimitiveType("i32"),
            long => new PrimitiveType("i64"),
            float => new PrimitiveType("f32"),
            double => new PrimitiveType("f64"),
            _ => new PrimitiveType("i32")
        };
    }

    private static IType InferIdentifierType(IdentifierNode identifier)
    {
        return identifier.Name switch
        {
            "i8" => new PrimitiveType("i8"),
            "i16" => new PrimitiveType("i16"),
            "i32" => new PrimitiveType("i32"),
            "i64" => new PrimitiveType("i64"),
            "u8" => new PrimitiveType("u8"),
            "u16" => new PrimitiveType("u16"),
            "u32" => new PrimitiveType("u32"),
            "u64" => new PrimitiveType("u64"),
            "f32" => new PrimitiveType("f32"),
            "f64" => new PrimitiveType("f64"),
            "bool" => new PrimitiveType("bool"),
            "string" => new PrimitiveType("string"),
            "void" => new PrimitiveType("void"),
            "true" => new PrimitiveType("bool"),
            "false" => new PrimitiveType("bool"),
            "null" => new NullableType(UnknownType.Instance),
            _ => UnknownType.Instance
        };
    }

    private IType InferBinaryType(TermBinaryExpression binary)
    {
        var leftType = InferTypeOfExpression(binary.Left);
        var rightType = InferTypeOfExpression(binary.Right);

        return binary.Operator switch
        {
            "==" or "!=" or "<" or ">" or "<=" or ">=" => new PrimitiveType("bool"),
            "&&" or "||" => new PrimitiveType("bool"),
            "+" when leftType is PrimitiveType { Name: "string" } || rightType is PrimitiveType { Name: "string" } => new PrimitiveType("string"),
            "+" or "-" or "*" or "/" or "%" => InferArithmeticResultType(leftType, rightType),
            _ => UnknownType.Instance
        };
    }

    private static IType InferArithmeticResultType(IType left, IType right)
    {
        if (left is PrimitiveType { Name: "f64" } || right is PrimitiveType { Name: "f64" })
        {
            return new PrimitiveType("f64");
        }

        if (left is PrimitiveType { Name: "f32" } || right is PrimitiveType { Name: "f32" })
        {
            return new PrimitiveType("f32");
        }

        if (left is PrimitiveType { Name: "i64" } || right is PrimitiveType { Name: "i64" })
        {
            return new PrimitiveType("i64");
        }

        if (left is PrimitiveType { Name: "i32" } && right is PrimitiveType { Name: "i32" })
        {
            return new PrimitiveType("i32");
        }

        return UnknownType.Instance;
    }

    private IType InferCallType(TermCallExpression call)
    {
        var calleeType = InferTypeOfExpression(call.Callee);
        if (calleeType is FunctionType functionType)
        {
            return functionType.ReturnType;
        }

        return UnknownType.Instance;
    }

    private IType InferMemberAccessType(TermDotExpression dot)
    {
        var targetType = InferTypeOfExpression(dot.Target);
        if (targetType is NamedType namedType)
        {
            foreach (var member in namedType.Members)
            {
                if (member.Name == dot.MemberName)
                {
                    return member.Type ?? UnknownType.Instance;
                }
            }
        }

        return UnknownType.Instance;
    }

    private IType InferIndexType(ValkyrieNode target)
    {
        var targetType = InferTypeOfExpression(target);
        if (targetType is ArrayType arrayType)
        {
            return arrayType.ElementType;
        }

        if (targetType is GenericType genericType && genericType.TypeArguments.Count > 0)
        {
            return genericType.TypeArguments[0];
        }

        return UnknownType.Instance;
    }

    private IType BuildFunctionType(MicroDeclaration micro)
    {
        var parameterTypes = new List<IType>(micro.Parameters.Count);
        foreach (var parameter in micro.Parameters)
        {
            parameterTypes.Add(Bridge.ConvertTypeAnnotation(parameter.ParamType));
        }

        var returnType = micro.ReturnType is not null
            ? Bridge.ConvertTypeAnnotation(micro.ReturnType)
            : new PrimitiveType("void");

        return new FunctionType(parameterTypes, returnType);
    }
}
