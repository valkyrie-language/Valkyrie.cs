using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Valkyrie.Tests.PackageManagerTests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _handlers = new();

    public void RegisterHandler(string url, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handlers[url] = handler;
    }

    public void RegisterJsonResponse(string url, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _handlers[url] = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestUrl = request.RequestUri?.ToString();
        if (requestUrl is not null)
        {
            var match = _handlers
                .Where(kvp => requestUrl.StartsWith(kvp.Key))
                .OrderByDescending(kvp => kvp.Key.Length)
                .FirstOrDefault();

            if (match.Value is not null)
            {
                return Task.FromResult(match.Value(request));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"{{\"error\":\"Not Found\"}}")
        });
    }
}