using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Asgard.CLI.Compiler;

/* VoaFetchService / VoaFetchCache / FetchResponse 等类型均在本命名空间内 */

namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA 开发服务器，提供 HTTP 静态文件服务和 WebSocket 热重载
/// </summary>
public sealed class VoaDevServer : IDisposable
{
    private readonly string _projectDir;
    private readonly string _host;
    private readonly int _port;
    private readonly VoaProjectConfig _config;
    private readonly HttpListener _httpListener;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly AwslRenderer _renderer;
    private readonly AwslSsrRenderer _ssrRenderer;
    private readonly VoaFileWatcher? _fileWatcher;
    private readonly VoaErrorOverlay _errorOverlay;
    private readonly VoaBridge _bridge;
    private readonly VoaCompiler _compiler;
    private readonly VoaFetchService _fetchService;
    private readonly Dictionary<string, string> _routeTable = new();  /* path → pageFilePath */
    private readonly Dictionary<string, string> _layoutTable = new(); /* pageFilePath → layoutFilePath */
    private readonly Dictionary<string, DateTime> _ssgBuildTimes = new(); /* path → buildTime, ISR 过期检测 */
    private readonly Dictionary<string, int> _isrTtl = new();           /* path → revalidate seconds */
    private VoaBuildResult? _lastBuildResult;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public VoaDevServer(string projectDir, string host, int port, VoaProjectConfig config)
    {
        _projectDir = projectDir;
        _host = host;
        _port = port;
        _config = config;
        _renderer = new AwslRenderer();
        _ssrRenderer = new AwslSsrRenderer();
        _errorOverlay = new VoaErrorOverlay();
        _bridge = new VoaBridge();
        _fetchService = new VoaFetchService();

        _bridge.RegisterHandler("voa.revalidateTag", (param) =>
        {
            var tag = param.TryGetValue("tag", out var t) ? t?.ToString() : null;
            if (string.IsNullOrEmpty(tag))
            {
                return Task.FromResult<BridgeResult>(new BridgeResult { Error = "tag 参数缺失" });
            }

            var count = _fetchService.Cache.InvalidateByTag(tag);
            return Task.FromResult(new BridgeResult
            {
                Data = new Dictionary<string, object?> { ["invalidated"] = count }
            });
        });

        _bridge.RegisterHandler("voa.revalidatePath", (param) =>
        {
            var path = param.TryGetValue("path", out var p) ? p?.ToString() : null;
            if (string.IsNullOrEmpty(path))
            {
                return Task.FromResult<BridgeResult>(new BridgeResult { Error = "path 参数缺失" });
            }

            var count = _fetchService.Cache.InvalidateByPath(path);
            return Task.FromResult(new BridgeResult
            {
                Data = new Dictionary<string, object?> { ["invalidated"] = count }
            });
        });

        _bridge.RegisterHandler("voa.cacheStats", (_) =>
        {
            var stats = _fetchService.Cache.GetStats();
            return Task.FromResult(new BridgeResult
            {
                Data = new Dictionary<string, object?>
                {
                    ["entries"] = stats.Entries,
                    ["hits"] = stats.Hits,
                    ["misses"] = stats.Misses,
                    ["evictions"] = stats.Evictions
                }
            });
        });

        _bridge.RegisterHandler("voa.clearCache", (_) =>
        {
            _fetchService.Cache.Clear();
            _fetchService.ClearDeduplicationMap();
            return Task.FromResult(new BridgeResult { Data = new Dictionary<string, object?> { ["cleared"] = true } });
        });
        _compiler = new VoaCompiler();

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://{host}:{port}/");

        if (config.HotReload.Enabled)
        {
            _fileWatcher = new VoaFileWatcher(
                projectDir,
                config.HotReload.Watch,
                config.HotReload.Ignore,
                config.HotReload.Debounce
            );
            _fileWatcher.OnFileChanged += HandleFileChanged;
            _fileWatcher.OnError += HandleCompileError;
        }
    }

    public event Action<string, string>? OnLog;

    /// <summary>HMR 延迟测量事件，每次热重载完成时触发</summary>
    public event Action<HmrTiming>? OnHmrTiming;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _httpListener.Start();

        Log("info", $"VOA 开发服务器已启动 → http://{_host}:{_port}/");
        Log("info", $"项目目录：{_projectDir}");
        Log("info", $"热重载：{(_config.HotReload.Enabled ? "开启" : "关闭")}");

        ScanRouteTable(_projectDir);

        if (_config.IsFrontend)
        {
            if (_config.Target == "wasm")
            {
                Log("info", "模式：前端开发（VOA → WASM → WebView）");
                RebuildWasm();
            }
            else
            {
                Log("info", "模式：前端开发（AWSL → HTML/CSS/JS）");
            }
        }
        else if (_config.IsBackend)
        {
            Log("info", "模式：后端开发（API 服务）");
        }

        _fileWatcher?.Start();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var context = await _httpListener.GetContextAsync();

                if (_cts.Token.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleRequestAsync(context, _cts.Token);
            }
        }
        catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Stop()
    {
        _fileWatcher?.Stop();
        _cts?.Cancel();

        if (_httpListener.IsListening)
        {
            _httpListener.Stop();
        }

        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch
            {
            }
        }

        _clients.Clear();
        Log("info", "VOA 开发服务器已停止");
    }

    #region HTTP Request Handling

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var method = context.Request.HttpMethod;

        try
        {
            if (context.Request.IsWebSocketRequest)
            {
                await HandleWebSocketAsync(context, ct);
                return;
            }

            Log("request", $"{method} {path}");

            switch (path)
            {
                case "/__voa_bridge":
                    await HandleBridgeRequestAsync(context, ct);
                    break;
                case "/__voa_hmr":
                    await HandleHmrConnectAsync(context, ct);
                    break;
                default:
                    await HandleStaticFileAsync(context, path, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log("error", $"请求处理失败：{ex.Message}");
            await SendErrorResponseAsync(context, 500, "内部服务器错误", ct);
        }
    }

    private async Task HandleStaticFileAsync(HttpListenerContext context, string path, CancellationToken ct)
    {
        var sourceDir = Path.Combine(_projectDir, "source");
        var assetsDir = Path.Combine(_projectDir, "assets");
        var distDir = Path.Combine(_projectDir, _config.Build.Output);

        /* 路由表匹配优先 */
        if (_routeTable.TryGetValue(path, out var pageFilePath) && File.Exists(pageFilePath))
        {
            await ServePageWithLayoutAsync(context, pageFilePath, ct);
            return;
        }

        /* 根路径特殊处理 */
        if ((path == "/" || path == "/index.html") && _routeTable.TryGetValue("/", out var indexFilePath))
        {
            await ServePageWithLayoutAsync(context, indexFilePath, ct);
            return;
        }

        string? filePath = null;

        if (path == "/" || path == "/index.html")
        {
            if (_config.Target == "wasm" && _lastBuildResult is { Success: true })
            {
                var htmlFile = _lastBuildResult.OutputFiles.Find(f => f.EndsWith(".html")) ?? "index.html";
                filePath = Path.Combine(_lastBuildResult.OutputDirectory, htmlFile);
                if (File.Exists(filePath))
                {
                    await ServeRegularFileAsync(context, filePath, ct);
                    return;
                }
            }

            filePath = FindIndexFile(sourceDir);
        }
        else if (_config.Target == "wasm" && _lastBuildResult is { Success: true })
        {
            var wasmDir = _lastBuildResult.OutputDirectory;
            var wasmPath = Path.Combine(wasmDir, path.TrimStart('/'));
            if (File.Exists(wasmPath))
            {
                await ServeRegularFileAsync(context, wasmPath, ct);
                return;
            }

            var relativePath = path.TrimStart('/');
            filePath = FindFile(sourceDir, assetsDir, distDir, relativePath);
        }
        else
        {
            var relativePath = path.TrimStart('/');
            filePath = FindFile(sourceDir, assetsDir, distDir, relativePath);
        }

        if (filePath is null)
        {
            await SendErrorResponseAsync(context, 404, "文件未找到", ct);
            return;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".awsl")
        {
            await ServeAwslFileAsync(context, filePath, ct);
        }
        else
        {
            await ServeRegularFileAsync(context, filePath, ct);
        }
    }

    private async Task ServeAwslFileAsync(HttpListenerContext context, string filePath, CancellationToken ct)
    {
        try
        {
            var source = await File.ReadAllTextAsync(filePath, ct);

            var query = context.Request.Url?.Query ?? "";
            var useSsr = query.Contains("ssr=1") || query.Contains("ssr=true");

            if (useSsr)
            {
                var ssrResult = _ssrRenderer.RenderSsr(source, filePath);
                var moduleName = Path.GetFileNameWithoutExtension(filePath);
                var ssrHtml = AwslSsrRenderer.GenerateSsrPage(
                    moduleName,
                    new[] { ssrResult },
                    _config.Target == "wasm" ? $"{moduleName}.wasm" : null
                );

                if (_config.HotReload.Enabled)
                {
                    ssrHtml = _errorOverlay.InjectHmrScript(ssrHtml, _host, _port);
                }

                var ssrBytes = Encoding.UTF8.GetBytes(ssrHtml);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = ssrBytes.Length;
                await context.Response.OutputStream.WriteAsync(ssrBytes, ct);
            }
            else
            {
                var result = _renderer.Render(source, filePath);

                var html = result.Html;

                if (_config.HotReload.Enabled)
                {
                    html = _errorOverlay.InjectHmrScript(html, _host, _port);
                }

                var bytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, ct);
            }
        }
        catch (Exception ex)
        {
            var errorHtml = _errorOverlay.RenderErrorPage(ex.Message, filePath);
            var bytes = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task ServeRegularFileAsync(HttpListenerContext context, string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            await SendErrorResponseAsync(context, 404, "文件未找到", ct);
            return;
        }

        var contentType = GetContentType(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);

        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    #endregion

    #region WebSocket HMR

    private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken ct)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        var ws = wsContext.WebSocket;
        var clientId = Guid.NewGuid().ToString("N")[..8];

        _clients[clientId] = ws;
        Log("hmr", $"客户端连接：{clientId}（共 {_clients.Count} 个）");

        try
        {
            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleHmrMessageAsync(clientId, message, ct);
                }
            }
        }
        catch (WebSocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            Log("hmr", $"客户端断开：{clientId}（剩余 {_clients.Count} 个）");

            try
            {
                ws.Dispose();
            }
            catch
            {
            }
        }
    }

    private async Task HandleHmrMessageAsync(string clientId, string message, CancellationToken ct)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<HmrMessage>(message);
            if (msg is null) return;

            switch (msg.Type)
            {
                case "ping":
                    await SendToClientAsync(clientId, new HmrMessage { Type = "pong" }, ct);
                    break;
                case "bridge_call":
                    var bridgeResult = await _bridge.HandleCallAsync(msg.Payload?.ToString() ?? "");
                    await SendToClientAsync(clientId, new HmrMessage
                    {
                        Type = "bridge_result",
                        Payload = bridgeResult
                    }, ct);
                    break;
            }
        }
        catch (JsonException)
        {
        }
    }

    private async Task BroadcastHmrUpdateAsync(string filePath, string updateType, CancellationToken ct)
    {
        if (_clients.IsEmpty) return;

        var message = new HmrMessage
        {
            Type = "update",
            Payload = new
            {
                path = filePath,
                updateType,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var deadClients = new List<string>();

        foreach (var kvp in _clients)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    await kvp.Value.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                }
                else
                {
                    deadClients.Add(kvp.Key);
                }
            }
            catch
            {
                deadClients.Add(kvp.Key);
            }
        }

        foreach (var id in deadClients)
        {
            _clients.TryRemove(id, out _);
        }
    }

    private async Task BroadcastErrorAsync(string error, string? filePath, CancellationToken ct)
    {
        if (_clients.IsEmpty) return;

        var message = new HmrMessage
        {
            Type = "error",
            Payload = new
            {
                error,
                filePath,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var kvp in _clients)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    await kvp.Value.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                }
            }
            catch
            {
            }
        }
    }

    private Task SendToClientAsync(string clientId, HmrMessage message, CancellationToken ct)
    {
        if (!_clients.TryGetValue(clientId, out var ws)) return Task.CompletedTask;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        return ws.State == WebSocketState.Open
            ? ws.SendAsync(segment, WebSocketMessageType.Text, true, ct)
            : Task.CompletedTask;
    }

    #endregion

    #region Bridge

    private async Task HandleBridgeRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.HttpMethod != "POST")
        {
            await SendErrorResponseAsync(context, 405, "方法不允许", ct);
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);

        var result = await _bridge.HandleCallAsync(body);
        var responseJson = JsonSerializer.Serialize(result);

        var bytes = Encoding.UTF8.GetBytes(responseJson);
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private async Task HandleHmrConnectAsync(HttpListenerContext context, CancellationToken ct)
    {
        var html = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>VOA HMR</title></head>
<body><h1>VOA HMR Endpoint</h1><p>WebSocket 热重载端点</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    #endregion

    #region File Change Handling

    private void HandleFileChanged(string filePath)
    {
        var relativePath = Path.GetRelativePath(_projectDir, filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var updateType = extension switch
        {
            ".awsl" => "component",
            ".v" => "module",
            ".css" => "style",
            _ => "asset"
        };

        var timing = new HmrTiming
        {
            FileChangedAt = DateTime.UtcNow,
            FilePath = relativePath
        };

        Log("hmr", $"文件变更：{relativePath}（{updateType}）");

        if (_config.Target == "wasm" && extension is ".v" or ".awsl")
        {
            timing = timing with { CompileStartAt = DateTime.UtcNow };
            RebuildWasm();
            timing = timing with { CompileEndAt = DateTime.UtcNow };

            Log("hmr_perf", $"HMR 编译耗时：{timing.CompileMs:F1}ms | 文件：{relativePath}");

            if (timing.CompileMs > 50)
            {
                Log("hmr_warn", $"⚠️ HMR 编译延迟 {timing.CompileMs:F1}ms 超过 50ms 目标");
            }
        }

        _ = BroadcastHmrUpdateAsync(relativePath, updateType, _cts?.Token ?? CancellationToken.None);

        timing = timing with { BroadcastAt = DateTime.UtcNow };
        OnHmrTiming?.Invoke(timing);

        Log("hmr_perf", $"HMR 总延迟：{timing.TotalMs:F1}ms（检测 {timing.DetectMs:F1}ms + 编译 {timing.CompileMs:F1}ms）");
    }

    private void HandleCompileError(string error, string? filePath)
    {
        Log("error", $"编译错误：{error}");
        _ = BroadcastErrorAsync(error, filePath, _cts?.Token ?? CancellationToken.None);
    }

    #endregion

    #region Utility

    /// <summary>
    ///     扫描 source/pages/ 目录，构建路径→页面文件和 Layout→页面 的映射
    /// </summary>
    private void ScanRouteTable(string projectDir)
    {
        var pagesDir = Path.Combine(projectDir, "source", "pages");
        if (!Directory.Exists(pagesDir))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(pagesDir, "*.awsl", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(pagesDir, file);
            var route = FilePathToRoute(relativePath);

            if (string.IsNullOrEmpty(route))
            {
                continue;
            }

            _routeTable[route] = file;
        }

        Log("info", $"路由扫描完成：{_routeTable.Count} 个页面");

        /* 为每个页面查找最近的 layout */
        foreach (var kvp in _routeTable)
        {
            var layout = FindLayoutForPage(pagesDir, kvp.Value);
            if (layout != null)
            {
                _layoutTable[kvp.Value] = layout;
            }
        }

        Log("info", $"Layout 映射完成：{_layoutTable.Count} 个页面有 Layout");
    }

    /// <summary>
    ///     文件相对路径转路由路径
    /// </summary>
    private static string FilePathToRoute(string relativePath)
    {
        var route = relativePath.Replace('\\', '/');
        route = Path.ChangeExtension(route, null);

        if (string.IsNullOrEmpty(route))
        {
            return "/";
        }

        /* 处理 index */
        if (route == "index")
        {
            return "/";
        }

        if (route.EndsWith("/index"))
        {
            route = route[..^6];
        }

        /* 处理动态参数 [xxx] → :xxx */
        var segments = route.Split('/');
        for (var k = 0; k < segments.Length; k++)
        {
            var seg = segments[k];
            if (seg.StartsWith('[') && seg.EndsWith(']'))
            {
                var inner = seg[1..^1];
                segments[k] = ":" + inner;
            }
        }

        route = "/" + string.Join("/", segments);
        return route.TrimEnd('/');
    }

    /// <summary>
    ///     为页面文件查找最近的 layout 文件（向上遍历目录）
    /// </summary>
    private static string? FindLayoutForPage(string pagesDir, string pageFilePath)
    {
        var pageDir = Path.GetDirectoryName(pageFilePath);
        if (pageDir is null) return null;

        /* 从页面所在目录向上查找 layout.awsl */
        while (pageDir != null && pageDir.StartsWith(pagesDir))
        {
            var layoutPath = Path.Combine(pageDir, "layout.awsl");
            if (File.Exists(layoutPath))
            {
                return layoutPath;
            }

            if (pageDir == pagesDir)
            {
                break;
            }

            pageDir = Path.GetDirectoryName(pageDir);
        }

        return null;
    }

    /// <summary>
    ///     渲染页面并包裹 Layout（根据 renderMode 选择 SSR/CSR/ISR 路径）
    /// </summary>
    private async Task ServePageWithLayoutAsync(HttpListenerContext context, string pageFilePath, CancellationToken ct)
    {
        var requestPath = context.Request.Url?.AbsolutePath ?? "/";
        var renderMode = _config.GetRenderMode(requestPath);
        var routeModeConfig = _config.Routes?.Find(r => _config.GetRenderMode(requestPath) == r.Mode);

        try
        {
            var pageSource = await File.ReadAllTextAsync(pageFilePath, ct);
            var effectiveSource = pageSource;

            if (_layoutTable.TryGetValue(pageFilePath, out var layoutFilePath) && File.Exists(layoutFilePath))
            {
                var layoutSource = await File.ReadAllTextAsync(layoutFilePath, ct);
                effectiveSource = WrapWithLayout(layoutSource, pageSource);
            }

            string html;
            var contentType = "text/html; charset=utf-8";

            switch (renderMode)
            {
                case "ssr":
                    /* 请求级 SSR：构造请求数据字典传入渲染器 */
                    var requestData = new Dictionary<string, object>
                    {
                        ["path"] = requestPath,
                        ["query"] = context.Request.Url?.Query ?? "",
                        ["params"] = ExtractRouteParams(requestPath, pageFilePath)
                    };
                    var ssrResult = _ssrRenderer.RenderSsr(effectiveSource, pageFilePath, requestData);
                    var moduleName = Path.GetFileNameWithoutExtension(pageFilePath);
                    html = AwslSsrRenderer.GenerateSsrPage(
                        moduleName,
                        new[] { ssrResult },
                        _config.Target == "wasm" ? $"{moduleName}.wasm" : null
                    );
                    break;

                case "isr":
                    /* ISR：检查是否过期，过期则后台重建 */
                    var ttl = routeModeConfig?.Revalidate ?? 60;
                    _isrTtl[requestPath] = ttl;

                    if (_ssgBuildTimes.TryGetValue(requestPath, out var buildTime))
                    {
                        var age = (DateTime.UtcNow - buildTime).TotalSeconds;
                        if (age > ttl)
                        {
                            /* 后台异步重建 */
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var result = _renderer.Render(effectiveSource, pageFilePath);
                                    _ssgBuildTimes[requestPath] = DateTime.UtcNow;
                                    Log("info", $"ISR 后台重建完成：{requestPath}");
                                }
                                catch (Exception ex)
                                {
                                    Log("warn", $"ISR 重建失败：{requestPath} — {ex.Message}");
                                }
                            }, CancellationToken.None);
                        }

                        /* 注入 Cache-Control 头：stale-while-revalidate */
                        context.Response.Headers.Add("Cache-Control", $"public, s-maxage={ttl}, stale-while-revalidate");
                    }
                    else
                    {
                        _ssgBuildTimes[requestPath] = DateTime.UtcNow;
                    }

                    /* 首次请求走 CSR 渲染并缓存构建时间 */
                    var isrRender = _renderer.Render(effectiveSource, pageFilePath);
                    html = isrRender.Html;
                    break;

                default: /* csr */
                    var csrResult = _renderer.Render(effectiveSource, pageFilePath);
                    html = csrResult.Html;
                    break;
            }

            if (_config.HotReload.Enabled)
            {
                html = _errorOverlay.InjectHmrScript(html, _host, _port);
            }

            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        catch (Exception ex)
        {
            var errorHtml = _errorOverlay.RenderErrorPage(ex.Message, pageFilePath);
            var bytes = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        finally
        {
            context.Response.Close();
        }
    }

    /// <summary>
    ///     将页面内容注入到 Layout 的 slot 占位符中
    /// </summary>
    private static string WrapWithLayout(string layoutSource, string pageSource)
    {
        /* 将 slot 节点替换为页面 widget 内容 */
        /* 提取页面 widget 标签内容（不含 widget 标签本身） */
        var pageBody = ExtractWidgetBody(pageSource);

        /* 替换 layout 中的 slot */
        return layoutSource.Replace("<slot />", pageBody)
            .Replace("<slot/>", pageBody)
            .Replace("<slot></slot>", pageBody);
    }

    /// <summary>
    ///     提取 widget 标签内的主体内容（去除 widget 标签）
    /// </summary>
    private static string ExtractWidgetBody(string source)
    {
        var widgetStart = source.IndexOf("<widget>", StringComparison.Ordinal);
        if (widgetStart < 0) return source;

        /* 找到 widget props 属性的结束 */
        var afterStart = source.IndexOf('>', widgetStart);
        if (afterStart < 0) return source;

        var widgetEnd = source.IndexOf("</widget>", afterStart, StringComparison.Ordinal);
        if (widgetEnd < 0) return source;

        var body = source[(afterStart + 1)..widgetEnd];
        return body.Trim();
    }

    /// <summary>
    ///     从请求路径中提取动态路由参数（如 /blog/hello → {slug: "hello"}）
    /// </summary>
    private static Dictionary<string, object> ExtractRouteParams(string requestPath, string pageFilePath)
    {
        var result = new Dictionary<string, object>();

        var pagesDir = Path.GetDirectoryName(pageFilePath);
        if (pagesDir is null) return result;

        /* 将文件路径转换为路由模式（file-router.v 的逆操作） */
        var fileName = Path.GetFileNameWithoutExtension(pageFilePath);
        var routeSegments = requestPath.Trim('/').Split('/');
        var patternSegments = fileName.Split('_'); /* 用 _ 模拟 [id] pattern */

        for (var i = 0; i < routeSegments.Length && i < patternSegments.Length; i++)
        {
            var seg = routeSegments[i];
            var pat = patternSegments[i];

            if (pat.StartsWith('[') && pat.EndsWith(']'))
            {
                var paramName = pat[1..^1];
                result[paramName] = seg;
            }
        }

        /* 也存入原始路径段列表供 catch-all 使用 */
        result["_segments"] = routeSegments;

        return result;
    }

    private void RebuildWasm()
    {
        var outputDir = Path.Combine(_projectDir, _config.Build.Output);
        var result = _compiler.Build(_projectDir, outputDir, "wasm", false);

        if (result.Success)
        {
            _lastBuildResult = result;
            Log("info", $"WASM 重新编译完成（{result.OutputFiles.Count} 个产出文件）");
        }
        else
        {
            Log("error", $"WASM 编译失败：{result.Error}");
        }
    }

    private string? FindIndexFile(string sourceDir)
    {
        var candidates = new[] { "index.awsl", "index.html", "app.awsl", "app.html" };

        foreach (var candidate in candidates)
        {
            var path = Path.Combine(sourceDir, candidate);
            if (File.Exists(path)) return path;
        }

        var pagesDir = Path.Combine(sourceDir, "pages");
        if (Directory.Exists(pagesDir))
        {
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(pagesDir, candidate);
                if (File.Exists(path)) return path;
            }
        }

        return null;
    }

    private static string? FindFile(string sourceDir, string assetsDir, string distDir, string relativePath)
    {
        var paths = new[]
        {
            Path.Combine(sourceDir, relativePath),
            Path.Combine(assetsDir, relativePath),
            Path.Combine(distDir, relativePath)
        };

        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }

        var awslPath = Path.Combine(sourceDir, Path.ChangeExtension(relativePath, ".awsl"));
        if (File.Exists(awslPath)) return awslPath;

        return null;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".mjs" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".wasm" => "application/wasm",
            ".wat" => "text/plain; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            _ => "application/octet-stream"
        };
    }

    private async Task SendErrorResponseAsync(HttpListenerContext context, int statusCode, string message,
        CancellationToken ct)
    {
        var errorHtml = _errorOverlay.RenderErrorPage(message, null, statusCode);
        var bytes = Encoding.UTF8.GetBytes(errorHtml);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private void Log(string category, string message)
    {
        OnLog?.Invoke(category, message);
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _fileWatcher?.Dispose();
        _httpListener.Close();
    }
}