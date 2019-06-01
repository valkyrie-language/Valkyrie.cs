namespace Asgard.CLI.Compiler;

/// <summary>
///     AWSL 组件声明
/// </summary>
public sealed class AwslComponentDecl
{
    public string Name { get; set; } = string.Empty;
    public AwslPropsDecl Props { get; set; } = new();
    public List<AwslIRNode> Nodes { get; set; } = [];
    public List<AwslSignalDecl> Signals { get; set; } = [];
    public List<AwslStyledCss> StyledCss { get; set; } = [];
    public List<AwslIslandDecl> Islands { get; set; } = [];
}