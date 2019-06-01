namespace Asgard.CLI.Compiler;

public sealed record AwslStaticAttr : AwslAttrNode
{
    public string Value { get; set; } = string.Empty;
}