namespace Valhalla;

/// <summary>
/// 瓦尓哈拉 API 异常，当服务器返回非 2xx 状态码时抛出
/// </summary>
public class ValhallaApiException : Exception
{
    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 服务端返回的错误响应体
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// 创建 API 异常
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="message">错误描述</param>
    /// <param name="responseBody">服务端响应体</param>
    public ValhallaApiException(int statusCode, string message, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}