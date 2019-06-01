using System.Collections.Concurrent;
using System.Text.Json;

namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA 前后端通信桥，支持 WebSocket RPC 调用
/// </summary>
public sealed class VoaBridge
{
    private readonly ConcurrentDictionary<string, Func<JsonElement, Task<object?>>> _handlers = new();

    public VoaBridge()
    {
        RegisterDefaultHandlers();
    }

    /// <summary>
    ///     注册 RPC 方法处理器
    /// </summary>
    public void RegisterHandler(string method, Func<JsonElement, Task<object?>> handler)
    {
        _handlers[method] = handler;
    }

    /// <summary>
    ///     处理 RPC 调用
    /// </summary>
    public async Task<BridgeResult> HandleCallAsync(string requestBody)
    {
        BridgeRequest? request;

        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(requestBody);
        }
        catch (JsonException ex)
        {
            return BridgeResult.Fail(-32700, $"解析错误：{ex.Message}");
        }

        if (request is null || string.IsNullOrEmpty(request.Method))
        {
            return BridgeResult.Fail(-32600, "无效请求");
        }

        if (!_handlers.TryGetValue(request.Method, out var handler))
        {
            return BridgeResult.Fail(-32601, $"方法未找到：{request.Method}");
        }

        try
        {
            var result = await handler(request.Params);
            return BridgeResult.Success(request.Id, result);
        }
        catch (Exception ex)
        {
            return BridgeResult.Fail(-32603, $"内部错误：{ex.Message}", request.Id);
        }
    }

    private void RegisterDefaultHandlers()
    {
        RegisterHandler("voa.ping", _ => Task.FromResult<object?>("pong"));
        RegisterHandler("voa.getState", _ => Task.FromResult<object?>(new { status = "running" }));
        RegisterHandler("voa.getConfig", _ => Task.FromResult<object?>(new { version = "0.0.1" }));

        RegisterHandler("voa.navigate", async param =>
        {
            var path = param.TryGetProperty("path", out var p) ? p.GetString() : "/";
            return new { path, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        });

        RegisterHandler("voa.invoke", async param =>
        {
            var service = param.TryGetProperty("service", out var s) ? s.GetString() : "";
            var method = param.TryGetProperty("method", out var m) ? m.GetString() : "";
            var args = param.TryGetProperty("args", out var a) ? a : default;
            return new { service, method, result = "ok" };
        });
    }
}