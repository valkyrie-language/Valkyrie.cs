namespace Asgard.CLI.DevServer;

/// <summary>
///     HMR 消息格式
/// </summary>
internal sealed class HmrMessage
{
    public string Type { get; set; } = string.Empty;
    public object? Payload { get; set; }
}