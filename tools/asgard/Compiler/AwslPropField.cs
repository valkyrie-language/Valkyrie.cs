namespace Asgard.CLI.Compiler;

public sealed class AwslPropField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? DefaultValue { get; set; }
    public bool Required { get; set; }
}