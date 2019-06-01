namespace Legion.Version;

public class YearlyVersionRange
{
    public string Raw { get; }

    private readonly int? _yearly;
    private readonly int? _major;
    private readonly int? _minor;
    private readonly int? _patch;
    private readonly bool _isInclusiveOrHigher;

    private YearlyVersionRange(string raw, int? yearly, int? major, int? minor, int? patch, bool isInclusiveOrHigher = false)
    {
        Raw = raw;
        _yearly = yearly;
        _major = major;
        _minor = minor;
        _patch = patch;
        _isInclusiveOrHigher = isInclusiveOrHigher;
    }

    public static YearlyVersionRange Parse(string range)
    {
        range = range.Trim();

        if (range == "*")
        {
            return new YearlyVersionRange(range, null, null, null, null);
        }

        // 检测 + 后缀，表示包括该版本及更高
        bool isInclusiveOrHigher = range.EndsWith("+");
        if (isInclusiveOrHigher)
        {
            range = range.Substring(0, range.Length - 1);
        }

        var parts = range.Split('.');
        if (parts.Length < 1 || parts.Length > 4)
        {
            throw new FormatException($"无效的 YearlyVersion 范围格式: {range}，期望格式: yearly 或 yearly.major 或 yearly.major.minor 或 yearly.major.minor.patch");
        }

        int? yearly = ParsePart(parts[0]);

        // 如果 yearly 是 *，后面都不看
        if (!yearly.HasValue)
        {
            return new YearlyVersionRange(range, null, null, null, null);
        }

        int? major = parts.Length > 1 ? ParsePart(parts[1]) : null;

        // 如果 major 是 *，后面都不看
        if (parts.Length > 1 && !major.HasValue)
        {
            return new YearlyVersionRange(range, yearly, null, null, null);
        }

        int? minor = parts.Length > 2 ? ParsePart(parts[2]) : null;

        // 如果 minor 是 *，后面都不看
        if (parts.Length > 2 && !minor.HasValue)
        {
            return new YearlyVersionRange(range, yearly, major, null, null);
        }

        int? patch = parts.Length > 3 ? ParsePart(parts[3]) : null;

        return new YearlyVersionRange(range, yearly, major, minor, patch, isInclusiveOrHigher);
    }

    public bool Satisfies(YearlyVersion version)
    {
        // 如果是 + 模式，表示最低版本要求（包括该版本及更高）
        if (_isInclusiveOrHigher)
        {
            return IsVersionAtLeast(version);
        }

        if (_yearly.HasValue && version.Yearly != _yearly.Value)
        {
            return false;
        }

        if (_major.HasValue && version.Major != _major.Value)
        {
            return false;
        }

        if (_minor.HasValue && version.Minor != _minor.Value)
        {
            return false;
        }

        if (_patch.HasValue && version.Patch != _patch.Value)
        {
            return false;
        }

        return true;
    }

    private bool IsVersionAtLeast(YearlyVersion version)
    {
        if (_yearly.HasValue && version.Yearly < _yearly.Value)
        {
            return false;
        }

        if (_yearly.HasValue && version.Yearly > _yearly.Value)
        {
            return true;
        }

        if (_major.HasValue && version.Major < _major.Value)
        {
            return false;
        }

        if (_major.HasValue && version.Major > _major.Value)
        {
            return true;
        }

        if (_minor.HasValue && version.Minor < _minor.Value)
        {
            return false;
        }

        if (_minor.HasValue && version.Minor > _minor.Value)
        {
            return true;
        }

        if (_patch.HasValue && version.Patch < _patch.Value)
        {
            return false;
        }

        if (_patch.HasValue && version.Patch > _patch.Value)
        {
            return true;
        }

        return true;
    }

    public override string ToString()
    {
        return Raw;
    }

    private static int? ParsePart(string part)
    {
        if (part == "*")
        {
            return null;
        }

        if (int.TryParse(part, out var value))
        {
            return value;
        }

        throw new FormatException($"无效的范围部分: {part}，期望数字或 *");
    }
}