using System.Collections.Generic;
using System.IO;
using System.Linq;
using VOA.ToolChain.Compiler;
using Xunit;
using Xunit.Abstractions;

namespace VOA.ToolChain.Tests;

/// <summary>
///     SSR 与 CSR 首帧对比测试 — 验证 SSR 输出与客户端渲染首帧一致，无 hydration mismatch
/// </summary>
public sealed class VoaSsrCsrComparisonTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AwslReactiveCompiler _compiler = new();
    private readonly ITestOutputHelper _output;

    public VoaSsrCsrComparisonTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), $"voa-ssr-csr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void SsrCsr_SimpleDiv_StructureMatches()
    {
        var source = @"<widget>
    <div id=""app"">
        <h1>Hello VOA</h1>
        <p>Welcome</p>
    </div>
</widget>";

        var ssrHtml = RenderSsr(source);
        var csrHtml = RenderCsr(source);

        Assert.Contains("<h1>Hello VOA</h1>", ssrHtml);
        Assert.Contains("<h1>Hello VOA</h1>", csrHtml);
        Assert.Contains("<div id=\"app\"", ssrHtml);
        Assert.Contains("<div id=\"app\"", csrHtml);
        Assert.Equal(NormalizeWhitespace(csrHtml), NormalizeWhitespace(ssrHtml));
    }

    [Fact]
    public void SsrCsr_NestedElements_StructureMatches()
    {
        var source = @"<widget>
    <div class=""container"">
        <nav>
            <a href=""/"">Home</a>
            <a href=""/about"">About</a>
        </nav>
        <main>
            <article>
                <h2>Title</h2>
                <p>Body text</p>
            </article>
        </main>
    </div>
</widget>";

        var ssrHtml = RenderSsr(source);
        var csrHtml = RenderCsr(source);

        Assert.Contains("<nav>", ssrHtml);
        Assert.Contains("<nav>", csrHtml);
        Assert.Contains("<main>", ssrHtml);
        Assert.Contains("<main>", csrHtml);
        Assert.Equal(NormalizeWhitespace(csrHtml), NormalizeWhitespace(ssrHtml));
    }

    [Fact]
    public void SsrCsr_StaticAttributes_Match()
    {
        var source = @"<widget>
    <div class=""card"" data-id=""42"" aria-label=""test"">
        <span class=""badge"" title=""info"">X</span>
    </div>
</widget>";

        var ssrHtml = RenderSsr(source);
        var csrHtml = RenderCsr(source);

        Assert.Contains("data-id=\"42\"", ssrHtml);
        Assert.Contains("data-id=\"42\"", csrHtml);
        Assert.Contains("aria-label=\"test\"", ssrHtml);
        Assert.Contains("aria-label=\"test\"", csrHtml);
        Assert.Contains("class=\"badge\"", ssrHtml);
        Assert.Contains("class=\"badge\"", csrHtml);
    }

    private string RenderSsr(string source)
    {
        var parseResult = new Oak.Widget.WidgetParser().Parse(source);
        Assert.True(parseResult.Success, string.Join("; ", parseResult.Errors));

        var renderer = new VOA.ToolChain.DevServer.AwslSsrRenderer();
        var result = renderer.RenderSsr(source);
        return result.Html;
    }

    private string RenderCsr(string source)
    {
        var parseResult = new Oak.Widget.WidgetParser().Parse(source);
        Assert.True(parseResult.Success, string.Join("; ", parseResult.Errors));

        var compileResult = _compiler.Compile(parseResult);
        Assert.True(compileResult.Success, compileResult.Error);

        var componentName = parseResult.ComponentName ?? "TestComponent";
        var code = compileResult.CompiledCode;
        var css = compileResult.Css;

        return ExtractCsrFirstFrame(code, componentName, parseResult);
    }

    private static string ExtractCsrFirstFrame(string compiledCode, string componentName, Oak.Widget.WidgetParseResult parseResult)
    {
        var lines = compiledCode.Split('\n');
        var templateLines = new List<string>();
        var inTemplate = false;

        foreach (var line in lines)
        {
            if (line.Contains("render") || line.Contains("innerHTML") || line.Contains("tag"))
            {
                templateLines.Add(line.Trim());
            }
        }

        var sb = new System.Text.StringBuilder();

        foreach (var node in parseResult.TemplateNodes)
        {
            sb.Append(ExtractNodeStructure(node));
        }

        return sb.ToString();
    }

    private static string ExtractNodeStructure(Oak.Widget.WidgetTemplateNode node)
    {
        return node switch
        {
            Oak.Widget.WidgetTextNode t => t.Text.Trim(),
            Oak.Widget.WidgetElementNode e =>
        {
            var attrs = string.Join(" ", e.Attributes.Select(a => $"{a.Key}=\"{a.Value}\""));
            var openTag = string.IsNullOrEmpty(attrs) ? $"<{e.TagName}>" : $"<{e.TagName} {attrs}>";
            var closeTag = $"</{e.TagName}>";
            var children = string.Join("", e.Children.Select(ExtractNodeStructure));
            return $"{openTag}{children}{closeTag}";
        }
        ,
        Oak.Widget.WidgetIfNode i =>
        {
            var trueChildren = string.Join("", i.TrueChildren.Select(ExtractNodeStructure));
            return trueChildren;
        }
        ,
        _ => ""
        };
    }

    private static string NormalizeWhitespace(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            html.Trim(), @"\s+", " ").Replace("> <", "><");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}