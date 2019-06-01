using System.Net;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
/// 支持多请求路由的 HTTP 消息处理器，最长前缀优先匹配
/// </summary>
public class RoutingFakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string prefix, HttpStatusCode status, string content)> _routes = new();

    public void AddRoute(string urlPrefix, HttpStatusCode status, string content)
    {
        _routes.Add((urlPrefix, status, content));
        _routes.Sort((a, b) => b.prefix.Length.CompareTo(a.prefix.Length));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestUrl = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var (prefix, status, content) in _routes)
        {
            if (requestUrl.StartsWith(prefix))
            {
                var response = new HttpResponseMessage(status)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
                };

                return Task.FromResult(response);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}