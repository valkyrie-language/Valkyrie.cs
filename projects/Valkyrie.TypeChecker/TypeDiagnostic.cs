namespace Valkyrie.TypeChecker;

/// <summary>
///     类型检查诊断信///     包含错误代码、消息、位置和修复建议
/// </summary>
public sealed class TypeDiagnostic
{
    /// <summary>错误代码（如 VALK2001/summary>
    public string Code { get; }

    /// <summary>错误消息（中文描述）</summary>
    public string Message { get; }

    /// <summary>诊断严重/summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>行号</summary>
    public int Line { get; }

    /// <summary>列号</summary>
    public int Column { get; }

    /// <summary>文件路径</summary>
    public string? FilePath { get; }

    /// <summary>修复建议（可选，用于 IDE 快速修复）</summary>
    public string? Suggestion { get; init; }

    /// <summary>文档链接（可选，指向在线帮助页）</summary>
    public string? DocumentationUrl { get; init; }

    /// <summary>相关诊断（可选，链式错误的上下文/summary>
    public TypeDiagnostic? RelatedDiagnostic { get; init; }

    public TypeDiagnostic(string code, string message, DiagnosticSeverity severity,
        int line = 0, int column = 0, string? filePath = null)
    {
        Code = code;
        Message = message;
        Severity = severity;
        Line = line;
        Column = column;
        FilePath = filePath;
    }

    /// <summary>
    ///     创建带修复建议的诊断
    /// </summary>
    public static TypeDiagnostic WithSuggestion(string code, string message, string suggestion,
        int line = 0, int column = 0, string? filePath = null)
    {
        return new TypeDiagnostic(code, message, DiagnosticSeverity.Error, line, column, filePath)
        {
            Suggestion = suggestion,
        };
    }

    /// <summary>
    ///     创建带文档链接的诊断
    /// </summary>
    public static TypeDiagnostic WithDoc(string code, string message, string docUrl,
        int line = 0, int column = 0, string? filePath = null)
    {
        return new TypeDiagnostic(code, message, DiagnosticSeverity.Error, line, column, filePath)
        {
            DocumentationUrl = docUrl,
        };
    }

    public override string ToString()
    {
        var location = Line > 0 ? $"({Line}:{Column}) " : "";
        var result = $"{location}{Severity.ToString().ToLower()}: {Code} {Message}";

        if (Suggestion != null)
        {
            result += $"  💡 建议：{Suggestion}";
        }

        if (DocumentationUrl != null)
        {
            result += $"  📖 文档：{DocumentationUrl}";
        }

        return result;
    }
}