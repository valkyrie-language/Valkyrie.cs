using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class MavenRegistryTests
{
    [Fact]
    public void ParseMavenCoordinates_WithColon_SeparatesGroupAndArtifact()
    {
        MavenRegistry.ParseMavenCoordinates("com.google.guava:guava", out string groupId, out string artifactId);

        Assert.Equal("com.google.guava", groupId);
        Assert.Equal("guava", artifactId);
    }

    [Fact]
    public void ParseMavenCoordinates_WithoutColon_UsesSame()
    {
        MavenRegistry.ParseMavenCoordinates("simple-name", out string groupId, out string artifactId);

        Assert.Equal("simple-name", groupId);
        Assert.Equal("simple-name", artifactId);
    }

    [Fact]
    public async Task GetPackageAsync_LatestVersion_ReturnsCorrectPackage()
    {
        string searchJson = @"{
                ""response"": {
                    ""docs"": [
                        { ""g"": ""com.google.guava"", ""a"": ""guava"", ""v"": ""33.0.0"" },
                        { ""g"": ""com.google.guava"", ""a"": ""guava"", ""v"": ""32.0.0"" }
                    ]
                }
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://search.maven.org/solrsearch/select",
            HttpStatusCode.OK, searchJson);
        handler.AddRoute("https://repo1.maven.org/maven2/com/google/guava/guava/33.0.0/guava-33.0.0.pom",
            HttpStatusCode.NotFound, "");

        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        var package = await registry.GetPackageAsync("com.google.guava:guava", "latest");

        Assert.NotNull(package);
        Assert.Equal("com.google.guava:guava", package.Name);
        Assert.Equal("33.0.0", package.Version);
        Assert.Contains("guava-33.0.0.jar", package.DistTarball);
    }

    [Fact]
    public async Task GetPackageAsync_WithPom_ParsesDependencies()
    {
        string searchJson = @"{
                ""response"": {
                    ""docs"": [
                        { ""g"": ""com.example"", ""a"": ""lib"", ""v"": ""1.0.0"" }
                    ]
                }
            }";

        string pomContent = @"<?xml version=""1.0""?>
            <project>
                <groupId>com.example</groupId>
                <artifactId>lib</artifactId>
                <version>1.0.0</version>
                <dependencies>
                    <dependency>
                        <groupId>org.slf4j</groupId>
                        <artifactId>slf4j-api</artifactId>
                        <version>2.0.0</version>
                    </dependency>
                    <dependency>
                        <groupId>com.google.code.gson</groupId>
                        <artifactId>gson</artifactId>
                        <version>2.10.1</version>
                    </dependency>
                </dependencies>
            </project>";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://search.maven.org/solrsearch/select",
            HttpStatusCode.OK, searchJson);
        handler.AddRoute("https://repo1.maven.org/maven2/com/example/lib/1.0.0/lib-1.0.0.pom",
            HttpStatusCode.OK, pomContent);

        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        var package = await registry.GetPackageAsync("com.example:lib", "latest");

        Assert.Equal(2, package.Dependencies.Count);
        Assert.Contains("org.slf4j:slf4j-api:2.0.0", package.Dependencies);
        Assert.Contains("com.google.code.gson:gson:2.10.1", package.Dependencies);
    }

    [Fact]
    public async Task GetPackageAsync_NotFound_ThrowsRegistryException()
    {
        string searchJson = @"{ ""response"": { ""docs"": [] } }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://search.maven.org/solrsearch/select",
            HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.GetPackageAsync("nonexistent:lib", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ReturnsResults()
    {
        string searchJson = @"{
                ""response"": {
                    ""docs"": [
                        { ""g"": ""com.google.guava"", ""a"": ""guava"", ""v"": ""33.0.0"" },
                        { ""g"": ""org.slf4j"", ""a"": ""slf4j-api"", ""v"": ""2.0.0"" }
                    ]
                }
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://search.maven.org/solrsearch/select",
            HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        var results = await registry.SearchPackagesAsync("guava");

        Assert.Equal(2, results.Count);
        Assert.Equal("com.google.guava:guava", results[0].Name);
        Assert.Equal("org.slf4j:slf4j-api", results[1].Name);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_ReturnsVersionList()
    {
        string searchJson = @"{
                ""response"": {
                    ""docs"": [
                        { ""g"": ""com.google.guava"", ""a"": ""guava"", ""v"": ""33.0.0"" },
                        { ""g"": ""com.google.guava"", ""a"": ""guava"", ""v"": ""32.0.0"" }
                    ]
                }
            }";

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://search.maven.org/solrsearch/select",
            HttpStatusCode.OK, searchJson);

        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        var versions = await registry.GetPackageVersionsAsync("com.google.guava:guava");

        Assert.Equal(2, versions.Count);
        Assert.Contains("33.0.0", versions);
    }

    [Fact]
    public async Task PublishPackageAsync_RequiresAuthToken()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var registry = new MavenRegistry(client);

        var options = new PublishOptions { PackageName = "test:lib", Version = "1.0.0" };

        await Assert.ThrowsAsync<RegistryException>(
            () => registry.PublishPackageAsync(options, Array.Empty<byte>()));
    }
}