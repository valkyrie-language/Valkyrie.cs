using Xunit;

namespace Valhalla.Tests;

public class ValhallaIncarnationTests
{
    [Fact]
    public void 创建_合法值_成功()
    {
        var inc = new ValhallaIncarnation(1);
        Assert.Equal(1, inc.Number);
    }

    [Fact]
    public void 创建_零值_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new ValhallaIncarnation(0));
    }

    [Fact]
    public void 创建_负值_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new ValhallaIncarnation(-1));
    }

    [Fact]
    public void Next_递增()
    {
        var inc = new ValhallaIncarnation(1);
        var next = inc.Next();
        Assert.Equal(2, next.Number);
    }

    [Fact]
    public void Matches_相同编号_返回真()
    {
        var a = new ValhallaIncarnation(3);
        var b = new ValhallaIncarnation(3);
        Assert.True(a.Matches(b));
    }

    [Fact]
    public void Matches_不同编号_返回假()
    {
        var a = new ValhallaIncarnation(3);
        var b = new ValhallaIncarnation(4);
        Assert.False(a.Matches(b));
    }

    [Fact]
    public void 相等运算符_相同编号_相等()
    {
        var a = new ValhallaIncarnation(5);
        var b = new ValhallaIncarnation(5);
        Assert.True(a == b);
    }
}