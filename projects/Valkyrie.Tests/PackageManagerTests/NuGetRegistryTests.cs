using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class NuGetRegistryTests
{
    private static NuGetRegistry CreateRegistry(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new NuGetRegistry(httpClient);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseNuGetLatestResponse()
    {
        var handler = new MockHttpMessageHandler();
        var registrationResponse = """
            {
                "items": [{
                    "items": [{
                        "catalogEntry": {
                            "id": "Newtonsoft.Json",
                            "version": "13.0.3",
                            "description": "Json.NET is a popular high-performance JSON framework for .NET",
                            "authors": "James Newton-King",
                            "licenseExpression": "MIT",
                            "projectUrl": "https://www.newtonsoft.com/json",
                            "dependencyGroups": []
                        },
                        "packageContent": "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg"
                    }]
                }]
            }
            """;

        handler.RegisterJsonResponse("https://api.nuget.org/v3/registration5-gz-semver2/newtonsoft.json/index.json", registrationResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("Newtonsoft.Json", "latest");

        Assert.Equal("Newtonsoft.Json", package.Name);
        Assert.Equal("13.0.3", package.Version);
        Assert.Equal("MIT", package.License);
        Assert.Equal("James Newton-King", package.Author);
        Assert.NotNull(package.DistTarball);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseNuGetSpecificVersionResponse()
    {
        var handler = new MockHttpMessageHandler();
        var versionResponse = """
            {
                "catalogEntry": {
                    "id": "Serilog",
                    "version": "3.1.1",
                    "description": "Simple logging with structured log events",
                    "authors": "Serilog Contributors",
                    "licenseExpression": "Apache-2.0",
                    "projectUrl": "https://serilog.net",
                    "dependencyGroups": []
                },
                "packageContent": "https://api.nuget.org/v3-flatcontainer/serilog/3.1.1/serilog.3.1.1.nupkg"
            }
            """;

        handler.RegisterJsonResponse("https://api.nuget.org/v3/registration5-gz-semver2/serilog/3.1.1.json", versionResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("Serilog", "3.1.1");

        Assert.Equal("Serilog", package.Name);
        Assert.Equal("3.1.1", package.Version);
        Assert.Equal("Apache-2.0", package.License);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseNuGetDependencies()
    {
        var handler = new MockHttpMessageHandler();
        var versionResponse = """
            {
                "catalogEntry": {
                    "id": "Microsoft.Extensions.DependencyInjection",
                    "version": "8.0.0",
                    "description": "Dependency injection",
                    "authors": "Microsoft",
                    "dependencyGroups": [{
                        "targetFramework": ".NETStandard2.0",
                        "dependencies": [{
                            "id": "Microsoft.Extensions.DependencyInjection.Abstractions",
                            "range": "[8.0.0, )"
                        }]
                    }]
                },
                "packageContent": "https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection/8.0.0/microsoft.extensions.dependencyinjection.8.0.0.nupkg"
            }
            """;

        handler.RegisterJsonResponse("https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.dependencyinjection/8.0.0.json", versionResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("Microsoft.Extensions.DependencyInjection", "8.0.0");

        Assert.NotEmpty(package.Dependencies);
        Assert.True(package.DependencyVersions.ContainsKey("Microsoft.Extensions.DependencyInjection.Abstractions"));
    }

    [Fact]
    public async Task GetPackageAsync_ShouldThrowOnNotFound()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://api.nuget.org/v3/registration5-gz-semver2/nonexistent/package/index.json", _ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.GetPackageAsync("nonexistent", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ShouldParseSearchResponse()
    {
        var handler = new MockHttpMessageHandler();
        var searchResponse = """
            {
                "totalHits": 2,
                "data": [
                    {
                        "id": "Newtonsoft.Json",
                        "version": "13.0.3",
                        "description": "Json.NET",
                        "authors": "James Newton-King",
                        "licenseExpression": "MIT"
                    },
                    {
                        "id": "System.Text.Json",
                        "version": "8.0.0",
                        "description": "System.Text.Json",
                        "authors": "Microsoft"
                    }
                ]
            }
            """;

        handler.RegisterJsonResponse("https://api.nuget.org/v3/query", searchResponse);

        var registry = CreateRegistry(handler);
        var packages = await registry.SearchPackagesAsync("json");

        Assert.Equal(2, packages.Count);
        Assert.Equal("Newtonsoft.Json", packages[0].Name);
        Assert.Equal("13.0.3", packages[0].Version);
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldRequireApiKey()
    {
        var handler = new MockHttpMessageHandler();
        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.PublishPackageAsync(new PublishOptions
            {
                PackageName = "TestPackage",
                Version = "1.0.0"
            }, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldReturnSuccessOnOk()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://api.nuget.org/api/v2/package", _ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var registry = CreateRegistry(handler);
        var result = await registry.PublishPackageAsync(new PublishOptions
        {
            PackageName = "TestPackage",
            Version = "1.0.0",
            AuthToken = "test-api-key"
        }, new byte[] { 1, 2, 3 });

        Assert.True(result.Success);
        Assert.Contains("NuGet", result.Message!);
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldReturnFailureOnConflict()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://api.nuget.org/api/v2/package", _ =>
            new HttpResponseMessage(HttpStatusCode.Conflict));

        var registry = CreateRegistry(handler);
        var result = await registry.PublishPackageAsync(new PublishOptions
        {
            PackageName = "TestPackage",
            Version = "1.0.0",
            AuthToken = "test-api-key"
        }, new byte[] { 1, 2, 3 });

        Assert.False(result.Success);
        Assert.Contains("已存在", result.Message!);
    }
}
