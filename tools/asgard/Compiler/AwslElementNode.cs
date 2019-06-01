namespace Asgard.CLI.Compiler;

/// <summary>
///     HTML 元素节点
/// </summary>
public sealed record AwslElementNode : AwslIRNode
{
    public string Tag { get; set; } = "div";
    public List<AwslAttrNode> Attrs { get; set; } = [];
    public List<AwslIRNode> Children { get; set; } = [];
    public int NodeId { get; set; }
}