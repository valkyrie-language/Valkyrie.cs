namespace Asgard.CLI.Compiler;

/// <summary>
///     插值表达式节点 {expr}
/// </summary>
public sealed record AwslInterpolationNode : AwslIRNode
{
    public string ExprId { get; set; } = string.Empty;
}