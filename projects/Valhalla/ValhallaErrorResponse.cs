using System.Collections.Generic;

namespace Valhalla;

/// <summary>
/// 瓦尓哈拉统一错误响应格式
/// </summary>
public class ValhallaErrorResponse
{
    /// <summary>
    /// 错误描述信息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 错误码（如 NOT_FOUND、VALIDATION_ERROR、INTERNAL_ERROR）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 额外详情
    /// </summary>
    public Dictionary<string, string>? Details { get; set; }

    /// <summary>
    /// 创建一个 404 错误响应
    /// </summary>
    public static ValhallaErrorResponse NotFound(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "NOT_FOUND", Details = details };
    }

    /// <summary>
    /// 创建一个 400 验证错误响应
    /// </summary>
    public static ValhallaErrorResponse ValidationError(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "VALIDATION_ERROR", Details = details };
    }

    /// <summary>
    /// 创建一个 500 内部错误响应
    /// </summary>
    public static ValhallaErrorResponse InternalError(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "INTERNAL_ERROR", Details = details };
    }

    /// <summary>
    /// 创建一个 409 冲突错误响应
    /// </summary>
    public static ValhallaErrorResponse Conflict(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "CONFLICT", Details = details };
    }

    /// <summary>
    /// 创建一个 401 未认证错误响应
    /// </summary>
    public static ValhallaErrorResponse Unauthorized(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "UNAUTHORIZED", Details = details };
    }

    /// <summary>
    /// 创建一个 403 禁止访问错误响应
    /// </summary>
    public static ValhallaErrorResponse Forbidden(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { Error = message, Code = "FORBIDDEN", Details = details };
    }
}