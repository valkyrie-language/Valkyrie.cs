namespace Valkyrie.Tests.PackageManagerTests;

public class YearlyVersionTests
{
    [Fact]
    public void Parse_ShouldParseBasicVersion()
    {
        var version = YearlyVersion.Parse("2024.1.0.0");

        Assert.Equal(2024, version.Yearly);
        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
        Assert.Null(version.BuildInfo);
    }

    [Fact]
    public void Parse_ShouldParseVersionWithBuildInfo()
    {
        var version = YearlyVersion.Parse("2024.1.0.0-rc1");

        Assert.Equal(2024, version.Yearly);
        Assert.Equal("rc1", version.BuildInfo);
    }

    [Fact]
    public void Parse_ShouldThrowForInvalidFormat()
    {
        Assert.Throws<FormatException>(() => YearlyVersion.Parse("1.0.0"));
        Assert.Throws<FormatException>(() => YearlyVersion.Parse("not-a-version"));
    }

    [Fact]
    public void TryParse_ShouldReturnFalseForInvalidVersion()
    {
        bool result = YearlyVersion.TryParse("invalid", out var version);

        Assert.False(result);
        Assert.Null(version);
    }

    [Fact]
    public void IsResearch_ShouldReturnTrueForYearlyZero()
    {
        var version = YearlyVersion.Parse("0.1.0.0");

        Assert.True(version.IsResearch);
        Assert.False(version.IsBeta);
        Assert.False(version.IsStable);
    }

    [Fact]
    public void IsBeta_ShouldReturnTrueForMajorZero()
    {
        var version = YearlyVersion.Parse("2024.0.5.3");

        Assert.False(version.IsResearch);
        Assert.True(version.IsBeta);
        Assert.False(version.IsStable);
    }

    [Fact]
    public void IsStable_ShouldReturnTrueForYearlyAndMajorNonZero()
    {
        var version = YearlyVersion.Parse("2024.1.0.0");

        Assert.False(version.IsResearch);
        Assert.False(version.IsBeta);
        Assert.True(version.IsStable);
    }

    [Fact]
    public void CompareTo_ShouldOrderVersionsCorrectly()
    {
        var v1 = YearlyVersion.Parse("2024.1.0.0");
        var v2 = YearlyVersion.Parse("2024.2.0.0");
        var v3 = YearlyVersion.Parse("2025.1.0.0");

        Assert.True(v1 < v2);
        Assert.True(v2 < v3);
    }

    [Fact]
    public void Equals_ShouldCompareCorrectly()
    {
        var v1 = YearlyVersion.Parse("2024.1.0.0");
        var v2 = YearlyVersion.Parse("2024.1.0.0");
        var v3 = YearlyVersion.Parse("2024.1.0.1");

        Assert.Equal(v1, v2);
        Assert.NotEqual(v1, v3);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var version = new YearlyVersion(2024, 1, 2, 3, "rc1");

        Assert.Equal("2024.1.2.3-rc1", version.ToString());
    }

    [Fact]
    public void YearlyVersionRange_ExactMatch_ShouldMatchExact()
    {
        var range = YearlyVersionRange.Parse("2024.1.0.0");
        var version = YearlyVersion.Parse("2024.1.0.0");

        Assert.True(range.Satisfies(version));
    }

    [Fact]
    public void YearlyVersionRange_ExactMatch_ShouldNotMatchDifferent()
    {
        var range = YearlyVersionRange.Parse("2024.1.0.0");
        var version = YearlyVersion.Parse("2024.1.0.1");

        Assert.False(range.Satisfies(version));
    }

    [Fact]
    public void YearlyVersionRange_PatchWildcard_ShouldMatchAnyPatch()
    {
        var range = YearlyVersionRange.Parse("2024.1.0.*");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.99")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.1.0")));
    }

    [Fact]
    public void YearlyVersionRange_MinorWildcard_ShouldMatchAnyMinorAndPatch()
    {
        var range = YearlyVersionRange.Parse("2024.1.*.*");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.5.3")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.2.0.0")));
    }

    [Fact]
    public void YearlyVersionRange_MajorWildcard_ShouldMatchAnyMajorMinorPatch()
    {
        var range = YearlyVersionRange.Parse("2024.*");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.0.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.5.3")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2025.1.0.0")));
    }

    [Fact]
    public void YearlyVersionRange_AllWildcard_ShouldMatchAny()
    {
        var range = YearlyVersionRange.Parse("*");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("0.0.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2025.99.99.99")));
    }

    [Fact]
    public void YearlyVersionRange_YearlyOnly_ShouldMatchAnyMajorMinorPatch()
    {
        var range = YearlyVersionRange.Parse("2024");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.0.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.5.3")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2025.1.0.0")));
    }

    [Fact]
    public void YearlyVersionRange_YearlyMajor_ShouldMatchAnyMinorPatch()
    {
        var range = YearlyVersionRange.Parse("2024.1");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.5.3")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.2.0.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2025.1.0.0")));
    }

    [Fact]
    public void YearlyVersionRange_BuildInfo_ShouldParticipateInComparison()
    {
        var v1 = YearlyVersion.Parse("2024.1.0.0-alpha");
        var v2 = YearlyVersion.Parse("2024.1.0.0-beta");
        var v3 = YearlyVersion.Parse("2024.1.0.0-rc1");

        Assert.True(v1 < v2);
        Assert.True(v2 < v3);
    }

    [Fact]
    public void YearlyVersionRange_BuildInfo_EqualsShouldIncludeBuildInfo()
    {
        var v1 = YearlyVersion.Parse("2024.1.0.0-alpha");
        var v2 = YearlyVersion.Parse("2024.1.0.0-alpha");
        var v3 = YearlyVersion.Parse("2024.1.0.0-beta");

        Assert.Equal(v1, v2);
        Assert.NotEqual(v1, v3);
    }

    [Fact]
    public void YearlyVersionRange_InclusiveOrHigher_ExactVersion_ShouldMatchSameAndHigher()
    {
        var range = YearlyVersionRange.Parse("2024.1.3+");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.3.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.3.5")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.4.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.2.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2025.0.0.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.2.9")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.0.5.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2023.9.9.9")));
    }

    [Fact]
    public void YearlyVersionRange_InclusiveOrHigher_YearlyMajor_ShouldMatchSameAndHigher()
    {
        var range = YearlyVersionRange.Parse("2024.1+");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.5.3")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.2.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2025.0.0.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.0.9.9")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2023.9.9.9")));
    }

    [Fact]
    public void YearlyVersionRange_InclusiveOrHigher_YearlyOnly_ShouldMatchSameAndHigher()
    {
        var range = YearlyVersionRange.Parse("2024+");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.0.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.5.3.2")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2025.0.0.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2099.99.99.99")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2023.9.9.9")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("0.0.0.0")));
    }

    [Fact]
    public void YearlyVersionRange_InclusiveOrHigher_FullVersion_ShouldMatchSameAndHigher()
    {
        var range = YearlyVersionRange.Parse("2024.1.3.2+");

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.3.2")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.3.3")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.4.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2025.0.0.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.3.1")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.2.9")));
    }

    [Fact]
    public void YearlyVersionRange_InclusiveOrHigher_SkipVulnerableVersion()
    {
        var range = YearlyVersionRange.Parse("2024.1.3+");

        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.2.0")));
        Assert.False(range.Satisfies(YearlyVersion.Parse("2024.1.2.5")));

        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.3.0")));
        Assert.True(range.Satisfies(YearlyVersion.Parse("2024.1.4.0")));
    }
}