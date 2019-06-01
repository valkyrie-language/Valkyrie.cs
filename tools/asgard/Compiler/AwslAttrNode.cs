namespace Asgard.CLI.Compiler;

/// <summary>
///     HTML 属性节点
/// </summary>
public abstract record AwslAttrNode
{
    public string Name { get; set; } = string.Empty;
}