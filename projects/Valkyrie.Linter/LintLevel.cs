namespace Valkyrie.Linter;

/// <summary>
/// Lint 诊断严重级别
/// </summary>
public enum LintLevel
{
    /// <summary>
    /// 仅供参考，不标记为问题
    /// </summary>
    Info,

    /// <summary>
    /// 建议修改，但不强制
    /// </summary>
    Warning,

    /// <summary>
    /// 必须修改，视为错误
    /// </summary>
    Error,

    /// <summary>
    /// 规则已禁用
    /// </summary>
    Off
}