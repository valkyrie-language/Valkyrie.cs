namespace Legion.Registry;

/// <summary>
/// 发布结果
/// </summary>
public class PublishResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 包名称
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 发布后的 URL
    /// </summary>
    public string? PublishedUrl { get; set; }
}