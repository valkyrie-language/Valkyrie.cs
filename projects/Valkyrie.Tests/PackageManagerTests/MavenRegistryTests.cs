using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class MavenRegistryTests
{
    private static MavenRegistry CreateRegistry(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new MavenRegistry(httpClient);
    }

    [Fact]
    public void ParseMavenCoordinates_ShouldParseColonSeparated()
    {
        MavenRegistry.ParseMavenCoordinates("com.google.guava:guava", out string groupId, out string artifactId);

        Assert.Equal("com.google.guava", groupId);
        Assert.Equal("guava", artifactId);
    }

    [Fact]
    public void ParseMavenCoordinates_ShouldHandleNoColon()
    {
        MavenRegistry.ParseMavenCoordinates("guava", out string groupId, out string artifactId);

        Assert.Equal("guava", groupId);
        Assert.Equal("guava", artifactId);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseMavenSearchResponse()
    {
        var handler = new MockHttpMessageHandler();
        var searchResponse = """
            {
                "response": {
                    "numFound": 1,
                    "docs": [{
                        "g": "com.google.guava",
                        "a": "guava",
                        "v": "33.0.0-jre"
                    }]
                }
            }
            """;

        handler.RegisterJsonResponse("https://search.maven.org/solrsearch/select", searchResponse);

        string pomContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project>
    <dependencies>
        <dependency>
            <groupId>com.google.guava</groupId>
            <artifactId>failureaccess</artifactId>
            <version>1.0.2</version>
        </dependency>
    </dependencies>
</project>";
        handler.RegisterHandler("https://repo1.maven.org/maven2/com/google/guava/guava/", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(pomContent, System.Text.Encoding.UTF8, "application/xml")
            });

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("com.google.guava:guava", "latest");

        Assert.Equal("com.google.guava:guava", package.Name);
        Assert.Equal("33.0.0-jre", package.Version);
        Assert.Equal("com.google.guava", package.Author);
        Assert.NotNull(package.DistTarball);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldThrowOnNotFound()
    {
        var handler = new MockHttpMessageHandler();
        var emptyResponse = """
            {
                "response": {
                    "numFound": 0,
                    "docs": []
                }
            }
            """;

        handler.RegisterJsonResponse("https://search.maven.org/solrsearch/select", emptyResponse);

        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.GetPackageAsync("nonexistent:artifact", "latest"));
    }

    [Fact]
    public async Task SearchPackagesAsync_ShouldParseSearchResponse()
    {
        var handler = new MockHttpMessageHandler();
        var searchResponse = """
            {
                "response": {
                    "numFound": 2,
                    "docs": [
                        { "g": "org.apache.commons", "a": "commons-lang3", "v": "3.14.0" },
                        { "g": "org.apache.commons", "a": "commons-text", "v": "1.11.0" }
                    ]
                }
            }
            """;

        handler.RegisterJsonResponse("https://search.maven.org/solrsearch/select", searchResponse);

        var registry = CreateRegistry(handler);
        var packages = await registry.SearchPackagesAsync("commons");

        Assert.Equal(2, packages.Count);
        Assert.Equal("org.apache.commons:commons-lang3", packages[0].Name);
        Assert.Equal("3.14.0", packages[0].Version);
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldRequireAuthToken()
    {
        var handler = new MockHttpMessageHandler();
        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.PublishPackageAsync(new PublishOptions
            {
                PackageName = "com.example:test",
                Version = "1.0.0"
            }, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task PublishPackageAsync_ShouldReturnSuccessOnOk()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://s01.oss.sonatype.org/service/local/staging/deploy/maven2", _ =>
            new HttpResponseMessage(HttpStatusCode.Created));

        var registry = CreateRegistry(handler);
        var result = await registry.PublishPackageAsync(new PublishOptions
        {
            PackageName = "com.example:test",
            Version = "1.0.0",
            AuthToken = "test-token"
        }, new byte[] { 1, 2, 3 });

        Assert.True(result.Success);
        Assert.Contains("Maven", result.Message!);
    }
}
