using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;

namespace Valkyrie.Tests.ParserTests;

public class ChannelConditionalParserTests : ValkyrieParserTestBase
{
    private ProgramRoot Parse(string source)
    {
        return ParseWithTimeout(source, ValkyrieLanguage.Standard);
    }

    #region 基本 Channel 条件编译解析

    [Fact]
    public void Parse_ChannelWithSingleFunction_ShouldReturnChannelConditionalDecl()
    {
        var source = @"
#channel(steam) {
    fn buyItem(itemId: i32) { }
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        Assert.Single(result.Declarations);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal("steam", channel.ChannelName);
        Assert.Single(channel.Declarations);
        var fn = Assert.IsType<MicroDeclaration>(channel.Declarations[0]);
        Assert.Equal("buyItem", fn.Name);
    }

    [Fact]
    public void Parse_ChannelWithMultipleDeclarations_ShouldContainAll()
    {
        var source = @"
#channel(mobile) {
    micro touchHandler() { }
    micro swipeHandler() { }
    micro pinchHandler() { }
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal("mobile", channel.ChannelName);
        Assert.Equal(3, channel.Declarations.Count);
    }

    [Fact]
    public void Parse_ChannelWithEmptyBody_ShouldHaveNoDeclarations()
    {
        var source = "#channel(console) { }";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal("console", channel.ChannelName);
        Assert.Empty(channel.Declarations);
    }

    [Fact]
    public void Parse_MultipleChannels_ShouldReturnMultipleChannelDecls()
    {
        var source = @"
#channel(steam) {
    micro steamInit() { }
}

#channel(mobile) {
    micro mobileInit() { }
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        Assert.Equal(2, result.Declarations.Count);

        var steamChannel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal("steam", steamChannel.ChannelName);

        var mobileChannel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[1]);
        Assert.Equal("mobile", mobileChannel.ChannelName);
    }

    #endregion

    #region Channel 内部复杂声明解析

    [Fact]
    public void Parse_ChannelWithComponentAndSystem_ShouldParseCorrectly()
    {
        var source = @"
#channel(steam) {
    component SteamInventory {
        itemId: i32
        quantity: i32
    }

    system SteamSync {
        query = Query.all(SteamInventory)
        micro sync() { }
    }
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal(2, channel.Declarations.Count);

        var comp = Assert.IsType<ComponentDeclaration>(channel.Declarations[0]);
        Assert.Equal("SteamInventory", comp.Name);

        var sys = Assert.IsType<SystemDeclaration>(channel.Declarations[1]);
        Assert.Equal("SteamSync", sys.Name);
    }

    [Fact]
    public void Parse_ChannelWithImports_ShouldParseCorrectly()
    {
        var source = @"
#channel(mobile) {
    import mobile.platform
    import mobile.input
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal(2, channel.Declarations.Count);

        var import1 = Assert.IsType<UsingDeclaration>(channel.Declarations[0]);
        Assert.Equal("mobile.platform", import1.ModulePath);

        var import2 = Assert.IsType<UsingDeclaration>(channel.Declarations[1]);
        Assert.Equal("mobile.input", import2.ModulePath);
    }

    [Fact]
    public void Parse_ChannelWithVariables_ShouldParseCorrectly()
    {
        var source = @"
#channel(debug) {
    let isDebug: bool = true
    let debugLevel: int = 3
}
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        var channel = Assert.IsType<ChannelConditionalDecl>(result.Declarations[0]);
        Assert.Equal(2, channel.Declarations.Count);

        var var1 = Assert.IsType<LetDeclaration>(channel.Declarations[0]);
        Assert.Equal("isDebug", var1.Name);

        var var2 = Assert.IsType<LetDeclaration>(channel.Declarations[1]);
        Assert.Equal("debugLevel", var2.Name);
    }

    #endregion

    #region Channel 与普通声明混合解析

    [Fact]
    public void Parse_ChannelMixedWithTopLevelDeclarations_ShouldWorkCorrectly()
    {
        var source = @"
import core.math

component Position {
    x: float
    y: float
}

#channel(steam) {
    micro buyItem(itemId: i32) { }
}

micro update() { }
";

        var result = Parse(source);

        AssertParseResultNotNull(result);
        Assert.Equal(4, result.Declarations.Count);

        Assert.IsType<UsingDeclaration>(result.Declarations[0]);
        Assert.IsType<ComponentDeclaration>(result.Declarations[1]);
        Assert.IsType<ChannelConditionalDecl>(result.Declarations[2]);
        Assert.IsType<MicroDeclaration>(result.Declarations[3]);
    }

    #endregion
}
