using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class NpmRegistryTests
{
    private static NpmRegistry CreateRegistry(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new NpmRegistry(httpClient);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseNpmResponse()
    {
        var handler = new MockHttpMessageHandler();
        var npmResponse = """
            {
                "name": "lodash",
                "description": "Lodash modular utilities",
                "homepage": "https://lodash.com/",
                "license": "MIT",
                "dist-tags": { "latest": "4.17.21" },
                "versions": {
                    "4.17.21": {
                        "name": "lodash",
                        "version": "4.17.21",
                        "description": "Lodash modular utilities",
                        "license": "MIT",
                        "dependencies": {},
                        "dist": {
                            "tarball": "https://registry.npmjs.org/lodash/-/lodash-4.17.21.tgz",
                            "integrity": "sha512-v2kDEe57lecTulaDIuNTPy3Ry4gLGJ6Z1O3vE1krgXZNrsQ+LFTGHVxVjcXPs17LhbZVGedAJv8XZ1tvj5FvSg=="
                        }
                    }
                }
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/lodash", npmResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("lodash", "latest");

        Assert.Equal("lodash", package.Name);
        Assert.Equal("4.17.21", package.Version);
        Assert.Equal("Lodash modular utilities", package.Description);
        Assert.Equal("MIT", package.License);
        Assert.NotNull(package.DistTarball);
        Assert.NotNull(package.DistIntegrity);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldThrowOnNotFound()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://registry.npmjs.org/nonexistent-pkg", _ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.GetPackageAsync("nonexistent-pkg", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ShouldParseSearchResponse()
    {
        var handler = new MockHttpMessageHandler();
        var searchResponse = """
            {
                "objects": [
                    {
                        "package": {
                            "name": "lodash",
                            "version": "4.17.21",
                            "description": "Lodash modular utilities",
                            "links": { "npm": "https://www.npmjs.com/package/lodash" }
                        }
                    }
                ]
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/-/v1/search", searchResponse);

        var registry = CreateRegistry(handler);
        var packages = await registry.SearchPackagesAsync("lodash");

        Assert.NotEmpty(packages);
        Assert.Equal("lodash", packages[0].Name);
        Assert.Equal("4.17.21", packages[0].Version);
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldRequireAuthToken()
    {
        var handler = new MockHttpMessageHandler();
        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.PublishPackageAsync(new PublishOptions
            {
                PackageName = "test-pkg",
                Version = "1.0.0",
                RegistryName = "npm"
            }, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldReturnSuccessOnOk()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://registry.npmjs.org/test-pkg", _ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var registry = CreateRegistry(handler);
        var result = await registry.PublishPackageAsync(new PublishOptions
        {
            PackageName = "test-pkg",
            Version = "1.0.0",
            AuthToken = "test-token"
        }, new byte[] { 1, 2, 3 });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldReturnFailureOnConflict()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://registry.npmjs.org/test-pkg", _ =>
            new HttpResponseMessage(HttpStatusCode.Conflict));

        var registry = CreateRegistry(handler);
        var result = await registry.PublishPackageAsync(new PublishOptions
        {
            PackageName = "test-pkg",
            Version = "1.0.0",
            AuthToken = "test-token"
        }, new byte[] { 1, 2, 3 });

        Assert.False(result.Success);
        Assert.Contains("已存在", result.Message!);
    }
}
