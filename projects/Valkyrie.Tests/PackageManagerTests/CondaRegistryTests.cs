using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class CondaRegistryTests
{
    private static CondaRegistry CreateRegistry(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new CondaRegistry(httpClient);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldParseCondaResponse()
    {
        var handler = new MockHttpMessageHandler();
        var condaResponse = """
            {
                "name": "numpy",
                "summary": "Array processing for numbers",
                "latest_version": "1.24.0",
                "license": "BSD-3-Clause",
                "home": "https://numpy.org",
                "files": [{
                    "download_url": "https://conda.anaconda.org/conda-forge/numpy-1.24.0-py310h2e18e57_0.conda"
                }]
            }
            """;

        handler.RegisterJsonResponse("https://api.anaconda.org/package/conda-forge/numpy", condaResponse);

        var registry = CreateRegistry(handler);
        var package = await registry.GetPackageAsync("numpy", "latest");

        Assert.Equal("numpy", package.Name);
        Assert.Equal("1.24.0", package.Version);
        Assert.Equal("BSD-3-Clause", package.License);
        Assert.NotNull(package.DistTarball);
    }

    [Fact]
    public async Task GetPackageAsync_ShouldThrowOnNotFound()
    {
        var handler = new MockHttpMessageHandler();
        handler.RegisterHandler("https://api.anaconda.org/package/conda-forge/nonexistent", _ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var registry = CreateRegistry(handler);

        await Assert.ThrowsAsync<RegistryException>(() =>
            registry.GetPackageAsync("nonexistent", "latest"));
    }
}
