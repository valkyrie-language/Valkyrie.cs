namespace Valkyrie.Formatter;

public sealed class FormatterResult
{
    public string FormattedText { get; }
    public bool Changed { get; }
    public IReadOnlyList<FormatterDiagnostic> Diagnostics { get; }

    public FormatterResult(string formattedText, bool changed, IReadOnlyList<FormatterDiagnostic> diagnostics)
    {
        FormattedText = formattedText;
        Changed = changed;
        Diagnostics = diagnostics;
    }
}