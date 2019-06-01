namespace Legion.Package;

/// <summary>
/// 已缓存的包条目
/// </summary>
public class CachedPackage
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 本地存储路径
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// 缓存时间
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}