namespace Valhalla;

/// <summary>
/// 版本级元信息（不参与校验，可随时修改）
/// </summary>
public class ValhallaVersionMeta
{
    /// <summary>所属版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>README 内容（Markdown 或 HTML）</summary>
    public string? Readme { get; set; }

    /// <summary>变更说明</summary>
    public string? Changelog { get; set; }

    /// <summary>是否为预览版</summary>
    public bool PreRelease { get; set; }

    /// <summary>是否已弃用</summary>
    public bool Deprecated { get; set; }

    /// <summary>弃用说明</summary>
    public string? DeprecationNote { get; set; }

    /// <summary>发布时间</summary>
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>下载计数</summary>
    public long DownloadCount { get; set; }
}