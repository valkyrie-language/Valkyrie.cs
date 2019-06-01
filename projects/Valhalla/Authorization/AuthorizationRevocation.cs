namespace Valhalla.Authorization;

/// <summary>
/// 授权撤销事件
/// </summary>
public class AuthorizationRevocation
{
    /// <summary>被撤销的命名空间</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>被撤销者公钥指纹</summary>
    public string Grantee { get; set; } = string.Empty;

    /// <summary>根组织 incarnation</summary>
    public int Incarnation { get; set; } = 1;

    /// <summary>撤销时间</summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>撤销者公钥指纹</summary>
    public string Revoker { get; set; } = string.Empty;

    /// <summary>撤销者 Ed25519 签名（Base64）</summary>
    public string Signature { get; set; } = string.Empty;
}