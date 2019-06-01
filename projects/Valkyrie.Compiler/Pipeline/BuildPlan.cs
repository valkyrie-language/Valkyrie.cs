namespace Valkyrie.Compiler.Pipeline;

/// <summary>
/// 编译计划。
/// 围绕模块名、`CanonicalTriple` 与输出选项收拢编译输入。
/// </summary>
public sealed record BuildPlan(
    string ModuleName,
    string CanonicalTriple,
    string? FilePath = null,
    bool EnableDebugArtifacts = false,
    int OptimizationLevel = 0);
