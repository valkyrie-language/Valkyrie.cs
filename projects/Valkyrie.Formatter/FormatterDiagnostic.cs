namespace Valkyrie.Formatter;

public sealed class FormatterDiagnostic
{
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }

    public FormatterDiagnostic(string message, int line = 0, int column = 0)
    {
        Message = message;
        Line = line;
        Column = column;
    }
}