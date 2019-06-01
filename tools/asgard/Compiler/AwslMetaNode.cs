namespace Asgard.CLI.Compiler;

/// <summary>
///     Meta 标签节点（Head / Script）
/// </summary>
public sealed record AwslMetaNode : AwslIRNode
{
    public AwslMetaKind Kind { get; set; }
    public List<AwslIRNode> ContentNodes { get; set; } = [];
}