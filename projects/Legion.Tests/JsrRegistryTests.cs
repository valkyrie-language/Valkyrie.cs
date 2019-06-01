using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class JsrRegistryTests
{
    private const string JsrPackageJson = @"{
            ""scope"": ""testscope"",
            ""name"": ""testpkg"",
            ""latestVersion"": ""1.2.3"",
            ""description"": ""jsr 测试包"",
            ""license"": ""MIT"",
            ""versions"": [""1.0.0"", ""1.1.0"", ""1.2.3""]
        }";

    [Fact]
    public async Task GetPackageAsync_LatestVersion_ReturnsCorrectPackage()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg",
            HttpStatusCode.OK, JsrPackageJson);
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg/versions/1.2.3",
            HttpStatusCode.OK, @"{
                    ""version"": ""1.2.3"",
                    ""manifest"": {
                        ""dependencies"": {
                            ""@std/fs"": { ""constraint"": ""^1.0.0"" },
                            ""@std/path"": { ""constraint"": ""^0.5.0"" }
                        }
                    },
                    ""downloadUrl"": ""https://jsr.io/api/packages/@testscope/testpkg/versions/1.2.3/download""
                }");

        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var package = await registry.GetPackageAsync("@testscope/testpkg", "latest");

        Assert.NotNull(package);
        Assert.Equal("@testscope/testpkg", package.Name);
        Assert.Equal("1.2.3", package.Version);
        Assert.Equal("jsr 测试包", package.Description);
        Assert.Equal("MIT", package.License);
    }

    [Fact]
    public async Task GetPackageAsync_IncludesDependencies()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg",
            HttpStatusCode.OK, JsrPackageJson);
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg/versions/1.2.3",
            HttpStatusCode.OK, @"{
                    ""version"": ""1.2.3"",
                    ""manifest"": {
                        ""dependencies"": {
                            ""@std/fs"": { ""constraint"": ""^1.0.0"" },
                            ""@std/path"": { ""constraint"": ""^0.5.0"" },
                            ""lodash"": ""~4.17.0""
                        }
                    },
                    ""downloadUrl"": ""https://jsr.io/download""
                }");

        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var package = await registry.GetPackageAsync("@testscope/testpkg", "latest");

        Assert.Equal(3, package.Dependencies.Count);
        Assert.Contains("@std/fs@^1.0.0", package.Dependencies);
        Assert.Contains("@std/path@^0.5.0", package.Dependencies);
        Assert.Contains("lodash@~4.17.0", package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NoDependencies_EmptyList()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg",
            HttpStatusCode.OK, JsrPackageJson);
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg/versions/1.2.3",
            HttpStatusCode.OK, @"{ ""version"": ""1.2.3"", ""manifest"": {} }");

        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var package = await registry.GetPackageAsync("@testscope/testpkg", "latest");

        Assert.Empty(package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NotFound_ThrowsRegistryException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.GetPackageAsync("nonexistent", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ReturnsResults()
    {
        string searchJson = @"{
                ""items"": [
                    { ""scope"": ""testscope"", ""name"": ""testpkg"", ""latestVersion"": ""1.2.3"", ""description"": ""测试"", ""license"": ""MIT"" },
                    { ""scope"": """", ""name"": ""simplepkg"", ""latestVersion"": ""2.0.0"", ""description"": ""简单包"" }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://jsr.io/api/packages?query=test", HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var results = await registry.SearchPackagesAsync("test");

        Assert.Equal(2, results.Count);
        Assert.Equal("@testscope/testpkg", results[0].Name);
        Assert.Equal("1.2.3", results[0].Version);
        Assert.Equal("simplepkg", results[1].Name);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_ReturnsVersionList()
    {
        string versionsJson = @"{
                ""items"": [
                    { ""version"": ""1.0.0"" },
                    { ""version"": ""1.1.0"" },
                    { ""version"": ""1.2.3"" }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://jsr.io/api/packages/@testscope%2Ftestpkg/versions?limit=100",
            HttpStatusCode.OK, versionsJson);

        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var versions = await registry.GetPackageVersionsAsync("@testscope/testpkg");

        Assert.Equal(3, versions.Count);
        Assert.Contains("1.2.3", versions);
    }

    [Fact]
    public async Task PublishPackageAsync_RequiresAuthToken()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new JsrRegistry(client);

        var options = new PublishOptions { PackageName = "test", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.PublishPackageAsync(options, Array.Empty<byte>()));
    }
}