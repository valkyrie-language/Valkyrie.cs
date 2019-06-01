namespace Asgard.CLI.DevServer;

/// <summary>
///     AWSL SSR 渲染结果
/// </summary>
public sealed class AwslSsrRenderResult
{
    public string Html { get; init; } = string.Empty;
    public string Css { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public string InitialStateJson { get; init; } = "{}";
    public string Scope { get; init; } = string.Empty;
    public List<string> HeadTags { get; init; } = [];
    public List<string> ScriptTags { get; init; } = [];
    public bool IsSuspense { get; init; }
    public string? FallbackHtml { get; init; }
}