using System.Text.Json;

namespace Asgard.CLI.DevServer;

/// <summary>
///     RPC 请求
/// </summary>
internal sealed class BridgeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Method { get; set; } = string.Empty;
    public JsonElement Params { get; set; }
}