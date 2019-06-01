using System.Collections.Generic;
using System.Linq;
using Asgard.CLI.DevServer;
using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class AwslSsrRendererTests
{
    private readonly AwslSsrRenderer _renderer = new();

    [Fact]
    public void RenderSsr_SimpleComponent_GeneratesHtml()
    {
        var source = @"<widget>
    <div>Hello World</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Equal("hello-world", result.Scope);
        Assert.Contains("data-voa-ssr=\"HelloWorld\"", result.Html);
        Assert.Contains("Hello World", result.Html);
    }

    [Fact]
    public void RenderSsr_ComponentName_KebabCase()
    {
        var source = @"<widget name=""MyComponent"">
    <div>test</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Equal("voa-ssr-my-component", result.Scope);
    }

    [Fact]
    public void RenderSsr_InitialState_JsonSerialized()
    {
        var source = @"<widget>
    <script>
        let count: i32 = 0
    </script>
    <div>{count}</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Contains("data-voa-state=", result.Html);
        Assert.Contains("count", result.Html);
    }

    [Fact]
    public void RenderSsr_HeadTag_ExtractedToHeadTags()
    {
        var source = @"<widget>
    <Head>
        <link rel=""stylesheet"" href=""/theme.css"">
        <meta name=""description"" content=""My page"">
    </Head>
    <div>Content</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Single(result.HeadTags);
        Assert.Contains("link", result.HeadTags[0]);
        Assert.Contains("meta", result.HeadTags[0]);
        Assert.DoesNotContain("<Head>", result.Html);
    }

    [Fact]
    public void RenderSsr_ScriptTag_ExtractedToScriptTags()
    {
        var source = @"<widget>
    <Script src=""/analytics.js""></Script>
    <div>Content</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Single(result.ScriptTags);
        Assert.Contains("analytics.js", result.ScriptTags[0]);
        Assert.DoesNotContain("<Script", result.Html);
    }

    [Fact]
    public void RenderSsr_InlineScriptTag_ExtractedToScriptTags()
    {
        var source = @"<widget>
    <Script>
        console.log('hello')
    </Script>
    <div>Content</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Single(result.ScriptTags);
        Assert.Contains("console.log", result.ScriptTags[0]);
    }

    [Fact]
    public void RenderSsr_Suspense_IsSuspenseTrue()
    {
        var source = @"<widget>
    <Suspense>
        <Fallback>
            <div>Loading...</div>
        </Fallback>
        <div>Content</div>
    </Suspense>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.True(result.IsSuspense);
        Assert.NotNull(result.FallbackHtml);
        Assert.Contains("Loading...", result.FallbackHtml);
    }

    [Fact]
    public void RenderSsr_Suspense_DefaultFallback()
    {
        var source = @"<widget>
    <Suspense>
        <div>Content</div>
    </Suspense>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.True(result.IsSuspense);
        Assert.Contains("Loading...", result.FallbackHtml);
    }

    [Fact]
    public void RenderSsr_ConditionalTruthy_EvaluatesCondition()
    {
        var source = @"<widget>
    <script>
        let visible: bool = true
    </script>
    <if condition={visible}>
        <div>Shown</div>
    </if>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Contains("Shown", result.Html);
        Assert.DoesNotContain("data-voa-state", result.Html);
    }

    [Fact]
    public void RenderSsr_ConditionalFalsy_ShowsElseBranch()
    {
        var source = @"<widget>
    <script>
        let visible: bool = false
    </script>
    <if condition={visible}>
        <div>Shown</div>
    <else/>
        <div>Hidden</div>
    </if>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Contains("Hidden", result.Html);
        Assert.DoesNotContain("Shown", result.Html);
    }

    [Fact]
    public void RenderSsr_ForLoop_IteratesItems()
    {
        var source = @"<widget>
    <script>
        let items: list = [""a"", ""b"", ""c""]
    </script>
    <loop item in {items}>
        <span>{item}</span>
    </loop>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Contains("a", result.Html);
        Assert.Contains("b", result.Html);
        Assert.Contains("c", result.Html);
    }

    [Fact]
    public void RenderSsr_Interpolation_escapesHtml()
    {
        var source = @"<widget>
    <script>
        let msg: string = ""<script>alert('xss')</script>""
    </script>
    <div>{msg}</div>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.DoesNotContain("<script>", result.Html);
        Assert.Contains("&lt;", result.Html);
    }

    [Fact]
    public void RenderSsr_EventBinding_NotRendered()
    {
        var source = @"<widget>
    <button on:click={handleClick}>Click</button>
</widget>";

        var result = _renderer.RenderSsr(source);

        Assert.Contains("<button", result.Html);
        Assert.DoesNotContain("addEventListener", result.Html);
    }

    [Fact]
    public void GenerateSsrPage_IncludesHeadTags()
    {
        var result = new AwslSsrRenderResult
        {
            Html = "<div>Test</div>",
            ComponentName = "Test",
            Scope = "voa-ssr-test",
            InitialStateJson = "{}",
            HeadTags = new List<string> { "<link rel=\"canonical\" href=\"/test\">" }
        };

        var page = AwslSsrRenderer.GenerateSsrPage("test", new[] { result }, null);

        Assert.Contains("<link rel=\"canonical\" href=\"/test\">", page);
    }

    [Fact]
    public void GenerateSsrPage_IncludesScriptTags()
    {
        var result = new AwslSsrRenderResult
        {
            Html = "<div>Test</div>",
            ComponentName = "Test",
            Scope = "voa-ssr-test",
            InitialStateJson = "{}",
            ScriptTags = new List<string> { "<script src=\"/analytics.js\"></script>" }
        };

        var page = AwslSsrRenderer.GenerateSsrPage("test", new[] { result }, null);

        Assert.Contains("<script src=\"/analytics.js\"></script>", page);
    }

    [Fact]
    public void GenerateSsrPage_Suspense_IncludesWrapper()
    {
        var result = new AwslSsrRenderResult
        {
            Html = "<div>Async content</div>",
            ComponentName = "AsyncPage",
            Scope = "voa-ssr-async-page",
            InitialStateJson = "{}",
            IsSuspense = true,
            FallbackHtml = "<div>Loading...</div>"
        };

        var page = AwslSsrRenderer.GenerateSsrPage("async-page", new[] { result }, null);

        Assert.Contains("voa-suspense-wrapper", page);
        Assert.Contains("data-suspense=\"loading\"", page);
        Assert.Contains("MutationObserver", page);
    }

    [Fact]
    public void GenerateHydrationScript_HydratesAllComponents()
    {
        var result = new AwslSsrRenderResult
        {
            Html = "<div>Test</div>",
            ComponentName = "Test",
            Scope = "voa-ssr-test",
            InitialStateJson = "{\"count\": 0}"
        };

        var script = AwslSsrRenderer.GenerateHydrationScript(new[] { result }, "test", null);

        Assert.Contains("hydrateComponent", script);
        Assert.Contains("data-voa-ssr", script);
        Assert.Contains("data-voa-state", script);
    }
}
