using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legion.Registry;

/// <summary>
/// 注册表重试配置
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    /// 最大重试次数，默认 3
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 初始延迟，默认 500ms
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// 延迟倍数，默认 2（指数退避）
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// 最大延迟上限，默认 10s
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 判断响应是否应重试，默认仅对 429 和 5xx 重试
    /// </summary>
    public Func<HttpResponseMessage, bool>? ShouldRetry { get; init; }

    /// <summary>
    /// 默认重试配置
    /// </summary>
    public static RetryConfig Default { get; } = new();
}

/// <summary>
/// 注册表抽象基类，提供 HTTP 客户端管理、重试策略、日志支持和通用 JSON 序列化
/// </summary>
public abstract class RegistryBase : IRegistry
{
    /// <summary>
    /// JSON 序列化选项，使用 CamelCase 命名策略和不区分大小写的属性匹配
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private string _endpoint;

    /// <summary>
    /// 创建注册表适配器实例
    /// </summary>
    /// <param name="name">注册表名称标识</param>
    /// <param name="defaultEndpoint">默认 API 端点地址</param>
    /// <param name="httpClient">HTTP 客户端实例，若不提供则自动创建</param>
    /// <param name="logger">日志记录器，若不提供则使用空记录器</param>
    protected RegistryBase(string name, string defaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
    {
        Name = name;
        _endpoint = defaultEndpoint.TrimEnd('/');
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Endpoint
    {
        get => _endpoint;
        set => _endpoint = value.TrimEnd('/');
    }

    /// <summary>
    /// 获取重试配置，子类可重写以自定义策略
    /// </summary>
    protected virtual RetryConfig RetryConfig => RetryConfig.Default;

    /// <inheritdoc />
    public abstract Task<Package> GetPackageAsync(string packageName, string version);

    /// <inheritdoc />
    public abstract Task<List<Package>> SearchPackagesAsync(string query);

    /// <inheritdoc />
    public abstract Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData);

    /// <inheritdoc />
    public abstract Task<string> DownloadPackageAsync(Package package, string targetDirectory);

    /// <inheritdoc />
    public abstract Task<List<string>> GetPackageVersionsAsync(string packageName);

    /// <inheritdoc />
    public abstract Task<TokenVerifyResult> VerifyTokenAsync(string token);

    /// <summary>
    /// 获取内部 HTTP 客户端实例
    /// </summary>
    protected HttpClient HttpClient => _httpClient;

    /// <summary>
    /// 获取日志记录器
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// 构建完整的 API URL
    /// </summary>
    /// <param name="path">相对于端点的路径</param>
    protected string BuildUrl(string path)
    {
        return $"{_endpoint}/{path.TrimStart('/')}";
    }

    /// <summary>
    /// 发送 GET 请求并将响应反序列化为指定类型，支持重试和取消
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="url">请求 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRetryAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

        if (result is null)
        {
            _logger.LogError("反序列化 JSON 响应失败: {Url}", url);
            throw new RegistryException($"反序列化响应失败: {url}");
        }

        return result;
    }

    /// <summary>
    /// 发送 GET 请求并返回原始响应消息，支持重试
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> GetHttpResponseAsync(string url, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
    }

    /// <summary>
    /// 发送带 Bearer 认证头的 GET 请求，支持重试
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="token">认证令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> GetAuthenticatedAsync(string url, string token, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            return request;
        }, cancellationToken);
    }

    /// <summary>
    /// 发送带自定义认证头和 HTTP 方法的请求，支持重试
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="url">请求 URL</param>
    /// <param name="token">认证令牌</param>
    /// <param name="content">请求体内容，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            if (content is not null)
            {
                request.Content = content;
            }

            return request;
        }, cancellationToken);
    }

    /// <summary>
    /// 发送自定义请求并获取原始响应，支持重试和取消
    /// </summary>
    /// <param name="requestFactory">请求工厂函数</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var config = RetryConfig;
        var lastException = (Exception?)null;

        for (var attempt = 0; attempt <= config.MaxRetries; attempt++)
        {
            try
            {
                using var request = requestFactory();
                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                var shouldRetry = config.ShouldRetry?.Invoke(response)
                                  ?? IsDefaultRetryable(response);

                if (!shouldRetry || attempt >= config.MaxRetries)
                {
                    response.EnsureSuccessStatusCode();
                }

                _logger.LogWarning(
                    "请求 {Url} 返回 {StatusCode}，第 {Attempt}/{MaxRetries} 次重试",
                    request.RequestUri, (int)response.StatusCode, attempt + 1, config.MaxRetries);

                var delay = TimeSpan.FromMilliseconds(
                    config.InitialDelay.TotalMilliseconds * Math.Pow(config.BackoffMultiplier, attempt));

                if (delay > config.MaxDelay)
                {
                    delay = config.MaxDelay;
                }

                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                if (attempt >= config.MaxRetries)
                {
                    _logger.LogError(ex, "请求已达最大重试次数 {MaxRetries}", config.MaxRetries);
                    throw;
                }

                _logger.LogWarning(ex, "请求异常，第 {Attempt}/{MaxRetries} 次重试", attempt + 1, config.MaxRetries);

                var delay = TimeSpan.FromMilliseconds(
                    config.InitialDelay.TotalMilliseconds * Math.Pow(config.BackoffMultiplier, attempt));

                if (delay > config.MaxDelay)
                {
                    delay = config.MaxDelay;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new RegistryException("请求失败，已达最大重试次数");
    }

    /// <summary>
    /// 释放 HTTP 客户端资源
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Legion.Registry/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private static bool IsDefaultRetryable(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        return statusCode == 429 || statusCode is >= 500 and < 600;
    }

    /// <summary>
    ///     验证下载文件的完整性（SRI 格式：sha256-xxx 或 sha512-xxx）
    ///     如果 Package.DistIntegrity 为空则跳过校验
    /// </summary>
    /// <param name="filePath">待校验的文件路径</param>
    /// <param name="expectedIntegrity">期望的完整性值（SRI 格式），为空则跳过</param>
    /// <exception cref="RegistryException">校验失败时抛出</exception>
    protected static void VerifyDownloadIntegrity(string filePath, string? expectedIntegrity)
    {
        if (string.IsNullOrEmpty(expectedIntegrity))
            return;

        if (!File.Exists(filePath))
            throw new RegistryException($"完整性校验失败：文件不存在 {filePath}");

        var separatorIndex = expectedIntegrity.IndexOf('-');
        if (separatorIndex < 0)
            throw new RegistryException($"完整性校验失败：无效的 SRI 格式 '{expectedIntegrity}'");

        var algorithm = expectedIntegrity[..separatorIndex];
        var expectedHash = expectedIntegrity[(separatorIndex + 1)..];

        byte[] actualHashBytes;
        using (var stream = File.OpenRead(filePath))
        {
            actualHashBytes = algorithm switch
            {
                "sha256" => SHA256.HashData(stream),
                "sha384" => SHA384.HashData(stream),
                "sha512" => SHA512.HashData(stream),
                _ => throw new RegistryException($"完整性校验失败：不支持的哈希算法 '{algorithm}'")
            };
        }

        var actualHash = Convert.ToBase64String(actualHashBytes);

        if (actualHash != expectedHash)
        {
            throw new RegistryException(
                $"完整性校验失败：{algorithm} 不匹配" +
                $"（期望 {expectedHash[..16]}...，实际 {actualHash[..16]}...）");
        }

        _ = algorithm;
    }

    /// <summary>
    ///     计算文件的 SRI 完整性值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="algorithm">哈希算法，默认 sha512</param>
    /// <returns>SRI 格式的完整性值（如 sha512-xxxx）</returns>
    public static string ComputeFileIntegrity(string filePath, string algorithm = "sha512")
    {
        if (!File.Exists(filePath))
            throw new RegistryException($"计算完整性失败：文件不存在 {filePath}");

        byte[] hashBytes;
        using (var stream = File.OpenRead(filePath))
        {
            hashBytes = algorithm switch
            {
                "sha256" => SHA256.HashData(stream),
                "sha384" => SHA384.HashData(stream),
                "sha512" => SHA512.HashData(stream),
                _ => throw new RegistryException($"不支持的哈希算法 '{algorithm}'")
            };
        }

        return $"{algorithm}-{Convert.ToBase64String(hashBytes)}";
    }
}