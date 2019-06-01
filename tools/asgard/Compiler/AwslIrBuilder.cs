using Oak.Widget;

namespace Asgard.CLI.Compiler;

/// <summary>
///     AWSL WidgetParseResult → AwslIr 转换器
///     将 Oak Parser 生成的 AWSL AST 转换为 GGScript 中间表示
/// </summary>
public sealed class AwslIrBuilder
{
    private int _nodeIdCounter;

    public AwslCompilationUnit Build(string source)
    {
        var parser = new WidgetParser();
        var parseResult = parser.Parse(source);

        if (!parseResult.Success)
        {
            throw new InvalidOperationException(
                $"AWSL 解析失败：{string.Join("; ", parseResult.Errors)}");
        }

        return Build(parseResult);
    }

    public AwslCompilationUnit Build(WidgetParseResult parseResult)
    {
        var unit = new AwslCompilationUnit();
        var component = BuildComponent(parseResult);
        unit.Components.Add(component);
        return unit;
    }

    private AwslComponentDecl BuildComponent(WidgetParseResult parseResult)
    {
        var component = new AwslComponentDecl
        {
            Name = parseResult.ComponentName ?? "AnonymousComponent",
            Props = BuildProps(parseResult.Properties),
            Signals = ExtractSignals(parseResult),
            StyledCss = ExtractCss(parseResult),
            Islands = ExtractIslands(parseResult),
            Nodes = parseResult.TemplateNodes.Select(BuildNode).ToList()
        };

        return component;
    }

    private AwslPropsDecl BuildProps(IReadOnlyList<WidgetProperty> properties)
    {
        var props = new AwslPropsDecl();

        foreach (var prop in properties)
        {
            props.Fields.Add(new AwslPropField
            {
                Name = prop.Name,
                Type = prop.Type ?? "string",
                DefaultValue = prop.DefaultValue
            });
        }

        return props;
    }

    private List<AwslSignalDecl> ExtractSignals(WidgetParseResult parseResult)
    {
        var signals = new List<AwslSignalDecl>();

        foreach (var prop in parseResult.Properties.Where(p => p.IsMutable))
        {
            signals.Add(new AwslSignalDecl
            {
                Name = prop.Name,
                Type = prop.Type ?? "any",
                InitialValue = prop.DefaultValue
            });
        }

        return signals;
    }

    private List<AwslStyledCss> ExtractCss(WidgetParseResult parseResult)
    {
        if (string.IsNullOrEmpty(parseResult.Css))
        {
            return [];
        }

        var componentName = parseResult.ComponentName ?? "component";
        var scope = ToKebabCase(componentName);

        return
        [
            new AwslStyledCss
            {
                Scope = $"voa-{scope}",
                Css = parseResult.Css
            }
        ];
    }

    private List<AwslIslandDecl> ExtractIslands(WidgetParseResult parseResult)
    {
        return [];
    }

    private AwslIRNode BuildNode(WidgetTemplateNode node)
    {
        return node switch
        {
            WidgetTextNode t => new AwslTextNode { Text = t.Text },
            WidgetElementNode e => BuildElementNode(e),
            WidgetIfNode i => BuildConditionalNode(i),
            WidgetForNode f => BuildForNode(f),
            WidgetInterpolationNode interp => new AwslInterpolationNode { ExprId = interp.Expression },
            _ => new AwslTextNode { Text = string.Empty }
        };
    }

    private AwslElementNode BuildElementNode(WidgetElementNode node)
    {
        var id = ++_nodeIdCounter;

        var el = new AwslElementNode
        {
            Tag = node.TagName,
            NodeId = id,
            Attrs = BuildAttrs(node),
            Children = node.Children.Select(BuildNode).ToList()
        };

        return el;
    }

    private List<AwslAttrNode> BuildAttrs(WidgetElementNode node)
    {
        var attrs = new List<AwslAttrNode>();

        foreach (var (name, value) in node.Attributes)
        {
            if (name.StartsWith("on:"))
            {
                attrs.Add(new AwslEventAttr
                {
                    Name = name,
                    EventType = name[3..],
                    HandlerId = value
                });
            }
            else if (value.Contains("{") && value.Contains("}"))
            {
                attrs.Add(new AwslDynamicAttr
                {
                    Name = name,
                    ExprId = value.Trim('{', '}')
                });
            }
            else
            {
                attrs.Add(new AwslStaticAttr
                {
                    Name = name,
                    Value = value
                });
            }
        }

        return attrs;
    }

    private AwslConditionalNode BuildConditionalNode(WidgetIfNode node)
    {
        return new AwslConditionalNode
        {
            CondExprId = node.Condition,
            ThenNodes = node.TrueChildren.Select(BuildNode).ToList(),
            ElseNodes = node.FalseChildren.Select(BuildNode).ToList()
        };
    }

    private AwslForNode BuildForNode(WidgetForNode node)
    {
        return new AwslForNode
        {
            VarName = node.VariableName,
            IterableExprId = node.IterableExpression,
            BodyNodes = node.Children.Select(BuildNode).ToList(),
            KeyExprId = node.KeyExpression
        };
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "component";
        }

        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                chars.Add('-');
            }

            chars.Add(char.ToLowerInvariant(name[i]));
        }

        return new string(chars.ToArray());
    }
}