using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Shader;
using Oak.Valkyrie.AST.Type;
using Nyar.Semantic;
using Valkyrie.TypeChecker.TypeSystem;
using ValkyrieTypeChecker = Valkyrie.TypeChecker;

namespace Valkyrie.Analyzer;

/// <summary>
///     Valkyrie 语义桥接器 —— 将 Valkyrie 类型检查结果转换为 Nyar 语义模型
/// </summary>
public sealed class ValkyrieSemanticBridge
{
    private readonly Dictionary<ValkyrieType, IType> _typeCache;

    public ValkyrieSemanticBridge()
    {
        _typeCache = new Dictionary<ValkyrieType, IType>();
    }

    #region 公共 API

    public SemanticModel BuildSemanticModel(
        ValkyrieTypeChecker.TypeCheckResult typeCheckResult,
        CompilationUnit compilationUnit,
        string filePath)
    {
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(filePath, symbolTable);

        IndexDeclarations(compilationUnit, globalScope, filePath, model);

        foreach (var diag in typeCheckResult.Diagnostics) model.AddDiagnostic(ConvertDiagnostic(diag, filePath));

        return model;
    }

    public IType ConvertType(ValkyrieType valkyrieType)
    {
        if (_typeCache.TryGetValue(valkyrieType, out var cached)) return cached;

        var result = ConvertTypeCore(valkyrieType);
        _typeCache[valkyrieType] = result;
        return result;
    }

    public Symbol ConvertSymbol(TypeChecker.Scope.Symbol valkyrieSymbol, Scope containingScope,
        string? filePath = null)
    {
        var kind = ConvertSymbolKind(valkyrieSymbol.Kind);
        var type = ConvertType(valkyrieSymbol.Type);
        var accessibility = valkyrieSymbol.IsExported
            ? SymbolAccessibility.Public
            : SymbolAccessibility.Private;

        return new Symbol(
            valkyrieSymbol.Name,
            kind,
            accessibility,
            type,
            containingScope,
            isReadOnly: !valkyrieSymbol.IsMutable,
            filePath: filePath);
    }

    public SemanticDiagnostic ConvertDiagnostic(ValkyrieTypeChecker.TypeDiagnostic diagnostic, string? filePath = null)
    {
        var level = diagnostic.Severity switch
        {
            ValkyrieTypeChecker.DiagnosticSeverity.Error => DiagnosticLevel.Error,
            ValkyrieTypeChecker.DiagnosticSeverity.Warning => DiagnosticLevel.Warning,
            _ => DiagnosticLevel.Info
        };

        var sourceSpan = SourceSpan.SingleLine(
            diagnostic.Line > 0 ? diagnostic.Line : 1,
            diagnostic.Column > 0 ? diagnostic.Column : 1,
            diagnostic.Message.Length);

        if (filePath is not null) sourceSpan = sourceSpan with { FilePath = filePath };

        return new SemanticDiagnostic(level, diagnostic.Message, sourceSpan, diagnostic.Code, filePath);
    }

    public SymbolKind ConvertSymbolKind(TypeChecker.Scope.SymbolKind valkyrieKind)
    {
        return valkyrieKind switch
        {
            TypeChecker.Scope.SymbolKind.Variable => SymbolKind.Variable,
            TypeChecker.Scope.SymbolKind.Parameter => SymbolKind.Parameter,
            TypeChecker.Scope.SymbolKind.Function => SymbolKind.Function,
            TypeChecker.Scope.SymbolKind.Component => SymbolKind.Class,
            TypeChecker.Scope.SymbolKind.System => SymbolKind.Class,
            TypeChecker.Scope.SymbolKind.Widget => SymbolKind.Class,
            TypeChecker.Scope.SymbolKind.Plugin => SymbolKind.Module,
            TypeChecker.Scope.SymbolKind.Struct => SymbolKind.Class,
            TypeChecker.Scope.SymbolKind.Enum => SymbolKind.Enum,
            TypeChecker.Scope.SymbolKind.Union => SymbolKind.Class,
            TypeChecker.Scope.SymbolKind.Import => SymbolKind.Import,
            TypeChecker.Scope.SymbolKind.Field => SymbolKind.Field,
            _ => SymbolKind.Variable
        };
    }

    #endregion

    #region 类型转换

    private IType ConvertTypeCore(ValkyrieType valkyrieType)
    {
        if (valkyrieType.IsError) return ErrorType.Instance;

        return valkyrieType.Kind switch
        {
            TypeKind.Primitive => ConvertPrimitiveType(valkyrieType),
            TypeKind.Function => ConvertFunctionType(valkyrieType),
            TypeKind.Array => ConvertArrayType(valkyrieType),
            TypeKind.Map => ConvertMapType(valkyrieType),
            TypeKind.Nullable => ConvertNullableType(valkyrieType),
            TypeKind.Component => new NamedType(valkyrieType.Name, "component"),
            TypeKind.System => new NamedType(valkyrieType.Name, "system"),
            TypeKind.Widget => new NamedType(valkyrieType.Name, "widget"),
            TypeKind.Plugin => new NamedType(valkyrieType.Name, "plugin"),
            TypeKind.Struct => new NamedType(valkyrieType.Name, "struct"),
            TypeKind.Enum => new NamedType(valkyrieType.Name, "enum"),
            TypeKind.Union => new NamedType(valkyrieType.Name, "union"),
            TypeKind.Shader => new NamedType(valkyrieType.Name, "shader"),
            TypeKind.Generic => ConvertGenericType(valkyrieType),
            TypeKind.Unknown when valkyrieType.Name == "auto" => AutoType.Instance,
            TypeKind.Unknown => new NamedType(valkyrieType.Name, "unknown"),
            TypeKind.Error => ErrorType.Instance,
            _ => UnknownType.Instance
        };
    }

    private IType ConvertPrimitiveType(ValkyrieType valkyrieType)
    {
        return new PrimitiveType(valkyrieType.Name);
    }

    private IType ConvertFunctionType(ValkyrieType valkyrieType)
    {
        var paramTypes = new List<IType>();

        if (valkyrieType.Parameters is not null)
            foreach (var param in valkyrieType.Parameters)
                paramTypes.Add(ConvertType(param.Type));

        var returnType = valkyrieType.ReturnType is not null
            ? ConvertType(valkyrieType.ReturnType)
            : new PrimitiveType("void");

        return new FunctionType(paramTypes, returnType);
    }

    private IType ConvertArrayType(ValkyrieType valkyrieType)
    {
        if (valkyrieType.GenericArgs.Count > 0)
            return new GenericType(valkyrieType.Name, ConvertGenericArgs(valkyrieType.GenericArgs));

        return new GenericType(valkyrieType.Name, []);
    }

    private IType ConvertMapType(ValkyrieType valkyrieType)
    {
        return new GenericType(valkyrieType.Name, ConvertGenericArgs(valkyrieType.GenericArgs));
    }

    private IType ConvertNullableType(ValkyrieType valkyrieType)
    {
        if (valkyrieType.GenericArgs.Count > 0) return new NullableType(ConvertType(valkyrieType.GenericArgs[0]));

        return UnknownType.Instance;
    }

    private IType ConvertGenericType(ValkyrieType valkyrieType)
    {
        return new GenericType(valkyrieType.Name, ConvertGenericArgs(valkyrieType.GenericArgs));
    }

    private List<IType> ConvertGenericArgs(IReadOnlyList<ValkyrieType> genericArgs)
    {
        var result = new List<IType>(genericArgs.Count);
        foreach (var arg in genericArgs) result.Add(ConvertType(arg));

        return result;
    }

    #endregion

    #region SourceSpan 转换

    private static SourceSpan ToSourceSpan(SourceSpan? span, string? filePath = null)
    {
        if (span is null) return default;

        var s = span.Value;
        var result = new SourceSpan(s.StartLine, s.StartColumn, s.EndLine, s.EndColumn);
        if (filePath is not null) return result;

        return result;
    }

    private static SourceSpan ToSourceSpan(TextSpan span, string? filePath = null)
    {
        return new SourceSpan(span.Start, 0, span.End, 0, filePath);
    }

    #endregion

    #region 声明索引

    private void IndexDeclarations(CompilationUnit compilationUnit, Scope globalScope, string filePath,
        SemanticModel model)
    {
        foreach (var decl in compilationUnit.Declarations) IndexDeclaration(decl, globalScope, filePath, model);
    }

    private void IndexDeclaration(ValkyrieNode node, Scope scope, string filePath, SemanticModel model)
    {
        switch (node)
        {
            case ComponentDeclaration comp:
                IndexComponentDecl(comp, scope, filePath, model);
                break;
            case SystemDeclaration sys:
                IndexSystemDecl(sys, scope, filePath, model);
                break;
            case MicroDeclaration func:
                IndexFunctionDecl(func, scope, filePath, model);
                break;
            case LetDeclaration varDecl:
                IndexVariableDecl(varDecl, scope, filePath, model);
                break;
            case StructureDeclaration structDecl:
                IndexStructDecl(structDecl, scope, filePath, model);
                break;
            case ShaderDecl shaderDecl:
                IndexShaderDecl(shaderDecl, scope, filePath, model);
                break;
            case WidgetDecl widgetDecl:
                IndexWidgetDecl(widgetDecl, scope, filePath, model);
                break;
            case UsingDeclaration importDecl:
                IndexImportDecl(importDecl, scope, filePath, model);
                break;
        }
    }

    private void IndexComponentDecl(ComponentDeclaration comp, Scope scope, string filePath, SemanticModel model)
    {
        var fields = new List<ISymbol>();
        foreach (var field in comp.Fields)
        {
            var fieldType = ConvertTypeAnnotation(field.FieldType);
            var fieldSymbol = new Symbol(field.Name, SymbolKind.Field, SymbolAccessibility.Public,
                fieldType, scope,
                definitionSpan: default,
                definitionSourceSpan: ToSourceSpan(field.Span, filePath),
                filePath: filePath);
            fields.Add(fieldSymbol);
        }

        var compType = new NamedType(comp.Name, "component", members: fields);
        var symbol = new Symbol(comp.Name, SymbolKind.Class, SymbolAccessibility.Public,
            compType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(comp.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(comp.GetHashCode(), symbol);
        model.BindType(comp.GetHashCode(), compType);
    }

    private void IndexSystemDecl(SystemDeclaration sys, Scope scope, string filePath, SemanticModel model)
    {
        var sysType = new NamedType(sys.Name, "system");
        var symbol = new Symbol(sys.Name, SymbolKind.Class, SymbolAccessibility.Public,
            sysType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(sys.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(sys.GetHashCode(), symbol);
        model.BindType(sys.GetHashCode(), sysType);
    }

    private void IndexFunctionDecl(MicroDeclaration func, Scope scope, string filePath, SemanticModel model)
    {
        var paramTypes = new List<IType>();
        foreach (var param in func.Parameters) paramTypes.Add(ConvertTypeAnnotation(param.ParamType));

        var returnType = func.ReturnType is not null
            ? ConvertTypeAnnotation(func.ReturnType)
            : new PrimitiveType("void");

        var funcType = new FunctionType(paramTypes, returnType);
        var symbol = new Symbol(func.Name, SymbolKind.Function, SymbolAccessibility.Public,
            funcType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(func.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(func.GetHashCode(), symbol);
        model.BindType(func.GetHashCode(), funcType);
    }

    private void IndexVariableDecl(LetDeclaration varDecl, Scope scope, string filePath, SemanticModel model)
    {
        var varType = varDecl.VarType is not null
            ? ConvertTypeAnnotation(varDecl.VarType)
            : UnknownType.Instance;
        var symbol = new Symbol(varDecl.Name, SymbolKind.Variable, SymbolAccessibility.Private,
            varType, scope,
            isReadOnly: !varDecl.IsMutable,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(varDecl.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(varDecl.GetHashCode(), symbol);
        model.BindType(varDecl.GetHashCode(), varType);
    }

    private void IndexStructDecl(StructureDeclaration structDecl, Scope scope, string filePath, SemanticModel model)
    {
        var fields = new List<ISymbol>();
        foreach (var field in structDecl.Fields)
        {
            var fieldType = ConvertTypeAnnotation(field.FieldType);
            var fieldSymbol = new Symbol(field.Name, SymbolKind.Field, SymbolAccessibility.Public,
                fieldType, scope,
                definitionSpan: default,
                definitionSourceSpan: ToSourceSpan(field.Span, filePath),
                filePath: filePath);
            fields.Add(fieldSymbol);
        }

        var structType = new NamedType(structDecl.Name, "struct", members: fields);
        var symbol = new Symbol(structDecl.Name, SymbolKind.Class, SymbolAccessibility.Public,
            structType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(structDecl.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(structDecl.GetHashCode(), symbol);
        model.BindType(structDecl.GetHashCode(), structType);
    }

    private void IndexShaderDecl(ShaderDecl shaderDecl, Scope scope, string filePath, SemanticModel model)
    {
        var shaderType = new NamedType(shaderDecl.Name, "shader");
        var symbol = new Symbol(shaderDecl.Name, SymbolKind.Class, SymbolAccessibility.Public,
            shaderType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(shaderDecl.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(shaderDecl.GetHashCode(), symbol);
        model.BindType(shaderDecl.GetHashCode(), shaderType);
    }

    private void IndexWidgetDecl(WidgetDecl widgetDecl, Scope scope, string filePath, SemanticModel model)
    {
        var widgetType = new NamedType(widgetDecl.Name, "widget");
        var symbol = new Symbol(widgetDecl.Name, SymbolKind.Class, SymbolAccessibility.Public,
            widgetType, scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(widgetDecl.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(widgetDecl.GetHashCode(), symbol);
        model.BindType(widgetDecl.GetHashCode(), widgetType);
    }

    private void IndexImportDecl(UsingDeclaration importDecl, Scope scope, string filePath, SemanticModel model)
    {
        var symbol = new Symbol(importDecl.ModulePath, SymbolKind.Import,
            containingScope: scope,
            definitionSpan: default,
            definitionSourceSpan: ToSourceSpan(importDecl.Span, filePath),
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(importDecl.GetHashCode(), symbol);
    }

    #endregion

    #region TypeAnnotation 转换

    internal IType ConvertTypeAnnotation(TypeNode typeNode)
    {
        if (typeNode is null) return UnknownType.Instance;

        if (typeNode is TypeUnaryExpression { Operator: "?" } nullableNode)
        {
            var inner = ConvertTypeAnnotation(nullableNode.Operand);
            return new NullableType(inner);
        }

        return ConvertTypeAnnotationCore(typeNode);
    }

    private IType ConvertTypeAnnotationCore(TypeNode typeNode)
    {
        if (typeNode.Name == "union")
        {
            var members = new List<IType>();
            foreach (var member in typeNode.GenericArgs) members.Add(ConvertTypeAnnotation(member));

            return new NamedType(typeNode.Name ?? "union", "union", typeArguments: members);
        }

        if (typeNode.Name == "intersection")
        {
            var members = new List<IType>();
            foreach (var member in typeNode.GenericArgs) members.Add(ConvertTypeAnnotation(member));

            return new NamedType(typeNode.Name ?? "intersection", "intersection", typeArguments: members);
        }

        if (typeNode.Name == "function" || typeNode.Name == "fn")
        {
            var paramTypes = new List<IType>();
            foreach (var arg in typeNode.GenericArgs.Take(typeNode.GenericArgs.Count - 1))
                paramTypes.Add(ConvertTypeAnnotation(arg));

            var returnType = typeNode.GenericArgs.Count > 0
                ? ConvertTypeAnnotation(typeNode.GenericArgs.Last())
                : new PrimitiveType("void");

            return new FunctionType(paramTypes, returnType);
        }

        if (typeNode.Name == "list" || typeNode.Name == "List" || typeNode.Name == "array" || typeNode.Name == "Array")
        {
            if (typeNode.GenericArgs.Count > 0)
            {
                var elementType = ConvertTypeAnnotation(typeNode.GenericArgs[0]);
                return new NamedType("list", "list", typeArguments: [elementType]);
            }
        }

        return new NamedType(typeNode.Name, typeNode.Name, typeArguments: typeNode.GenericArgs.Select(ConvertTypeAnnotation).ToList());
    }

    #endregion
}
