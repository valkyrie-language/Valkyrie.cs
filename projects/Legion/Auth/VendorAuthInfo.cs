namespace Legion.Auth;

/// <summary>
/// Vendor 认证状态
/// </summary>
public class VendorAuthInfo
{
    /// <summary>
    /// Vendor 名称
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// 注册表端点地址
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 认证令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime LoggedInAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 当前用户标识（登录成功后获取）
    /// </summary>
    public string? CurrentUser { get; set; }

    /// <summary>
    /// 令牌是否已过期
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// 是否已登录
    /// </summary>
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token) && !IsExpired;
}