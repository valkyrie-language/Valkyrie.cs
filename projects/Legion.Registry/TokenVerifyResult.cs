namespace Legion.Registry;

/// <summary>
/// 令牌验证结果
/// </summary>
public class TokenVerifyResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    public static TokenVerifyResult Success(string username, DateTime? expiresAt = null)
    {
        return new TokenVerifyResult
        {
            Valid = true,
            Username = username,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    public static TokenVerifyResult Failure(string errorMessage)
    {
        return new TokenVerifyResult
        {
            Valid = false,
            ErrorMessage = errorMessage
        };
    }
}