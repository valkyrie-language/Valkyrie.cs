namespace Valkyrie.Tests.PackageManagerTests;

public class SemanticVersionTests
{
    [Fact]
    public void Parse_ShouldParseBasicVersion()
    {
        var version = SemanticVersion.Parse("1.2.3");

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Null(version.PreRelease);
        Assert.Null(version.BuildMetadata);
    }

    [Fact]
    public void Parse_ShouldParsePreReleaseVersion()
    {
        var version = SemanticVersion.Parse("1.2.3-alpha.1");

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal("alpha.1", version.PreRelease);
    }

    [Fact]
    public void Parse_ShouldParseBuildMetadata()
    {
        var version = SemanticVersion.Parse("1.2.3+build.123");

        Assert.Equal(1, version.Major);
        Assert.Equal("build.123", version.BuildMetadata);
    }

    [Fact]
    public void Parse_ShouldParseVersionWithVPrefix()
    {
        var version = SemanticVersion.Parse("v1.2.3");

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
    }

    [Fact]
    public void TryParse_ShouldReturnFalseForInvalidVersion()
    {
        bool result = SemanticVersion.TryParse("not-a-version", out var version);

        Assert.False(result);
        Assert.Null(version);
    }

    [Fact]
    public void CompareTo_ShouldOrderVersionsCorrectly()
    {
        var v1 = SemanticVersion.Parse("1.0.0");
        var v2 = SemanticVersion.Parse("2.0.0");
        var v3 = SemanticVersion.Parse("2.1.0");
        var v4 = SemanticVersion.Parse("2.1.1");

        Assert.True(v1 < v2);
        Assert.True(v2 < v3);
        Assert.True(v3 < v4);
    }

    [Fact]
    public void CompareTo_PreReleaseShouldBeLessThanRelease()
    {
        var preRelease = SemanticVersion.Parse("1.0.0-alpha");
        var release = SemanticVersion.Parse("1.0.0");

        Assert.True(preRelease < release);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var version = new SemanticVersion(1, 2, 3, "beta.1", "build.456");

        Assert.Equal("1.2.3-beta.1+build.456", version.ToString());
    }

    [Fact]
    public void Equals_ShouldCompareCorrectly()
    {
        var v1 = SemanticVersion.Parse("1.2.3");
        var v2 = SemanticVersion.Parse("1.2.3");
        var v3 = SemanticVersion.Parse("1.2.4");

        Assert.Equal(v1, v2);
        Assert.NotEqual(v1, v3);
    }
}