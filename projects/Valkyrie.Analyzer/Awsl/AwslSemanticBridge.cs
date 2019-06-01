using System.Text;
using Oak.Diagnostics;
using Oak.Widget;
using Nyar.Semantic;

namespace Valkyrie.Analyzer.Awsl;

/// <summary>
///     AWSL 语义桥接器 —— 将 AWSL Widget 解析结果转换为 Nyar 语义模型
/// </summary>
public sealed class AwslSemanticBridge
{
    #region 公共 API

    public SemanticModel BuildSemanticModel(WidgetParseResult parseResult, string filePath)
    {
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(filePath, symbolTable);

        IndexWidgetDeclarations(parseResult, globalScope, filePath, model);
        ValidateWidgetSemantics(parseResult, globalScope, filePath, model);

        return model;
    }

    public IType ConvertWidgetPropertyType(string typeName)
    {
        return typeName switch
        {
            "f64" or "number" => new PrimitiveType("f64"),
            "f32" => new PrimitiveType("f32"),
            "i32" or "int" => new PrimitiveType("i32"),
            "i64" => new PrimitiveType("i64"),
            "bool" or "boolean" => new PrimitiveType("bool"),
            "string" or "str" => new PrimitiveType("string"),
            "list" or "array" => new GenericType("list", [UnknownType.Instance]),
            "map" or "object" => new GenericType("map", [UnknownType.Instance, UnknownType.Instance]),
            "auto" => AutoType.Instance,
            "" => UnknownType.Instance,
            _ => new NamedType(typeName, "widget_prop")
        };
    }

    public IType InferValueKindType(WidgetValueKind kind)
    {
        return kind switch
        {
            WidgetValueKind.Boolean => new PrimitiveType("bool"),
            WidgetValueKind.Number => new PrimitiveType("f64"),
            WidgetValueKind.String => new PrimitiveType("string"),
            WidgetValueKind.Array => new GenericType("list", [UnknownType.Instance]),
            WidgetValueKind.Object => new GenericType("map", [UnknownType.Instance, UnknownType.Instance]),
            WidgetValueKind.Identifier => UnknownType.Instance,
            WidgetValueKind.Expression => UnknownType.Instance,
            _ => UnknownType.Instance
        };
    }

    #endregion

    #region 声明索引

    private void IndexWidgetDeclarations(WidgetParseResult parseResult, Scope globalScope, string filePath,
        SemanticModel model)
    {
        var widgetScope = globalScope.GetOrCreateChildScope(parseResult.Name);

        var widgetType = new NamedType(parseResult.Name, "widget");
        var widgetSymbol = new Symbol(
            parseResult.Name,
            SymbolKind.Class,
            SymbolAccessibility.Public,
            widgetType,
            globalScope,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { FilePath = filePath },
            filePath: filePath);
        globalScope.Define(widgetSymbol);
        model.BindSymbol(parseResult.GetHashCode(), widgetSymbol);
        model.BindType(parseResult.GetHashCode(), widgetType);

        foreach (var prop in parseResult.Properties) IndexProperty(prop, widgetScope, filePath, model);

        foreach (var method in parseResult.Methods) IndexMethod(method, widgetScope, filePath, model);
    }

    private void IndexProperty(WidgetProperty prop, Scope scope, string filePath, SemanticModel model)
    {
        var propType = ConvertWidgetPropertyType(prop.TypeName);

        if (prop.DefaultValueKind != WidgetValueKind.None && propType is AutoType or UnknownType)
            propType = InferValueKindType(prop.DefaultValueKind);

        var symbol = new Symbol(
            prop.Name,
            SymbolKind.Property,
            prop.IsReadonly ? SymbolAccessibility.Private : SymbolAccessibility.Public,
            propType,
            scope,
            isReadOnly: prop.IsReadonly,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { FilePath = filePath },
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(prop.GetHashCode(), symbol);
        model.BindType(prop.GetHashCode(), propType);
    }

    private void IndexMethod(WidgetMethod method, Scope scope, string filePath, SemanticModel model)
    {
        var returnType = new PrimitiveType("void");
        var paramTypes = new List<IType>();

        if (!string.IsNullOrWhiteSpace(method.Parameters))
        {
            var paramNames = method.Parameters.Split(',');
            foreach (var _ in paramNames) paramTypes.Add(UnknownType.Instance);
        }

        var funcType = new FunctionType(paramTypes, returnType);
        var symbol = new Symbol(
            method.Name,
            SymbolKind.Method,
            SymbolAccessibility.Public,
            funcType,
            scope,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { FilePath = filePath },
            filePath: filePath);
        scope.Define(symbol);
        model.BindSymbol(method.GetHashCode(), symbol);
        model.BindType(method.GetHashCode(), funcType);
    }

    #endregion

    #region 语义验证

    private void ValidateWidgetSemantics(WidgetParseResult parseResult, IScope globalScope, string filePath,
        SemanticModel model)
    {
        var widgetScope = globalScope.GetChildScope(parseResult.Name);
        if (widgetScope is null) return;

        ValidateTemplateNodes(parseResult.TemplateNodes, widgetScope, filePath, model);

        ValidatePropertyDefaults(parseResult.Properties, widgetScope, filePath, model);
    }

    private void ValidateTemplateNodes(IReadOnlyList<WidgetTemplateNode> nodes, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var node in nodes) ValidateTemplateNode(node, scope, filePath, model);
    }

    private void ValidateTemplateNode(WidgetTemplateNode node, IScope scope, string filePath, SemanticModel model)
    {
        switch (node)
        {
            case WidgetInterpolationNode interpolation:
                ValidateInterpolationExpression(interpolation.Expression, scope, filePath, model);
                break;

            case WidgetElementNode element:
                ValidateElementAttributes(element, scope, filePath, model);
                ValidateTemplateNodes(element.Children, scope, filePath, model);
                break;

            case WidgetIfNode ifNode:
                ValidateInterpolationExpression(ifNode.Condition, scope, filePath, model);
                ValidateTemplateNodes(ifNode.Children, scope, filePath, model);
                ValidateTemplateNodes(ifNode.ElseChildren, scope, filePath, model);
                break;

            case WidgetForNode forNode:
                if (scope is Scope concreteScope)
                {
                    var forScope = concreteScope.GetOrCreateChildScope("for_" + forNode.Iterator);
                    var iteratorSymbol = new Symbol(
                        forNode.Iterator,
                        SymbolKind.Variable,
                        SymbolAccessibility.Private,
                        UnknownType.Instance,
                        forScope);
                    forScope.Define(iteratorSymbol);

                    var iterableSymbol = scope.LookupRecursive(forNode.Iterable);
                    if (iterableSymbol is null)
                        model.AddDiagnostic(new SemanticDiagnostic(
                            DiagnosticLevel.Warning,
                            $"未定义的可迭代对象 '{forNode.Iterable}'",
                            default,
                            "AW0501",
                            filePath));

                    ValidateTemplateNodes(forNode.Children, forScope, filePath, model);
                }

                break;
        }
    }

    private void ValidateInterpolationExpression(string expression, IScope scope, string filePath, SemanticModel model)
    {
        if (string.IsNullOrWhiteSpace(expression)) return;

        var identifiers = ExtractIdentifiers(expression);
        foreach (var ident in identifiers)
        {
            var resolved = scope.LookupRecursive(ident);
            if (resolved is null)
                model.AddDiagnostic(new SemanticDiagnostic(
                    DiagnosticLevel.Warning,
                    $"模板中未定义的标识符 '{ident}'",
                    default,
                    "AW0530",
                    filePath));
        }
    }

    private void ValidateElementAttributes(WidgetElementNode element, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var attr in element.Attributes)
            if (attr.Value.StartsWith('{') && attr.Value.EndsWith('}'))
            {
                var expr = attr.Value[1..^1].Trim();
                ValidateInterpolationExpression(expr, scope, filePath, model);
            }
    }

    private void ValidatePropertyDefaults(IReadOnlyList<WidgetProperty> properties, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var prop in properties)
        {
            if (prop.DefaultValue is null || prop.DefaultValueKind == WidgetValueKind.None) continue;

            if (prop.DefaultValueKind == WidgetValueKind.Identifier)
            {
                var resolved = scope.LookupRecursive(prop.DefaultValue);
                if (resolved is null)
                    model.AddDiagnostic(new SemanticDiagnostic(
                        DiagnosticLevel.Warning,
                        $"属性 '{prop.Name}' 的默认值引用了未定义的标识符 '{prop.DefaultValue}'",
                        default,
                        "AW0531",
                        filePath));
            }

            var declaredType = ConvertWidgetPropertyType(prop.TypeName);
            var inferredType = InferValueKindType(prop.DefaultValueKind);

            if (declaredType is not AutoType and not UnknownType
                && inferredType is not UnknownType
                && !declaredType.IsAssignableFrom(inferredType))
                model.AddDiagnostic(new SemanticDiagnostic(
                    DiagnosticLevel.Warning,
                    $"属性 '{prop.Name}' 默认值类型不匹配：期望 {declaredType.Name}，实际 {inferredType.Name}",
                    default,
                    "AW0401",
                    filePath));
        }
    }

    #endregion

    #region 标识符提取

    private static List<string> ExtractIdentifiers(string expression)
    {
        var identifiers = new List<string>();
        var buffer = new StringBuilder();
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            if (inString)
            {
                if (c == stringChar && (i == 0 || expression[i - 1] != '\\')) inString = false;

                continue;
            }

            if (c == '\'' || c == '"' || c == '`')
            {
                inString = true;
                stringChar = c;
                if (buffer.Length > 0) ProcessBuffer(buffer, identifiers);

                continue;
            }

            if (char.IsLetterOrDigit(c) || c == '_')
            {
                buffer.Append(c);
            }
            else
            {
                if (buffer.Length > 0) ProcessBuffer(buffer, identifiers);
            }
        }

        if (buffer.Length > 0) ProcessBuffer(buffer, identifiers);

        return identifiers;
    }

    private static void ProcessBuffer(StringBuilder buffer, List<string> identifiers)
    {
        var token = buffer.ToString();
        buffer.Clear();

        if (token.Length == 0) return;

        if (!char.IsLetter(token[0]) && token[0] != '_') return;

        var keywords = new HashSet<string>
        {
            "true", "false", "null", "undefined",
            "if", "else", "for", "while", "return",
            "let", "const", "var", "function",
            "new", "this", "super", "class",
            "import", "export", "from", "as",
            "typeof", "instanceof", "in", "of"
        };

        if (!keywords.Contains(token)) identifiers.Add(token);
    }

    #endregion
}
