using System.Text;
using System.Text.RegularExpressions;

namespace Asgard.CLI.DevServer;

/// <summary>
///     AWSL 组件渲染器，将 WidgetParseResult AST 渲染为 HTML/CSS/JS
/// </summary>
public sealed class AwslRenderer
{
    private readonly WidgetParser _parser = new();

    /// <summary>
    ///     渲染 AWSL 源码为完整的 HTML 页面
    /// </summary>
    public AwslRenderResult Render(string source, string filePath = "")
    {
        var parseResult = _parser.Parse(source, filePath);
        return RenderParsed(parseResult);
    }

    /// <summary>
    ///     渲染已解析的 Widget 组件为完整 HTML 页面
    /// </summary>
    public AwslRenderResult RenderParsed(WidgetParseResult parseResult)
    {

        var componentName = parseResult.Name;
        var componentVar = ToCamelCase(componentName);

        var htmlBuilder = new StringBuilder();
        var cssBuilder = new StringBuilder();
        var jsBuilder = new StringBuilder();

        var componentStyles = RenderStyles(parseResult.Styles, componentName);
        if (!string.IsNullOrEmpty(componentStyles))
        {
            cssBuilder.AppendLine(componentStyles);
        }

        var templateJs = RenderTemplateToJs(parseResult.TemplateNodes, componentVar);
        jsBuilder.AppendLine($"const {componentVar} = (() => {{");

        jsBuilder.AppendLine("  const state = {};");
        foreach (var prop in parseResult.Properties)
        {
            var defaultValue = prop.DefaultValueKind switch
            {
                WidgetValueKind.String => $"\"{prop.DefaultValue}\"",
                WidgetValueKind.Boolean => prop.DefaultValue?.ToLower() ?? "false",
                WidgetValueKind.Number => prop.DefaultValue ?? "0",
                WidgetValueKind.Array => prop.DefaultValue ?? "[]",
                _ => prop.DefaultValue ?? "null"
            };
            jsBuilder.AppendLine($"  state.{prop.Name} = {defaultValue};");
        }

        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  function render() {");
        jsBuilder.AppendLine($"    const root = document.getElementById('voa-app');");
        jsBuilder.AppendLine("    if (!root) return;");
        jsBuilder.AppendLine($"    root.innerHTML = {templateJs};");
        jsBuilder.AppendLine("  }");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  function setState(updates) {");
        jsBuilder.AppendLine("    Object.assign(state, updates);");
        jsBuilder.AppendLine("    render();");
        jsBuilder.AppendLine("  }");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  render();");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  return { state, setState, render };");
        jsBuilder.AppendLine("})();");

        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html lang=\"zh-CN\">");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("  <meta charset=\"utf-8\">");
        htmlBuilder.AppendLine($"  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        htmlBuilder.AppendLine($"  <title>{componentName}</title>");

        if (cssBuilder.Length > 0)
        {
            htmlBuilder.AppendLine("  <style>");
            htmlBuilder.AppendLine(cssBuilder.ToString().TrimEnd());
            htmlBuilder.AppendLine("  </style>");
        }

        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");
        htmlBuilder.AppendLine("  <div id=\"voa-app\"></div>");

        if (jsBuilder.Length > 0)
        {
            htmlBuilder.AppendLine("  <script>");
            htmlBuilder.AppendLine(jsBuilder.ToString().TrimEnd());
            htmlBuilder.AppendLine("  </script>");
        }

        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return new AwslRenderResult
        {
            Html = htmlBuilder.ToString(),
            Css = cssBuilder.ToString(),
            JavaScript = jsBuilder.ToString(),
            ComponentName = componentName
        };
    }

    #region Template → JS

    private string RenderTemplateToJs(IReadOnlyList<WidgetTemplateNode> nodes, string componentVar)
    {
        if (nodes.Count == 0) return "''";

        var parts = new List<string>();

        foreach (var node in nodes)
        {
            var js = RenderNodeToJs(node, componentVar);
            if (!string.IsNullOrEmpty(js))
            {
                parts.Add(js);
            }
        }

        if (parts.Count == 0) return "''";
        if (parts.Count == 1) return parts[0];

        var sb = new StringBuilder();
        sb.Append('[');
        sb.Append(string.Join(", ", parts));
        sb.Append("].join('')");
        return sb.ToString();
    }

    private string RenderNodeToJs(WidgetTemplateNode node, string componentVar)
    {
        return node switch
        {
            WidgetTextNode textNode => EscapeJsString(textNode.Text),
            WidgetInterpolationNode interpNode => RenderInterpolation(interpNode, componentVar),
            WidgetElementNode elementNode => RenderElement(elementNode, componentVar),
            WidgetIfNode ifNode => RenderIf(ifNode, componentVar),
            WidgetForNode forNode => RenderFor(forNode, componentVar),
            _ => "''"
        };
    }

    private static string RenderInterpolation(WidgetInterpolationNode node, string componentVar)
    {
        var expr = node.Expression.Trim();
        return $"String({ResolveExpression(expr, componentVar)})";
    }

    private string RenderElement(WidgetElementNode node, string componentVar)
    {
        var sb = new StringBuilder();
        sb.Append($"'<{node.TagName}'");

        foreach (var attr in node.Attributes)
        {
            if (attr.Key.StartsWith("on"))
            {
                var eventName = attr.Key[2..].ToLower();
                var handler = attr.Value;
                sb.Append($" + ' {attr.Key}=\"{EscapeHtmlAttr(handler)}\"'");
            }
            else if (IsExpressionBinding(attr.Value))
            {
                var expr = ExtractExpression(attr.Value);
                sb.Append($" + ' {attr.Key}=\"' + String({ResolveExpression(expr, componentVar)}) + '\"'");
            }
            else
            {
                sb.Append($" + ' {attr.Key}=\"{EscapeHtmlAttr(attr.Value)}\"'");
            }
        }

        if (node.IsSelfClosing)
        {
            sb.Append(" + ' />'");
            return sb.ToString();
        }

        sb.Append(" + '>'");

        if (node.Children.Count > 0)
        {
            sb.Append(" + ");
            sb.Append(RenderTemplateToJs(node.Children, componentVar));
        }

        sb.Append($" + '</{node.TagName}>'");
        return sb.ToString();
    }

    private string RenderIf(WidgetIfNode node, string componentVar)
    {
        var condition = ResolveExpression(node.Condition, componentVar);
        var thenJs = RenderTemplateToJs(node.Children, componentVar);
        return $"({condition} ? {thenJs} : '')";
    }

    private string RenderFor(WidgetForNode node, string componentVar)
    {
        var iterator = node.Iterator;
        var iterable = ResolveExpression(node.Iterable, componentVar);
        var bodyJs = RenderTemplateToJs(node.Children, componentVar);

        return $"({iterable}.map(({iterator}, __index) => {bodyJs}).join(''))";
    }

    private static string ResolveExpression(string expr, string componentVar)
    {
        var trimmed = expr.Trim();

        if (trimmed.StartsWith("state.") || trimmed.StartsWith("Math.") || trimmed.StartsWith("Date.") ||
            trimmed.StartsWith("JSON.") || trimmed.StartsWith("console.") || trimmed.StartsWith("window.") ||
            trimmed.StartsWith("document."))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("!") || trimmed.StartsWith("(") || trimmed.StartsWith("[") ||
            trimmed.Contains("&&") || trimmed.Contains("||") || trimmed.Contains("==") ||
            trimmed.Contains("!=") || trimmed.Contains(">") || trimmed.Contains("<") ||
            trimmed.Contains("+") || trimmed.Contains("-") || trimmed.Contains("*") ||
            trimmed.Contains("/"))
        {
            return ReplaceStateReferences(trimmed, componentVar);
        }

        if (IsJsKeyword(trimmed) || IsLiteral(trimmed))
        {
            return trimmed;
        }

        return $"{componentVar}.state.{trimmed}";
    }

    private static string ReplaceStateReferences(string expr, string componentVar)
    {
        var pattern = @"\b([a-zA-Z_]\w*)\b";
        return Regex.Replace(expr, pattern, match =>
        {
            var name = match.Groups[1].Value;
            if (IsJsKeyword(name) || IsLiteral(name) || name.StartsWith("state.") ||
                name == "Math" || name == "Date" || name == "JSON" || name == "console" ||
                name == "window" || name == "document" || name == "true" || name == "false" ||
                name == "null" || name == "undefined" || name == "this")
            {
                return name;
            }

            return $"{componentVar}.state.{name}";
        });
    }

    private static bool IsJsKeyword(string s)
    {
        return s is "if" or "else" or "for" or "while" or "do" or "switch" or "case" or "break"
            or "continue" or "return" or "function" or "var" or "let" or "const" or "class"
            or "new" or "typeof" or "instanceof" or "void" or "delete" or "in" or "of"
            or "try" or "catch" or "finally" or "throw" or "async" or "await" or "yield"
            or "import" or "export" or "default" or "from";
    }

    private static bool IsLiteral(string s)
    {
        return s is "true" or "false" or "null" or "undefined" or "NaN" or "Infinity"
               || double.TryParse(s, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out _)
               || (s.StartsWith("\"") && s.EndsWith("\""))
               || (s.StartsWith("'") && s.EndsWith("'"))
               || (s.StartsWith("`") && s.EndsWith("`"));
    }

    private static bool IsExpressionBinding(string value)
    {
        return value.StartsWith("{") && value.EndsWith("}");
    }

    private static string ExtractExpression(string binding)
    {
        return binding[1..^1].Trim();
    }

    #endregion

    #region Styles

    private static string RenderStyles(IReadOnlyDictionary<string, string> styles, string componentName)
    {
        if (styles.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        var scope = $"voa-{ToKebabCase(componentName)}";

        foreach (var kvp in styles)
        {
            var className = kvp.Key;
            var properties = kvp.Value;
            sb.AppendLine($".{scope} .{className} {{ {properties} }}");
        }

        return sb.ToString();
    }

    #endregion

    #region Utility

    private static string EscapeJsString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "''";

        var escaped = text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        return $"'{escaped}'";
    }

    private static string EscapeHtmlAttr(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var parts = name.Split('-', '_', ' ');
        var sb = new StringBuilder();

        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i])) continue;

            if (i == 0)
            {
                sb.Append(parts[i].ToLowerInvariant());
            }
            else
            {
                sb.Append(char.ToUpperInvariant(parts[i][0]));
                sb.Append(parts[i][1..].ToLowerInvariant());
            }
        }

        return sb.ToString();
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    #endregion
}