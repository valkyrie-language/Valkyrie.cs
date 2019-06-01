namespace Valkyrie.Tests.PackageManagerTests;

public class VersionRangeTests
{
    [Fact]
    public void Parse_ExactVersion_ShouldMatchExact()
    {
        var range = VersionRange.Parse("1.2.3");
        var version = SemanticVersion.Parse("1.2.3");

        Assert.True(range.Satisfies(version));
    }

    [Fact]
    public void Parse_ExactVersion_ShouldNotMatchDifferent()
    {
        var range = VersionRange.Parse("1.2.3");
        var version = SemanticVersion.Parse("1.2.4");

        Assert.False(range.Satisfies(version));
    }

    [Fact]
    public void Parse_CaretRange_ShouldMatchCompatible()
    {
        var range = VersionRange.Parse("^1.2.3");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.2.3")));
        Assert.True(range.Satisfies(SemanticVersion.Parse("1.9.9")));
        Assert.False(range.Satisfies(SemanticVersion.Parse("2.0.0")));
    }

    [Fact]
    public void Parse_TildeRange_ShouldMatchCompatible()
    {
        var range = VersionRange.Parse("~1.2.3");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.2.3")));
        Assert.True(range.Satisfies(SemanticVersion.Parse("1.2.9")));
        Assert.False(range.Satisfies(SemanticVersion.Parse("1.3.0")));
    }

    [Fact]
    public void Parse_GreaterThanOrEqual_ShouldMatchCorrectly()
    {
        var range = VersionRange.Parse(">=1.2.0");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.2.0")));
        Assert.True(range.Satisfies(SemanticVersion.Parse("2.0.0")));
        Assert.False(range.Satisfies(SemanticVersion.Parse("1.1.9")));
    }

    [Fact]
    public void Parse_LessThan_ShouldMatchCorrectly()
    {
        var range = VersionRange.Parse("<2.0.0");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.9.9")));
        Assert.False(range.Satisfies(SemanticVersion.Parse("2.0.0")));
    }

    [Fact]
    public void Parse_Wildcard_ShouldMatchAny()
    {
        var range = VersionRange.Parse("*");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.0.0")));
        Assert.True(range.Satisfies(SemanticVersion.Parse("99.99.99")));
    }

    [Fact]
    public void Parse_Latest_ShouldMatchAny()
    {
        var range = VersionRange.Parse("latest");

        Assert.True(range.Satisfies(SemanticVersion.Parse("1.0.0")));
    }
}