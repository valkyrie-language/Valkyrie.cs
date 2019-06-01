namespace Valkyrie.Linter;

/// <summary>
/// Lint 检查结果
/// </summary>
public sealed class LintResult
{
    /// <summary>
    /// 所有诊断信息
    /// </summary>
    public IReadOnlyList<LintDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// 是否有 Error 级别的问题
    /// </summary>
    public bool HasErrors => _hasErrors ??= Diagnostics.Any(d => d.Level == LintLevel.Error);

    /// <summary>
    /// 是否有 Warning 或更高级别的问题
    /// </summary>
    public bool HasWarnings => _hasWarnings ??= Diagnostics.Any(d => d.Level >= LintLevel.Warning);

    private bool? _hasErrors;
    private bool? _hasWarnings;
}