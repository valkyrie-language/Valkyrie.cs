using Oak.Von;

namespace Valkyrie.Formatter;

/// <summary>
/// 换行符类型
/// </summary>
public enum NewLineType
{
    /// <summary>
    /// 跟随系统
    /// </summary>
    System = 0,

    /// <summary>
    /// LF（Unix/macOS）
    /// </summary>
    LF = 1,

    /// <summary>
    /// CRLF（Windows）
    /// </summary>
    CRLF = 2
}

public sealed class FormatterConfig
{
    public string IndentStyle { get; set; } = "space";
    public int IndentSize { get; set; } = 4;
    public int MaxLineWidth { get; set; } = 120;
    public bool TrailingNewline { get; set; } = true;
    public bool SpaceAroundOperator { get; set; } = true;
    public bool SpaceAfterComma { get; set; } = true;
    public bool SpaceBeforeColon { get; set; } = false;
    public bool SpaceAfterColon { get; set; } = true;
    public string BraceStyle { get; set; } = "same_line";
    public bool SemicolonRequired { get; set; } = true;
    public int BlankLinesBetweenDeclarations { get; set; } = 1;
    public bool AlignConsecutiveDeclarations { get; set; } = false;

    /// <summary>
    /// 换行符类型
    /// </summary>
    public NewLineType NewLineType { get; set; } = NewLineType.System;

    /// <summary>
    /// 换行符文本
    /// </summary>
    public string NewLine => NewLineType switch
    {
        NewLineType.CRLF => "\r\n",
        NewLineType.LF => "\n",
        _ => System.Environment.NewLine
    };

    public static FormatterConfig Default => new();

    public static FormatterConfig Compact => new()
    {
        IndentSize = 2,
        BlankLinesBetweenDeclarations = 0
    };

    /// <summary>
    /// 从 legion.von 的 [formatter] 节读取配置
    /// 未配置的项保留默认值
    /// </summary>
    public static FormatterConfig FromLegionVon(GonValue vonRoot)
    {
        var config = new FormatterConfig();
        var formatterSection = FindSection(vonRoot, "formatter");
        if (formatterSection is null)
        {
            return config;
        }

        config.IndentStyle = GetStringField(formatterSection, "indent_style") ?? config.IndentStyle;
        config.IndentSize = GetIntField(formatterSection, "indent_size") ?? config.IndentSize;
        config.MaxLineWidth = GetIntField(formatterSection, "max_line_width") ?? config.MaxLineWidth;
        config.TrailingNewline = GetBoolField(formatterSection, "trailing_newline") ?? config.TrailingNewline;
        config.SpaceAroundOperator = GetBoolField(formatterSection, "space_around_operator") ?? config.SpaceAroundOperator;
        config.SpaceAfterComma = GetBoolField(formatterSection, "space_after_comma") ?? config.SpaceAfterComma;
        config.SpaceBeforeColon = GetBoolField(formatterSection, "space_before_colon") ?? config.SpaceBeforeColon;
        config.SpaceAfterColon = GetBoolField(formatterSection, "space_after_colon") ?? config.SpaceAfterColon;
        config.BraceStyle = GetStringField(formatterSection, "brace_style") ?? config.BraceStyle;
        config.SemicolonRequired = GetBoolField(formatterSection, "semicolon_required") ?? config.SemicolonRequired;
        config.BlankLinesBetweenDeclarations = GetIntField(formatterSection, "blank_lines_between_declarations") ?? config.BlankLinesBetweenDeclarations;
        config.AlignConsecutiveDeclarations = GetBoolField(formatterSection, "align_consecutive_declarations") ?? config.AlignConsecutiveDeclarations;
        config.NewLineType = GetNewLineType(formatterSection);

        return config;
    }

    public string GetIndent(int level)
    {
        var indent = IndentStyle == "tab" ? "\t" : new string(' ', IndentSize);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < level; i++)
        {
            sb.Append(indent);
        }
        return sb.ToString();
    }

    private static GonValue? FindSection(GonValue root, string sectionName)
    {
        if (root.Type != GonValueType.Object || root.Fields is null)
        {
            return null;
        }

        if (root.Fields.TryGetValue(sectionName, out var section) && section.Type == GonValueType.Object)
        {
            return section;
        }

        return null;
    }

    private static string? GetStringField(GonValue obj, string fieldName)
    {
        if (obj.Fields is null)
        {
            return null;
        }

        if (obj.Fields.TryGetValue(fieldName, out var value) && value.Type == GonValueType.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static int? GetIntField(GonValue obj, string fieldName)
    {
        if (obj.Fields is null)
        {
            return null;
        }

        if (obj.Fields.TryGetValue(fieldName, out var value))
        {
            if (value.Type == GonValueType.Integer && int.TryParse(value.GetIntegerString(), out var intValue))
            {
                return intValue;
            }
        }

        return null;
    }

    private static bool? GetBoolField(GonValue obj, string fieldName)
    {
        if (obj.Fields is null)
        {
            return null;
        }

        if (obj.Fields.TryGetValue(fieldName, out var value) && value.Type == GonValueType.Boolean)
        {
            return value.GetBoolean();
        }

        return null;
    }

    private static NewLineType GetNewLineType(GonValue obj)
    {
        var value = GetStringField(obj, "newline_type");
        return value switch
        {
            "lf" => NewLineType.LF,
            "crlf" => NewLineType.CRLF,
            _ => NewLineType.System
        };
    }
}
