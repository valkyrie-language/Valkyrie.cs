using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Asgard.CLI.Compiler;

/// <summary>
///     AWSL 响应式编译器：将 WidgetParseResult AST 编译为使用 voa-runtime.js 的细粒度响应式 JS 代码
///     编译策略：Signal 驱动状态 + Effect 驱动 DOM 更新 + 无 innerHTML
///     设计目标：SolidJS 风格细粒度响应式
/// </summary>
public sealed class AwslReactiveCompiler
{
    private int _varCounter;
    private HashSet<string> _signalNames = [];
    private HashSet<string> _memoNames = [];
    private HashSet<string> _propNames = [];
    private HashSet<string> _eventHandlers = [];
    private HashSet<string> _componentNames = [];
    private HashSet<string> _methodNames = [];

    public void RegisterComponentNames(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _componentNames.Add(name);
            _componentNames.Add(ToPascalCase(name));
        }
    }

    public AwslCompileResult Compile(WidgetParseResult parseResult, string wasmModuleName)
    {
        _varCounter = 0;
        _signalNames = [];
        _memoNames = [];
        _propNames = [];
        _eventHandlers = [];
        _methodNames = parseResult.Methods.Select(m => m.Name).ToHashSet();

        var (islandType, hydrateStrategy) = DetectIslandType(parseResult);

        var name = parseResult.Name;
        var js = new StringBuilder();
        var css = new StringBuilder();

        js.AppendLine($"// 组件：{name}");

        if (islandType is not null)
        {
            js.AppendLine($"// Island 类型：{islandType}, 策略：{hydrateStrategy}");
        }

        js.AppendLine($"function {ToPascalCase(name)}(props) {{");

        EmitVoaDestructure(js);
        js.AppendLine();

        EmitProps(js, parseResult.Properties);
        js.AppendLine();

        EmitSignals(js, parseResult.Properties);
        js.AppendLine();

        EmitMemos(js, parseResult.Properties);
        js.AppendLine();

        var rootVar = EmitTemplate(js, parseResult.TemplateNodes);
        js.AppendLine();

        EmitLifecycleHooks(js, parseResult);
        js.AppendLine();

        EmitEventHandlers(js, parseResult);
        js.AppendLine();

        js.AppendLine($"    return {rootVar};");
        js.AppendLine("}");

        if (islandType is not null)
        {
            js.AppendLine();
            js.AppendLine($"Voa.registerIsland('{name}', {{");
            js.AppendLine($"    factory: {ToPascalCase(name)},");
            js.AppendLine($"    strategy: Voa.IslandStrategy.{GetStrategyEnum(hydrateStrategy!)},");
            js.AppendLine($"    islandType: '{islandType}'");
            js.AppendLine($"}});");
        }

        if (parseResult.Styles.Count > 0)
        {
            var scope = $"voa-{ToKebabCase(name)}";
            foreach (var kvp in parseResult.Styles)
            {
                css.AppendLine($".{scope} .{kvp.Key} {{ {kvp.Value} }}");
            }
        }

        return new AwslCompileResult
        {
            ComponentName = name,
            JavaScript = js.ToString(),
            Css = css.ToString(),
            IslandType = islandType,
            HydrateStrategy = hydrateStrategy
        };
    }

    private static (string? IslandType, string? Strategy) DetectIslandType(WidgetParseResult parseResult)
    {
        var hasMutableProps = parseResult.Properties.Any(p =>
            !p.IsReadonly || p.DefaultValue is not null);
        var hasMethods = parseResult.Methods.Count > 0;
        var hasScript = hasMutableProps || hasMethods;

        if (!hasScript)
        {
            return ("static", null);
        }

        var hasInteractiveMethods = parseResult.Methods.Any(m =>
            m.Name.Contains("click", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("submit", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("toggle", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("select", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("hover", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("drag", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("scroll", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("input", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("change", StringComparison.OrdinalIgnoreCase));

        if (hasInteractiveMethods)
        {
            return ("hydrated", "interaction");
        }

        if (hasMutableProps)
        {
            return ("hydrated", "visible");
        }

        return ("hydrated", "idle");
    }

    private static string GetStrategyEnum(string strategy)
    {
        return strategy switch
        {
            "idle" => "Idle",
            "visible" => "Visible",
            "interaction" => "Interaction",
            "media" => "Media",
            _ => "Load"
        };
    }

    private static void EmitVoaDestructure(StringBuilder js)
    {
        js.AppendLine("    const { createSignal, createEffect, createMemo, createElement,");
        js.AppendLine("            createTextNode, dynamicText, dynamicAttribute, conditional,");
        js.AppendLine("            listMap, insertNode, removeNode, setAttribute, setProperty,");
        js.AppendLine("            createComponent, onMount, onCleanup } = Voa;");
    }

    #region Props 生成

    private void EmitProps(StringBuilder js, IReadOnlyList<WidgetProperty> properties)
    {
        var propDecls = properties.Where(p => p.IsReadonly && p.DefaultValue == null).ToList();
        if (propDecls.Count == 0) return;

        js.AppendLine("    // Props");

        foreach (var prop in propDecls)
        {
            _propNames.Add(prop.Name);
            js.AppendLine($"    const [get_{ToSnakeCase(prop.Name)}, set_{ToSnakeCase(prop.Name)}] = createSignal(props?.{ToSnakeCase(prop.Name)});");
        }
    }

    #endregion

    #region Signal 生成

    private void EmitSignals(StringBuilder js, IReadOnlyList<WidgetProperty> properties)
    {
        var signals = properties
            .Where(p => !_propNames.Contains(p.Name))
            .Where(p => !IsComputedProperty(p))
            .ToList();

        if (signals.Count == 0) return;

        js.AppendLine("    // 响应式状态");

        foreach (var prop in signals)
        {
            _signalNames.Add(prop.Name);
            var defaultValue = FormatDefaultValue(prop);
            js.AppendLine($"    const [get_{ToSnakeCase(prop.Name)}, set_{ToSnakeCase(prop.Name)}] = createSignal({defaultValue});");
        }
    }

    private void EmitMemos(StringBuilder js, IReadOnlyList<WidgetProperty> properties)
    {
        var memos = properties
            .Where(p => !_propNames.Contains(p.Name))
            .Where(IsComputedProperty)
            .ToList();

        if (memos.Count == 0) return;

        js.AppendLine("    // 派生状态");

        foreach (var prop in memos)
        {
            _memoNames.Add(prop.Name);
            var expr = ResolveValueExpr(prop.DefaultValue ?? "undefined");
            js.AppendLine($"    const get_{ToSnakeCase(prop.Name)} = createMemo(() => {expr});");
        }
    }

    private static bool IsComputedProperty(WidgetProperty prop)
    {
        if (prop.DefaultValueKind == WidgetValueKind.Expression && prop.DefaultValue != null)
        {
            return true;
        }

        if (prop.IsReadonly && prop.DefaultValue != null && prop.DefaultValueKind != WidgetValueKind.None)
        {
            var val = prop.DefaultValue.Trim();
            if (val.Contains('+') || val.Contains('-') || val.Contains('*') ||
                val.Contains('/') || val.Contains('?') || val.Contains(':') ||
                val.Contains("&&") || val.Contains("||") || val.Contains("==") ||
                val.Contains("!=") || val.Contains('>') || val.Contains('<') ||
                val.Contains(".length") || val.Contains(".filter") || val.Contains(".map") ||
                val.Contains(".reduce") || val.Contains(".join"))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 模板 → DOM 创建

    private string EmitTemplate(StringBuilder js, IReadOnlyList<WidgetTemplateNode> nodes)
    {
        js.AppendLine("    // DOM 构建");

        if (nodes.Count == 0)
        {
            js.AppendLine("    const __root = createElement('div');");
            return "__root";
        }

        if (nodes.Count == 1)
        {
            return EmitNode(js, nodes[0], "    ");
        }

        js.AppendLine("    const __root = createElement('div');");
        foreach (var node in nodes)
        {
            var childVar = EmitNode(js, node, "    ");
            if (childVar != "null")
            {
                js.AppendLine($"    insertNode(__root, {childVar});");
            }
        }

        return "__root";
    }

    private string EmitNode(StringBuilder js, WidgetTemplateNode node, string indent)
    {
        return node switch
        {
            WidgetTextNode textNode => EmitText(js, textNode, indent),
            WidgetInterpolationNode interpNode => EmitInterpolation(js, interpNode, indent),
            WidgetElementNode elementNode => EmitElement(js, elementNode, indent),
            WidgetIfNode ifNode => EmitConditional(js, ifNode, indent),
            WidgetForNode forNode => EmitList(js, forNode, indent),
            _ => EmitPlaceholder(js, indent)
        };
    }

    private string EmitText(StringBuilder js, WidgetTextNode node, string indent)
    {
        if (string.IsNullOrWhiteSpace(node.Text)) return "null";

        var varName = NextVar();
        js.AppendLine($"{indent}const {varName} = createTextNode({FormatJsString(node.Text)});");
        return varName;
    }

    private string EmitInterpolation(StringBuilder js, WidgetInterpolationNode node, string indent)
    {
        var expr = node.Expression.Trim();
        var varName = NextVar();

        if (expr.Contains('.') && !expr.StartsWith("Math.") && !expr.StartsWith("Date."))
        {
            var resolved = ReplaceIdentifiersWithGetters(expr);
            if (IsReactiveExpr(expr))
            {
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            }
            else
            {
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
            }
        }
        else
        {
            var resolved = ResolveValueExpr(expr);

            if (IsReactiveExpr(expr))
            {
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            }
            else
            {
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
            }
        }

        return varName;
    }

    private string EmitElement(StringBuilder js, WidgetElementNode node, string indent)
    {
        if (IsComponentTag(node.TagName))
        {
            return EmitComponent(js, node, indent);
        }

        var varName = NextVar();
        js.AppendLine($"{indent}const {varName} = createElement('{node.TagName}');");

        foreach (var attr in node.Attributes)
        {
            EmitAttribute(js, varName, attr.Key, attr.Value, indent);
        }

        foreach (var child in node.Children)
        {
            var childVar = EmitNode(js, child, indent);
            if (childVar != "null")
            {
                js.AppendLine($"{indent}insertNode({varName}, {childVar});");
            }
        }

        return varName;
    }

    private string EmitComponent(StringBuilder js, WidgetElementNode node, string indent)
    {
        var varName = NextVar();
        var componentName = node.TagName;

        var propsObj = new StringBuilder();
        propsObj.Append("{");

        var first = true;
        foreach (var attr in node.Attributes)
        {
            if (!first) propsObj.Append(", ");
            first = false;

            var propName = ToSnakeCase(attr.Key);
            var propValue = IsSignalName(attr.Value) || IsReactiveExpr(attr.Value)
                ? ResolveValueExpr(attr.Value)
                : FormatJsString(attr.Value);

            propsObj.Append($"{propName}: {propValue}");
        }

        propsObj.Append("}");

        js.AppendLine($"{indent}const {varName} = createComponent({componentName}, {propsObj});");

        return varName;
    }

    private void EmitAttribute(StringBuilder js, string elVar, string name, string value, string indent)
    {
        if (name == "v-model" || name == "vModel")
        {
            EmitVModel(js, elVar, value, indent);
            return;
        }

        if (name.StartsWith("on:") || name.StartsWith("on"))
        {
            var eventName = name.StartsWith("on:") ? name[3..] : name[2..].ToLowerInvariant();
            _eventHandlers.Add(value);
            js.AppendLine($"{indent}{elVar}.addEventListener('{eventName}', {value});");
            return;
        }

        if (IsSignalName(value))
        {
            var getter = $"get_{ToSnakeCase(value)}()";

            if (name == "value")
            {
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', {getter}); }});");
            }
            else if (name == "checked")
            {
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'checked', {getter}); }});");
            }
            else if (name == "class" || name == "className")
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'class', () => {getter});");
            }
            else if (name == "style")
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'style', () => {getter});");
            }
            else
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, '{name}', () => String({getter}));");
            }

            return;
        }

        if (IsReactiveExpr(value))
        {
            var resolved = ResolveValueExpr(value);

            if (name == "value")
            {
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', {resolved}); }});");
            }
            else if (name == "checked")
            {
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'checked', {resolved}); }});");
            }
            else if (name == "class" || name == "className")
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'class', () => {resolved});");
            }
            else if (name == "style")
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'style', () => {resolved});");
            }
            else
            {
                js.AppendLine($"{indent}dynamicAttribute({elVar}, '{name}', () => String({resolved}));");
            }

            return;
        }

        if (name == "class" || name == "className")
        {
            js.AppendLine($"{indent}setAttribute({elVar}, 'class', {FormatJsString(value)});");
        }
        else if (name == "value")
        {
            js.AppendLine($"{indent}setProperty({elVar}, 'value', {FormatJsString(value)});");
        }
        else if (name == "checked")
        {
            js.AppendLine($"{indent}setProperty({elVar}, 'checked', true);");
        }
        else if (value == name)
        {
            js.AppendLine($"{indent}setAttribute({elVar}, '{name}', true);");
        }
        else
        {
            js.AppendLine($"{indent}setAttribute({elVar}, '{name}', {FormatJsString(value)});");
        }
    }

    private void EmitVModel(StringBuilder js, string elVar, string signalName, string indent)
    {
        if (!IsSignalName(signalName)) return;

        var pascal = ToSnakeCase(signalName);
        js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', get_{pascal}()); }});");
        js.AppendLine($"{indent}{elVar}.addEventListener('input', (e) => {{ set_{pascal}(e.target.value); }});");
    }

    private string EmitConditional(WidgetIfNode node, string indent = "    ")
    {
        var inner = indent + "    ";
        var elVar = _varCounter.Use("cond");

        var condition = ReplaceIdentifiersWithGetters(node.Condition);

        js.AppendLine($"{indent}let {elVar};");

        js.AppendLine($"{indent}createEffect(() => {{");
        js.AppendLine($"{inner}if ({condition} != null && {condition} !== false) {{");

        var thenEl = EmitChildren(node.Children, inner + "    ");
        js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {thenEl});");
        js.AppendLine($"{inner}}} else {{");

        var elseChildren = node.ElseChildren;
        if (elseChildren.Count == 1 && elseChildren[0] is WidgetIfNode elif)
        {
            var elifEl = EmitElement(js, elif, inner + "    ");
            js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {elifEl});");
        }
        else
        {
            var elseEl = EmitChildren(elseChildren, inner + "    ");
            js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {elseEl});");
        }

        js.AppendLine($"{inner}}}");
        js.AppendLine($"{indent}}});");

        return elVar;
    }

    private string EmitList(StringBuilder js, WidgetForNode node, string indent)
    {
        var iterable = ReplaceIdentifiersWithGetters(node.Iterable);
        var varName = NextVar();

        var mapFn = BuildMapFn(js, node.Iterator, node.Children, indent + "    ");

        js.AppendLine($"{indent}const {varName} = listMap(() => {iterable}, {mapFn});");

        return varName;
    }

    private string BuildFactory(StringBuilder js, IReadOnlyList<WidgetTemplateNode> nodes, string indent)
    {
        if (nodes.Count == 0) return "() => createElement('span')";
        if (nodes.Count == 1)
        {
            var childVar = EmitNode(js, nodes[0], indent);
            return $"() => {childVar}";
        }

        var rootVar = NextVar();
        js.AppendLine($"{indent}const {rootVar} = createElement('div');");

        foreach (var node in nodes)
        {
            var childVar = EmitNode(js, node, indent);
            if (childVar != "null")
            {
                js.AppendLine($"{indent}insertNode({rootVar}, {childVar});");
            }
        }

        return $"() => {rootVar}";
    }

    private string BuildMapFn(StringBuilder js, string iterator, IReadOnlyList<WidgetTemplateNode> nodes, string indent)
    {
        js.AppendLine($"{indent}const __item = ({iterator}, __idx) => {{");

        if (nodes.Count == 0)
        {
            js.AppendLine($"{indent}    return createElement('span');");
        }
        else if (nodes.Count == 1)
        {
            var childVar = EmitNodeWithIterator(js, nodes[0], indent + "    ", iterator);
            js.AppendLine($"{indent}    return {childVar};");
        }
        else
        {
            var rootVar = NextVar();
            js.AppendLine($"{indent}    const {rootVar} = createElement('div');");

            foreach (var childNode in nodes)
            {
                var childVar = EmitNodeWithIterator(js, childNode, indent + "    ", iterator);
                if (childVar != "null")
                {
                    js.AppendLine($"{indent}    insertNode({rootVar}, {childVar});");
                }
            }

            js.AppendLine($"{indent}    return {rootVar};");
        }

        js.AppendLine($"{indent}}};");
        return "__item";
    }

    private string EmitNodeWithIterator(StringBuilder js, WidgetTemplateNode node, string indent, string iterator)
    {
        return node switch
        {
            WidgetElementNode elementNode => EmitElementWithIterator(js, elementNode, indent, iterator),
            WidgetInterpolationNode interpNode => EmitInterpolationWithIterator(js, interpNode, indent, iterator),
            _ => EmitNode(js, node, indent)
        };
    }

    private string EmitInterpolationWithIterator(StringBuilder js, WidgetInterpolationNode node, string indent, string iterator)
    {
        var expr = node.Expression.Trim();
        var varName = NextVar();

        if (expr.Contains('.') && expr.StartsWith(iterator))
        {
            js.AppendLine($"{indent}const {varName} = createTextNode(String({expr}));");
        }
        else
        {
            var resolved = ResolveValueExpr(expr);
            if (IsReactiveExpr(expr))
            {
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            }
            else
            {
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
            }
        }

        return varName;
    }

    private string EmitElementWithIterator(StringBuilder js, WidgetElementNode node, string indent, string iterator)
    {
        var varName = NextVar();
        js.AppendLine($"{indent}const {varName} = createElement('{node.TagName}');");
        js.AppendLine($"{indent}{varName}.__voa_idx = __idx;");

        foreach (var attr in node.Attributes)
        {
            EmitAttributeWithIterator(js, varName, attr.Key, attr.Value, indent, iterator);
        }

        foreach (var child in node.Children)
        {
            var childVar = EmitNodeWithIterator(js, child, indent, iterator);
            if (childVar != "null")
            {
                js.AppendLine($"{indent}insertNode({varName}, {childVar});");
            }
        }

        return varName;
    }

    private void EmitAttributeWithIterator(StringBuilder js, string elVar, string name, string value, string indent, string iterator)
    {
        if (name == "v-model" || name == "vModel")
        {
            EmitVModel(js, elVar, value, indent);
            return;
        }

        if (name.StartsWith("on:") || name.StartsWith("on"))
        {
            var eventName = name.StartsWith("on:") ? name[3..] : name[2..].ToLowerInvariant();
            _eventHandlers.Add(value);
            js.AppendLine($"{indent}{elVar}.addEventListener('{eventName}', {value});");
            return;
        }

        if (value.Contains('.') && value.StartsWith(iterator))
        {
            var prop = value[(iterator.Length + 1)..];

            if (name == "checked")
            {
                js.AppendLine($"{indent}setProperty({elVar}, 'checked', {iterator}.{prop});");
            }
            else if (name == "value")
            {
                js.AppendLine($"{indent}setProperty({elVar}, 'value', {iterator}.{prop});");
            }
            else
            {
                js.AppendLine($"{indent}setAttribute({elVar}, '{name}', {iterator}.{prop});");
            }

            return;
        }

        EmitAttribute(js, elVar, name, value, indent);
    }

    #endregion

    #region 事件处理器生成

    private void EmitLifecycleHooks(StringBuilder js, WidgetParseResult parseResult)
    {
        var hasMount = parseResult.Methods.Any(m => m.Name == "onMount" || m.Name == "mounted");
        var hasDestroy = parseResult.Methods.Any(m => m.Name == "onDestroy" || m.Name == "destroyed" || m.Name == "onCleanup");

        var needsLifecycle = hasMount || hasDestroy || _eventHandlers.Count > 0;

        if (!needsLifecycle) return;

        js.AppendLine("    // 生命周期");

        if (hasMount)
        {
            var mountMethod = parseResult.Methods.First(m => m.Name == "onMount" || m.Name == "mounted");
            var body = TranslateMethodBody(mountMethod.Body);
            js.AppendLine($"    onMount(() => {{ {body} }});");
        }

        if (hasDestroy)
        {
            var destroyMethod = parseResult.Methods.First(m => m.Name == "onDestroy" || m.Name == "destroyed" || m.Name == "onCleanup");
            var body = TranslateMethodBody(destroyMethod.Body);
            js.AppendLine($"    onCleanup(() => {{ {body} }});");
        }
    }

    private void EmitEventHandlers(StringBuilder js, WidgetParseResult parseResult)
    {
        if (parseResult.Methods.Count == 0 && _eventHandlers.Count == 0) return;

        js.AppendLine("    // 微过程");

        foreach (var method in parseResult.Methods)
        {
            if (method.Name is "onMount" or "mounted" or "onDestroy" or "destroyed" or "onCleanup")
            {
                continue;
            }

            var body = TranslateMethodBody(method.Body);
            var paramsStr = string.IsNullOrEmpty(method.Parameters) ? "event" : method.Parameters;

            js.AppendLine($"    function {method.Name}({paramsStr}) {{");

            if (!string.IsNullOrWhiteSpace(body))
            {
                js.AppendLine($"        {body}");
            }
            else
            {
                js.AppendLine($"        console.debug('事件触发: {method.Name}', event);");
            }

            js.AppendLine($"    }}");
        }

        foreach (var handler in _eventHandlers)
        {
            var alreadyDefined = parseResult.Methods.Any(m => m.Name == handler);
            if (alreadyDefined) continue;

            js.AppendLine($"    function {handler}(event) {{");
            js.AppendLine($"        console.warn('事件处理桩: {handler}', event);");
            js.AppendLine($"    }}");
        }
    }

    private string TranslateMethodBody(string body)
    {
        var lines = body.Split('\n');
        var result = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var translated = TranslateStatement(trimmed);
            result.AppendLine($"        {translated}");
        }

        return result.ToString().TrimEnd();
    }

    private string TranslateStatement(string stmt)
    {
        var s = stmt.Trim();

        if (s.StartsWith("return "))
        {
            var expr = s[7..].TrimEnd(';');
            return $"return {TranslateExpr(expr)};";
        }

        if (s.StartsWith("let ") || s.StartsWith("const ") || s.StartsWith("var "))
        {
            return TranslateVarDeclaration(s);
        }

        if (s.StartsWith("if "))
        {
            return TranslateExpr(s);
        }

        var assignMatch = Regex.Match(s, @"^(\w+(?:\.\w+)*)\s*=\s*(.+);?$");
        if (assignMatch.Success)
        {
            var target = assignMatch.Groups[1].Value;
            var value = assignMatch.Groups[2].Value.TrimEnd(';');
            return TranslateAssignment(target, value);
        }

        if (s.EndsWith("++"))
        {
            var name = s[..^2].Trim();
            if (IsSignalName(name))
            {
                return $"set{ToSnakeCase(name)}(get{ToSnakeCase(name)}() + 1);";
            }

            return s;
        }

        if (s.EndsWith("--"))
        {
            var name = s[..^2].Trim();
            if (IsSignalName(name))
            {
                return $"set_{ToSnakeCase(name)}(get_{ToSnakeCase(name)}() - 1);";
            }

            return s;
        }

        return TranslateExpr(s);
    }

    private string TranslateVarDeclaration(string s)
    {
        var declMatch = Regex.Match(s, @"^(let|const|var)\s+(\w+)(?::\s*\w+)?\s*=\s*(.+?);?$");
        if (!declMatch.Success) return s;

        var keyword = declMatch.Groups[1].Value;
        var name = declMatch.Groups[2].Value;
        var value = declMatch.Groups[3].Value.TrimEnd(';');

        return $"{keyword} {name} = {TranslateExpr(value)};";
    }

    private string TranslateAssignment(string target, string valueExpr)
    {
        if (target.Contains('.'))
        {
            return $"{target} = {TranslateExpr(valueExpr)};";
        }

        if (IsSignalName(target))
        {
            var translated = TranslateExpr(valueExpr);
            return $"set_{ToSnakeCase(target)}({translated});";
        }

        return $"{target} = {TranslateExpr(valueExpr)};";
    }

    private string TranslateExpr(string expr)
    {
        return ReplaceIdentifiersWithGetters(expr);
    }

    #endregion

    #region 表达式解析

    private bool IsSignalName(string name)
    {
        return _signalNames.Contains(name) || _memoNames.Contains(name) || _propNames.Contains(name);
    }

    private bool IsComponentTag(string tagName)
    {
        if (tagName.Length > 0 && char.IsUpper(tagName[0])) return true;

        return _componentNames.Contains(tagName);
    }

    private string ResolveValueExpr(string expr)
    {
        var trimmed = expr.Trim();

        if (trimmed.StartsWith("state.") || trimmed.StartsWith("props."))
        {
            return ReplaceIdentifiersWithGetters(trimmed);
        }

        if (IsJsKeyword(trimmed) || IsLiteral(trimmed))
        {
            return trimmed;
        }

        if (trimmed.EndsWith("()"))
        {
            return trimmed;
        }

        if (IsSignalName(trimmed))
        {
            return $"get_{ToSnakeCase(trimmed)}()";
        }

        if (_methodNames.Contains(trimmed))
        {
            return $"{trimmed}()";
        }

        if (trimmed.Contains(".") && !trimmed.StartsWith("Math.") && !trimmed.StartsWith("Date."))
        {
            return ReplaceIdentifiersWithGetters(trimmed);
        }

        if (trimmed.Contains("&&") || trimmed.Contains("||") || trimmed.Contains("==") ||
            trimmed.Contains("!=") || trimmed.Contains(">") || trimmed.Contains("<") ||
            trimmed.Contains("+") || trimmed.Contains("-") || trimmed.Contains("*") ||
            trimmed.Contains("/") || trimmed.Contains("?") || trimmed.Contains(":"))
        {
            return ReplaceIdentifiersWithGetters(trimmed);
        }

        if (trimmed.StartsWith("!") || trimmed.StartsWith("(") || trimmed.StartsWith("["))
        {
            return ReplaceIdentifiersWithGetters(trimmed);
        }

        return ReplaceIdentifiersWithGetters(trimmed);
    }

    private string ReplaceIdentifiersWithGetters(string expr)
    {
        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in Regex.Matches(expr, @"\b([a-zA-Z_]\w*)\b"))
        {
            result.Append(expr[lastIndex..match.Index]);
            var name = match.Groups[1].Value;

            if (IsJsKeyword(name) || IsLiteral(name) ||
                name is "Math" or "Date" or "JSON" or "console" or "window" or "document"
                    or "this" or "undefined" or "NaN" or "Infinity" or "String" or "Number"
                    or "Boolean" or "Array" or "Object" or "parseInt" or "parseFloat"
                    or "props")
            {
                result.Append(name);
            }
            else if (IsSignalName(name))
            {
                result.Append($"get_{ToSnakeCase(name)}()");
            }
            else
            {
                result.Append(name);
            }

            lastIndex = match.Index + match.Length;
        }

        result.Append(expr[lastIndex..]);
        return result.ToString();
    }

    private bool IsReactiveExpr(string expr)
    {
        var trimmed = expr.Trim();

        if (IsJsKeyword(trimmed) || IsLiteral(trimmed)) return false;
        if (trimmed.StartsWith("'") || trimmed.StartsWith("\"") || trimmed.StartsWith("`")) return false;
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;

        foreach (Match match in Regex.Matches(expr, @"\b([a-zA-Z_]\w*)\b"))
        {
            var name = match.Groups[1].Value;
            if (IsSignalName(name)) return true;
        }

        return false;
    }

    #endregion

    #region 工具方法

    private string NextVar() => $"__v{_varCounter++}";

    private static string FormatDefaultValue(WidgetProperty prop)
    {
        if (prop.DefaultValueKind == WidgetValueKind.Expression)
        {
            return "null";
        }

        if (prop.DefaultValueKind == WidgetValueKind.None)
        {
            return prop.TypeName switch
            {
                "bool" => "false",
                "i8" or "i16" or "i32" or "i64" => "0",
                "u8" or "u16" or "u32" or "u64" => "0",
                "f32" or "f64" => "0.0",
                "string" => "\"\"",
                "list" or "list<" => "[]",
                "map" or "map<" => "{}",
                _ => "null"
            };
        }

        return prop.DefaultValueKind switch
        {
            WidgetValueKind.String => $"\"{prop.DefaultValue}\"",
            WidgetValueKind.Boolean => prop.DefaultValue?.ToLower() ?? "false",
            WidgetValueKind.Number => prop.DefaultValue ?? "0",
            WidgetValueKind.Array => prop.DefaultValue ?? "[]",
            WidgetValueKind.Object => prop.DefaultValue ?? "{}",
            _ => prop.DefaultValue ?? "null"
        };
    }

    private static string FormatJsString(string text)
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

    private static bool IsJsKeyword(string s)
    {
        return s is "if" or "else" or "for" or "while" or "do" or "switch" or "case" or "break"
            or "continue" or "return" or "function" or "var" or "let" or "const" or "class"
            or "new" or "typeof" or "instanceof" or "void" or "delete" or "in" or "of"
            or "try" or "catch" or "finally" or "throw" or "async" or "await" or "yield"
            or "import" or "export" or "default" or "from" or "true" or "false" or "null"
            or "undefined" or "micro";
    }

    private static bool IsLiteral(string s)
    {
        return s is "true" or "false" or "null" or "undefined" or "NaN" or "Infinity"
               || double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
               || (s.StartsWith("\"") && s.EndsWith("\""))
               || (s.StartsWith("'") && s.EndsWith("'"))
               || (s.StartsWith("`") && s.EndsWith("`"));
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();
        sb.Append(char.ToUpperInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '-' || c == '_' || c == ' ' || c == '.')
            {
                if (i + 1 < name.Length)
                {
                    sb.Append(char.ToUpperInvariant(name[i + 1]));
                    i++;
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '-' || c == ' ' || c == '.')
            {
                sb.Append('_');
            }
            else if (c == '_')
            {
                sb.Append('_');
            }
            else if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
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

    private static string EmitPlaceholder(StringBuilder js, string indent)
    {
        var varName = "__ph";
        js.AppendLine($"{indent}const {varName} = createElement('span');");
        return varName;
    }

    #endregion
}