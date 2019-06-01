namespace Asgard.CLI.DevServer;

/// <summary>
///     AWSL 渲染结果
/// </summary>
public sealed class AwslRenderResult
{
    public string Html { get; init; } = string.Empty;
    public string Css { get; init; } = string.Empty;
    public string JavaScript { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
}