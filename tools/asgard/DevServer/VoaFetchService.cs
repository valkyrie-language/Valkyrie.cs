using System.Collections.Concurrent;
using System.Text;

namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA Fetch 服务 — HTTP 后端 + 缓存 + 去重，仅供 DevServer/SSR 使用
/// </summary>
public sealed class VoaFetchService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly VoaFetchCache _cache;
    private readonly ConcurrentDictionary<string, Task<FetchResponse>> _deduplicationMap = new();
    private bool _disposed;

    public VoaFetchService(VoaFetchCache? cache = null)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Asgard-VOA-Fetch/0.1");
        _cache = cache ?? new VoaFetchCache();
    }

    public VoaFetchCache Cache => _cache;

    /// <summary>
    ///     执行 HTTP 请求并返回结构化响应（含缓存逻辑）
    /// </summary>
    public async Task<FetchResponse> FetchAsync(string url, FetchOptions? options = null)
    {
        var opt = options ?? new FetchOptions();
        var cacheKey = BuildCacheKey(url, opt);

        if (_deduplicationMap.TryGetValue(cacheKey, out var pendingTask))
        {
            return await pendingTask;
        }

        var task = FetchInternalAsync(url, opt, cacheKey);
        _deduplicationMap[cacheKey] = task;

        try
        {
            return await task;
        }
        finally
        {
            _deduplicationMap.TryRemove(cacheKey, out _);
        }
    }

    private async Task<FetchResponse> FetchInternalAsync(string url, FetchOptions opt, string cacheKey)
    {
        if (opt.Cache == "force-cache" || opt.NextRevalidate > 0)
        {
            if (_cache.TryGet(cacheKey, out var cachedResponse))
            {
                cachedResponse.Cached = true;
                return cachedResponse;
            }
        }

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(opt.Method ?? "GET"), url);

            if (opt.Headers?.Count > 0)
            {
                foreach (var kvp in opt.Headers)
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            if (!string.IsNullOrEmpty(opt.Body) && opt.Method is "POST" or "PUT" or "PATCH")
            {
                request.Content = new StringContent(opt.Body, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var fetchResponse = new FetchResponse
            {
                Ok = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode,
                StatusText = response.ReasonPhrase ?? "",
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = body,
                Cached = false,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (opt.Cache == "force-cache" || opt.NextRevalidate > 0)
            {
                _cache.Set(cacheKey, fetchResponse, opt.NextRevalidate > 0 ? opt.NextRevalidate : 3600);
            }

            return fetchResponse;
        }
        catch (Exception ex)
        {
            return new FetchResponse
            {
                Ok = false,
                Status = 0,
                StatusText = ex.Message,
                Headers = new Dictionary<string, string>(),
                Body = "",
                Cached = false,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }

    public async Task FetchStreamAsync(string url, Func<string, Task> onChunk, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var buffer = new StringBuilder();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.StartsWith("data: "))
            {
                buffer.Append(line[6..]);
            }
            else if (string.IsNullOrEmpty(line) && buffer.Length > 0)
            {
                await onChunk(buffer.ToString());
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            await onChunk(buffer.ToString());
        }
    }

    public void ClearDeduplicationMap()
    {
        _deduplicationMap.Clear();
    }

    private static string BuildCacheKey(string url, FetchOptions opt)
    {
        var key = $"{opt.Method ?? "GET"}:{url}";
        if (!string.IsNullOrEmpty(opt.Body))
        {
            key += $":{opt.Body}";
        }

        return key;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}

public sealed class FetchOptions
{
    public string? Method { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public string? Cache { get; set; }
    public int NextRevalidate { get; set; }
    public List<string>? NextTags { get; set; }
}

public sealed class FetchResponse
{
    public bool Ok { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
    public bool Cached { get; set; }
    public long Timestamp { get; set; }
}
