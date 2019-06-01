using System;
using System.Collections.Generic;

namespace Valhalla;

/// <summary>
/// 包版本清单，对应 manifest.json
/// </summary>
public class PackageManifest
{
    /// <summary>规范包名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>当前化身编号</summary>
    public int Incarnation { get; set; } = 1;

    /// <summary>发布者公钥指纹</summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>根组织路径（如果属于某组织）</summary>
    public string? NamespaceRoot { get; set; }

    /// <summary>首次注册时间</summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>包状态</summary>
    public PackageStatus Status { get; set; } = PackageStatus.Active;

    /// <summary>PURGE 原因</summary>
    public string? PurgeReason { get; set; }

    /// <summary>PURGE 时间</summary>
    public DateTime? PurgedAt { get; set; }

    /// <summary>所有版本（键为版本号）</summary>
    public Dictionary<string, VersionEntry> Versions { get; set; } = new();
}