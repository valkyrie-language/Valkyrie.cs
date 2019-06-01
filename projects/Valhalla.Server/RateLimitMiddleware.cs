using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Valhalla.Server;

/// <summary>
/// 速率限制中间件，基于令牌桶算法限制每个 IP 的请求频率
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    /// <summary>
    /// 创建速率限制中间件
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="maxRequests">时间窗口内允许的最大请求数</param>
    /// <param name="windowSeconds">时间窗口秒数</param>
    public RateLimitMiddleware(RequestDelegate next, int maxRequests = 100, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var now = DateTime.UtcNow;

        var entry = _entries.AddOrUpdate(
            clientIp,
            _ => new RateLimitEntry { Count = 1, WindowStart = now },
            (_, existing) =>
            {
                if (now - existing.WindowStart > _window)
                {
                    return new RateLimitEntry { Count = 1, WindowStart = now };
                }

                existing.Count++;
                return existing;
            });

        var remaining = Math.Max(0, _maxRequests - entry.Count);
        var resetTime = entry.WindowStart + _window;

        context.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();

        if (entry.Count > _maxRequests)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = ((int)(resetTime - now).TotalSeconds).ToString();
            context.Response.ContentType = "application/json; charset=utf-8";

            var errorResponse = new
            {
                error = "too_many_requests",
                message = $"请求频率超过限制（{_maxRequests} 次/{(int)_window.TotalSeconds} 秒），请稍后重试",
                retry_after = (int)(resetTime - now).TotalSeconds
            };

            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 清理过期的速率限制条目
    /// </summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _entries)
        {
            if (now - kvp.Value.WindowStart > _window)
            {
                _entries.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}