namespace Valhalla.Audit;

/// <summary>
/// 单条审计日志条目
/// </summary>
public class AuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AuditOperation Operation { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Sha256 { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string ActorRole { get; set; } = "publisher";
    public Dictionary<string, string> Details { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
}