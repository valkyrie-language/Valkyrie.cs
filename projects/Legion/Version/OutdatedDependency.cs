namespace Legion.Version;

/// <summary>
/// 过期依赖信息
/// </summary>
public class OutdatedDependency
{
    /// <summary>
    /// 包名
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 当前安装版本
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最新可用版本
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// legion.von 中的版本约束
    /// </summary>
    public string Constraint { get; set; } = string.Empty;

    /// <summary>
    /// 是否为重大更新（主版本号不同）
    /// </summary>
    public bool IsMajorUpdate
    {
        get
        {
            var currentParts = CurrentVersion.Split('.');
            var latestParts = LatestVersion.Split('.');

            if (currentParts.Length > 0 && latestParts.Length > 0)
            {
                return currentParts[0] != latestParts[0];
            }

            return false;
        }
    }
}