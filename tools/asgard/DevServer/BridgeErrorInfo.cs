namespace Asgard.CLI.DevServer;

/// <summary>
///     RPC 错误
/// </summary>
public sealed class BridgeErrorInfo
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
}