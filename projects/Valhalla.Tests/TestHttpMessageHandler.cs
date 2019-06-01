using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Valhalla.Tests;

/// <summary>
/// 测试用 HTTP 消息处理器，按方法+路径路由，支持查询参数和二进制响应
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly List<RouteEntry> _routes = new();
    private readonly List<SequentialRouteEntry> _sequentialRoutes = new();

    public void AddGet(string pathAndQuery, HttpStatusCode status, string content,
        Dictionary<string, string>? headers = null)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(content);
        _routes.Add(new RouteEntry(HttpMethod.Get, pathAndQuery, status, bodyBytes,
            "application/json; charset=utf-8", headers));
    }

    public void AddGetBytes(string pathAndQuery, HttpStatusCode status, byte[] content,
        Dictionary<string, string>? headers = null)
    {
        _routes.Add(new RouteEntry(HttpMethod.Get, pathAndQuery, status, content,
            "application/octet-stream", headers));
    }

    public void AddPost(string pathAndQuery, HttpStatusCode status, string content,
        Dictionary<string, string>? headers = null)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(content);
        _routes.Add(new RouteEntry(HttpMethod.Post, pathAndQuery, status, bodyBytes,
            "application/json; charset=utf-8", headers));
    }

    /// <summary>
    /// 添加顺序响应的 GET 路由，每次请求依次返回数组中的响应
    /// </summary>
    /// <param name="pathAndQuery">路径与查询参数</param>
    /// <param name="responses">顺序响应数组，每个元素为 (状态码, 响应内容)</param>
    public void AddSequentialGet(string pathAndQuery, (HttpStatusCode Status, string Content)[] responses)
    {
        var entries = responses.Select(r =>
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(r.Content);
            return new RouteEntry(HttpMethod.Get, pathAndQuery, r.Status, bodyBytes,
                "application/json; charset=utf-8", null);
        }).ToArray();

        _sequentialRoutes.Add(new SequentialRouteEntry(pathAndQuery, entries));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string fullUrl = request.RequestUri?.PathAndQuery ?? string.Empty;

        foreach (var seqRoute in _sequentialRoutes)
        {
            if (fullUrl == seqRoute.PathAndQuery)
            {
                var entry = seqRoute.ConsumeNext();
                var response = BuildResponse(entry);
                return Task.FromResult(response);
            }
        }

        foreach (var route in _routes)
        {
            if (request.Method == route.Method && fullUrl == route.PathAndQuery)
            {
                return Task.FromResult(BuildResponse(route));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(@"{""error"":""not found""}",
                Encoding.UTF8, "application/json")
        });
    }

    private static HttpResponseMessage BuildResponse(RouteEntry entry)
    {
        var response = new HttpResponseMessage(entry.Status)
        {
            Content = new ByteArrayContent(entry.BodyBytes)
        };
        response.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(entry.ContentType);

        foreach (var (key, value) in entry.Headers)
        {
            response.Headers.TryAddWithoutValidation(key, value);
        }

        return response;
    }

    private record RouteEntry(
        HttpMethod Method,
        string PathAndQuery,
        HttpStatusCode Status,
        byte[] BodyBytes,
        string ContentType,
        Dictionary<string, string>? Headers
    )
    {
        public Dictionary<string, string> Headers { get; } = Headers ?? new();
    }

    /// <summary>
    /// 顺序路由条目，维护消费索引
    /// </summary>
    private class SequentialRouteEntry
    {
        private readonly RouteEntry[] _entries;
        private int _index;

        public string PathAndQuery { get; }

        public SequentialRouteEntry(string pathAndQuery, RouteEntry[] entries)
        {
            PathAndQuery = pathAndQuery;
            _entries = entries;
            _index = 0;
        }

        public RouteEntry ConsumeNext()
        {
            var entry = _entries[_index];
            if (_index < _entries.Length - 1)
            {
                _index++;
            }

            return entry;
        }
    }
}