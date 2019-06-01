using System.Net;
using Xunit;
using Valhalla.Client;

namespace Valhalla.Tests;

public class ValhallaClientTests
{
    private const string BaseUrl = "https://valhalla.test";

    private static string ManifestJson => @"
{
    ""name"": ""test.pkg"",
    ""incarnation"": 1,
    ""publisher"": ""abc123"",
    ""registeredAt"": ""2025-01-01T00:00:00Z"",
    ""status"": ""active"",
    ""versions"": {
        ""1.0.0"": {
            ""version"": ""1.0.0"",
            ""status"": ""active"",
            ""packageDigest"": ""aabbcc"",
            ""packageSize"": 1024,
            ""publishedAt"": ""2025-01-01T00:00:00Z""
        },
        ""2.0.0"": {
            ""version"": ""2.0.0"",
            ""status"": ""active"",
            ""packageDigest"": ""ddeeff"",
            ""packageSize"": 2048,
            ""publishedAt"": ""2025-02-01T00:00:00Z""
        }
    }
}";

    private static string VersionJson => @"{
        ""version"": ""1.0.0"",
        ""status"": ""active"",
        ""packageDigest"": ""abc123"",
        ""packageSize"": 1024,
        ""publishedAt"": ""2025-01-01T00:00:00Z""
    }";

    [Fact]
    public async Task GetManifestAsync_正常包_返回manifest()
    {
        var handler = new TestHttpMessageHandler();
        handler.AddGet("/api/packages/test.pkg/manifest", HttpStatusCode.OK, ManifestJson);

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(BaseUrl, http);

        var manifest = await client.GetManifestAsync("test.pkg");

        Assert.NotNull(manifest);
        Assert.Equal("test.pkg", manifest!.Name);
        Assert.Equal(1, manifest.Incarnation);
        Assert.Equal("abc123", manifest.Publisher);
        Assert.Equal(2, manifest.Versions.Count);
    }

    [Fact]
    public async Task GetManifestAsync_不存在的包_返回null()
    {
        var handler = new TestHttpMessageHandler();
        handler.AddGet("/api/packages/unknown/manifest", HttpStatusCode.NotFound,
            @"{""error"":""not found""}");

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(BaseUrl, http);

        var manifest = await client.GetManifestAsync("unknown");
        Assert.Null(manifest);
    }

    [Fact]
    public async Task ListPackagesAsync_返回分页结果()
    {
        string response = @"{
            ""packages"": [
                {
                    ""name"": ""org.lib"",
                    ""latestVersion"": ""1.0.0"",
                    ""publisher"": ""key123"",
                    ""incarnation"": 1,
                    ""status"": ""active"",
                    ""downloadCount"": 100,
                    ""createdAt"": ""2025-01-01T00:00:00Z"",
                    ""updatedAt"": ""2025-02-01T00:00:00Z""
                }
            ],
            ""total"": 1,
            ""page"": 1,
            ""size"": 20
        }";

        var handler = new TestHttpMessageHandler();
        handler.AddGet("/api/packages?page=1&size=20", HttpStatusCode.OK, response);

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(BaseUrl, http);

        var result = await client.ListPackagesAsync();

        Assert.NotNull(result);
        Assert.Single(result.Packages);
        Assert.Equal("org.lib", result.Packages[0].Name);
        Assert.Equal(1, result.Total);
    }

    /// <summary>
    /// 重试相关测试
    /// </summary>
    public class RetryTests
    {
        private const string BaseUrl = "https://valhalla.test";

        [Fact]
        public async Task 重试策略_服务端500错误_自动重试成功()
        {
            var handler = new TestHttpMessageHandler();
            handler.AddSequentialGet(
                "/api/packages/retry.pkg/versions/1.0.0",
                new[]
                {
                    (HttpStatusCode.InternalServerError, @"{""error"":""server error 1""}"),
                    (HttpStatusCode.InternalServerError, @"{""error"":""server error 2""}"),
                    (HttpStatusCode.OK, VersionJson)
                });

            using var http = new HttpClient(handler);
            var client = new ValhallaClient(BaseUrl, http, maxRetries: 3, retryBaseDelayMs: 1);

            var version = await client.GetVersionAsync("retry.pkg", "1.0.0");

            Assert.NotNull(version);
            Assert.Equal("1.0.0", version!.Version);
            Assert.Equal("abc123", version.PackageDigest);
        }

        [Fact]
        public async Task 重试策略_超出最大重试次数_抛出异常()
        {
            const int maxRetries = 2;

            var handler = new TestHttpMessageHandler();
            handler.AddSequentialGet(
                "/api/packages/fail.pkg/versions/1.0.0",
                new[]
                {
                    (HttpStatusCode.InternalServerError, @"{""error"":""error 1""}"),
                    (HttpStatusCode.InternalServerError, @"{""error"":""error 2""}"),
                    (HttpStatusCode.InternalServerError, @"{""error"":""error 3""}")
                });

            using var http = new HttpClient(handler);
            var client = new ValhallaClient(BaseUrl, http, maxRetries: maxRetries, retryBaseDelayMs: 1);

            var ex = await Assert.ThrowsAsync<ValhallaApiException>(
                () => client.GetVersionAsync("fail.pkg", "1.0.0"));

            Assert.Equal(500, ex.StatusCode);
        }

        [Fact]
        public async Task 重试策略_禁用重试_首次失败即抛出()
        {
            var handler = new TestHttpMessageHandler();
            handler.AddSequentialGet(
                "/api/packages/noretry.pkg/versions/1.0.0",
                new[]
                {
                    (HttpStatusCode.InternalServerError, @"{""error"":""first error""}"),
                    (HttpStatusCode.OK, VersionJson)
                });

            using var http = new HttpClient(handler);
            var client = new ValhallaClient(BaseUrl, http, maxRetries: 0, retryBaseDelayMs: 1);

            var ex = await Assert.ThrowsAsync<ValhallaApiException>(
                () => client.GetVersionAsync("noretry.pkg", "1.0.0"));

            Assert.Equal(500, ex.StatusCode);
        }
    }
}