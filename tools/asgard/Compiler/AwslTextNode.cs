namespace Asgard.CLI.Compiler;

/// <summary>
///     文本节点
/// </summary>
public sealed record AwslTextNode : AwslIRNode
{
    public string Text { get; set; } = string.Empty;
}