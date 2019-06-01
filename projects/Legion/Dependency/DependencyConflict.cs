using Legion.Version;

namespace Legion.Dependency;

public class DependencyConflict
{
    /// <summary>
    /// 冲突的包名称
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 请求的版本列表
    /// </summary>
    public List<string> RequestedVersions { get; set; } = new();

    /// <summary>
    /// 已解决的版本号
    /// </summary>
    public string? ResolvedVersion { get; set; }

    /// <summary>
    /// 冲突解决策略
    /// </summary>
    public ConflictResolutionStrategy ResolutionStrategy { get; set; }

    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool IsResolved => ResolvedVersion is not null;

    /// <summary>
    /// 是否为严重冲突（无法自动解决）
    /// </summary>
    public bool IsSevere
    {
        get
        {
            if (RequestedVersions.Count < 2)
            {
                return false;
            }

            var majorVersions = RequestedVersions
                .Select(v => SemanticVersion.TryParse(v.TrimStart('^', '~', '>', '<', '='), out var sv) ? sv.Major : -1)
                .Where(m => m >= 0)
                .Distinct()
                .ToList();

            return majorVersions.Count > 1;
        }
    }
}

/// <summary>
/// 冲突解决策略
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// 未解决
    /// </summary>
    None,

    /// <summary>
    /// 选择最高兼容版本
    /// </summary>
    HighestCompatible,

    /// <summary>
    /// 使用 overrides 强制指定版本
    /// </summary>
    Override,

    /// <summary>
    /// 需要用户手动解决
    /// </summary>
    Manual
}