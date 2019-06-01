namespace Asgard.CLI.Compiler;

/// <summary>
///     条件渲染节点 #if
/// </summary>
public sealed record AwslConditionalNode : AwslIRNode
{
    public string CondExprId { get; set; } = string.Empty;
    public List<AwslIRNode> ThenNodes { get; set; } = [];
    public List<AwslIRNode> ElseNodes { get; set; } = [];
}