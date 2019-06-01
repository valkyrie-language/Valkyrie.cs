using Asgard.CLI.Compiler;
using Oak.Widget;
using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class AwslReactiveCompilerTests {
    private readonly AwslReactiveCompiler _compiler = new();

    [Fact]
    public void Compile_SimpleComponent_GeneratesFunction() {
        var parseResult = new WidgetParseResult {
            Name = "hello",
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Equal("hello", result.ComponentName);
        Assert.Contains("function Hello(props)", result.JavaScript);
        Assert.Contains("createElement('div')", result.JavaScript);
    }

    [Fact]
    public void Compile_SignalProperty_GeneratesCreateSignal() {
        var parseResult = new WidgetParseResult {
            Name = "counter",
            Properties = [
                new WidgetProperty
                    { Name = "count", TypeName = "i32", DefaultValue = "0", DefaultValueKind = WidgetValueKind.Number }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "span" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createSignal(0)", result.JavaScript);
        Assert.Contains("getcount", result.JavaScript);
        Assert.Contains("setcount", result.JavaScript);
    }

    [Fact]
    public void Compile_BooleanProperty_GeneratesCorrectDefault() {
        var parseResult = new WidgetParseResult {
            Name = "toggle",
            Properties = [
                new WidgetProperty {
                    Name = "active", TypeName = "bool", DefaultValue = "true",
                    DefaultValueKind = WidgetValueKind.Boolean
                }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createSignal(true)", result.JavaScript);
    }

    [Fact]
    public void Compile_StringProperty_GeneratesQuotedDefault() {
        var parseResult = new WidgetParseResult {
            Name = "greeting",
            Properties = [
                new WidgetProperty {
                    Name = "name", TypeName = "string", DefaultValue = "World",
                    DefaultValueKind = WidgetValueKind.String
                }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createSignal(\"World\")", result.JavaScript);
    }

    [Fact]
    public void Compile_ReadonlyPropertyWithoutDefault_GeneratesProp() {
        var parseResult = new WidgetParseResult {
            Name = "card",
            Properties = [
                new WidgetProperty {
                    Name = "title", TypeName = "string", IsReadonly = true, DefaultValue = null,
                    DefaultValueKind = WidgetValueKind.None
                }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createSignal(props?.title)", result.JavaScript);
    }

    [Fact]
    public void Compile_ComputedProperty_GeneratesCreateMemo() {
        var parseResult = new WidgetParseResult {
            Name = "calc",
            Properties = [
                new WidgetProperty
                    { Name = "count", TypeName = "i32", DefaultValue = "0", DefaultValueKind = WidgetValueKind.Number },
                new WidgetProperty {
                    Name = "double_count", TypeName = "i32", DefaultValue = "count * 2",
                    DefaultValueKind = WidgetValueKind.Expression
                }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createMemo", result.JavaScript);
        Assert.Contains("getcount()", result.JavaScript);
    }

    [Fact]
    public void Compile_TextInterpolation_GeneratesDynamicText() {
        var parseResult = new WidgetParseResult {
            Name = "display",
            Properties = [
                new WidgetProperty {
                    Name = "message", TypeName = "string", DefaultValue = "\"hi\"",
                    DefaultValueKind = WidgetValueKind.String
                }
            ],
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "p",
                    Children = [new WidgetInterpolationNode { Expression = "message" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("dynamicText", result.JavaScript);
        Assert.Contains("getmessage()", result.JavaScript);
    }

    [Fact]
    public void Compile_IfConditional_GeneratesConditional() {
        var parseResult = new WidgetParseResult {
            Name = "cond",
            Properties = [
                new WidgetProperty {
                    Name = "visible", TypeName = "bool", DefaultValue = "true",
                    DefaultValueKind = WidgetValueKind.Boolean
                }
            ],
            TemplateNodes = [
                new WidgetIfNode {
                    Condition = "visible",
                    Children = [new WidgetTextNode { Text = "shown" }],
                    ElseChildren = [new WidgetTextNode { Text = "hidden" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("conditional(", result.JavaScript);
        Assert.Contains("getvisible()", result.JavaScript);
    }

    [Fact]
    public void Compile_ForLoop_GeneratesListMap() {
        var parseResult = new WidgetParseResult {
            Name = "list",
            Properties = [
                new WidgetProperty
                    { Name = "items", TypeName = "list", DefaultValue = "[]", DefaultValueKind = WidgetValueKind.Array }
            ],
            TemplateNodes = [
                new WidgetForNode {
                    Iterator = "item",
                    Iterable = "items",
                    Children = [new WidgetInterpolationNode { Expression = "item" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("listMap", result.JavaScript);
        Assert.Contains("getitems()", result.JavaScript);
    }

    [Fact]
    public void Compile_EventBinding_GeneratesAddEventListener() {
        var parseResult = new WidgetParseResult {
            Name = "clickable",
            Methods = [new WidgetMethod { Name = "handleClick", Parameters = "", Body = "count = count + 1" }],
            Properties = [
                new WidgetProperty
                    { Name = "count", TypeName = "i32", DefaultValue = "0", DefaultValueKind = WidgetValueKind.Number }
            ],
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "button",
                    Attributes = new Dictionary<string, string> { { "on:click", "handleClick" } },
                    Children = [new WidgetTextNode { Text = "Click" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("addEventListener('click', handleClick)", result.JavaScript);
        Assert.Contains("function handleClick()", result.JavaScript);
        Assert.Contains("setcount(getcount() + 1)", result.JavaScript);
    }

    [Fact]
    public void Compile_VModel_GeneratesTwoWayBinding() {
        var parseResult = new WidgetParseResult {
            Name = "input",
            Properties = [
                new WidgetProperty {
                    Name = "text", TypeName = "string", DefaultValue = "\"\"", DefaultValueKind = WidgetValueKind.String
                }
            ],
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "input",
                    Attributes = new Dictionary<string, string> { { "v-model", "text" } }
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createEffect", result.JavaScript);
        Assert.Contains("addEventListener('input'", result.JavaScript);
        Assert.Contains("settext(", result.JavaScript);
    }

    [Fact]
    public void Compile_Styles_GeneratesScopedCss() {
        var parseResult = new WidgetParseResult {
            Name = "styled",
            Styles = new Dictionary<string, string>
                { { "container", "padding: 10px" }, { "title", "font-size: 14px" } },
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains(".voa-styled .container { padding: 10px }", result.Css);
        Assert.Contains(".voa-styled .title { font-size: 14px }", result.Css);
    }

    [Fact]
    public void Compile_ComponentReference_GeneratesCreateComponent() {
        _compiler.RegisterComponentNames(["SearchBox"]);

        var parseResult = new WidgetParseResult {
            Name = "app",
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "SearchBox",
                    Attributes = new Dictionary<string, string> { { "query", "searchQuery" } }
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createComponent(SearchBox", result.JavaScript);
    }

    [Fact]
    public void Compile_MicroMethod_GeneratesFunction() {
        var parseResult = new WidgetParseResult {
            Name = "counter",
            Properties = [
                new WidgetProperty
                    { Name = "count", TypeName = "i32", DefaultValue = "0", DefaultValueKind = WidgetValueKind.Number }
            ],
            Methods = [
                new WidgetMethod { Name = "increment", Parameters = "", Body = "count = count + 1", IsMicro = true }
            ],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("function increment()", result.JavaScript);
        Assert.Contains("setcount(getcount() + 1)", result.JavaScript);
    }

    [Fact]
    public void Compile_DecrementOperator_GeneratesSignalUpdate() {
        var parseResult = new WidgetParseResult {
            Name = "counter",
            Properties = [
                new WidgetProperty
                    { Name = "count", TypeName = "i32", DefaultValue = "0", DefaultValueKind = WidgetValueKind.Number }
            ],
            Methods = [new WidgetMethod { Name = "decrement", Parameters = "", Body = "count--" }],
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("setcount(getcount() - 1)", result.JavaScript);
    }

    [Fact]
    public void Compile_StaticAttribute_GeneratesSetAttribute() {
        var parseResult = new WidgetParseResult {
            Name = "link",
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "a",
                    Attributes = new Dictionary<string, string> { { "href", "https://example.com" } },
                    Children = [new WidgetTextNode { Text = "Link" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("setAttribute(", result.JavaScript);
        Assert.Contains("'href'", result.JavaScript);
        Assert.Contains("'https://example.com'", result.JavaScript);
    }

    [Fact]
    public void Compile_EmptyTemplate_GeneratesDivRoot() {
        var parseResult = new WidgetParseResult {
            Name = "empty",
            TemplateNodes = []
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("createElement('div')", result.JavaScript);
    }

    [Fact]
    public void Compile_KebabCaseName_GeneratesPascalCaseFunction() {
        var parseResult = new WidgetParseResult {
            Name = "my-component",
            TemplateNodes = [new WidgetElementNode { TagName = "div" }]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("function MyComponent(props)", result.JavaScript);
    }

    [Fact]
    public void Compile_CamelCaseProperty_GeneratesSnakeCaseAccessor() {
        var parseResult = new WidgetParseResult {
            Name = "test",
            Properties = [
                new WidgetProperty {
                    Name = "isActive", TypeName = "bool", DefaultValue = "false",
                    DefaultValueKind = WidgetValueKind.Boolean
                }
            ],
            TemplateNodes = [
                new WidgetElementNode {
                    TagName = "span",
                    Children = [new WidgetInterpolationNode { Expression = "isActive" }]
                }
            ]
        };

        var result = _compiler.Compile(parseResult, "app");

        Assert.Contains("getis_active()", result.JavaScript);
        Assert.Contains("setis_active", result.JavaScript);
    }
}
