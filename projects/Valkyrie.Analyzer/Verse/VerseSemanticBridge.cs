using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Verse.AST;
using Nyar.Semantic;

namespace Valkyrie.Analyzer.Verse;

/// <summary>
///     Verse 类型种类
/// </summary>
public enum VerseTypeKind
{
    Unknown,
    Null,
    Bool,
    I32,
    I64,
    F32,
    F64,
    String,
    Scene,
    Label,
    Command
}

/// <summary>
///     Verse 语义桥接器 —— 将 Verse AST 转换为 Nyar 语义模型
/// </summary>
public sealed class VerseSemanticBridge
{
    private readonly Dictionary<string, Symbol> _labelSymbols;
    private readonly Dictionary<string, IType> _labelTypes;
    private readonly Dictionary<string, Symbol> _sceneSymbols;
    private readonly Dictionary<string, IType> _sceneTypes;

    public VerseSemanticBridge()
    {
        _sceneTypes = new Dictionary<string, IType>();
        _labelTypes = new Dictionary<string, IType>();
        _sceneSymbols = new Dictionary<string, Symbol>();
        _labelSymbols = new Dictionary<string, Symbol>();
    }

    #region 公共 API

    public SemanticModel BuildSemanticModel(CompilationUnit compilationUnit, string filePath)
    {
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(filePath, symbolTable);

        IndexDeclarations(compilationUnit, globalScope, filePath, model);
        ValidateReferences(compilationUnit, globalScope, filePath, model);
        InferAllExpressionTypes(compilationUnit, globalScope, model);

        return model;
    }

    public IType ConvertVerseType(VerseTypeKind kind, string? name = null)
    {
        return kind switch
        {
            VerseTypeKind.Unknown => UnknownType.Instance,
            VerseTypeKind.Null => new NullableType(UnknownType.Instance),
            VerseTypeKind.Bool => new PrimitiveType("bool"),
            VerseTypeKind.I32 => new PrimitiveType("i32"),
            VerseTypeKind.I64 => new PrimitiveType("i64"),
            VerseTypeKind.F32 => new PrimitiveType("f32"),
            VerseTypeKind.F64 => new PrimitiveType("f64"),
            VerseTypeKind.String => new PrimitiveType("string"),
            VerseTypeKind.Scene => new NamedType(name ?? "scene", "scene"),
            VerseTypeKind.Label => new NamedType(name ?? "label", "label"),
            VerseTypeKind.Command => new NamedType(name ?? "command", "command"),
            _ => UnknownType.Instance
        };
    }

    #endregion

    #region 声明索引

    private void IndexDeclarations(CompilationUnit ast, Scope globalScope, string filePath, SemanticModel model)
    {
        foreach (var decl in ast.Declarations) IndexDeclaration(decl, globalScope, filePath, model);
    }

    private void IndexDeclaration(AstNode node, Scope scope, string filePath, SemanticModel model)
    {
        switch (node)
        {
            case SceneDecl scene:
                IndexSceneDecl(scene, scope, filePath, model);
                break;
            case LabelDecl label:
                IndexLabelDecl(label, scope, filePath, model);
                break;
        }
    }

    private void IndexSceneDecl(SceneDecl scene, Scope scope, string filePath, SemanticModel model)
    {
        if (_sceneSymbols.ContainsKey(scene.Name))
        {
            model.AddDiagnostic(new SemanticDiagnostic(
                DiagnosticLevel.Error,
                $"重复的场景定义 '{scene.Name}'",
                scene.Span,
                "VS0401",
                default,
                filePath));
            return;
        }

        var sceneType = ConvertVerseType(VerseTypeKind.Scene, scene.Name);
        _sceneTypes[scene.Name] = sceneType;

        var symbol = new Symbol(
            scene.Name,
            SymbolKind.Class,
            SymbolAccessibility.Public,
            sceneType,
            scope,
            definitionSourceSpan: ToSourceSpan(scene.Span, filePath),
            filePath: filePath);
        _sceneSymbols[scene.Name] = symbol;
        scope.Define(symbol);
        model.BindSymbol(scene.GetHashCode(), symbol);
        model.BindType(scene.GetHashCode(), sceneType);

        var sceneScope = scope.GetOrCreateChildScope(scene.Name);

        foreach (var stmt in scene.Body) IndexStatementSymbols(stmt, sceneScope, filePath, model);
    }

    private void IndexLabelDecl(LabelDecl label, Scope scope, string filePath, SemanticModel model)
    {
        if (_labelSymbols.ContainsKey(label.Name))
        {
            model.AddDiagnostic(new SemanticDiagnostic(
                DiagnosticLevel.Error,
                $"重复的标签定义 '{label.Name}'",
                label.Span,
                "VS0402",
                default,
                filePath));
            return;
        }

        var labelType = ConvertVerseType(VerseTypeKind.Label, label.Name);
        _labelTypes[label.Name] = labelType;

        var symbol = new Symbol(
            label.Name,
            SymbolKind.Method,
            SymbolAccessibility.Public,
            labelType,
            scope,
            definitionSourceSpan: ToSourceSpan(label.Span, filePath),
            filePath: filePath);
        _labelSymbols[label.Name] = symbol;
        scope.Define(symbol);
        model.BindSymbol(label.GetHashCode(), symbol);
        model.BindType(label.GetHashCode(), labelType);
    }

    private void IndexStatementSymbols(AstNode stmt, Scope scope, string filePath, SemanticModel model)
    {
        switch (stmt)
        {
            case SetStmt set:
                IndexSetStmtSymbols(set, scope, filePath, model);
                break;
            case IfStmt ifStmt:
                IndexIfStmtSymbols(ifStmt, scope, filePath, model);
                break;
            case MenuDecl menu:
                IndexMenuSymbols(menu, scope, filePath, model);
                break;
            case LabelDecl label:
                IndexLabelDecl(label, scope, filePath, model);
                break;
        }
    }

    private void IndexSetStmtSymbols(SetStmt set, Scope scope, string filePath, SemanticModel model)
    {
        var name = set.VariableName;
        var valueType = InferType(set.Value, scope);

        if (scope.Lookup(name) is null)
        {
            var symbol = new Symbol(
                name,
                SymbolKind.Variable,
                SymbolAccessibility.Private,
                valueType,
                scope,
                definitionSourceSpan: ToSourceSpan(set.Span, filePath),
                filePath: filePath);
            scope.Define(symbol);
            model.BindSymbol(set.GetHashCode(), symbol);
            model.BindType(set.GetHashCode(), valueType);
        }
    }

    private void IndexIfStmtSymbols(IfStmt ifStmt, Scope scope, string filePath, SemanticModel model)
    {
        var ifScope = scope.GetOrCreateChildScope("if");

        foreach (var stmt in ifStmt.ThenBody) IndexStatementSymbols(stmt, ifScope, filePath, model);

        foreach (var elif in ifStmt.ElifBranches)
        {
            var elifScope = scope.GetOrCreateChildScope("elif");
            foreach (var stmt in elif.Body) IndexStatementSymbols(stmt, elifScope, filePath, model);
        }

        if (ifStmt.ElseBranch is not null)
        {
            var elseScope = scope.GetOrCreateChildScope("else");
            foreach (var stmt in ifStmt.ElseBranch.Body) IndexStatementSymbols(stmt, elseScope, filePath, model);
        }
    }

    private void IndexMenuSymbols(MenuDecl menu, Scope scope, string filePath, SemanticModel model)
    {
        foreach (var item in menu.Items)
        {
            var itemScope = scope.GetOrCreateChildScope("menu_item");
            foreach (var stmt in item.Body) IndexStatementSymbols(stmt, itemScope, filePath, model);
        }
    }

    #endregion

    #region 引用验证

    private void ValidateReferences(CompilationUnit ast, IScope globalScope, string filePath, SemanticModel model)
    {
        foreach (var decl in ast.Declarations)
            if (decl is SceneDecl scene)
                ValidateSceneReferences(scene, globalScope, filePath, model);
    }

    private void ValidateSceneReferences(SceneDecl scene, IScope globalScope, string filePath, SemanticModel model)
    {
        var sceneScope = globalScope.GetChildScope(scene.Name);
        foreach (var stmt in scene.Body) ValidateStatementReferences(stmt, sceneScope ?? globalScope, filePath, model);
    }

    private void ValidateStatementReferences(AstNode stmt, IScope scope, string filePath, SemanticModel model)
    {
        switch (stmt)
        {
            case JumpStmt jump:
                if (!_labelSymbols.ContainsKey(jump.Target) && !_sceneSymbols.ContainsKey(jump.Target))
                {
                    model.AddDiagnostic(new SemanticDiagnostic(
                        DiagnosticLevel.Error,
                        $"未定义的跳转目标 '{jump.Target}'",
                        jump.Span,
                        "VS0501",
                        default,
                        filePath));
                }
                else
                {
                    var targetSymbol = _labelSymbols.GetValueOrDefault(jump.Target)
                                       ?? _sceneSymbols.GetValueOrDefault(jump.Target);
                    if (targetSymbol is not null) model.BindSymbol(jump.GetHashCode(), targetSymbol);
                }

                if (jump.Condition is not null) ValidateExpressionReferences(jump.Condition, scope, filePath, model);

                break;

            case CallStmt call:
                if (!_sceneSymbols.ContainsKey(call.Target))
                {
                    model.AddDiagnostic(new SemanticDiagnostic(
                        DiagnosticLevel.Warning,
                        $"未定义的调用目标 '{call.Target}'",
                        call.Span,
                        "VS0502",
                        default,
                        filePath));
                }
                else
                {
                    var targetSymbol = _sceneSymbols.GetValueOrDefault(call.Target);
                    if (targetSymbol is not null) model.BindSymbol(call.GetHashCode(), targetSymbol);
                }

                foreach (var arg in call.Arguments) ValidateExpressionReferences(arg, scope, filePath, model);

                break;

            case IfStmt ifStmt:
                ValidateExpressionReferences(ifStmt.Condition, scope, filePath, model);
                var ifScope = scope.GetChildScope("if");
                foreach (var s in ifStmt.ThenBody) ValidateStatementReferences(s, ifScope ?? scope, filePath, model);

                foreach (var elif in ifStmt.ElifBranches)
                {
                    ValidateExpressionReferences(elif.Condition, scope, filePath, model);
                    var elifScope = scope.GetChildScope("elif");
                    foreach (var s in elif.Body) ValidateStatementReferences(s, elifScope ?? scope, filePath, model);
                }

                if (ifStmt.ElseBranch is not null)
                {
                    var elseScope = scope.GetChildScope("else");
                    foreach (var s in ifStmt.ElseBranch.Body)
                        ValidateStatementReferences(s, elseScope ?? scope, filePath, model);
                }

                break;

            case SetStmt set:
                ValidateExpressionReferences(set.Value, scope, filePath, model);
                break;

            case MenuDecl menu:
                foreach (var item in menu.Items)
                {
                    if (item.Condition is not null)
                        ValidateExpressionReferences(item.Condition, scope, filePath, model);

                    var itemScope = scope.GetChildScope("menu_item");
                    foreach (var s in item.Body) ValidateStatementReferences(s, itemScope ?? scope, filePath, model);
                }

                break;
        }
    }

    private void ValidateExpressionReferences(AstNode expr, IScope scope, string filePath, SemanticModel model)
    {
        switch (expr)
        {
            case IdentifierExpr ident:
                var resolved = scope.LookupRecursive(ident.Name);
                if (resolved is null)
                    model.AddDiagnostic(new SemanticDiagnostic(
                        DiagnosticLevel.Warning,
                        $"未定义的标识符 '{ident.Name}'",
                        ident.Span,
                        "VS0530",
                        default,
                        filePath));
                else
                    model.BindSymbol(ident.GetHashCode(), resolved);

                break;

            case BinaryExpr binary:
                ValidateExpressionReferences(binary.Left, scope, filePath, model);
                ValidateExpressionReferences(binary.Right, scope, filePath, model);
                break;

            case UnaryExpr unary:
                ValidateExpressionReferences(unary.Operand, scope, filePath, model);
                break;

            case MemberAccessExpr member:
                ValidateExpressionReferences(member.Object, scope, filePath, model);
                break;

            case AssignmentExpr assignment:
                ValidateExpressionReferences(assignment.Value, scope, filePath, model);
                break;
        }
    }

    #endregion

    #region 类型推断

    private void InferAllExpressionTypes(CompilationUnit ast, IScope globalScope, SemanticModel model)
    {
        foreach (var decl in ast.Declarations)
            if (decl is SceneDecl scene)
            {
                var sceneScope = globalScope.GetChildScope(scene.Name) ?? globalScope;
                foreach (var stmt in scene.Body) InferStatementExpressionTypes(stmt, sceneScope, model);
            }
    }

    private void InferStatementExpressionTypes(AstNode stmt, IScope scope, SemanticModel model)
    {
        switch (stmt)
        {
            case SetStmt set:
                var setType = InferType(set.Value, scope);
                model.BindType(set.Value.GetHashCode(), setType);
                break;

            case IfStmt ifStmt:
                var condType = InferType(ifStmt.Condition, scope);
                model.BindType(ifStmt.Condition.GetHashCode(), condType);
                var ifScope = scope.GetChildScope("if") ?? scope;
                foreach (var s in ifStmt.ThenBody) InferStatementExpressionTypes(s, ifScope, model);

                foreach (var elif in ifStmt.ElifBranches)
                {
                    var elifCondType = InferType(elif.Condition, scope);
                    model.BindType(elif.Condition.GetHashCode(), elifCondType);
                }

                if (ifStmt.ElseBranch is not null)
                {
                    var elseScope = scope.GetChildScope("else") ?? scope;
                    foreach (var s in ifStmt.ElseBranch.Body) InferStatementExpressionTypes(s, elseScope, model);
                }

                break;

            case MenuDecl menu:
                foreach (var item in menu.Items)
                {
                    if (item.Condition is not null)
                    {
                        var itemCondType = InferType(item.Condition, scope);
                        model.BindType(item.Condition.GetHashCode(), itemCondType);
                    }

                    var itemScope = scope.GetChildScope("menu_item") ?? scope;
                    foreach (var s in item.Body) InferStatementExpressionTypes(s, itemScope, model);
                }

                break;

            case JumpStmt jump:
                if (jump.Condition is not null)
                {
                    var jumpCondType = InferType(jump.Condition, scope);
                    model.BindType(jump.Condition.GetHashCode(), jumpCondType);
                }

                break;

            case CallStmt call:
                foreach (var arg in call.Arguments)
                {
                    var argType = InferType(arg, scope);
                    model.BindType(arg.GetHashCode(), argType);
                }

                break;
        }
    }

    public IType InferType(AstNode expr, IScope scope)
    {
        return expr switch
        {
            LiteralExpr lit => InferLiteralType(lit),
            IdentifierExpr ident => InferIdentifierType(ident, scope),
            BinaryExpr binary => InferBinaryType(binary, scope),
            UnaryExpr unary => InferUnaryType(unary, scope),
            MemberAccessExpr member => InferMemberAccessType(member, scope),
            AssignmentExpr assignment => InferAssignmentType(assignment, scope),
            _ => UnknownType.Instance
        };
    }

    private IType InferLiteralType(LiteralExpr lit)
    {
        return lit.LiteralKind switch
        {
            LiteralType.Number => int.TryParse(lit.Value, out _) ? new PrimitiveType("i32") : new PrimitiveType("f64"),
            LiteralType.String => new PrimitiveType("string"),
            LiteralType.Boolean => new PrimitiveType("bool"),
            LiteralType.Null => new NullableType(UnknownType.Instance),
            _ => UnknownType.Instance
        };
    }

    private IType InferIdentifierType(IdentifierExpr ident, IScope scope)
    {
        var symbol = scope.LookupRecursive(ident.Name);
        if (symbol is not null) return symbol.Type ?? UnknownType.Instance;

        return ident.Name switch
        {
            "true" or "false" => new PrimitiveType("bool"),
            "null" => new NullableType(UnknownType.Instance),
            _ => UnknownType.Instance
        };
    }

    private IType InferBinaryType(BinaryExpr binary, IScope scope)
    {
        var leftType = InferType(binary.Left, scope);
        var rightType = InferType(binary.Right, scope);

        return binary.Operator switch
        {
            "==" or "!=" or "<" or ">" or "<=" or ">=" or "&&" or "||" => new PrimitiveType("bool"),
            "+" when IsStringType(leftType) || IsStringType(rightType) => new PrimitiveType("string"),
            "+" or "-" or "*" or "/" or "%" => PromoteNumeric(leftType, rightType),
            _ => UnknownType.Instance
        };
    }

    private IType InferUnaryType(UnaryExpr unary, IScope scope)
    {
        var operandType = InferType(unary.Operand, scope);

        return unary.Operator switch
        {
            "-" => IsNumericType(operandType) ? operandType : UnknownType.Instance,
            "!" => new PrimitiveType("bool"),
            _ => UnknownType.Instance
        };
    }

    private IType InferMemberAccessType(MemberAccessExpr member, IScope scope)
    {
        var objectType = InferType(member.Object, scope);

        if (objectType is NamedType namedType)
            foreach (var m in namedType.Members)
                if (m.Name == member.Member)
                    return m.Type ?? UnknownType.Instance;

        if (objectType is PrimitiveType { Name: "string" })
            return member.Member switch
            {
                "length" => new PrimitiveType("i32"),
                "isEmpty" => new PrimitiveType("bool"),
                _ => UnknownType.Instance
            };

        return UnknownType.Instance;
    }

    private IType InferAssignmentType(AssignmentExpr assignment, IScope scope)
    {
        return InferType(assignment.Value, scope);
    }

    private IType PromoteNumeric(IType left, IType right)
    {
        var leftRank = GetNumericRank(left);
        var rightRank = GetNumericRank(right);

        if (leftRank < 0 || rightRank < 0) return UnknownType.Instance;

        return leftRank >= rightRank ? left : right;
    }

    private int GetNumericRank(IType type)
    {
        if (type is not PrimitiveType p) return -1;

        return p.Name switch
        {
            "i32" => 0,
            "i64" => 1,
            "f32" => 2,
            "f64" => 3,
            _ => -1
        };
    }

    private bool IsNumericType(IType type)
    {
        if (type is not PrimitiveType p) return false;

        return p.Name is "i32" or "i64" or "f32" or "f64";
    }

    private bool IsStringType(IType type)
    {
        return type is PrimitiveType { Name: "string" };
    }

    #endregion

    #region SourceSpan 转换

    private static SourceSpan ToSourceSpan(SourceSpan? span, string? filePath = null)
    {
        if (span is null) return default;

        var s = span.Value;
        var result = new SourceSpan(s.StartLine, s.StartColumn, s.EndLine, s.EndColumn);
        if (filePath is not null) result = result with { FilePath = filePath };

        return result;
    }

    private static SourceSpan ToSourceSpan(TextSpan span, string? filePath = null)
    {
        return new SourceSpan(span.Start, 0, span.End, 0, filePath);
    }

    #endregion
}
