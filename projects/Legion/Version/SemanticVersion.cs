using System.Text.RegularExpressions;

namespace Legion.Version;

public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }
    public string? BuildMetadata { get; }

    public SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    {
        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentException("版本号各部分不能为负数");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
    }

    public static SemanticVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("版本字符串不能为空");
        }

        version = version.TrimStart('v', 'V');

        var match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9.]+))?(?:\+([a-zA-Z0-9.]+))?$");
        if (!match.Success)
        {
            throw new FormatException($"无效的语义化版本格式: {version}");
        }

        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int patch = int.Parse(match.Groups[3].Value);
        string? preRelease = match.Groups[4].Success ? match.Groups[4].Value : null;
        string? buildMetadata = match.Groups[5].Success ? match.Groups[5].Value : null;

        return new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
    }

    public static bool TryParse(string version, out SemanticVersion? result)
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

    public bool Satisfies(VersionRange range)
    {
        return range.Satisfies(this);
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
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

        if (PreRelease is null && other.PreRelease is null)
        {
            return 0;
        }

        if (PreRelease is not null && other.PreRelease is null)
        {
            return -1;
        }

        if (PreRelease is null && other.PreRelease is not null)
        {
            return 1;
        }

        return ComparePreRelease(PreRelease ?? string.Empty, other.PreRelease ?? string.Empty);
    }

    /// <summary>
    /// 按 SemVer 2.0 规范第 11 条比较预发布标识符：
    /// 逐段比较，纯数字段按数值比较（1-11 > 1-2），纯字母或混合段按字符串比较
    /// </summary>
    private static int ComparePreRelease(string a, string b)
    {
        var aParts = a.Split('.');
        var bParts = b.Split('.');
        int minLen = Math.Min(aParts.Length, bParts.Length);

        for (int i = 0; i < minLen; i++)
        {
            bool aIsNum = int.TryParse(aParts[i], out int aNum);
            bool bIsNum = int.TryParse(bParts[i], out int bNum);

            if (aIsNum && bIsNum)
            {
                if (aNum != bNum)
                {
                    return aNum.CompareTo(bNum);
                }
            }
            else
            {
                int cmp = string.Compare(aParts[i], bParts[i], StringComparison.Ordinal);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
        }

        return aParts.Length.CompareTo(bParts.Length);
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               PreRelease == other.PreRelease;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SemanticVersion);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, PreRelease);
    }

    public override string ToString()
    {
        string version = $"{Major}.{Minor}.{Patch}";
        if (PreRelease is not null)
        {
            version += $"-{PreRelease}";
        }

        if (BuildMetadata is not null)
        {
            version += $"+{BuildMetadata}";
        }

        return version;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        if (left is null)
        {
            return right is not null;
        }

        return left.CompareTo(right) < 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        if (left is null)
        {
            return false;
        }

        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        if (left is null)
        {
            return true;
        }

        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.CompareTo(right) >= 0;
    }
}