namespace Legion.Registry;

/// <summary>
/// 注册表异常
/// </summary>
public class RegistryException : Exception
{
    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 创建注册表异常
    /// </summary>
    public RegistryException(string message, int statusCode = 500, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}