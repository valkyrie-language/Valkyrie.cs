using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class JsrRegistryTests
{
    private static JsrRegistry CreateRegistry(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new JsrRegistry(httpClient);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseJsrResponse()
    {
        var handler = new MockHttpMessageHandler();
        var jsrResponse = """
            {
                "scope": "std",
                "name": "assert",
                "description": "Assertion library",
                "latestVersion": "1.0.0",
                "license": "MIT"
            }
            """;

        handler.RegisterJsonResponse("https://jsr.io/api/packages/", jsrResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("@std/assert", "latest");

        Assert.Equal("@std/assert", package.Name);
        Assert.Equal("1.0.0", package.Version);
        Assert.Equal("Assertion library", package.Description);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldThrowOnNotFound()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://jsr.io/api/packages/", _ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.GetPackageAsync("nonexistent-pkg", "latest"));
    }
}
