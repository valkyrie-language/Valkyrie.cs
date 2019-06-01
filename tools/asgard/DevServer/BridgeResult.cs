namespace Asgard.CLI.DevServer;

/// <summary>
///     RPC 响应
/// </summary>
public sealed class BridgeResult
{
    public string Id { get; init; } = string.Empty;
    public object? Result { get; init; }
    public BridgeErrorInfo? ErrorInfo { get; init; }

    public bool IsSuccess => ErrorInfo is null;

    public static BridgeResult Success(string id, object? result)
    {
        return new BridgeResult { Id = id, Result = result };
    }

    public static BridgeResult Fail(int code, string message, string? id = null)
    {
        return new BridgeResult
        {
            Id = id ?? string.Empty,
            ErrorInfo = new BridgeErrorInfo { Code = code, Message = message }
        };
    }
}