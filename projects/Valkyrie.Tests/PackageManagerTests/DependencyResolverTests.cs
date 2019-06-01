namespace Valkyrie.Tests.PackageManagerTests;

public class DependencyResolverTests
{
    private static Dictionary<string, IRegistry> CreateMockRegistries()
    {
        var handler = new MockHttpMessageHandler();

        var npmFullResponse = """
            {
                "name": "test-package",
                "description": "Test package",
                "license": "MIT",
                "dist-tags": { "latest": "1.0.0" },
                "versions": {
                    "1.0.0": {
                        "name": "test-package",
                        "version": "1.0.0",
                        "dependencies": { "dep-a": "^2.0.0" }
                    }
                }
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/test-package", npmFullResponse);

        var npmVersionResponse = """
            {
                "name": "test-package",
                "version": "1.0.0",
                "description": "Test package",
                "license": "MIT",
                "dependencies": { "dep-a": "^2.0.0" }
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/test-package/1.0.0", npmVersionResponse);

        var depFullResponse = """
            {
                "name": "dep-a",
                "description": "Dependency A",
                "license": "MIT",
                "dist-tags": { "latest": "2.0.0" },
                "versions": {
                    "2.0.0": {
                        "name": "dep-a",
                        "version": "2.0.0",
                        "dependencies": {}
                    }
                }
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/dep-a", depFullResponse);

        var depVersionResponse = """
            {
                "name": "dep-a",
                "version": "2.0.0",
                "description": "Dependency A",
                "license": "MIT",
                "dependencies": {}
            }
            """;

        handler.RegisterJsonResponse("https://registry.npmjs.org/dep-a/2.0.0", depVersionResponse);
        handler.RegisterJsonResponse("https://registry.npmjs.org/dep-a/%5E2.0.0", depFullResponse);

        var httpClient = new HttpClient(handler);
        var registry = new NpmRegistry(httpClient);

        return new Dictionary<string, IRegistry> { { "npm", registry } };
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnDependencyNode()
    {
        var registries = CreateMockRegistries();
        var resolver = new DependencyResolver(registries);
        var node = await resolver.ResolveAsync("test-package", "1.0.0", "npm");

        Assert.Equal("test-package", node.PackageName);
        Assert.Equal("1.0.0", node.Version);
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveDependencies()
    {
        var registries = CreateMockRegistries();
        var resolver = new DependencyResolver(registries);
        var node = await resolver.ResolveAsync("test-package", "1.0.0", "npm");

        Assert.NotEmpty(node.Dependencies);
    }
}
