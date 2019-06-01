using Oak.Syntax;
using Oak.Valkyrie.Lexer;

namespace Valkyrie.Highlight;

/// <summary>
/// Valkyrie 语法高亮器
/// 使用 Oak.Valkyrie.Lexer 进行词法分析，将 Token 映射为 HighlightSpan
/// </summary>
public sealed class ValkyrieSyntaxHighlighter
{
    private static readonly Dictionary<int, HighlightKind> KindMap = new()
    {
        { ValkyrieNodeKind.Number.Value, HighlightKind.Number },
        { ValkyrieNodeKind.String.Value, HighlightKind.String },
        { ValkyrieNodeKind.Comment.Value, HighlightKind.Comment },
        { ValkyrieNodeKind.DocComment.Value, HighlightKind.Comment },
        { ValkyrieNodeKind.Attribute.Value, HighlightKind.Attribute },
        { ValkyrieNodeKind.Identifier.Value, HighlightKind.Identifier },
        { ValkyrieNodeKind.Operator.Value, HighlightKind.Operator },
        { ValkyrieNodeKind.Punctuation.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.Delimiter.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.Colon.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.DoubleColon.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.LeftParen.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.RightParen.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.LeftBracket.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.RightBracket.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.LeftBrace.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.RightBrace.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.Semicolon.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.Comma.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.Arrow.Value, HighlightKind.Delimiter },
        { ValkyrieNodeKind.True.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.False.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.Null.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.TemplateDirective.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.PrimaryKey.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.UniqueKey.Value, HighlightKind.Keyword },
        { ValkyrieNodeKind.StyleClass.Value, HighlightKind.TypeName },
        { ValkyrieNodeKind.Meta.Value, HighlightKind.Other },
        { ValkyrieNodeKind.MetaBlockStart.Value, HighlightKind.Other },
        { ValkyrieNodeKind.MetaBlockEnd.Value, HighlightKind.Other },
    };

    /// <summary>
    /// 对 Valkyrie 源码进行语法高亮
    /// </summary>
    /// <param name="source">Valkyrie 源码文本</param>
    /// <returns>高亮区间列表，按源码位置排序</returns>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new ValkyrieLexer();
        var tokens = lexer.Tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            if (token.Kind == ValkyrieNodeKind.Eof)
            {
                break;
            }

            offset = SkipWhitespace(source, offset);

            var kind = MapKind(token.Kind);

            spans.Add(new HighlightSpan
            {
                Kind = kind,
                Offset = offset,
                Length = token.Width
            });

            offset += token.Width;
        }

        return spans;
    }

    /// <summary>
    /// 将 Valkyrie NodeKind 映射为 HighlightKind
    /// </summary>
    private static HighlightKind MapKind(NodeKind kind)
    {
        if (KindMap.TryGetValue(kind.Value, out var mapped))
        {
            return mapped;
        }

        if (kind.IsKeyword())
        {
            return HighlightKind.Keyword;
        }

        return HighlightKind.Other;
    }

    /// <summary>
    /// 跳过空白字符，返回第一个非空白字符的偏移
    /// </summary>
    private static int SkipWhitespace(string source, int offset)
    {
        while (offset < source.Length && char.IsWhiteSpace(source[offset]))
        {
            offset++;
        }

        return offset;
    }
}
