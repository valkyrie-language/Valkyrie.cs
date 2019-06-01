using System.Text;
using System.Text.Json;
using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Shader;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.Highlight;
using ValkyrieTypeChecker = Valkyrie.TypeChecker.TypeChecker;
using TypeCheckResult = Valkyrie.TypeChecker.TypeCheckResult;
using TypeDiagnostic = Valkyrie.TypeChecker.TypeDiagnostic;
using DiagnosticSeverity = Valkyrie.TypeChecker.DiagnosticSeverity;

namespace Valkyrie.LSP;

/// <summary>
/// Valkyrie Language Server Protocol 实现
/// 基于 stdio JSON-RPC 传输，提供 Diagnostics / SemanticTokens / Completion / Hover / Definition
/// </summary>
public sealed class ValkyrieLanguageServer
{
    private static readonly Dictionary<string, Tuple<string, string, string>> ActiveDocuments = new(StringComparer.Ordinal);
    private static readonly ValkyrieTypeChecker TypeChecker = new();
    private static readonly ValkyrieSyntaxHighlighter Highlighter = new();

    private static readonly string[] Keywords =
    [
        "import", "export", "micro", "class", "struct", "component",
        "system", "widget", "plugin", "shader", "uniform", "varying",
        "texture", "sampler", "vi", "ve", "if", "else", "for", "in",
        "while", "do", "break", "continue", "return", "match", "case",
        "discard", "resume", "auto", "lambda", "new", "this", "self",
        "null", "true", "false", "where", "typeof",
    ];

    private static readonly string[] PrimitiveTypes =
    [
        "i8", "i16", "i32", "i64", "u8", "u16", "u32", "u64",
        "f32", "f64", "bool", "string", "void", "none",
        "vec2", "vec3", "vec4", "ivec2", "ivec3", "ivec4",
        "uvec2", "uvec3", "uvec4", "mat3", "mat4",
    ];

    #region 入口

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        await RunAsync();
    }

    private static async Task RunAsync()
    {
        var input = Console.OpenStandardInput();
        var buffer = new byte[4096];
        var contentLength = 0;
        var readingHeaders = true;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bodyBuffer = new MemoryStream();

        while (true)
        {
            var read = await input.ReadAsync(buffer);
            if (read == 0)
            {
                break;
            }

            var span = buffer.AsSpan(0, read);
            if (readingHeaders)
            {
                var str = Encoding.ASCII.GetString(span);
                var headerEnd = str.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd >= 0)
                {
                    var headerPart = str[..headerEnd];
                    foreach (var line in headerPart.Split("\r\n"))
                    {
                        var colon = line.IndexOf(':');
                        if (colon > 0)
                        {
                            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
                        }
                    }

                    contentLength = int.TryParse(headers.GetValueOrDefault("Content-Length", "0"), out var cl) ? cl : 0;
                    headers.Clear();
                    readingHeaders = false;
                    bodyBuffer.SetLength(0);

                    var bodyStart = headerEnd + 4;
                    var remaining = read - bodyStart;
                    if (remaining > 0)
                    {
                        bodyBuffer.Write(buffer, bodyStart, remaining);
                    }
                }
            }
            else
            {
                bodyBuffer.Write(span);
            }

            if (!readingHeaders && bodyBuffer.Length >= contentLength)
            {
                var body = Encoding.UTF8.GetString(bodyBuffer.GetBuffer(), 0, contentLength);
                await ProcessMessageAsync(body);

                var extra = bodyBuffer.Length - contentLength;
                if (extra > 0)
                {
                    var extraBytes = new byte[extra];
                    Array.Copy(bodyBuffer.GetBuffer(), contentLength, extraBytes, 0, (int)extra);
                    var extraStr = Encoding.ASCII.GetString(extraBytes);
                    readingHeaders = true;
                    bodyBuffer.SetLength(0);

                    if (extraStr.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        headers = extraStr.Split("\r\n")
                            .TakeWhile(l => !string.IsNullOrEmpty(l))
                            .Select(l => l.Split(':', 2))
                            .Where(p => p.Length == 2)
                            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

                        contentLength = int.TryParse(headers.GetValueOrDefault("Content-Length", "0"), out var cl) ? cl : 0;
                        readingHeaders = false;
                        headers.Clear();
                    }
                }
                else
                {
                    readingHeaders = true;
                    bodyBuffer.SetLength(0);
                }
            }
        }
    }

    #endregion

    #region 消息处理

    private static async Task ProcessMessageAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var json = doc.RootElement;

        if (json.TryGetProperty("id", out var idElement) && json.TryGetProperty("method", out var methodElement))
        {
            await HandleRequestAsync(idElement, methodElement.GetString()!, json.GetProperty("params"));
        }
        else if (json.TryGetProperty("method", out var notifMethod))
        {
            await HandleNotificationAsync(notifMethod.GetString()!, json.GetProperty("params"));
        }
    }

    private static async Task HandleRequestAsync(JsonElement id, string method, JsonElement @params)
    {
        try
        {
            var result = await HandleMethodAsync(method, @params);
            await SendResponseAsync(id, result);
        }
        catch (Exception ex)
        {
            await SendErrorAsync(id, -32603, ex.Message);
        }
    }

    private static Task HandleNotificationAsync(string method, JsonElement @params)
    {
        _ = HandleMethodAsync(method, @params);
        return Task.CompletedTask;
    }

    #endregion

    #region LSP 方法分发

    private static Task<object?> HandleMethodAsync(string method, JsonElement @params)
    {
        return method switch
        {
            "initialize" => Task.FromResult<object?>(HandleInitialize(@params)),
            "initialized" => Task.FromResult<object?>(null),
            "textDocument/didOpen" => Task.FromResult<object?>(HandleDidOpen(@params)),
            "textDocument/didChange" => Task.FromResult<object?>(HandleDidChange(@params)),
            "textDocument/didClose" => Task.FromResult<object?>(HandleDidClose(@params)),
            "textDocument/diagnostic" => Task.FromResult<object?>(HandleDiagnostic(@params)),
            "textDocument/semanticTokens/full" => Task.FromResult<object?>(HandleSemanticTokens(@params)),
            "textDocument/completion" => Task.FromResult<object?>(HandleCompletion(@params)),
            "textDocument/hover" => Task.FromResult<object?>(HandleHover(@params)),
            "textDocument/definition" => Task.FromResult<object?>(HandleDefinition(@params)),
            "shutdown" => Task.FromResult<object?>(null),
            "exit" => Task.FromResult<object?>(HandleExit(@params)),
            _ => throw new NotSupportedException($"方法未实现：{method}")
        };
    }

    #endregion

    #region LSP 协议实现

    private static object HandleInitialize(JsonElement _)
    {
        return new
        {
            capabilities = new
            {
                textDocumentSync = new
                {
                    openClose = true,
                    change = 1,
                },
                diagnosticProvider = new
                {
                    interFileDependencies = false,
                    workspaceDiagnostics = false
                },
                semanticTokensProvider = new
                {
                    legend = new
                    {
                        tokenTypes = new[]
                        {
                            "keyword", "number", "string", "comment", "operator",
                            "type", "function", "variable", "parameter", "property",
                            "namespace", "decorator"
                        },
                        tokenModifiers = new[]
                        {
                            "declaration", "definition", "readonly", "static"
                        }
                    },
                    full = true
                },
                completionProvider = new
                {
                    resolveProvider = false,
                    triggerCharacters = new[] { ".", ":" }
                },
                hoverProvider = true,
                definitionProvider = true,
                documentFormattingProvider = true,
                documentSymbolProvider = true,
                referencesProvider = true,
            }
        };
    }

    private static object? HandleDidOpen(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;
        var text = td.GetProperty("text").GetString()!;
        var languageId = td.TryGetProperty("languageId", out var lid) ? lid.GetString()! : "v";

        ActiveDocuments[uri] = Tuple.Create(text, languageId, "1");

        return null;
    }

    private static object? HandleDidChange(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;
        var version = td.TryGetProperty("version", out var v) ? v.GetInt32().ToString() : "1";

        var changes = @params.GetProperty("contentChanges");
        if (changes.GetArrayLength() > 0)
        {
            var text = changes[changes.GetArrayLength() - 1].GetProperty("text").GetString()!;
            if (ActiveDocuments.TryGetValue(uri, out var current))
            {
                ActiveDocuments[uri] = Tuple.Create(text, current.Item2, version);
            }
            else
            {
                ActiveDocuments[uri] = Tuple.Create(text, "v", version);
            }
        }

        return null;
    }

    private static object? HandleDidClose(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;
        ActiveDocuments.Remove(uri);
        return null;
    }

    private static object? HandleDiagnostic(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return new { kind = "full", items = Array.Empty<object>() };
        }

        var diagnostics = GetDiagnostics(doc.Item1, uri);
        return new { kind = "full", items = diagnostics };
    }

    private static object? HandleSemanticTokens(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return new { data = Array.Empty<int>() };
        }

        var source = doc.Item1;
        var highlights = Highlighter.Highlight(source);
        var lines = source.Split('\n');

        var tokens = new List<int>();
        var prevLine = 0;
        var prevChar = 0;

        foreach (var span in highlights)
        {
            var position = FindLineColumn(lines, span.Offset);
            var line = position.Item1;
            var character = position.Item2;
            var deltaLine = line;
            var deltaStart = deltaLine == 0 ? character - prevChar : character;

            tokens.Add(deltaLine);
            tokens.Add(deltaStart);
            tokens.Add(span.Length);
            tokens.Add(MapHighlightKindToTokenType(span.Kind));
            tokens.Add(0);

            prevLine = line;
            prevChar = character;
        }

        return new { data = tokens.ToArray() };
    }

    private static object? HandleCompletion(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return new { isIncomplete = false, items = Array.Empty<object>() };
        }

        var position = @params.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();

        var completions = GetCompletions(doc.Item1, line, character);
        return new { isIncomplete = false, items = completions };
    }

    private static object? HandleHover(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return null;
        }

        var position = @params.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();

        var hover = GetHover(doc.Item1, uri, line, character);
        return hover;
    }

    private static object? HandleDefinition(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return null;
        }

        var position = @params.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();

        var offset = LineColToOffset(doc.Item1, line, character);
        var symbolName = GetSymbolAtOffset(doc.Item1, offset);

        if (symbolName is null)
        {
            return null;
        }

        var location = FindDeclarationLocation(doc.Item1, uri, symbolName);
        return location is not null ? new[] { location } : null;
    }

    private static object? HandleReferences(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return null;
        }

        var position = @params.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();

        var offset = LineColToOffset(doc.Item1, line, character);
        var symbolName = GetSymbolAtOffset(doc.Item1, offset);

        if (symbolName is null)
        {
            return null;
        }

        return FindAllReferences(doc.Item1, uri, symbolName);
    }

    private static object? HandleFormatting(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return null;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(doc.Item1);
            var ast = (CompilationUnit)parser.Parse(tokens);

            var formatter = new Valkyrie.Formatter.CodeFormatter();
            var formatted = formatter.Format(ast).FormattedText;

            if (formatted == doc.Item1)
            {
                return Array.Empty<object>();
            }

            var edits = new List<object>
            {
                new
                {
                    range = new
                    {
                        start = new { line = 0, character = 0 },
                        end = OffsetToLineColRange(doc.Item1, doc.Item1.Length)
                    },
                    newText = formatted
                }
            };

            return edits;
        }
        catch
        {
            return null;
        }
    }

    private static object? HandleDocumentSymbol(JsonElement @params)
    {
        var td = @params.GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;

        if (!ActiveDocuments.TryGetValue(uri, out var doc))
        {
            return Array.Empty<object>();
        }

        return GetDocumentSymbols(doc.Item1, uri);
    }

    private static object? HandleExit(JsonElement _)
    {
        Environment.Exit(0);
        return null;
    }

    #endregion

    #region 核心功能实现

    private static List<object> GetDiagnostics(string source, string uri)
    {
        var result = new List<object>();

        var diagnostics = new DiagnosticSink();
        var lexer = new ValkyrieLexer(diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);

        try
        {
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var diag in diagnostics.Errors)
            {
                var pos = OffsetToLineCol(source, diag.Span.Start);
                result.Add(new
                {
                    range = new
                    {
                        start = new { line = pos.Item1, character = pos.Item2 },
                        end = new { line = pos.Item1, character = pos.Item2 + Math.Max(1, diag.Span.Length) }
                    },
                    severity = 1,
                    message = diag.Message,
                    source = "v"
                });
            }

            diagnostics.Clear();

            var tcResult = TypeChecker.Check(ast, UriToFilePath(uri));
            foreach (var diag in tcResult.Diagnostics)
            {
                var severity = diag.Severity switch
                {
                    DiagnosticSeverity.Error => 1,
                    DiagnosticSeverity.Warning => 2,
                    _ => 3
                };

                var line = diag.Line > 0 ? diag.Line - 1 : 0;
                var col = diag.Column > 0 ? diag.Column - 1 : 0;

                result.Add(new
                {
                    range = new
                    {
                        start = new { line, character = col },
                        end = new { line, character = col + 1 }
                    },
                    severity,
                    message = $"[{diag.Code}] {diag.Message}",
                    source = "v"
                });
            }
        }
        catch
        {
            result.Add(new
            {
                range = new
                {
                    start = new { line = 0, character = 0 },
                    end = new { line = 0, character = 1 }
                },
                severity = 1,
                message = "源码解析失败",
                source = "v"
            });
        }

        return result;
    }

    private static List<object> GetCompletions(string source, int line, int character)
    {
        var completions = new List<object>();

        foreach (var kw in Keywords)
        {
            completions.Add(new
            {
                label = kw,
                kind = 14,
                detail = "关键字",
                sortText = $"0_{kw}"
            });
        }

        foreach (var pt in PrimitiveTypes)
        {
            completions.Add(new
            {
                label = pt,
                kind = 7,
                detail = $"基本类型 {pt}",
                sortText = $"1_{pt}"
            });
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var decl in ast.Declarations)
            {
                switch (decl)
                {
                    case FunctionDecl func:
                        completions.Add(new
                        {
                            label = func.Name,
                            kind = 3,
                            detail = "微",
                            sortText = $"2_{func.Name}"
                        });
                        break;
                    case VariableDecl varDecl:
                        completions.Add(new
                        {
                            label = varDecl.Name,
                            kind = 6,
                            detail = "变量",
                            sortText = $"2_{varDecl.Name}"
                        });
                        break;
                    case ClassDecl classDecl:
                        completions.Add(new
                        {
                            label = classDecl.Name,
                            kind = 14,
                            detail = "struct",
                            sortText = $"2_{classDecl.Name}"
                        });
                        break;
                    case ComponentDecl comp:
                        completions.Add(new
                        {
                            label = comp.Name,
                            kind = 7,
                            detail = "组件",
                            sortText = $"2_{comp.Name}"
                        });
                        break;
                }
            }
        }
        catch
        {
        }

        return completions;
    }

    private static object? GetHover(string source, string uri, int line, int character)
    {
        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var decl in ast.Declarations)
            {
                switch (decl)
                {
                    case FunctionDecl func:
                        if (ContainsPosition(source, func.Span, line, character))
                        {
                            return new
                            {
                                contents = new { kind = "markdown", value = $"**微** `{func.Name}`\n\n{func.Name}(...)" }
                            };
                        }
                        break;
                    case VariableDecl varDecl:
                        if (ContainsPosition(source, varDecl.Span, line, character))
                        {
                            return new
                            {
                                contents = new { kind = "markdown", value = $"**变量** `{varDecl.Name}`" }
                            };
                        }
                        break;
                    case ClassDecl classDecl:
                        if (ContainsPosition(source, classDecl.Span, line, character))
                        {
                            return new
                            {
                                contents = new { kind = "markdown", value = $"**类** `{classDecl.Name}`" }
                            };
                        }
                        break;
                    case ComponentDecl comp:
                        if (ContainsPosition(source, comp.Span, line, character))
                        {
                            return new
                            {
                                contents = new { kind = "markdown", value = $"**组件** `{comp.Name}`" }
                            };
                        }
                        break;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    #endregion

    #region 辅助方法

    private static string? GetSymbolAtOffset(string source, int offset)
    {
        if (offset < 0 || offset >= source.Length) return null;

        var start = offset;
        while (start > 0 && IsIdentifierChar(source[start - 1])) start--;
        var end = offset;
        while (end < source.Length && IsIdentifierChar(source[end])) end++;

        if (start == end) return null;
        return source[start..end];
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private static object? FindDeclarationLocation(string source, string uri, string symbolName)
    {
        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var decl in ast.Declarations)
            {
                var (name, span) = GetDeclarationNameAndSpan(decl);
                if (name == symbolName)
                {
                    var start = OffsetToLineCol(source, span.Start);
                    var end = OffsetToLineCol(source, span.Start + Math.Max(1, span.Length));
                    return new
                    {
                        uri,
                        range = new
                        {
                            start = new { line = start.Item1, character = start.Item2 },
                            end = new { line = end.Item1, character = end.Item2 }
                        }
                    };
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static List<object> FindAllReferences(string source, string uri, string symbolName)
    {
        var references = new List<object>();

        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var decl in ast.Declarations)
            {
                var (name, span) = GetDeclarationNameAndSpan(decl);
                if (name == symbolName)
                {
                    var start = OffsetToLineCol(source, span.Start);
                    var end = OffsetToLineCol(source, span.Start + Math.Max(1, span.Length));
                    references.Add(new
                    {
                        uri,
                        range = new
                        {
                            start = new { line = start.Item1, character = start.Item2 },
                            end = new { line = end.Item1, character = end.Item2 }
                        }
                    });
                }
            }

            var searchIndex = 0;
            while (searchIndex < source.Length)
            {
                var idx = source.IndexOf(symbolName, searchIndex, StringComparison.Ordinal);
                if (idx < 0) break;

                var prevChar = idx > 0 ? source[idx - 1] : '\0';
                var nextIdx = idx + symbolName.Length;
                var nextChar = nextIdx < source.Length ? source[nextIdx] : '\0';

                if (!IsIdentifierChar(prevChar) && !IsIdentifierChar(nextChar))
                {
                    var start = OffsetToLineCol(source, idx);
                    var end = OffsetToLineCol(source, nextIdx);
                    references.Add(new
                    {
                        uri,
                        range = new
                        {
                            start = new { line = start.Item1, character = start.Item2 },
                            end = new { line = end.Item1, character = end.Item2 }
                        }
                    });
                }

                searchIndex = nextIdx;
            }
        }
        catch
        {
        }

        return references;
    }

    private static (string Name, TextSpan Span) GetDeclarationNameAndSpan(AstNode decl)
    {
        return decl switch
        {
            FunctionDecl f => (f.Name, f.Span),
            VariableDecl v => (v.Name, v.Span),
            ClassDecl c => (c.Name, c.Span),
            StructureDecl s => (s.Name, s.Span),
            ComponentDecl c => (c.Name, c.Span),
            SystemDecl s => (s.Name, s.Span),
            WidgetDecl w => (w.Name, w.Span),
            PluginDecl p => (p.Name, p.Span),
            EnumDecl e => (e.Name, e.Span),
            FlagsDecl f => (f.Name, f.Span),
            UnionDecl u => (u.Name, u.Span),
            NamespaceDecl n => (n.Name, n.Span),
            _ => ("", default)
        };
    }

    private static List<object> GetDocumentSymbols(string source, string uri)
    {
        var symbols = new List<object>();

        try
        {
            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var tokens = lexer.Tokenize(source);
            var ast = (CompilationUnit)parser.Parse(tokens);

            foreach (var decl in ast.Declarations)
            {
                var (name, span) = GetDeclarationNameAndSpan(decl);
                if (string.IsNullOrEmpty(name)) continue;

                var start = OffsetToLineCol(source, span.Start);
                var end = OffsetToLineCol(source, span.Start + Math.Max(1, span.Length));

                var kind = decl switch
                {
                    FunctionDecl => 12,
                    VariableDecl => 13,
                    ClassDecl or StructureDecl => 23,
                    ComponentDecl => 7,
                    SystemDecl => 12,
                    EnumDecl or FlagsDecl => 10,
                    UnionDecl => 23,
                    NamespaceDecl => 3,
                    _ => 13
                };

                symbols.Add(new
                {
                    name,
                    kind,
                    range = new
                    {
                        start = new { line = start.Item1, character = start.Item2 },
                        end = new { line = end.Item1, character = end.Item2 }
                    },
                    selectionRange = new
                    {
                        start = new { line = start.Item1, character = start.Item2 },
                        end = new { line = start.Item1, character = start.Item2 + name.Length }
                    }
                });
            }
        }
        catch
        {
        }

        return symbols;
    }

    private static int LineColToOffset(string source, int line, int character)
    {
        var currentLine = 0;
        var lineStart = 0;

        for (var i = 0; i < source.Length; i++)
        {
            if (currentLine == line)
            {
                return Math.Min(lineStart + character, source.Length);
            }

            if (source[i] == '\n')
            {
                currentLine++;
                lineStart = i + 1;
            }
        }

        if (currentLine == line)
        {
            return Math.Min(lineStart + character, source.Length);
        }

        return source.Length;
    }

    private static object OffsetToLineColRange(string source, int offset)
    {
        var pos = OffsetToLineCol(source, offset);
        return new { line = pos.Item1, character = pos.Item2 };
    }

    private static Tuple<int, int> FindLineColumn(string[] lines, int offset)
    {
        var remaining = offset;
        for (var i = 0; i < lines.Length; i++)
        {
            var lineLen = lines[i].Length + (i < lines.Length - 1 ? 1 : 0);
            if (remaining < lineLen)
            {
                return Tuple.Create(i, remaining);
            }

            remaining -= lineLen;
        }

        return Tuple.Create(lines.Length - 1, Math.Max(0, remaining));
    }

    private static int MapHighlightKindToTokenType(HighlightKind kind)
    {
        return kind switch
        {
            HighlightKind.Keyword => 0,
            HighlightKind.Number => 1,
            HighlightKind.String => 2,
            HighlightKind.Comment => 3,
            HighlightKind.Operator => 4,
            HighlightKind.TypeName => 5,
            HighlightKind.Identifier => 6,
            HighlightKind.Attribute or HighlightKind.Delimiter => 10,
            _ => 6
        };
    }

    private static bool ContainsPosition(string source, TextSpan span, int line, int character)
    {
        var startPos = OffsetToLineCol(source, span.Start);
        var endPos = span.Length > 0
            ? OffsetToLineCol(source, span.Start + span.Length - 1)
            : startPos;

        return startPos.Item1 <= line && line <= endPos.Item1
            && (startPos.Item1 != endPos.Item1 || startPos.Item2 <= character && character <= endPos.Item2);
    }

    private static Tuple<int, int> OffsetToLineCol(string source, int offset)
    {
        var line = 0;
        var col = 0;
        for (var i = 0; i < Math.Min(offset, source.Length); i++)
        {
            if (source[i] == '\n')
            {
                line++;
                col = 0;
            }
            else
            {
                col++;
            }
        }

        return Tuple.Create(line, col);
    }

    private static string? UriToFilePath(string uri)
    {
        if (uri.StartsWith("file:///"))
        {
            return uri[8..].Replace('/', '\\');
        }

        if (uri.StartsWith("file://"))
        {
            return uri[7..].Replace('/', '\\');
        }

        return uri;
    }

    #endregion

    #region JSON-RPC 传输

    private static async ValueTask SendResponseAsync(JsonElement id, object? result)
    {
        var response = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id.ValueKind == JsonValueKind.Number ? id.GetInt64() : id.GetString(),
            ["result"] = result
        };

        await SendMessageAsync(response);
    }

    private static async ValueTask SendErrorAsync(JsonElement id, int code, string message)
    {
        var response = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id.ValueKind == JsonValueKind.Number ? id.GetInt64() : id.GetString(),
            ["error"] = new { code, message }
        };

        await SendMessageAsync(response);
    }

    private static async ValueTask SendNotificationAsync(string method, object @params)
    {
        var notification = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params
        };

        await SendMessageAsync(notification);
    }

    private static async ValueTask SendMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        var header = $"Content-Length: {bytes.Length}\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        var stdout = Console.OpenStandardOutput();
        await stdout.WriteAsync(headerBytes);
        await stdout.WriteAsync(bytes);
        await stdout.FlushAsync();
    }

    #endregion
}
