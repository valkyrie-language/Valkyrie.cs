namespace Legion.Version;

public class VersionRange
{
    public string Raw { get; }
    public SemanticVersion? MinVersion { get; }
    public bool MinInclusive { get; }
    public SemanticVersion? MaxVersion { get; }
    public bool MaxInclusive { get; }

    private VersionRange(string raw, SemanticVersion? minVersion, bool minInclusive, SemanticVersion? maxVersion, bool maxInclusive)
    {
        Raw = raw;
        MinVersion = minVersion;
        MinInclusive = minInclusive;
        MaxVersion = maxVersion;
        MaxInclusive = maxInclusive;
    }

    public static VersionRange Parse(string range)
    {
        range = range.Trim();

        if (range == "*" || range == "latest")
        {
            return new VersionRange(range, null, true, null, true);
        }

        // 精确版本：1.2.3
        if (!range.StartsWith("^") && !range.StartsWith("~") && !range.StartsWith(">") && !range.StartsWith("<") && !range.StartsWith("="))
        {
            var version = SemanticVersion.Parse(range);
            return new VersionRange(range, version, true, version, true);
        }

        // 兼容版本：^1.2.3
        if (range.StartsWith("^"))
        {
            var version = SemanticVersion.Parse(range.Substring(1));
            var maxVersion = new SemanticVersion(version.Major + 1, 0, 0);
            return new VersionRange(range, version, true, maxVersion, false);
        }

        // 近似版本：~1.2.3
        if (range.StartsWith("~"))
        {
            var version = SemanticVersion.Parse(range.Substring(1));
            var maxVersion = new SemanticVersion(version.Major, version.Minor + 1, 0);
            return new VersionRange(range, version, true, maxVersion, false);
        }

        // 大于等于：>=1.2.3
        if (range.StartsWith(">="))
        {
            var version = SemanticVersion.Parse(range.Substring(2));
            return new VersionRange(range, version, true, null, true);
        }

        // 大于：>1.2.3
        if (range.StartsWith(">"))
        {
            var version = SemanticVersion.Parse(range.Substring(1));
            return new VersionRange(range, version, false, null, true);
        }

        // 小于等于：<=1.2.3
        if (range.StartsWith("<="))
        {
            var version = SemanticVersion.Parse(range.Substring(2));
            return new VersionRange(range, null, true, version, true);
        }

        // 小于：<1.2.3
        if (range.StartsWith("<"))
        {
            var version = SemanticVersion.Parse(range.Substring(1));
            return new VersionRange(range, null, true, version, false);
        }

        throw new FormatException($"无法解析版本范围: {range}");
    }

    public bool Satisfies(SemanticVersion version)
    {
        if (MinVersion is not null)
        {
            int cmp = version.CompareTo(MinVersion);
            if (MinInclusive && cmp < 0)
            {
                return false;
            }

            if (!MinInclusive && cmp <= 0)
            {
                return false;
            }
        }

        if (MaxVersion is not null)
        {
            int cmp = version.CompareTo(MaxVersion);
            if (MaxInclusive && cmp > 0)
            {
                return false;
            }

            if (!MaxInclusive && cmp >= 0)
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString()
    {
        return Raw;
    }
}