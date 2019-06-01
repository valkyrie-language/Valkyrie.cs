namespace Valkyrie.Tests.PackageManagerTests;

public class LegionConfigTests
{
    [Fact]
    public void SetAndGet_ShouldWorkCorrectly()
    {
        var config = new LegionConfig();

        config.Set("registry", "jsr");
        Assert.Equal("jsr", config.Get("registry"));

        config.Set("timeout", "60");
        Assert.Equal("60", config.Get("timeout"));
    }

    [Fact]
    public void Validate_ShouldReturnTrueForValidConfig()
    {
        var config = new LegionConfig();

        Assert.True(config.Validate());
    }

    [Fact]
    public void Validate_ShouldReturnFalseForInvalidTimeout()
    {
        var config = new LegionConfig();
        config.Set("timeout", "0");

        Assert.False(config.Validate());
    }
}