using System.Text;

namespace Asgard.CLI.DevServer;

/// <summary>
///     AWSL 服务端渲染器，将组件渲染为静态 HTML 字符串。
///     用于 SSR 模式：服务端输出初始 HTML，客户端 hydration 后变为可交互。
///     与 AwslRenderer（innerHTML 模式）不同，SSR 渲染器：
///     - 输出纯静态 HTML（无 JS 渲染逻辑）
///     - 为每个组件添加 data-voa-ssr 属性用于 hydration 定位
///     - 内联初始状态为 JSON（data-voa-state）供客户端 hydration 读取
///     - 不生成事件处理器（hydration 阶段由客户端绑定）
/// </summary>
public sealed class AwslSsrRenderer
{
    private readonly WidgetParser _parser = new();

    /// <summary>
    ///     请求级 SSR 渲染：注入 get_data() 返回值作为组件的 data prop
    /// </summary>
    public AwslSsrRenderResult RenderSsr(string source, string filePath, Dictionary<string, object>? requestData = null)
    {
        if (requestData != null && requestData.Count > 0)
        {
            _ssrDataContext = requestData;
        }

        return RenderSsrInternal(source, filePath);
    }

    private Dictionary<string, object>? _ssrDataContext;

    /// <summary>
    ///     SSR 渲染内部实现：解析 AWSL → 调用已存在的 RenderParsedSsr
    /// </summary>
    private AwslSsrRenderResult RenderSsrInternal(string source, string filePath)
    {
        var parseResult = _parser.Parse(source, filePath);
        return RenderParsedSsr(parseResult);
    }

    /// <summary>
    ///     将已解析的 Widget 组件渲染为 SSR HTML
    /// </summary>
    public AwslSsrRenderResult RenderParsedSsr(WidgetParseResult parseResult)
    {
        var componentName = parseResult.Name;
        var scope = $"voa-ssr-{ToKebabCase(componentName)}";

        var htmlBuilder = new StringBuilder();
        var cssBuilder = new StringBuilder();

        var componentStyles = RenderStyles(parseResult.Styles, componentName);
        if (!string.IsNullOrEmpty(componentStyles))
        {
            cssBuilder.AppendLine(componentStyles);
        }

        var initialState = BuildInitialStateJson(parseResult.Properties);

        htmlBuilder.AppendLine($"<div class=\"{scope}\" data-voa-ssr=\"{componentName}\" data-voa-state=\"{EscapeAttr(initialState)}\">");

        htmlBuilder.AppendLine("</div>");

        var headTags = new List<string>();
        var scriptTags = new List<string>();
        var isSuspense = false;
        var fallbackHtml = (string?)null;

        foreach (var node in parseResult.TemplateNodes)
        {
            var (nodeHtml, nodeHeadTags, nodeScriptTags, nodeSuspense, nodeFallback) =
                RenderNodeToSsrHtmlWithMeta(node, parseResult.Properties);
            htmlBuilder.Append(nodeHtml);
            headTags.AddRange(nodeHeadTags);
            scriptTags.AddRange(nodeScriptTags);
            if (nodeSuspense)
            {
                isSuspense = true;
                fallbackHtml = nodeFallback;
            }
        }

        return new AwslSsrRenderResult
        {
            Html = htmlBuilder.ToString(),
            Css = cssBuilder.ToString(),
            ComponentName = componentName,
            InitialStateJson = initialState,
            Scope = scope,
            HeadTags = headTags,
            ScriptTags = scriptTags,
            IsSuspense = isSuspense,
            FallbackHtml = fallbackHtml
        };
    }

    /// <summary>
    ///     生成客户端 hydration 脚本
    /// </summary>
    public static string GenerateHydrationScript(AwslSsrRenderResult[] results, string moduleName, string? wasmFileName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();
        sb.AppendLine("  var Voa = window.Voa || {};");
        sb.AppendLine("  var hydrated = new Set();");
        sb.AppendLine();
        sb.AppendLine("  function toPascal(name) {");
        sb.AppendLine("    return name.replace(/[-_]\\w/g, function(m) {");
        sb.AppendLine("      return m.charAt(1).toUpperCase();");
        sb.AppendLine("    }).replace(/^./, function(m) {");
        sb.AppendLine("      return m.toUpperCase();");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function hydrateComponent(el) {");
        sb.AppendLine("    var name = el.getAttribute('data-voa-ssr');");
        sb.AppendLine("    var stateJson = el.getAttribute('data-voa-state');");
        sb.AppendLine("    if (!name || hydrated.has(name)) return;");
        sb.AppendLine("    hydrated.add(name);");
        sb.AppendLine();
        sb.AppendLine("    try {");
        sb.AppendLine("      var pascalName = toPascal(name);");
        sb.AppendLine("      var state = stateJson ? JSON.parse(stateJson) : {};");
        sb.AppendLine("      console.log('[VOA SSR] hydrating: ' + name, state);");
        sb.AppendLine();
        sb.AppendLine("      var componentFn = window[pascalName];");
        sb.AppendLine("      if (!componentFn && Voa['_' + name]) {");
        sb.AppendLine("        componentFn = Voa['_' + name];");
        sb.AppendLine("      }");
        sb.AppendLine();
        sb.AppendLine("      if (typeof componentFn === 'function') {");
        sb.AppendLine("        var newEl = componentFn(state);");
        sb.AppendLine("        if (newEl) {");
        sb.AppendLine("          el.innerHTML = '';");
        sb.AppendLine("          while (newEl.firstChild) {");
        sb.AppendLine("            el.appendChild(newEl.firstChild);");
        sb.AppendLine("          }");
        sb.AppendLine("          el.removeAttribute('data-voa-state');");
        sb.AppendLine("          el.setAttribute('data-voa-hydrated', 'true');");
        sb.AppendLine("        }");
        sb.AppendLine("      } else {");
        sb.AppendLine("        console.warn('[VOA SSR] component not found: ' + name + ' (' + pascalName + ')');");
        sb.AppendLine("        el.setAttribute('data-voa-hydrated', 'fallback');");
        sb.AppendLine("      }");
        sb.AppendLine("    } catch(e) {");
        sb.AppendLine("      console.error('[VOA SSR] hydration failed: ' + name, e);");
        sb.AppendLine("      el.setAttribute('data-voa-hydrated', 'error');");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function hydrateAll() {");
        sb.AppendLine("    var elements = document.querySelectorAll('[data-voa-ssr]');");
        sb.AppendLine("    for (var i = 0; i < elements.length; i++) {");
        sb.AppendLine("      hydrateComponent(elements[i]);");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  if (document.readyState === 'loading') {");
        sb.AppendLine("    document.addEventListener('DOMContentLoaded', hydrateAll);");
        sb.AppendLine("  } else {");
        sb.AppendLine("    hydrateAll();");
        sb.AppendLine("  }");
        sb.AppendLine("})();");

        return sb.ToString();
    }

    /// <summary>
    ///     生成完整的 SSR HTML 页面
    /// </summary>
    public static string GenerateSsrPage(string moduleName, AwslSsrRenderResult[] results, string? wasmFileName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"  <title>{moduleName} - VOA</title>");

        var allCss = new StringBuilder();
        var allHeadTags = new List<string>();
        var allScriptTags = new List<string>();
        var hasSuspense = false;

        foreach (var result in results)
        {
            if (!string.IsNullOrEmpty(result.Css))
            {
                allCss.AppendLine(result.Css.TrimEnd());
            }

            allHeadTags.AddRange(result.HeadTags);
            allScriptTags.AddRange(result.ScriptTags);

            if (result.IsSuspense)
            {
                hasSuspense = true;
            }
        }

        foreach (var headTag in allHeadTags)
        {
            sb.AppendLine($"  {headTag}");
        }

        if (allCss.Length > 0)
        {
            sb.AppendLine("  <style>");
            sb.AppendLine(allCss.ToString().TrimEnd());
            sb.AppendLine("  </style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div id=\"app\">");

        foreach (var result in results)
        {
            if (result.IsSuspense && !string.IsNullOrEmpty(result.FallbackHtml))
            {
                sb.AppendLine($"  <div class=\"voa-suspense-wrapper\" data-suspense=\"loading\">");
                sb.AppendLine($"    {result.FallbackHtml}");
                sb.AppendLine($"  </div>");
            }
            else
            {
                sb.AppendLine(result.Html);
            }
        }

        sb.AppendLine("  </div>");

        if (hasSuspense)
        {
            sb.AppendLine("  <script>");
            sb.AppendLine("    (function() {");
            sb.AppendLine("      var suspenseObserver = new MutationObserver(function(mutations) {");
            sb.AppendLine("        mutations.forEach(function(m) {");
            sb.AppendLine("          if (m.type === 'attributes' && m.attributeName === 'data-suspense') {");
            sb.AppendLine("            var el = m.target;");
            sb.AppendLine("            if (el.dataset.suspense === 'resolved') {");
            sb.AppendLine("              el.classList.add('voa-suspense-resolved');");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("      });");
            sb.AppendLine("      document.querySelectorAll('.voa-suspense-wrapper').forEach(function(el) {");
            sb.AppendLine("        suspenseObserver.observe(el, { attributes: true });");
            sb.AppendLine("      });");
            sb.AppendLine("    })();");
            sb.AppendLine("  </script>");
        }

        foreach (var scriptTag in allScriptTags)
        {
            sb.AppendLine($"  {scriptTag}");
        }

        sb.AppendLine("  <script src=\"voa-runtime.js\"></script>");

        if (wasmFileName is not null)
        {
            sb.AppendLine($"  <script src=\"{wasmFileName.Replace(".wasm", ".js")}\"></script>");
        }

        sb.AppendLine($"  <script src=\"{moduleName}.js\"></script>");

        sb.AppendLine("  <script>");
        sb.AppendLine(GenerateHydrationScript(results, moduleName, wasmFileName));
        sb.AppendLine("  </script>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    #region Template → SSR HTML

    private (string html, List<string> headTags, List<string> scriptTags, bool isSuspense, string? fallback) RenderNodeToSsrHtmlWithMeta(
        WidgetTemplateNode node, IReadOnlyList<WidgetProperty> properties)
    {
        var headTags = new List<string>();
        var scriptTags = new List<string>();
        var isSuspense = false;
        var fallback = (string?)null;

        var html = node switch
        {
            WidgetTextNode textNode => textNode.Text,
            WidgetInterpolationNode interpNode => RenderInterpolationSsr(interpNode, properties),
            WidgetElementNode elementNode => RenderElementSsrWithMeta(elementNode, properties, headTags, scriptTags, ref isSuspense, ref fallback),
            WidgetIfNode ifNode => RenderIfSsr(ifNode, properties),
            WidgetForNode forNode => RenderForSsr(forNode, properties),
            _ => string.Empty
        };

        return (html, headTags, scriptTags, isSuspense, fallback);
    }

    private string RenderNodeToSsrHtml(WidgetTemplateNode node, IReadOnlyList<WidgetProperty> properties)
    {
        return node switch
        {
            WidgetTextNode textNode => textNode.Text,
            WidgetInterpolationNode interpNode => RenderInterpolationSsr(interpNode, properties),
            WidgetElementNode elementNode => RenderElementSsr(elementNode, properties),
            WidgetIfNode ifNode => RenderIfSsr(ifNode, properties),
            WidgetForNode forNode => RenderForSsr(forNode, properties),
            _ => string.Empty
        };
    }

    private string RenderElementSsrWithMeta(WidgetElementNode node, IReadOnlyList<WidgetProperty> properties,
        List<string> headTags, List<string> scriptTags, ref bool isSuspense, ref string? fallback)
    {
        if (node.TagName == "Head")
        {
            var inner = RenderChildrenSsr(node.Children, properties);
            headTags.Add(inner);
            return string.Empty;
        }

        if (node.TagName == "Script")
        {
            var src = node.Attributes.TryGetValue("src", out var s) ? s : "";
            var content = RenderChildrenSsr(node.Children, properties);
            if (!string.IsNullOrEmpty(src))
            {
                scriptTags.Add($"<script src=\"{src}\"></script>");
            }
            else if (!string.IsNullOrEmpty(content))
            {
                scriptTags.Add($"<script>{content}</script>");
            }
            return string.Empty;
        }

        if (node.TagName == "Suspense")
        {
            return RenderSuspenseSsr(node, properties, ref isSuspense, ref fallback);
        }

        return RenderElementSsr(node, properties);
    }

    private string RenderSuspenseSsr(WidgetElementNode node, IReadOnlyList<WidgetProperty> properties,
        ref bool isSuspense, ref string? fallback)
    {
        var fallbackNode = node.Children.FirstOrDefault(
            c => c is WidgetElementNode e && e.TagName == "Fallback");
        var contentNode = node.Children.FirstOrDefault(
            c => c is WidgetElementNode e && e.TagName != "Fallback");

        if (fallbackNode is WidgetElementNode fn)
        {
            fallback = $"<div class=\"voa-suspense-fallback\">{RenderChildrenSsr(fn.Children, properties)}</div>";
        }
        else
        {
            fallback = "<div class=\"voa-suspense-fallback\">Loading...</div>";
        }

        if (contentNode is WidgetElementNode cn)
        {
            return RenderChildrenSsr(cn.Children, properties);
        }

        return fallback;
    }

    private string RenderChildrenSsr(IReadOnlyList<WidgetTemplateNode> children, IReadOnlyList<WidgetProperty> properties)
    {
        var sb = new StringBuilder();
        foreach (var child in children)
        {
            sb.Append(RenderNodeToSsrHtml(child, properties));
        }
        return sb.ToString();
    }

    private static string RenderInterpolationSsr(WidgetInterpolationNode node, IReadOnlyList<WidgetProperty> properties)
    {
        var expr = node.Expression.Trim();
        var value = ResolveSsrExpression(expr, properties);
        return EscapeHtml(value);
    }

    private string RenderElementSsr(WidgetElementNode node, IReadOnlyList<WidgetProperty> properties)
    {
        var sb = new StringBuilder();
        sb.Append($"<{node.TagName}");

        foreach (var attr in node.Attributes)
        {
            if (attr.Key.StartsWith("on"))
            {
                continue;
            }

            if (IsExpressionBinding(attr.Value))
            {
                var expr = ExtractExpression(attr.Value);
                var value = ResolveSsrExpression(expr, properties);
                sb.Append($" {attr.Key}=\"{EscapeAttr(value)}\"");
            }
            else
            {
                sb.Append($" {attr.Key}=\"{EscapeAttr(attr.Value)}\"");
            }
        }

        if (node.IsSelfClosing)
        {
            sb.Append(" />");
            return sb.ToString();
        }

        sb.Append(">");

        if (node.Children.Count > 0)
        {
            foreach (var child in node.Children)
            {
                sb.Append(RenderNodeToSsrHtml(child, properties));
            }
        }

        sb.Append($"</{node.TagName}>");
        return sb.ToString();
    }

    private string RenderIfSsr(WidgetIfNode node, IReadOnlyList<WidgetProperty> properties)
    {
        var condition = ResolveSsrExpression(node.Condition, properties);

        if (IsTruthy(condition))
        {
            var sb = new StringBuilder();
            foreach (var child in node.Children)
            {
                sb.Append(RenderNodeToSsrHtml(child, properties));
            }

            return sb.ToString();
        }

        if (node.ElseChildren.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var child in node.ElseChildren)
            {
                sb.Append(RenderNodeToSsrHtml(child, properties));
            }

            return sb.ToString();
        }

        return string.Empty;
    }

    private string RenderForSsr(WidgetForNode node, IReadOnlyList<WidgetProperty> properties)
    {
        var iterableValue = ResolveSsrExpression(node.Iterable, properties);
        var items = ParseArrayValue(iterableValue);

        if (items is null || items.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < items.Count; i++)
        {
            var itemProps = CreateIterationProperties(properties, node.Iterator, items[i], i);
            foreach (var child in node.Children)
            {
                sb.Append(RenderNodeToSsrHtml(child, itemProps));
            }
        }

        return sb.ToString();
    }

    #endregion

    #region SSR 表达式求值

    private static string ResolveSsrExpression(string expr, IReadOnlyList<WidgetProperty> properties)
    {
        var trimmed = expr.Trim();

        foreach (var prop in properties)
        {
            if (trimmed == prop.Name || trimmed == $"state.{prop.Name}")
            {
                return prop.DefaultValue ?? "";
            }

            if (trimmed.StartsWith($"{prop.Name}.") || trimmed.StartsWith($"state.{prop.Name}."))
            {
                var accessor = trimmed.Contains("state.") ? trimmed["state.".Length..] : trimmed;
                return ResolvePropertyAccessor(accessor, prop.DefaultValue);
            }
        }

        if (trimmed.StartsWith("Math.") || trimmed.StartsWith("Date.") || trimmed.StartsWith("JSON."))
        {
            return "";
        }

        return EvalSsrExpression(trimmed, properties);
    }

    private static string EvalSsrExpression(string expr, IReadOnlyList<WidgetProperty> properties)
    {
        var tokens = TokenizeExpression(expr);
        if (tokens.Count == 0)
        {
            return "";
        }

        if (tokens.Count == 1)
        {
            return ResolveLiteralOrProp(tokens[0], properties);
        }

        var ternaryResult = TryEvalTernary(tokens, properties);
        if (ternaryResult is not null)
        {
            return ternaryResult;
        }

        return EvalBinaryExpression(tokens, properties);
    }

    private static string ResolveLiteralOrProp(string token, IReadOnlyList<WidgetProperty> properties)
    {
        var trimmed = token.Trim();

        if (trimmed == "true") return "true";
        if (trimmed == "false") return "false";
        if (trimmed is "null" or "undefined") return "";
        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
        {
            return trimmed[1..^1];
        }

        if (trimmed.StartsWith("'") && trimmed.EndsWith("'"))
        {
            return trimmed[1..^1];
        }

        foreach (var prop in properties)
        {
            if (trimmed == prop.Name || trimmed == $"state.{prop.Name}")
            {
                return prop.DefaultValue ?? "";
            }

            if (trimmed.StartsWith($"state.{prop.Name}.") || trimmed.StartsWith($"{prop.Name}."))
            {
                var accessor = trimmed.StartsWith("state.")
                    ? trimmed["state.".Length..]
                    : trimmed;
                return ResolvePropertyAccessor(accessor, prop.DefaultValue);
            }
        }

        return "";
    }

    private static string? TryEvalTernary(List<ExpressionToken> tokens, IReadOnlyList<WidgetProperty> properties)
    {
        var questionIdx = FindTopLevelOperator(tokens, "?");
        if (questionIdx < 0)
        {
            return null;
        }

        var colonIdx = FindTopLevelOperator(tokens, ":", questionIdx + 1);
        if (colonIdx < 0)
        {
            return null;
        }

        var condition = EvalBinaryExpression(
            tokens.GetRange(0, questionIdx), properties);
        var isTruthy = IsTruthy(condition);

        if (isTruthy)
        {
            return EvalBinaryExpression(
                tokens.GetRange(questionIdx + 1, colonIdx - questionIdx - 1), properties);
        }

        return EvalBinaryExpression(
            tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1), properties);
    }

    private static string EvalBinaryExpression(List<ExpressionToken> tokens, IReadOnlyList<WidgetProperty> properties)
    {
        if (tokens.Count == 0) return "";
        if (tokens.Count == 1) return ResolveLiteralOrProp(tokens[0].Value, properties);

        for (var precedence = 0; precedence <= 8; precedence++)
        {
            var matchIdx = FindOperatorWithPrecedence(tokens, precedence);
            if (matchIdx < 0) continue;

            var op = tokens[matchIdx].Value;

            if (op == "!")
            {
                var operand = EvalBinaryExpression(
                    tokens.GetRange(matchIdx + 1, tokens.Count - matchIdx - 1), properties);
                return IsTruthy(operand) ? "false" : "true";
            }

            var left = EvalBinaryExpression(
                tokens.GetRange(0, matchIdx), properties);
            var right = EvalBinaryExpression(
                tokens.GetRange(matchIdx + 1, tokens.Count - matchIdx - 1), properties);

            return EvalBinaryOp(left, op, right);
        }

        return "";
    }

    private static string EvalBinaryOp(string left, string op, string right)
    {
        var leftNum = ParseNumber(left);
        var rightNum = ParseNumber(right);

        switch (op)
        {
            case "+":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value + rightNum.Value).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                return left + right;

            case "-":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value - rightNum.Value).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                return left;

            case "*":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value * rightNum.Value).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                return left;

            case "/":
                if (leftNum is not null && rightNum is not null && rightNum.Value != 0)
                {
                    return (leftNum.Value / rightNum.Value).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                return left;

            case "==":
                return left == right ? "true" : "false";

            case "!=":
                return left != right ? "true" : "false";

            case ">":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value > rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) > 0 ? "true" : "false";

            case "<":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value < rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) < 0 ? "true" : "false";

            case ">=":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value >= rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) >= 0 ? "true" : "false";

            case "<=":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value <= rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) <= 0 ? "true" : "false";

            case "&&":
                return IsTruthy(left) && IsTruthy(right) ? "true" : "false";

            case "||":
                return IsTruthy(left) || IsTruthy(right) ? "true" : "false";

            default:
                return left;
        }
    }

    private static double? ParseNumber(string value)
    {
        if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
        {
            return num;
        }

        return null;
    }

    private static int FindOperatorWithPrecedence(List<ExpressionToken> tokens, int precedence)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].IsOperator && OperatorPrecedence(tokens[i].Value) == precedence
                                     && tokens[i].Depth == tokens[^1].Depth)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindTopLevelOperator(List<ExpressionToken> tokens, string op, int startIdx = 0)
    {
        for (var i = startIdx; i < tokens.Count; i++)
        {
            if (tokens[i].Value == op && tokens[i].Depth == tokens[0].Depth
                                      && tokens[i].IsOperator)
            {
                return i;
            }
        }

        return -1;
    }

    private static List<ExpressionToken> TokenizeExpression(string expr)
    {
        var tokens = new List<ExpressionToken>();
        var depth = 0;
        var i = 0;

        while (i < expr.Length)
        {
            var c = expr[i];

            if (c == '(')
            {
                depth++;
                var innerExpr = ReadParenthesized(expr, ref i);
                innerExpr = innerExpr[1..^1];
                var innerTokens = TokenizeExpression(innerExpr);
                tokens.Add(new ExpressionToken($"({innerExpr})", false, depth - 1));
                depth--;
            }
            else if (c == '"' || c == '\'')
            {
                var strVal = ReadQuotedString(expr, ref i);
                tokens.Add(new ExpressionToken(strVal, false, depth));
            }
            else if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
            {
                i++;
            }
            else if (c == '!' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("!=", true, depth));
                i += 2;
            }
            else if (c == '=' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("==", true, depth));
                i += 2;
            }
            else if (c == '>' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken(">=", true, depth));
                i += 2;
            }
            else if (c == '<' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("<=", true, depth));
                i += 2;
            }
            else if (c == '&' && i + 1 < expr.Length && expr[i + 1] == '&')
            {
                tokens.Add(new ExpressionToken("&&", true, depth));
                i += 2;
            }
            else if (c == '|' && i + 1 < expr.Length && expr[i + 1] == '|')
            {
                tokens.Add(new ExpressionToken("||", true, depth));
                i += 2;
            }
            else if (c is '+' or '-' or '*' or '/' or '>' or '<' or '!' or '?' or ':')
            {
                tokens.Add(new ExpressionToken(c.ToString(), true, depth));
                i++;
            }
            else
            {
                var ident = ReadIdentifier(expr, ref i);
                tokens.Add(new ExpressionToken(ident, false, depth));
            }
        }

        return tokens;
    }

    private static string ReadParenthesized(string expr, ref int i)
    {
        var start = i;
        var depth = 1;
        i++;

        while (i < expr.Length && depth > 0)
        {
            var c = expr[i];

            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '"' || c == '\'') ReadQuotedString(expr, ref i);

            i++;
        }

        return expr[start..i];
    }

    private static string ReadQuotedString(string expr, ref int i)
    {
        var quote = expr[i];
        var start = i;
        i++;

        while (i < expr.Length && expr[i] != quote)
        {
            if (expr[i] == '\\') i++;
            i++;
        }

        i++;
        return expr[start..i];
    }

    private static string ReadIdentifier(string expr, ref int i)
    {
        var start = i;

        while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] is '.' or '_' or '$'))
        {
            i++;
        }

        return expr[start..i];
    }

    private static int OperatorPrecedence(string op)
    {
        return op switch
        {
            "!" => 8,
            "*" or "/" => 7,
            "+" or "-" => 6,
            ">" or "<" or ">=" or "<=" => 5,
            "==" or "!=" => 4,
            "&&" => 3,
            "||" => 2,
            "?" or ":" => 1,
            _ => 0
        };
    }

    private sealed class ExpressionToken
    {
        public string Value { get; }
        public bool IsOperator { get; }
        public int Depth { get; }

        public ExpressionToken(string value, bool isOperator, int depth)
        {
            Value = value;
            IsOperator = isOperator;
            Depth = depth;
        }
    }

    private static string ResolvePropertyAccessor(string accessor, string? defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue)) return "";

        var parts = accessor.Split('.');
        if (parts.Length <= 1) return defaultValue;

        return defaultValue;
    }

    private static bool IsTruthy(string value)
    {
        return value is not ("false" or "0" or "" or "null" or "undefined" or "NaN");
    }

    private static List<string>? ParseArrayValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            var inner = value[1..^1].Trim();
            if (string.IsNullOrEmpty(inner)) return [];

            return inner.Split(',')
                .Select(s => s.Trim().Trim('"', '\''))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return null;
    }

    private static List<WidgetProperty> CreateIterationProperties(
        IReadOnlyList<WidgetProperty> baseProps, string iteratorName, string itemValue, int index)
    {
        var props = baseProps.ToList();
        props.Add(new WidgetProperty
        {
            Name = iteratorName,
            DefaultValue = itemValue,
            DefaultValueKind = WidgetValueKind.String
        });
        props.Add(new WidgetProperty
        {
            Name = "__index",
            DefaultValue = index.ToString(),
            DefaultValueKind = WidgetValueKind.Number
        });
        return props;
    }

    #endregion

    #region 初始状态 JSON

    private static string BuildInitialStateJson(IReadOnlyList<WidgetProperty> properties)
    {
        if (properties.Count == 0) return "{}";

        var sb = new StringBuilder();
        sb.Append('{');

        var first = true;
        foreach (var prop in properties)
        {
            if (!first) sb.Append(',');
            first = false;

            sb.Append($"\"{prop.Name}\":");

            sb.Append(prop.DefaultValueKind switch
            {
                WidgetValueKind.String => $"\"{EscapeJsonString(prop.DefaultValue ?? "")}\"",
                WidgetValueKind.Boolean => (prop.DefaultValue ?? "false").ToLowerInvariant(),
                WidgetValueKind.Number => prop.DefaultValue ?? "0",
                WidgetValueKind.Array => prop.DefaultValue ?? "[]",
                WidgetValueKind.Object => prop.DefaultValue ?? "{}",
                WidgetValueKind.Expression => FormatExpressionDefaultValue(prop.DefaultValue, prop.TypeName),
                WidgetValueKind.None => FormatNoneDefaultValue(prop.TypeName),
                _ => $"\"{EscapeJsonString(prop.DefaultValue ?? "")}\""
            });
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static string FormatExpressionDefaultValue(string? defaultValue, string typeName)
    {
        if (string.IsNullOrEmpty(defaultValue))
        {
            return "null";
        }

        var trimmed = defaultValue.Trim();

        if (trimmed == "true") return "true";
        if (trimmed == "false") return "false";
        if (trimmed is "null" or "undefined") return "null";

        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
        {
            return trimmed;
        }

        return typeName switch
        {
            "bool" => trimmed == "true" ? "true" : "false",
            "i8" or "i16" or "i32" or "i64" or "u8" or "u16" or "u32" or "u64" or "f32" or "f64" => "0",
            _ => $"\"{EscapeJsonString(trimmed)}\""
        };
    }

    private static string FormatNoneDefaultValue(string typeName)
    {
        return typeName switch
        {
            "bool" => "false",
            "i8" or "i16" or "i32" or "i64" or "u8" or "u16" or "u32" or "u64" or "f32" or "f64" => "0",
            "string" => "\"\"",
            "list" => "[]",
            "map" => "{}",
            _ => "null"
        };
    }

    #endregion

    #region Styles

    private static string RenderStyles(IReadOnlyDictionary<string, string> styles, string componentName)
    {
        if (styles.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        var scope = $"voa-ssr-{ToKebabCase(componentName)}";

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

    private static bool IsExpressionBinding(string value)
    {
        return value.StartsWith("{") && value.EndsWith("}");
    }

    private static string ExtractExpression(string binding)
    {
        return binding[1..^1].Trim();
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string EscapeAttr(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
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