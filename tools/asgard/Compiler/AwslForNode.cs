namespace Asgard.CLI.Compiler;

/// <summary>
///     列表渲染节点 #for
/// </summary>
public sealed record AwslForNode : AwslIRNode
{
    public string VarName { get; set; } = string.Empty;
    public string IterableExprId { get; set; } = string.Empty;
    public List<AwslIRNode> BodyNodes { get; set; } = [];
    public string? KeyExprId { get; set; }
}