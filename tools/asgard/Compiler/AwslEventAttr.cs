namespace Asgard.CLI.Compiler;

public sealed record AwslEventAttr : AwslAttrNode
{
    public string EventType { get; set; } = string.Empty;
    public string HandlerId { get; set; } = string.Empty;
}