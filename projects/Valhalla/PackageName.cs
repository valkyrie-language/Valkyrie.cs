using System;
using System.Text.RegularExpressions;

namespace Valhalla;

/// <summary>
/// 瓦尓哈拉包名，支持标准化算法
/// </summary>
public readonly struct PackageName : IEquatable<PackageName>
{
    private static readonly Regex CanonicalPattern = new(
        @"^[a-z0-9]+(\.[a-z0-9]+)*$",
        RegexOptions.Compiled);

    /// <summary>
    /// 规范形式的包名
    /// </summary>
    public string Canonical { get; }

    /// <summary>
    /// 根组织前缀（用第一个 . 分割），若无组织返回完整包名
    /// </summary>
    public string Root => Canonical.IndexOf('.') is int idx and >= 0
        ? Canonical.Substring(0, idx)
        : Canonical;

    /// <summary>
    /// 将任意形式的名字标准化
    /// </summary>
    /// <param name="raw">用户输入的原始名字</param>
    public PackageName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("包名不能为空", nameof(raw));
        }

        string lower = raw.ToLowerInvariant();
        string replaced = Regex.Replace(lower, @"[_\-\s]+", ".");
        string collapsed = Regex.Replace(replaced, @"\.{2,}", ".");
        string trimmed = collapsed.Trim('.');

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException($"包名 '{raw}' 标准化后为空", nameof(raw));
        }

        if (!CanonicalPattern.IsMatch(trimmed))
        {
            throw new ArgumentException(
                $"包名 '{raw}' 标准化为 '{trimmed}' 后包含非法字符", nameof(raw));
        }

        Canonical = trimmed;
    }

    /// <summary>
    /// 判断此包名是否属于指定根组织
    /// </summary>
    public bool BelongsToOrg(string orgCanonical)
    {
        return Canonical == orgCanonical
               || Canonical.StartsWith(orgCanonical + ".", StringComparison.Ordinal);
    }

    /// <summary>
    /// 获取指定深度的前缀部分
    /// </summary>
    /// <param name="depth">深度（1=仅 root，2=root.first，以此类推）</param>
    public string GetPrefix(int depth)
    {
        string[] parts = Canonical.Split('.');

        if (depth >= parts.Length)
        {
            return Canonical;
        }

        return string.Join(".", parts, 0, depth);
    }

    public bool Equals(PackageName other) => Canonical == other.Canonical;

    public override bool Equals(object? obj) => obj is PackageName other && Equals(other);

    public override int GetHashCode() => Canonical.GetHashCode();

    public override string ToString() => Canonical;

    public static bool operator ==(PackageName left, PackageName right) => left.Equals(right);

    public static bool operator !=(PackageName left, PackageName right) => !left.Equals(right);
}