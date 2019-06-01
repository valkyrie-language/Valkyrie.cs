namespace Asgard.CLI.DevServer;

/// <summary>
///     源码上下文（错误行附近的源码片段）
/// </summary>
public sealed class VoaSourceContext
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string[] Lines { get; set; } = [];
}