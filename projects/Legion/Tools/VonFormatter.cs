using System.Text;
using Oak.Data;

namespace Legion.Tools;

public static class VonFormatter
{
    public static string Format(SerdeValue value, int indent = 0)
    {
        var sb = new StringBuilder();
        FormatValue(sb, value, indent);
        return sb.ToString();
    }

    private static void FormatValue(StringBuilder sb, SerdeValue value, int indent)
    {
        switch (value.Type)
        {
            case SerdeValueType.Null:
                sb.Append("null");
                break;
            case SerdeValueType.Boolean:
                sb.Append(value.GetBoolean() ? "true" : "false");
                break;
            case SerdeValueType.Integer:
                sb.Append(value.GetIntegerString() ?? "0");
                break;
            case SerdeValueType.Decimal:
                sb.Append(value.GetDecimalString() ?? "0");
                break;
            case SerdeValueType.String:
                sb.Append('"');
                sb.Append(EscapeString(value.GetString() ?? ""));
                sb.Append('"');
                break;
            case SerdeValueType.Array:
                FormatArray(sb, value, indent);
                break;
            case SerdeValueType.Object:
                FormatObject(sb, value, indent);
                break;
        }
    }

    private static void FormatObject(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.Fields is null || value.Fields.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        string indentStr = new string(' ', indent * 4);
        string innerIndentStr = new string(' ', (indent + 1) * 4);

        sb.AppendLine("{");

        var fields = value.Fields.ToList();
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            sb.Append(innerIndentStr);
            sb.Append(NeedsQuoting(field.Key) ? $"\"{EscapeString(field.Key)}\"" : field.Key);
            sb.Append(": ");

            if (field.Value.Type == SerdeValueType.Object || field.Value.Type == SerdeValueType.Array)
            {
                FormatValue(sb, field.Value, indent + 1);
            }
            else
            {
                FormatValue(sb, field.Value, 0);
            }

            if (i < fields.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.Append(indentStr);
        sb.Append('}');
    }

    private static void FormatArray(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.Elements is null || value.Elements.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        bool allSimple = value.Elements.All(e =>
            e.Type != SerdeValueType.Object && e.Type != SerdeValueType.Array);

        if (allSimple)
        {
            sb.Append("[ ");
            for (int i = 0; i < value.Elements.Count; i++)
            {
                FormatValue(sb, value.Elements[i], 0);
                if (i < value.Elements.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" ]");
        }
        else
        {
            string indentStr = new string(' ', indent * 4);
            string innerIndentStr = new string(' ', (indent + 1) * 4);

            sb.AppendLine("[");
            for (int i = 0; i < value.Elements.Count; i++)
            {
                sb.Append(innerIndentStr);
                FormatValue(sb, value.Elements[i], indent + 1);
                if (i < value.Elements.Count - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            sb.Append(indentStr);
            sb.Append(']');
        }
    }

    private static bool NeedsQuoting(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return true;
        }

        if (!char.IsLetter(key[0]) && key[0] != '_')
        {
            return true;
        }

        foreach (char c in key)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return true;
            }
        }

        if (key == "true" || key == "false" || key == "null")
        {
            return true;
        }

        return false;
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}