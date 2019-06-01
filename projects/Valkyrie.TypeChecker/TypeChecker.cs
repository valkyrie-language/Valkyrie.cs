using Oak.Valkyrie.AST;

namespace Valkyrie.TypeChecker;

/// <summary>
///     Valkyrie 类型检查器最小实现。
///     当前仅维持运行时编译链路可用，完整类型系统后续恢复。
/// </summary>
public sealed class TypeChecker
{
    /// <summary>
    ///     检查编译单元并返回诊断结果。
    ///     现阶段返回空诊断，避免无关目标阻塞构建。
    /// </summary>
    public TypeCheckResult Check(CompilationUnit compilationUnit, string? filePath = null, string? sourceText = null)
    {
        return new TypeCheckResult([]);
    }
}
