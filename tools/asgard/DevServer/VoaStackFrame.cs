namespace Asgard.CLI.DevServer;

/// <summary>
///     堆栈帧信息
/// </summary>
public sealed class VoaStackFrame
{
    public string FunctionName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public bool IsNative { get; set; }
}