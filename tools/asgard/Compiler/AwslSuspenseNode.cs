namespace Asgard.CLI.Compiler;

/// <summary>
///     Suspense 异步加载节点
/// </summary>
public sealed record AwslSuspenseNode : AwslIRNode
{
    public List<AwslIRNode> FallbackNodes { get; set; } = [];
    public List<AwslIRNode> ContentNodes { get; set; } = [];
}