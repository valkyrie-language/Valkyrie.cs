namespace Asgard.CLI.Compiler;

public sealed class VoaBuildResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public List<string> OutputFiles { get; set; } = [];
}