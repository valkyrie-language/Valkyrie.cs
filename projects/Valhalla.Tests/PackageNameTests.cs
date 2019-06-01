using Xunit;

namespace Valhalla.Tests;

public class PackageNameTests
{
    [Theory]
    [InlineData("org-pkg", "org.pkg")]
    [InlineData("org_pkg", "org.pkg")]
    [InlineData("ORG_PKG", "org.pkg")]
    [InlineData("Org.Pkg", "org.pkg")]
    [InlineData("org..pkg", "org.pkg")]
    [InlineData(" -org-pkg_ ", "org.pkg")]
    [InlineData("my-org.tool", "my.org.tool")]
    [InlineData("foo", "foo")]
    [InlineData("simple-name", "simple.name")]
    public void 标准化_各种输入形式_应产生规范形式(string raw, string expected)
    {
        var name = new PackageName(raw);
        Assert.Equal(expected, name.Canonical);
    }

    [Fact]
    public void 标准化_纯字母数字_保持不变()
    {
        var name = new PackageName("hello");
        Assert.Equal("hello", name.Canonical);
    }

    [Fact]
    public void 标准化_空字符串_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new PackageName(""));
    }

    [Fact]
    public void 标准化_纯空白_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new PackageName("   "));
    }

    [Fact]
    public void 标准化_连续点_折叠为单个()
    {
        var name = new PackageName("a...b");
        Assert.Equal("a.b", name.Canonical);
    }

    [Fact]
    public void BelongsToOrg_完全匹配_返回真()
    {
        var name = new PackageName("myco.tool");
        Assert.True(name.BelongsToOrg("myco"));
    }

    [Fact]
    public void BelongsToOrg_子命名空间_返回真()
    {
        var name = new PackageName("myco.team.lib");
        Assert.True(name.BelongsToOrg("myco"));
    }

    [Fact]
    public void BelongsToOrg_不同组织_返回假()
    {
        var name = new PackageName("other.tool");
        Assert.False(name.BelongsToOrg("myco"));
    }

    [Fact]
    public void Root_无组织_返回自身()
    {
        var name = new PackageName("tool");
        Assert.Equal("tool", name.Root);
    }

    [Fact]
    public void Root_有组织_返回根前缀()
    {
        var name = new PackageName("myco.team.lib");
        Assert.Equal("myco", name.Root);
    }

    [Fact]
    public void GetPrefix_深度1_返回root()
    {
        var name = new PackageName("myco.team.lib");
        Assert.Equal("myco", name.GetPrefix(1));
    }

    [Fact]
    public void GetPrefix_深度2_返回两级()
    {
        var name = new PackageName("myco.team.lib");
        Assert.Equal("myco.team", name.GetPrefix(2));
    }

    [Fact]
    public void GetPrefix_超出深度_返回完整名()
    {
        var name = new PackageName("myco.team.lib");
        Assert.Equal("myco.team.lib", name.GetPrefix(5));
    }

    [Fact]
    public void 相等性_相同规范名_相等()
    {
        var a = new PackageName("org-pkg");
        var b = new PackageName("ORG_PKG");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}