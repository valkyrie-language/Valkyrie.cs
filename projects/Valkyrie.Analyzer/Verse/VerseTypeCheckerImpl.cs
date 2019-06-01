using Oak.Verse.AST;

namespace Valkyrie.Analyzer.Verse;

/// <summary>
///     Verse 类型检查器实现
/// </summary>
public sealed class VerseTypeCheckerImpl
{
    public VerseTypeCheckerImpl()
    {
        Bridge = new VerseSemanticBridge();
    }

    public VerseSemanticBridge Bridge { get; }

    public Nyar.Semantic.SemanticModel CheckCompilationUnit(CompilationUnit compilationUnit, string? filePath = null)
    {
        return Bridge.BuildSemanticModel(compilationUnit, filePath ?? "");
    }
}
