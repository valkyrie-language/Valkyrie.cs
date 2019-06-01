namespace Valhalla.Client;

/// <summary>
/// protoswap.lock 完整内容
/// </summary>
public class ValhallaLockContent
{
    /// <summary>条目字典，键为规范包名</summary>
    public Dictionary<string, ValhallaLockEntry> Entries { get; set; } = new();

    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}