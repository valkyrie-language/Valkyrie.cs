using System.Text.RegularExpressions;

namespace Legion.Version;

public class YearlyVersion : IComparable<YearlyVersion>, IEquatable<YearlyVersion>
{
    public int Yearly { get; }
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? BuildInfo { get; }

    public bool IsResearch => Yearly == 0;
    public bool IsBeta => Yearly > 0 && Major == 0;
    public bool IsStable => Yearly > 0 && Major > 0;

    public YearlyVersion(int yearly, int major, int minor, int patch, string? buildInfo = null)
    {
        if (yearly < 0 || major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentException("版本号各部分不能为负数");
        }

        Yearly = yearly;
        Major = major;
        Minor = minor;
        Patch = patch;
        BuildInfo = buildInfo;
    }

    public static YearlyVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("版本字符串不能为空");
        }

        // 格式: yearly.major.minor.patch-build_info
        var match = Regex.Match(version.Trim(), @"^(\d+)\.(\d+)\.(\d+)\.(\d+)(?:-(.+))?$");
        if (!match.Success)
        {
            throw new FormatException($"无效的 YearlyVersion 格式: {version}，期望格式: yearly.major.minor.patch 或 yearly.major.minor.patch-build_info");
        }

        int yearly = int.Parse(match.Groups[1].Value);
        int major = int.Parse(match.Groups[2].Value);
        int minor = int.Parse(match.Groups[3].Value);
        int patch = int.Parse(match.Groups[4].Value);
        string? buildInfo = match.Groups[5].Success ? match.Groups[5].Value : null;

        return new YearlyVersion(yearly, major, minor, patch, buildInfo);
    }

    public static bool TryParse(string version, out YearlyVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public bool Satisfies(YearlyVersionRange range)
    {
        return range.Satisfies(this);
    }

    public int CompareTo(YearlyVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (Yearly != other.Yearly)
        {
            return Yearly.CompareTo(other.Yearly);
        }

        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        if (Patch != other.Patch)
        {
            return Patch.CompareTo(other.Patch);
        }

        // BuildInfo 参与比较，按字符串排序
        return string.Compare(BuildInfo, other.BuildInfo, StringComparison.Ordinal);
    }

    public bool Equals(YearlyVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Yearly == other.Yearly &&
               Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               BuildInfo == other.BuildInfo;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as YearlyVersion);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Yearly, Major, Minor, Patch, BuildInfo);
    }

    public override string ToString()
    {
        string version = $"{Yearly}.{Major}.{Minor}.{Patch}";
        if (BuildInfo is not null)
        {
            version += $"-{BuildInfo}";
        }

        return version;
    }

    public static bool operator ==(YearlyVersion? left, YearlyVersion? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(YearlyVersion? left, YearlyVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(YearlyVersion left, YearlyVersion right)
    {
        if (left is null)
        {
            return right is not null;
        }

        return left.CompareTo(right) < 0;
    }

    public static bool operator >(YearlyVersion left, YearlyVersion right)
    {
        if (left is null)
        {
            return false;
        }

        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(YearlyVersion left, YearlyVersion right)
    {
        if (left is null)
        {
            return true;
        }

        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(YearlyVersion left, YearlyVersion right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.CompareTo(right) >= 0;
    }
}