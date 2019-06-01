using System.IO;
using System.Linq;
using Oak.Widget;
using VOA.ToolChain.Compiler;
using Xunit;
using Xunit.Abstractions;

namespace VOA.ToolChain.Tests;

public sealed class VoaComponentInteractionTests
{
    private readonly ITestOutputHelper _output;
    private readonly AwslReactiveCompiler _compiler = new();
    private readonly WidgetParser _parser = new();

    public VoaComponentInteractionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string CompileComponent(string source)
    {
        var parser = new WidgetParser();
        var parseResult = parser.Parse(source);
        Assert.True(parseResult.Success, string.Join("; ", parseResult.Errors));

        var compiler = new AwslReactiveCompiler();
        var result = compiler.Compile(parseResult);
        Assert.True(result.Success, result.Error);

        return result.CompiledCode;
    }

    [Fact]
    public void Button_VariantPrimary_HasPrimaryClass()
    {
        var source = @"<widget>
    <div class=""voa-btn-wrapper"">
        <button class=""voa-btn primary"">Confirm</button>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("primary", compiled);
        Assert.Contains("voa-btn", compiled);
    }

    [Fact]
    public void Button_Disabled_HasDisabledAttribute()
    {
        var source = @"<widget>
    <script>
        let disabled: bool = true
    </script>
    <button disabled={disabled}>Disabled</button>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("disabled", compiled);
    }

    [Fact]
    public void Button_Click_EmitsEvent()
    {
        var source = @"<widget>
    <button on:click={handleClick}>Click</button>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("addEventListener", compiled);
        Assert.Contains("click", compiled);
    }

    [Fact]
    public void Card_TitleAndSubtitle_Rendered()
    {
        var source = @"<widget>
    <div class=""voa-card"">
        <div class=""voa-card-header"">
            <h3>My Title</h3>
            <span>My Subtitle</span>
        </div>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("My Title", compiled);
        Assert.Contains("My Subtitle", compiled);
    }

    [Fact]
    public void Card_VariantElevated_HasElevatedClass()
    {
        var source = @"<widget>
    <script>
        let variant: string = ""elevated""
    </script>
    <div class=""voa-card {variant}"">Content</div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("elevated", compiled);
    }

    [Fact]
    public void Card_Footer_Rendered()
    {
        var source = @"<widget>
    <div class=""voa-card"">
        <div class=""voa-card-footer"">Footer Content</div>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("Footer Content", compiled);
    }

    [Fact]
    public void Input_ValueChange_EmitsInputEvent()
    {
        var source = @"<widget>
    <script>
        let value: string = """"
    </script>
    <input value={value} on:input={handleInput} />
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("addEventListener", compiled);
        Assert.Contains("input", compiled);
    }

    [Fact]
    public void Input_Error_ShowsError()
    {
        var source = @"<widget>
    <script>
        let error: string = ""Invalid input""
    </script>
    <if condition={error}>
        <span class=""error"">{error}</span>
    <else/>
    </if>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("Invalid input", compiled);
    }

    [Fact]
    public void Input_Disabled_HasDisabledAttribute()
    {
        var source = @"<widget>
    <script>
        let disabled: bool = true
    </script>
    <input disabled={disabled} />
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("disabled", compiled);
    }

    [Fact]
    public void Input_SizeDifferent_HasDifferentRender()
    {
        var sourceSmall = @"<widget>
    <input class=""small"" />
</widget>";

        var sourceLarge = @"<widget>
    <input class=""large"" />
</widget>";

        var smallCompiled = CompileComponent(sourceSmall);
        var largeCompiled = CompileComponent(sourceLarge);

        Assert.Contains("small", smallCompiled);
        Assert.Contains("large", largeCompiled);
        Assert.NotEqual(smallCompiled, largeCompiled);
    }

    [Fact]
    public void List_Items_RendersAllItems()
    {
        var source = @"<widget>
    <script>
        let items: list = [""A"", ""B"", ""C""]
    </script>
    <loop item in {items}>
        <div>{item}</div>
    </loop>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("\"A\"", compiled);
        Assert.Contains("\"B\"", compiled);
        Assert.Contains("\"C\"", compiled);
    }

    [Fact]
    public void List_Empty_NoItems()
    {
        var source = @"<widget>
    <script>
        let items: list = []
    </script>
    <if condition={len(items) == 0}>
        <div>No items</div>
    <else/>
        <loop item in {items}>
            <div>{item}</div>
        </loop>
    </if>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("No items", compiled);
    }

    [Fact]
    public void List_Header_RendersHeader()
    {
        var source = @"<widget>
    <div class=""voa-list-header"">My List</div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("My List", compiled);
    }

    [Fact]
    public void Modal_OpenTrue_Visible()
    {
        var source = @"<widget>
    <script>
        let open: bool = true
    </script>
    <if condition={open}>
        <div class=""voa-modal-overlay"">
            <div class=""voa-modal"">Content</div>
        </div>
    <else/>
    </if>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("voa-modal", compiled);
        Assert.Contains("Content", compiled);
    }

    [Fact]
    public void Modal_OpenFalse_NotVisible()
    {
        var source = @"<widget>
    <script>
        let open: bool = false
    </script>
    <if condition={open}>
        <div class=""voa-modal-overlay"">Content</div>
    <else/>
    </if>
</widget>";

        var compiled = CompileComponent(source);

        Assert.DoesNotContain("Content", compiled);
    }

    [Fact]
    public void Modal_CloseButton_ClosesModal()
    {
        var source = @"<widget>
    <script>
        let open: bool = true
    </script>
    <div class=""voa-modal"">
        <button on:click={handleClose}>Close</button>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("click", compiled);
        Assert.Contains("Close", compiled);
    }

    [Fact]
    public void Tabs_ActiveTab_ActiveClass()
    {
        var source = @"<widget>
    <script>
        let activeTab: string = ""tab2""
    </script>
    <div class=""voa-tabs"">
        <button class=""voa-tab"">Tab 1</button>
        <button class=""voa-tab active"">Tab 2</button>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("active", compiled);
        Assert.Contains("Tab 2", compiled);
    }

    [Fact]
    public void Tabs_TabClick_ChangesActive()
    {
        var source = @"<widget>
    <div class=""voa-tabs"">
        <button on:click={() => setActive(tab)}>Tab</button>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("click", compiled);
        Assert.Contains("addEventListener", compiled);
    }

    [Fact]
    public void Toast_Success_HasSuccessClass()
    {
        var source = @"<widget>
    <script>
        let type: string = ""success""
    </script>
    <div class=""voa-toast {type}"">
        <span>✓</span>
        <span>Operation succeeded</span>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("success", compiled);
        Assert.Contains("Operation succeeded", compiled);
    }

    [Fact]
    public void Toast_Error_HasErrorClass()
    {
        var source = @"<widget>
    <script>
        let type: string = ""error""
    </script>
    <div class=""voa-toast {type}"">
        <span>✕</span>
        <span>Operation failed</span>
    </div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("error", compiled);
        Assert.Contains("Operation failed", compiled);
    }

    [Fact]
    public void Toast_Warning_HasWarningClass()
    {
        var source = @"<widget>
    <script>
        let type: string = ""warning""
    </script>
    <div class=""voa-toast {type}"">Warning</div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("warning", compiled);
    }

    [Fact]
    public void Toast_Info_HasInfoClass()
    {
        var source = @"<widget>
    <script>
        let type: string = ""info""
    </script>
    <div class=""voa-toast {type}"">Info</div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("info", compiled);
    }

    [Fact]
    public void Button_SignalState_ReactiveBinding()
    {
        var source = @"<widget>
    <script>
        let count: i32 = 0
    </script>
    <button on:click={() => count = count + 1}>
        Clicked {count} times
    </button>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("createSignal", compiled);
        Assert.Contains("count", compiled);
    }

    [Fact]
    public void Modal_SizeSmall_HasSmallClass()
    {
        var source = @"<widget>
    <script>
        let size: string = ""small""
    </script>
    <div class=""voa-modal {size}"">Small</div>
</widget>";

        var compiled = CompileComponent(source);

        Assert.Contains("small", compiled);
    }
}