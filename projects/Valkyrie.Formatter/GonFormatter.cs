using System.Text;
using Oak.Von;

namespace Valkyrie.Formatter;

/// <summary>
/// GGon (.von) 配置格式美化器
/// 将 GonValue 树按缩进格式化为美观文本
/// </summary>
public sealed class GonFormatter
{
    private readonly FormatterConfig _config;

    /// <summary>
    /// 使用指定配置创建美化器
    /// </summary>
    public GonFormatter(FormatterConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 使用默认配置创建美化器
    /// </summary>
    public GonFormatter()
        : this(FormatterConfig.Default)
    {
    }

    /// <summary>
    /// 美化 Gon 值，返回格式化文本
    /// </summary>
    public string Format(GonValue value)
    {
        var sb = new StringBuilder();
        FormatValue(sb, value, 0);
        return sb.ToString();
    }

    /// <summary>
    /// 美化 Gon 源文本（先解析再格式化）
    /// </summary>
    public string FormatText(string source)
    {
        var parser = new GonParser();
        var value = parser.Parse(source);
        return Format(value);
    }

    private void FormatValue(StringBuilder sb, GonValue value, int indent)
    {
        switch (value.Type)
        {
            case GonValueType.Null:
                sb.Append("null");
                break;

            case GonValueType.Boolean:
                sb.Append(value.GetBoolean() ? "true" : "false");
                break;

            case GonValueType.Integer:
                sb.Append(value.GetIntegerString() ?? "0");
                break;

            case GonValueType.Decimal:
                sb.Append(value.GetDecimalString() ?? "0.0");
                break;

            case GonValueType.String:
                FormatString(sb, value.GetString() ?? string.Empty);
                break;

            case GonValueType.Array:
                FormatArray(sb, value.Elements!, indent);
                break;

            case GonValueType.Object:
                FormatObject(sb, value, indent);
                break;
        }
    }

    private void FormatString(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        sb.Append('"');
    }

    private void FormatArray(StringBuilder sb, List<GonValue> elements, int indent)
    {
        if (elements.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        var allSimple = elements.TrueForAll(IsSimple);
        if (allSimple && FitsInLine(elements, indent))
        {
            sb.Append("[");
            for (var i = 0; i < elements.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                FormatValue(sb, elements[i], indent);
            }
            sb.Append("]");
            return;
        }

        sb.Append("[");
        sb.Append(NewLine);
        for (var i = 0; i < elements.Count; i++)
        {
            Indent(sb, indent + 1);
            FormatValue(sb, elements[i], indent + 1);
            if (i < elements.Count - 1)
            {
                sb.Append(',');
            }
            sb.Append(NewLine);
        }
        Indent(sb, indent);
        sb.Append(']');
    }

    private void FormatObject(StringBuilder sb, GonValue value, int indent)
    {
        var hasAnnotation = value.TypeName is not null || value.VariantName is not null;
        if (hasAnnotation)
        {
            if (value.TypeName is not null)
            {
                sb.Append(value.TypeName);
                sb.Append(' ');
            }
            if (value.VariantName is not null)
            {
                sb.Append(value.VariantName);
                sb.Append(' ');
            }
        }

        var fields = value.Fields;
        if (fields is null || fields.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        sb.Append('{');
        sb.Append(NewLine);
        foreach (var (name, fieldValue) in fields)
        {
            Indent(sb, indent + 1);
            sb.Append(name);
            sb.Append(": ");
            FormatValue(sb, fieldValue, indent + 1);
            sb.Append(NewLine);
        }
        Indent(sb, indent);
        sb.Append('}');
    }

    private bool IsSimple(GonValue value)
    {
        return value.Type switch
        {
            GonValueType.Null => true,
            GonValueType.Boolean => true,
            GonValueType.Integer => true,
            GonValueType.Decimal => true,
            GonValueType.String => true,
            _ => false
        };
    }

    private bool FitsInLine(List<GonValue> elements, int indent)
    {
        var sb = new StringBuilder();
        var prefix = new string(' ', indent * _config.IndentSize) + "[";
        sb.Append(prefix);
        for (var i = 0; i < elements.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            FormatValue(sb, elements[i], indent);
        }
        sb.Append(']');
        return sb.Length <= _config.MaxLineWidth;
    }

    private void Indent(StringBuilder sb, int depth)
    {
        sb.Append(new string(' ', depth * _config.IndentSize));
    }

    private string NewLine => _config.NewLineType switch
    {
        NewLineType.CRLF => "\r\n",
        NewLineType.LF => "\n",
        _ => Environment.NewLine
    };
}
