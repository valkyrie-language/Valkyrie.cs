namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA 结构化错误信息
/// </summary>
public sealed class VoaErrorInfo
{
    public string Severity { get; set; } = "error";
    public string SeverityLabel => Severity switch
    {
        "error" => "错误",
        "warning" => "警告",
        "syntax" => "语法错误",
        "compile" => "编译错误",
        "runtime" => "运行时错误",
        _ => "错误"
    };

    public string Title { get; set; } = "编译错误";
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public VoaSourceContext? SourceContext { get; set; }
    public List<VoaErrorInfo>? RelatedErrors { get; set; }
    public List<string>? Suggestions { get; set; }
    public string? DocumentationUrl { get; set; }
    public List<VoaStackFrame>? StackFrames { get; set; }
}