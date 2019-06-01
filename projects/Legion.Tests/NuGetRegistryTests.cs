using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class NuGetRegistryTests
{
    private const string NuGetLatestJson = @"{
            ""items"": [
                {
                    ""items"": [
                        {
                            ""catalogEntry"": {
                                ""version"": ""2.0.0"",
                                ""description"": ""高性能 JSON 序列化库"",
                                ""authors"": ""James Newton-King"",
                                ""licenseExpression"": ""MIT"",
                                ""projectUrl"": ""https://www.newtonsoft.com/json"",
                                ""dependencyGroups"": []
                            },
                            ""packageContent"": ""https://api.nuget.org/v3-flatcontainer/newtonsoft.json/2.0.0/newtonsoft.json.2.0.0.nupkg""
                        }
                    ]
                }
            ]
        }";

    [Fact]
    public async Task GetPackageAsync_LatestVersion_ReturnsCorrectPackage()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://api.nuget.org/v3/registration5-gz-semver2/newtonsoft.json/index.json",
            HttpStatusCode.OK, NuGetLatestJson);

        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var package = await registry.GetPackageAsync("Newtonsoft.Json", "latest");

        Assert.NotNull(package);
        Assert.Equal("Newtonsoft.Json", package.Name);
        Assert.Equal("2.0.0", package.Version);
        Assert.Equal("高性能 JSON 序列化库", package.Description);
        Assert.Equal("James Newton-King", package.Author);
        Assert.Equal("MIT", package.License);
        Assert.Equal("https://www.newtonsoft.com/json", package.Homepage);
        Assert.Contains("newtonsoft.json.2.0.0.nupkg", package.DistTarball);
    }

    [Fact]
    public async Task GetPackageAsync_WithDependencies_ParsesCorrectly()
    {
        string latestWithDeps = @"{
                ""items"": [
                    {
                        ""items"": [
                            {
                                ""catalogEntry"": {
                                    ""version"": ""8.0.0"",
                                    ""description"": ""EF Core"",
                                    ""authors"": ""Microsoft"",
                                    ""licenseExpression"": ""MIT"",
                                    ""projectUrl"": ""https://docs.microsoft.com/ef/core"",
                                    ""dependencyGroups"": [
                                        {
                                            ""targetFramework"": ""net8.0"",
                                            ""dependencies"": [
                                                { ""id"": ""Microsoft.Extensions.Caching.Memory"", ""range"": ""8.0.0"" },
                                                { ""id"": ""Microsoft.Extensions.Logging"", ""range"": ""[8.0.0, 9.0.0)"" }
                                            ]
                                        }
                                    ]
                                },
                                ""packageContent"": ""https://api.nuget.org/v3-flatcontainer/entityframeworkcore/8.0.0/entityframeworkcore.8.0.0.nupkg""
                            }
                        ]
                    }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://api.nuget.org/v3/registration5-gz-semver2/entityframeworkcore/index.json",
            HttpStatusCode.OK, latestWithDeps);

        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var package = await registry.GetPackageAsync("EntityFrameworkCore", "latest");

        Assert.Equal(2, package.Dependencies.Count);
        Assert.Contains("Microsoft.Extensions.Caching.Memory@8.0.0", package.Dependencies);
        Assert.Contains("Microsoft.Extensions.Logging@[8.0.0, 9.0.0)", package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_SpecificVersion_ReturnsCorrectPackage()
    {
        string specificVersionJson = @"{
                ""catalogEntry"": {
                    ""version"": ""1.0.0"",
                    ""description"": ""初始版本"",
                    ""authors"": ""作者"",
                    ""licenseExpression"": ""MIT"",
                    ""projectUrl"": ""https://example.com"",
                    ""dependencyGroups"": []
                },
                ""packageContent"": ""https://api.nuget.org/v3-flatcontainer/test.pkg/1.0.0/test.pkg.1.0.0.nupkg""
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://api.nuget.org/v3/registration5-gz-semver2/test.pkg/1.0.0.json",
            HttpStatusCode.OK, specificVersionJson);

        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var package = await registry.GetPackageAsync("Test.Pkg", "1.0.0");

        Assert.Equal("1.0.0", package.Version);
        Assert.Equal("初始版本", package.Description);
        Assert.Empty(package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NotFound_ThrowsRegistryException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.GetPackageAsync("Nonexistent.Pkg", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ReturnsResults()
    {
        string searchJson = @"{
                ""data"": [
                    { ""id"": ""Newtonsoft.Json"", ""version"": ""2.0.0"", ""description"": ""JSON 库"", ""projectUrl"": ""https://newtonsoft.com"", ""authors"": ""James"", ""licenseExpression"": ""MIT"" },
                    { ""id"": ""System.Text.Json"", ""version"": ""8.0.0"", ""description"": ""内置 JSON"", ""projectUrl"": ""https://dot.net"", ""authors"": ""Microsoft"", ""licenseExpression"": ""MIT"" }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://api.nuget.org/v3/query?q=json",
            HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var results = await registry.SearchPackagesAsync("json");

        Assert.Equal(2, results.Count);
        Assert.Equal("Newtonsoft.Json", results[0].Name);
        Assert.Equal("System.Text.Json", results[1].Name);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_ReturnsVersionList()
    {
        string versionsJson = @"{
                ""items"": [
                    {
                        ""items"": [
                            { ""catalogEntry"": { ""version"": ""1.0.0"" } },
                            { ""catalogEntry"": { ""version"": ""1.1.0"" } },
                            { ""catalogEntry"": { ""version"": ""2.0.0"" } }
                        ]
                    }
                ]
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://api.nuget.org/v3/registration5-gz-semver2/test.pkg/index.json",
            HttpStatusCode.OK, versionsJson);

        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var versions = await registry.GetPackageVersionsAsync("Test.Pkg");

        Assert.Equal(3, versions.Count);
        Assert.Contains("2.0.0", versions);
    }

    [Fact]
    public async Task PublishPackageAsync_RequiresAuthToken()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new NuGetRegistry(client);

        var options = new PublishOptions { PackageName = "Test.Pkg", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.PublishPackageAsync(options, Array.Empty<byte>()));
    }
}