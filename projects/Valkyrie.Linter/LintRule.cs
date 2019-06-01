using Oak.Syntax;
using Oak.Valkyrie.AST;

namespace Valkyrie.Linter;

/// <summary>
/// Lint 规则接口
/// </summary>
public interface ILintRule
{
    /// <summary>
    /// 规则 ID（如 VALK_L001）
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// 规则描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 默认严重级别
    /// </summary>
    LintLevel DefaultLevel { get; }

    /// <summary>
    /// 规则分类
    /// </summary>
    LintCategory Category { get; }

    /// <summary>
    /// 检查 AST 节点，返回诊断列表
    /// </summary>
    IReadOnlyList<LintDiagnostic> Check(CompilationUnit ast, string? filePath = null);
}

/// <summary>
/// Lint 规则分类
/// </summary>
public enum LintCategory
{
    /// <summary>
    /// 代码质量
    /// </summary>
    CodeQuality,

    /// <summary>
    /// ECS 架构约束
    /// </summary>
    Ecs,

    /// <summary>
    /// 安全规则
    /// </summary>
    Security
}