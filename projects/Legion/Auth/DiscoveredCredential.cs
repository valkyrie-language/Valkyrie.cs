namespace Legion.Auth;

/// <summary>
/// 自动发现的凭据
/// </summary>
public class DiscoveredCredential
{
    /// <summary>
    /// 认证令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 凭据来源描述
    /// </summary>
    public string Source { get; set; } = "未知来源";

    /// <summary>
    /// 凭据过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}