namespace Valhalla;

/// <summary>
/// 单个版本条目
/// </summary>
public class VersionEntry
{
    /// <summary>版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>版本状态</summary>
    public VersionStatus Status { get; set; } = VersionStatus.Active;

    /// <summary>屏蔽原因（如果状态为 Shielded）</summary>
    public string? ShieldReason { get; set; }

    /// <summary>屏蔽时间</summary>
    public DateTime? ShieldedAt { get; set; }

    /// <summary>.nyar 的 SHA-256</summary>
    public string PackageDigest { get; set; } = string.Empty;

    /// <summary>源码包的 SHA-256</summary>
    public string? SourceDigest { get; set; }

    /// <summary>.nyar 文件大小</summary>
    public long PackageSize { get; set; }

    /// <summary>源码包文件大小</summary>
    public long? SourceSize { get; set; }

    /// <summary>发布时间</summary>
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}