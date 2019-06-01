namespace Legion.Auth;

/// <summary>
/// Vendor 登录结果
/// </summary>
public class VendorLoginResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Vendor 名称
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 凭据来源
    /// </summary>
    public string? CredentialSource { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static VendorLoginResult Ok(string vendorName, string username, DateTime? expiresAt)
    {
        return new VendorLoginResult
        {
            Success = true,
            VendorName = vendorName,
            Username = username,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static VendorLoginResult Fail(string errorMessage)
    {
        return new VendorLoginResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}