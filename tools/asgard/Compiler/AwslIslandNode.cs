namespace Asgard.CLI.Compiler;

/// <summary>
///     Island 架构节点
/// </summary>
public sealed record AwslIslandNode : AwslIRNode
{
    public AwslIslandKind Kind { get; set; }
    public string ComponentRef { get; set; } = string.Empty;
    public Dictionary<string, string> Props { get; set; } = [];
}