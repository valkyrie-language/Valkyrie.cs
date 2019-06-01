namespace Asgard.CLI.Compiler;

public sealed record AwslDynamicAttr : AwslAttrNode
{
    public string ExprId { get; set; } = string.Empty;
}