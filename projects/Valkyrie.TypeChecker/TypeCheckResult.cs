namespace Valkyrie.TypeChecker;

public sealed class TypeCheckResult
{
    public IReadOnlyList<TypeDiagnostic> Diagnostics { get; }
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    public bool HasWarnings => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    public TypeCheckResult(IReadOnlyList<TypeDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }
}