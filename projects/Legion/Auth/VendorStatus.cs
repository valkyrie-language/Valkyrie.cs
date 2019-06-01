namespace Legion.Auth;

/// <summary>
/// Vendor 状态信息
/// </summary>
public class VendorStatus
{
    /// <summary>
    /// Vendor 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 注册表端点地址
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 是否已登录
    /// </summary>
    public bool IsLoggedIn { get; set; }

    /// <summary>
    /// 当前登录用户名
    /// </summary>
    public string? CurrentUser { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime? LoggedInAt { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否有官方工具凭据可用
    /// </summary>
    public bool HasOfficialCredentials { get; set; }

    /// <summary>
    /// 可用的官方工具凭据来源列表
    /// </summary>
    public List<string> AvailableCredentialSources { get; set; } = new();
}