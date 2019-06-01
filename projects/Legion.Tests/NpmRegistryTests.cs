using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class NpmRegistryTests
{
    private const string NpmPackageJson = @"{
            ""name"": ""test-package"",
            ""dist-tags"": {
                ""latest"": ""1.2.3""
            },
            ""description"": ""测试包描述"",
            ""homepage"": ""https://example.com"",
            ""author"": ""测试作者"",
            ""license"": ""MIT"",
            ""versions"": {
                ""1.2.3"": {
                    ""version"": ""1.2.3"",
                    ""description"": ""测试包描述"",
                    ""homepage"": ""https://example.com"",
                    ""author"": ""测试作者"",
                    ""license"": ""MIT"",
                    ""dependencies"": {
                        ""dep-a"": ""^2.0.0"",
                        ""dep-b"": ""~3.1.0""
                    },
                    ""dist"": {
                        ""tarball"": ""https://registry.npmjs.org/test-package/-/test-package-1.2.3.tgz"",
                        ""integrity"": ""sha512-test""
                    }
                },
                ""2.0.0"": {
                    ""version"": ""2.0.0"",
                    ""description"": ""大版本更新"",
                    ""dependencies"": {
                        ""dep-c"": ""1.0.0""
                    },
                    ""dist"": {
                        ""tarball"": ""https://registry.npmjs.org/test-package/-/test-package-2.0.0.tgz""
                    }
                }
            }
        }";

    private const string NpmSearchJson = @"{
            ""objects"": [
                {
                    ""package"": {
                        ""name"": ""test-package"",
                        ""version"": ""1.2.3"",
                        ""description"": ""测试包描述"",
                        ""links"": {
                            ""npm"": ""https://www.npmjs.com/package/test-package"",
                            ""tarball"": ""https://registry.npmjs.org/test-package/-/test-package-1.2.3.tgz""
                        },
                        ""publisher"": {
                            ""username"": ""测试作者""
                        }
                    }
                },
                {
                    ""package"": {
                        ""name"": ""another-pkg"",
                        ""version"": ""0.5.0"",
                        ""description"": ""另一个包""
                    }
                }
            ]
        }";

    [Fact]
    public async Task GetPackageAsync_LatestVersion_ReturnsCorrectPackage()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/test-package", HttpStatusCode.OK, NpmPackageJson);

        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var package = await registry.GetPackageAsync("test-package", "latest");

        Assert.NotNull(package);
        Assert.Equal("test-package", package.Name);
        Assert.Equal("1.2.3", package.Version);
        Assert.Equal("测试包描述", package.Description);
        Assert.Equal("https://example.com", package.Homepage);
        Assert.Equal("测试作者", package.Author);
        Assert.Equal("MIT", package.License);
        Assert.Equal("https://registry.npmjs.org/test-package/-/test-package-1.2.3.tgz", package.DistTarball);
        Assert.Equal("sha512-test", package.DistIntegrity);
    }

    [Fact]
    public async Task GetPackageAsync_LatestVersion_IncludesDependencies()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/test-package", HttpStatusCode.OK, NpmPackageJson);

        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var package = await registry.GetPackageAsync("test-package", "latest");

        Assert.Equal(2, package.Dependencies.Count);
        Assert.Contains("dep-a@^2.0.0", package.Dependencies);
        Assert.Contains("dep-b@~3.1.0", package.Dependencies);
        Assert.Equal("^2.0.0", package.DependencyVersions["dep-a"]);
        Assert.Equal("~3.1.0", package.DependencyVersions["dep-b"]);
    }

    [Fact]
    public async Task GetPackageAsync_SpecificVersion_ReturnsCorrectVersion()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/test-package/2.0.0", HttpStatusCode.OK, @"{
                ""version"": ""2.0.0"",
                ""description"": ""大版本更新"",
                ""dependencies"": { ""dep-c"": ""1.0.0"" },
                ""dist"": { ""tarball"": ""https://registry.npmjs.org/test-package/-/test-package-2.0.0.tgz"" }
            }");

        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var package = await registry.GetPackageAsync("test-package", "2.0.0");

        Assert.Equal("2.0.0", package.Version);
        Assert.Equal("大版本更新", package.Description);
        Assert.Single(package.Dependencies);
        Assert.Contains("dep-c@1.0.0", package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NotFound_ThrowsRegistryException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var ex = await Assert.ThrowsAsync<RegistryException>(
            () => registry.GetPackageAsync("nonexistent", "latest"));

        Assert.Equal(404, ex.StatusCode);
        Assert.Contains("未找到", ex.Message);
    }

    [Fact]
    public async Task SearchPackagesAsync_ReturnsResults()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/v1/search", HttpStatusCode.OK, NpmSearchJson);

        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var results = await registry.SearchPackagesAsync("test");

        Assert.Equal(2, results.Count);
        Assert.Equal("test-package", results[0].Name);
        Assert.Equal("1.2.3", results[0].Version);
        Assert.Equal("测试包描述", results[0].Description);
        Assert.Equal("another-pkg", results[1].Name);
        Assert.Equal("0.5.0", results[1].Version);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_ReturnsVersionList()
    {
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/test-package", HttpStatusCode.OK, NpmPackageJson);

        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var versions = await registry.GetPackageVersionsAsync("test-package");

        Assert.Equal(2, versions.Count);
        Assert.Contains("1.2.3", versions);
        Assert.Contains("2.0.0", versions);
    }

    [Fact]
    public async Task DownloadPackageAsync_RequiresDistTarball()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var package = new Package { Name = "no-tarball", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.DownloadPackageAsync(package, System.IO.Path.GetTempPath()));
    }

    [Fact]
    public async Task PublishPackageAsync_RequiresAuthToken()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new NpmRegistry(client);

        var options = new PublishOptions { PackageName = "test", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.PublishPackageAsync(options, Array.Empty<byte>()));
    }
}