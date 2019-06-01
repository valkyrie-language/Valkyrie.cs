namespace Valhalla.Client;

/// <summary>
/// protoswap.lock 中的单个条目
/// </summary>
public class ValhallaLockEntry
{
    /// <summary>化身计数器，PURGE 后递增</summary>
    public int Incarnation { get; set; } = 1;

    /// <summary>根组织路径（如果是组织包）</summary>
    public string? NamespaceRoot { get; set; }

    /// <summary>发布者 Ed25519 公钥指纹</summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>授权链根组织密钥</summary>
    public string? AuthorizationRoot { get; set; }

    /// <summary>授权链内容 SHA-256</summary>
    public string? AuthorizationHash { get; set; }

    /// <summary>锁定的版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>.nyar 的 SHA-256</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>注册表地址</summary>
    public string Registry { get; set; } = string.Empty;

    /// <summary>首次安装时间</summary>
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    /// <summary>包注册时间</summary>
    public DateTime RegisteredAt { get; set; }
}