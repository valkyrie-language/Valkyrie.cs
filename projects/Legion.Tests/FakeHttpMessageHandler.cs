using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
/// 测试用的 HTTP 消息处理器，返回预设的响应
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseContent;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        _statusCode = statusCode;
        _responseContent = responseContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}