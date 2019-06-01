using Oak.Syntax;
using Oak.Valkyrie.AST;

namespace Valkyrie.Linter;

/// <summary>
/// Lint 诊断信息
/// </summary>
public sealed record LintDiagnostic
{
    /// <summary>
    /// 规则 ID（如 VALK_L001）
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// 诊断消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 实际严重级别（考虑配置后的）
    /// </summary>
    public LintLevel Level { get; init; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public TextSpan Span { get; init; }

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// 行号（1-based）
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 列号（1-based）
    /// </summary>
    public int Column { get; init; }
}