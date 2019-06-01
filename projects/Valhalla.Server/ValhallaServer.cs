using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Valhalla;
using Valhalla.Config;
using Valhalla.Server.Auth;
using Valhalla.Server.Storage;

namespace Valhalla.Server;

/// <summary>
/// 瓦尓哈拉服务端入口，基于 Kestrel 的 HTTP 服务器
/// </summary>
public class ValhallaServer
{
    private readonly ValhallaConfig _config;
    private readonly IStorage _storage;
    private WebApplication? _app;

    /// <summary>
    /// 创建瓦尓哈拉服务端
    /// </summary>
    /// <param name="config">服务端配置</param>
    public ValhallaServer(ValhallaConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _storage = CreateStorage(config);
    }

    /// <summary>
    /// 获取存储后端
    /// </summary>
    public IStorage Storage => _storage;

    /// <summary>
    /// 获取服务端配置
    /// </summary>
    public ValhallaConfig Config => _config;

    /// <summary>
    /// 启动服务器
    /// </summary>
    public async Task StartAsync(string[]? args = null)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS",
            $"http://0.0.0.0:{_config.Port}");

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

        var trustedKeys = LoadTrustedKeys();

        var app = builder.Build();

        // CORS 中间件
        if (_config.Cors.Origins.Count > 0)
        {
            app.UseCors(policy =>
            {
                policy.WithOrigins(_config.Cors.Origins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        }

        // 速率限制中间件
        app.UseMiddleware<RateLimitMiddleware>(100, 60);

        // 请求日志中间件
        app.Use(async (context, next) =>
        {
            var startTime = DateTime.UtcNow;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var requestId = Guid.NewGuid().ToString("N")[..8];

            context.Response.Headers.Append("X-Request-Id", requestId);
            context.Response.Headers.Append("X-Powered-By", "Valhalla");

            await next(context);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 500)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (statusCode >= 400)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine($"[{requestId}] {method} {path} → {statusCode} ({elapsed:F0}ms)");
            Console.ResetColor();
        });

        // 全局异常处理中间件
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json; charset=utf-8";
                var errorResponse = ValhallaErrorResponse.InternalError("服务器内部错误",
                    new Dictionary<string, string> { ["exception"] = ex.GetType().Name });
                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        });

        // Ed25519 认证中间件
        if (_config.PubkeyRequired && trustedKeys.Count > 0)
        {
            app.UseMiddleware<Ed25519AuthMiddleware>(trustedKeys);
        }

        // 健康检查
        app.MapGet("/health", () => Results.Json(new
        {
            status = "healthy",
            name = _config.Name,
            version = "0.1.0",
            storage = _config.Storage.ToString().ToLowerInvariant(),
            uptime = Environment.TickCount64 / 1000
        }));

        // 就绪检查（包含存储后端连通性）
        app.MapGet("/ready", async (IStorage storage) =>
        {
            try
            {
                await storage.ListAsync("", CancellationToken.None);
                return Results.Json(new { status = "ready" });
            }
            catch
            {
                return Results.Json(new { status = "not_ready" }, statusCode: 503);
            }
        });

        // 注册 API 路由
        app.MapApiRoutes(_storage);

        _app = app;

        PrintStartupBanner();
        await app.RunAsync();
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public async Task StopAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
        }
    }

    #region 私有方法

    /// <summary>
    /// 打印启动横幅
    /// </summary>
    private void PrintStartupBanner()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╔═══════════════════════════════════════╗");
        Console.WriteLine("  ║     ⚔  Valhalla Registry Server      ║");
        Console.WriteLine("  ╚═══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  名称：{_config.Name}");
        Console.WriteLine($"  端口：{_config.Port}");
        Console.WriteLine($"  存储：{_config.Storage} ({_config.StoragePath})");
        Console.WriteLine($"  认证：{(_config.PubkeyRequired ? "Ed25519 公钥认证" : "开放模式")}");
        Console.WriteLine($"  注册：{_config.Registration}");
        Console.WriteLine($"  CORS：{(_config.Cors.Origins.Count > 0 ? string.Join(", ", _config.Cors.Origins) : "未配置")}");
        Console.WriteLine();
        Console.WriteLine($"  端点：http://0.0.0.0:{_config.Port}/api/packages");
        Console.WriteLine($"  健康检查：http://0.0.0.0:{_config.Port}/health");
        Console.WriteLine($"  就绪检查：http://0.0.0.0:{_config.Port}/ready");
        Console.WriteLine();
        Console.WriteLine("  按 Ctrl+C 停止服务器");
        Console.WriteLine();
    }

    /// <summary>
    /// 根据配置创建存储后端
    /// </summary>
    private static IStorage CreateStorage(ValhallaConfig config)
    {
        return config.Storage switch
        {
            StorageBackend.S3 => new S3Storage(
                config.StoragePath,
                config.S3?.Endpoint ?? string.Empty,
                config.S3?.Region ?? "us-east-1"),
            _ => new LocalStorage(config.StoragePath)
        };
    }

    /// <summary>
    /// 加载可信 Ed25519 公钥
    /// </summary>
    private Dictionary<string, byte[]> LoadTrustedKeys()
    {
        var keys = new Dictionary<string, byte[]>();
        string keyDir = Path.Combine(_config.StoragePath, "keys");

        if (!Directory.Exists(keyDir))
        {
            return keys;
        }

        foreach (string file in Directory.GetFiles(keyDir, "*.pub"))
        {
            try
            {
                string fingerprint = Path.GetFileNameWithoutExtension(file);
                byte[] keyBytes = File.ReadAllBytes(file);
                keys[fingerprint] = keyBytes;
            }
            catch
            {
                // 跳过无效的密钥文件
            }
        }

        return keys;
    }

    #endregion

    #region 静态入口

    /// <summary>
    /// 从命令行参数启动服务端
    /// </summary>
    public static async Task Main(string[] args)
    {
        string configPath = "./valhalla.von";

        // 解析 --config 和 --port 参数
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length)
            {
                configPath = args[++i];
            }
        }

        ValhallaConfig config;
        if (File.Exists(configPath))
        {
            config = await ValhallaConfig.LoadAsync(configPath);
        }
        else
        {
            Console.WriteLine($"配置文件 {configPath} 不存在，使用默认配置");
            config = ValhallaConfig.CreateDefault();
        }

        // --port 覆盖配置中的端口
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length
                                    && int.TryParse(args[++i], out int port))
            {
                config.Port = port;
            }
        }

        var server = new ValhallaServer(config);
        await server.StartAsync(args);
    }

    #endregion
}