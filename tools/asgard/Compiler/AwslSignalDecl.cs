namespace Asgard.CLI.Compiler;

/// <summary>
///     响应式 Signal 声明
/// </summary>
public sealed class AwslSignalDecl
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "any";
    public string? InitialValue { get; set; }
    public bool IsComputed { get; set; }
}