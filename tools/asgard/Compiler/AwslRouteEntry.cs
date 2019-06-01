namespace Asgard.CLI.Compiler;

public sealed class AwslRouteEntry
{
    public string Path { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public bool IsDynamic { get; set; }
    public List<string> Params { get; set; } = [];
}