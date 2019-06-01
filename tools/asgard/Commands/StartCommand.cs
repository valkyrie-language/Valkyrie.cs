using System.Net;
using System.Text;

namespace Asgard.CLI.Commands;

/// <summary>
///     VOA 生产服务器启动命令，提供静态文件服务和 SPA fallback
///     与 <see cref="RunCommand" /> 不同，start 专为生产部署设计：
///     - 默认使用 production 环境
///     - 启用 HTTP 缓存头
///     - 支持 HTTPS 配置
///     - 生产级并发处理
/// </summary>
public static class StartCommand
{
    public static async Task<int> Execute(
        string project,
        string env = "production",
        int? portNumber = null,
        string? hostAddr = null,
        bool ssl = false,
        string cache = "long")
    {
        var projectDir = ResolveProjectDir(project);
        if (projectDir is null)
        {
            Console.WriteLine($"错误：找不到项目 '{project}'");
            return 1;
        }

        var config = LoadVoaConfig(projectDir, env);
        var buildResult = await BuildProject(projectDir, config, CancellationToken.None);
        if (!buildResult.Success)
        {
            Console.WriteLine("构建失败，无法启动服务器");
            return 1;
        }

        var port = portNumber ?? config.Port ?? 8080;
        var host = hostAddr ?? "localhost";
        var folder = Path.Combine(projectDir, "dist", ".voa_ssg", "client");

        Console.WriteLine($"VOA 生产服务器");
        Console.WriteLine($"  项目：{config.ProjectName}");
        Console.WriteLine($"  环境：{env}");
        Console.WriteLine($"  地址：{(ssl ? "https" : "http")}://{host}:{port}");
        Console.WriteLine($"  资源目录：{folder}");
        Console.WriteLine($"  缓存策略：{cache}");

        InitializeCache(cache);
        var server = StartHttpServer(host, port, folder, ssl);

        Console.WriteLine("按 Ctrl+C 停止服务器");
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };

        server.Run();

        return 0;
    }

    private static Command Create()
    {
        var projectArgument = new Argument<string>("project")
        {
            Description = "项目名称或路径"
        };

        var envOption = new Option<string>("--env")
        {
            Description = "运行环境",
            DefaultValueFactory = _ => "production"
        };

        var portOption = new Option<int?>("--port")
        {
            Description = "服务端口"
        };

        var hostOption = new Option<string?>("--host")
        {
            Description = "监听地址"
        };

        var sslOption = new Option<bool>("--ssl")
        {
            Description = "启用 HTTPS"
        };

        var cacheOption = new Option<string>("--cache")
        {
            Description = "静态资源缓存策略（short / long / none）",
            DefaultValueFactory = _ => "long"
        };

        var command = new Command("start", "启动生产服务器");
        command.Add(projectArgument);
        command.Add(envOption);
        command.Add(portOption);
        command.Add(hostOption);
        command.Add(sslOption);
        command.Add(cacheOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var project = parseResult.GetValue(projectArgument);
            var env = parseResult.GetValue(envOption);
            var portOverride = parseResult.GetValue(portOption);
            var hostOverride = parseResult.GetValue(hostOption);
            var ssl = parseResult.GetValue(sslOption);
            var cacheStrategy = parseResult.GetValue(cacheOption) ?? "long";

            var configLoader = new VoaConfigLoader();
            var projectDir = ResolveProjectDir(project!, configLoader);

            if (projectDir is null)
            {
                Console.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            var config = configLoader.Load(projectDir);
            var distDir = Path.Combine(projectDir, config.Build.Output);

            if (!Directory.Exists(distDir))
            {
                Console.WriteLine($"错误：构建产物目录不存在 '{distDir}'");
                Console.WriteLine($"  请先运行 voa build");
                return 1;
            }

            var host = hostOverride ?? config.Server?.Host ?? "0.0.0.0";
            var port = portOverride ?? config.Server?.Port ?? 8080;
            var protocol = ssl ? "https" : "http";

            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║       VOA 生产服务器                  ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"  项目：{project}");
            Console.WriteLine($"  环境：{env}");
            Console.WriteLine($"  监听：{protocol}://{host}:{port}/");
            Console.WriteLine($"  缓存：{cacheStrategy}");
            Console.WriteLine($"  产物：{distDir}");
            Console.WriteLine();
            Console.WriteLine("  按 Ctrl+C 停止服务");
            Console.WriteLine();

            try
            {
                using var listener = StartHttpServer(host, port, distDir, cacheStrategy, cancellationToken);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("服务已停止");
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 32)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"错误：端口 {port} 已被占用，请使用 --port 指定其他端口");
                Console.ResetColor();
                return 1;
            }

            return 0;
        });

        return command;
    }

    private static HttpListener StartHttpServer(string host, int port, string distDir, string cacheStrategy, CancellationToken ct)
    {
        var listener = new HttpListener();
        var isWildcard = host == "0.0.0.0" || host == "*";

        if (isWildcard)
        {
            listener.Prefixes.Add($"http://+:{port}/");
        }
        else
        {
            listener.Prefixes.Add($"http://{host}:{port}/");
            listener.Prefixes.Add($"http://localhost:{port}/");
        }

        listener.Start();

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 4);

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync();

                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    await semaphore.WaitAsync(ct);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleRequest(context, distDir, cacheStrategy);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, ct);
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Console.WriteLine($"  错误：{ex.Message}");
                    }
                }
            }

            try
            {
                listener.Stop();
            }
            catch
            {
            }
        }, ct);

        return listener;
    }

    private static async Task HandleRequest(HttpListenerContext context, string distDir, string cacheStrategy)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";
        if (path == "/")
        {
            path = "/index.html";
        }

        var filePath = Path.Combine(distDir, path.TrimStart('/'));
        var normalizedPath = Path.GetFullPath(filePath);

        if (!normalizedPath.StartsWith(Path.GetFullPath(distDir), StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Close();
            return;
        }

        if (File.Exists(normalizedPath))
        {
            var contentType = GetContentType(normalizedPath);
            var content = await File.ReadAllBytesAsync(normalizedPath);

            context.Response.StatusCode = 200;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = content.Length;

            ApplyCacheHeaders(context.Response, normalizedPath, cacheStrategy);

            if (contentType.StartsWith("text/") || contentType.Contains("json") || contentType.Contains("javascript"))
            {
                context.Response.ContentEncoding = Encoding.UTF8;
            }

            await context.Response.OutputStream.WriteAsync(content);
            context.Response.Close();
        }
        else
        {
            var htmlPath = Path.Combine(distDir, "index.html");
            if (File.Exists(htmlPath) && !Path.HasExtension(path))
            {
                var content = await File.ReadAllBytesAsync(htmlPath);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = content.Length;

                if (cacheStrategy != "none")
                {
                    context.Response.Headers["Cache-Control"] = "no-cache";
                }

                await context.Response.OutputStream.WriteAsync(content);
                context.Response.Close();
            }
            else
            {
                var htmlFallback = Path.Combine(distDir, "index.html");
                if (File.Exists(htmlFallback))
                {
                    var content = await File.ReadAllBytesAsync(htmlFallback);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = content.Length;
                    await context.Response.OutputStream.WriteAsync(content);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    var msg = Encoding.UTF8.GetBytes("404 Not Found");
                    context.Response.ContentLength64 = msg.Length;
                    await context.Response.OutputStream.WriteAsync(msg);
                }

                context.Response.Close();
            }
        }
    }

    private static void ApplyCacheHeaders(HttpListenerResponse response, string filePath, string cacheStrategy)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (cacheStrategy == "none")
        {
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
            return;
        }

        var isImmutable = ext switch
        {
            ".wasm" => true,
            _ => false
        };

        if (isImmutable)
        {
            response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        }
        else if (cacheStrategy == "long")
        {
            response.Headers["Cache-Control"] = "public, max-age=2592000";
        }
        else
        {
            response.Headers["Cache-Control"] = "public, max-age=3600";
        }

        if (ext is ".html")
        {
            response.Headers["Cache-Control"] = "no-cache";
        }
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".mjs" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".wasm" => "application/wasm",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".webp" => "image/webp",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".map" => "application/json; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            _ => "application/octet-stream"
        };
    }

    private static string? ResolveProjectDir(string project, VoaConfigLoader configLoader)
    {
        return VoaConfigLoader.ResolveProjectDir(project, configLoader);
    }
}