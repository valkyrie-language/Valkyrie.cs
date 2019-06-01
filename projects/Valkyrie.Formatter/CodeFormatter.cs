using System.Text;
using Oak.Valkyrie.AST;

namespace Valkyrie.Formatter;

public sealed partial class CodeFormatter
{
    private readonly FormatterConfig _config;
    private readonly StringBuilder _sb;
    private int _indentLevel;
    private bool _atLineStart;
    private readonly List<FormatterDiagnostic> _diagnostics;

    public CodeFormatter(FormatterConfig? config = null)
    {
        _config = config ?? FormatterConfig.Default;
        _sb = new StringBuilder();
        _indentLevel = 0;
        _atLineStart = true;
        _diagnostics = new List<FormatterDiagnostic>();
    }

    public FormatterResult Format(CompilationUnit compilationUnit)
    {
        _sb.Clear();
        _indentLevel = 0;
        _atLineStart = true;
        _diagnostics.Clear();

        for (int i = 0; i < compilationUnit.Declarations.Count; i++)
        {
            FormatNode(compilationUnit.Declarations[i]);

            if (i < compilationUnit.Declarations.Count - 1)
            {
                for (int blank = 0; blank < _config.BlankLinesBetweenDeclarations; blank++)
                {
                    NewLine();
                }
            }
        }

        if (_config.TrailingNewline && _sb.Length > 0 && _sb[_sb.Length - 1] != '\n')
        {
            NewLine();
        }

        return new FormatterResult(_sb.ToString(), true, _diagnostics);
    }


    #region 辅助方法

    private void FormatAttributes(IReadOnlyList<AttributeDecl> attributes)
    {
        foreach (var attr in attributes)
        {
            WriteIndent();
            FormatAttribute(attr);
            NewLine();
        }
    }

    private void FormatAttributesInline(IReadOnlyList<AttributeDecl> attributes)
    {
        foreach (var attr in attributes)
        {
            FormatAttribute(attr);
            Write(" ");
        }
    }

    private void FormatAttribute(AttributeDecl attr)
    {
        Write("[");
        Write(attr.Name);

        if (attr.Arguments.Count > 0)
        {
            Write("(");

            for (int i = 0; i < attr.Arguments.Count; i++)
            {
                Write(attr.Arguments[i].Key);

                if (attr.Arguments[i].Value != "true")
                {
                    Write(" = ");
                    Write(attr.Arguments[i].Value);
                }

                if (i < attr.Arguments.Count - 1)
                {
                    Write(",");
                    if (_config.SpaceAfterComma)
                    {
                        Write(" ");
                    }
                }
            }

            Write(")");
        }

        Write("]");
    }

    private void WriteTypeAnnotation(TypeAnnotation type)
    {
        Write(type.Name);

        if (type.GenericArgs.Count > 0)
        {
            Write("<");

            for (int i = 0; i < type.GenericArgs.Count; i++)
            {
                WriteTypeAnnotation(type.GenericArgs[i]);

                if (i < type.GenericArgs.Count - 1)
                {
                    Write(",");
                    if (_config.SpaceAfterComma)
                    {
                        Write(" ");
                    }
                }
            }

            Write(">");
        }
    }

    private void OpenBrace()
    {
        if (_config.BraceStyle == "next_line")
        {
            NewLine();
            WriteIndent();
        }

        Write("{");
    }

    private void CloseBrace()
    {
        Write("}");
    }

    private void WriteSemicolon()
    {
        if (_config.SemicolonRequired)
        {
            Write(";");
        }
    }

    private void WriteIndent()
    {
        if (_atLineStart)
        {
            Write(_config.GetIndent(_indentLevel));
            _atLineStart = false;
        }
    }

    private void NewLine()
    {
        _sb.AppendLine();
        _atLineStart = true;
    }

    private void Write(string text)
    {
        _sb.Append(text);
        if (text.Length > 0)
        {
            _atLineStart = false;
        }
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    #endregion
}