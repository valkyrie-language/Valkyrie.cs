using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Valhalla;
using Valhalla.Audit;

namespace Valhalla.Client;

/// <summary>
/// 瓦尓哈拉 HTTP 客户端，封装所有 API 调用
/// </summary>
public class ValhallaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private readonly int _maxRetries;

    /// <summary>
    /// 重试基础延迟毫秒数
    /// </summary>
    private readonly int _retryBaseDelayMs;

    /// <summary>
    /// 创建瓦尓哈拉客户端
    /// </summary>
    /// <param name="baseUrl">注册表基础 URL，如 https://valhalla.example.com</param>
    /// <param name="httpClient">可选的 HttpClient，若未提供则创建新实例</param>
    /// <param name="maxRetries">最大重试次数（默认 3）</param>
    /// <param name="retryBaseDelayMs">重试基础延迟毫秒数（默认 1000，使用指数退避）</param>
    public ValhallaClient(string baseUrl, HttpClient? httpClient = null, int maxRetries = 3, int retryBaseDelayMs = 1000)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
        _maxRetries = maxRetries;
        _retryBaseDelayMs = retryBaseDelayMs;
    }

    /// <summary>
    /// 使用重试策略执行异步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    internal async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch (ValhallaApiException ex) when (ex.StatusCode >= 500)
            {
                lastException = ex;
            }
            catch (HttpRequestException)
            {
                lastException = null;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                lastException = null;
            }

            if (attempt < _maxRetries)
            {
                int delayMs = _retryBaseDelayMs * (1 << attempt);
                await Task.Delay(delayMs, ct);
            }
        }

        if (lastException is ValhallaApiException apiEx)
        {
            throw apiEx;
        }

        throw new ValhallaApiException(0, $"请求在 {_maxRetries} 次重试后仍然失败");
    }

    /// <summary>
    /// 获取注册表基础 URL
    /// </summary>
    public string BaseUrl => _baseUrl;

    /// <summary>
    /// 列出所有包（分页）
    /// </summary>
    public async Task<PackageListResponse> ListPackagesAsync(
        int page = 1,
        int size = 20,
        string? query = null,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages?page={page}&size={size}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            url += $"&query={Uri.EscapeDataString(query)}";
        }

        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt, allow404: false);
            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<PackageListResponse>(content, JsonOptions)
                   ?? new PackageListResponse();
        }, ct);
    }

    /// <summary>
    /// 获取包的版本清单
    /// </summary>
    /// <param name="packageName">规范包名</param>
    public async Task<PackageManifest?> GetManifestAsync(
        string packageName,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/manifest";
        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<PackageManifest>(content, JsonOptions);
        }, ct);
    }

    /// <summary>
    /// 获取指定版本的详细信息
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    public async Task<VersionEntry?> GetVersionAsync(
        string packageName,
        string version,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/versions/{Uri.EscapeDataString(version)}";
        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<VersionEntry>(content, JsonOptions);
        }, ct);
    }

    /// <summary>
    /// 下载 .nyar 字节码和源码包
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <returns>下载结果，包含 .nyar 和源码的字节数组及 SHA-256</returns>
    public async Task<ValhallaDownloadResult> DownloadAsync(
        string packageName,
        string version,
        CancellationToken ct = default)
    {
        return await DownloadAsync(packageName, version, null, ct);
    }

    /// <summary>
    /// 下载 .nyar 字节码和源码包，支持进度回调
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <param name="progress">进度回调（已下载字节数，总字节数，百分比）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>下载结果</returns>
    public async Task<ValhallaDownloadResult> DownloadAsync(
        string packageName,
        string version,
        Action<long, long, double>? progress,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/versions/{Uri.EscapeDataString(version)}/download";

        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return new ValhallaDownloadResult
                {
                    PackageName = packageName,
                    Version = version,
                    Error = "包或版本不存在"
                };
            }

            string? packageSha256 = null;
            if (response.Headers.TryGetValues("X-Content-SHA256", out var shaValues))
            {
                packageSha256 = string.Join("", shaValues);
            }

            string? sourceSha256 = null;
            if (response.Headers.TryGetValues("X-Source-SHA256", out var srcShaValues))
            {
                sourceSha256 = string.Join("", srcShaValues);
            }

            long? totalBytes = response.Content.Headers.ContentLength;
            byte[] fullData;

            if (progress is not null && totalBytes > 0)
            {
                using var stream = await response.Content.ReadAsStreamAsync(innerCt);
                using var ms = new MemoryStream();
                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, innerCt)) > 0)
                {
                    await ms.WriteAsync(buffer.AsMemory(0, bytesRead), innerCt);
                    totalRead += bytesRead;

                    double pct = (double)totalRead / totalBytes.Value * 100;
                    progress(totalRead, totalBytes.Value, pct);
                }

                fullData = ms.ToArray();
            }
            else
            {
                fullData = await response.Content.ReadAsByteArrayAsync(innerCt);
            }

            if (fullData.Length < 4)
            {
                throw new InvalidDataException("下载数据格式无效：数据太短");
            }

            int packageSize = BitConverter.ToInt32(fullData, 0);
            if (packageSize < 0 || 4 + packageSize > fullData.Length)
            {
                throw new InvalidDataException("下载数据格式无效：.nyar 大小异常");
            }

            byte[] packageData = new byte[packageSize];
            Array.Copy(fullData, 4, packageData, 0, packageSize);

            int chOffset = 4 + packageSize;
            byte[]? sourceData = null;

            if (chOffset + 4 <= fullData.Length)
            {
                int sourceSize = BitConverter.ToInt32(fullData, chOffset);
                if (sourceSize > 0 && chOffset + 4 + sourceSize <= fullData.Length)
                {
                    sourceData = new byte[sourceSize];
                    Array.Copy(fullData, chOffset + 4, sourceData, 0, sourceSize);
                }
            }

            return new ValhallaDownloadResult
            {
                PackageName = packageName,
                Version = version,
                PackageData = packageData,
                SourceData = sourceData,
                PackageSha256 = packageSha256 ?? string.Empty,
                SourceSha256 = sourceSha256
            };
        }, ct);
    }

    /// <summary>
    /// 发布包到注册表
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="version">版本号</param>
    /// <param name="tarballData">打包后的二进制数据</param>
    /// <param name="authToken">认证令牌</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>发布结果</returns>
    public async Task<PublishResponse> PublishAsync(
        string packageName,
        string version,
        byte[] tarballData,
        string? authToken = null,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/versions/{Uri.EscapeDataString(version)}";

        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(tarballData), "package", $"{packageName}-{version}.tar.gz");

            using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            var response = await _http.SendAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(innerCt);
                return new PublishResponse
                {
                    Success = false,
                    Message = $"发布失败：{(int)response.StatusCode} {response.ReasonPhrase}",
                    Error = errorBody
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<PublishResponse>(responseBody, JsonOptions)
                   ?? new PublishResponse { Success = true, Message = "发布成功" };
        }, ct);
    }

    /// <summary>
    /// 删除指定版本
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="version">版本号</param>
    /// <param name="authToken">认证令牌</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> DeleteVersionAsync(
        string packageName,
        string version,
        string authToken,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/versions/{Uri.EscapeDataString(version)}";

        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _http.SendAsync(request, innerCt);
            return response.IsSuccessStatusCode;
        }, ct);
    }

    /// <summary>
    /// 获取包的审计日志
    /// </summary>
    /// <param name="packageName">规范包名</param>
    public async Task<List<AuditEntry>?> GetAuditLogAsync(
        string packageName,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/audit";
        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<List<AuditEntry>>(content, JsonOptions);
        }, ct);
    }

    /// <summary>
    /// 获取包级元信息
    /// </summary>
    /// <param name="packageName">规范包名</param>
    public async Task<ValhallaPackageMeta?> GetPackageMetaAsync(
        string packageName,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/meta";
        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<ValhallaPackageMeta>(content, JsonOptions);
        }, ct);
    }

    /// <summary>
    /// 获取版本级元信息
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    public async Task<ValhallaVersionMeta?> GetVersionMetaAsync(
        string packageName,
        string version,
        CancellationToken ct = default)
    {
        string url = $"{_baseUrl}/api/packages/{Uri.EscapeDataString(packageName)}/versions/{Uri.EscapeDataString(version)}/meta";
        return await ExecuteWithRetryAsync(async innerCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendRequestAsync(request, innerCt);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(innerCt);
            return JsonSerializer.Deserialize<ValhallaVersionMeta>(content, JsonOptions);
        }, ct);
    }

    /// <summary>
    /// 统一发送 HTTP 请求并处理状态码
    /// </summary>
    /// <param name="request">HTTP 请求消息</param>
    /// <param name="ct">取消令牌</param>
    /// <param name="allow404">是否允许 404 状态码（返回响应而不抛异常）</param>
    /// <returns>HTTP 响应消息</returns>
    /// <exception cref="ValhallaApiException">当 allow404 为 false 且返回 404，或任何非 2xx 且非允许的 404 错误时抛出</exception>
    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken ct,
        bool allow404 = true)
    {
        var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        if (allow404 && (int)response.StatusCode == 404)
        {
            return response;
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        throw new ValhallaApiException(
            (int)response.StatusCode,
            $"瓦尓哈拉 API 请求失败: {(int)response.StatusCode} {response.ReasonPhrase}",
            responseBody);
    }

    /// <summary>
    /// 释放 HTTP 客户端资源
    /// </summary>
    public void Dispose()
    {
        _http.Dispose();
    }
}