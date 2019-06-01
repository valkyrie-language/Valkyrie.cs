using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Legion;
using Legion.Auth;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class RegistrySourceManagerTests
{
    private string GetTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "legion-tests", "rsm-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Constructor_InitializesDefaultSources()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        Assert.NotNull(manager.GetEndpoint("npm"));
        Assert.NotNull(manager.GetEndpoint("jsr"));
        Assert.NotNull(manager.GetEndpoint("conda"));
        Assert.NotNull(manager.GetEndpoint("maven"));
        Assert.NotNull(manager.GetEndpoint("nuget"));
    }

    [Fact]
    public void DefaultSources_HaveAllFive()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);
        manager.Load();

        Assert.Equal(5, manager.Sources.Count);
        Assert.Equal("https://registry.npmjs.org", manager.GetEndpoint("npm"));
        Assert.Equal("https://jsr.io", manager.GetEndpoint("jsr"));
        Assert.Equal("https://api.anaconda.org", manager.GetEndpoint("conda"));
        Assert.Equal("https://search.maven.org", manager.GetEndpoint("maven"));
        Assert.Equal("https://api.nuget.org/v3", manager.GetEndpoint("nuget"));
    }

    [Fact]
    public void SetEndpoint_UpdatesSource()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        manager.SetEndpoint("npm", "https://registry.npmmirror.com");

        Assert.Equal("https://registry.npmmirror.com", manager.GetEndpoint("npm"));
    }

    [Fact]
    public void RemoveEndpoint_RemovesSource()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        bool removed = manager.RemoveEndpoint("jsr");

        Assert.True(removed);
        Assert.Null(manager.GetEndpoint("jsr"));
    }

    [Fact]
    public void RemoveEndpoint_CaseInsensitive()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        bool removed = manager.RemoveEndpoint("NPM");

        Assert.True(removed);
        Assert.Null(manager.GetEndpoint("npm"));
    }

    [Fact]
    public async Task SaveAndLoad_PreservesCustomEndpoints()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        manager.SetEndpoint("npm", "https://custom-registry.example.com");
        manager.SetEndpoint("maven", "https://maven.example.com");
        manager.RemoveEndpoint("jsr");
        await manager.SaveAsync();

        var loaded = new RegistrySourceManager(configDir);
        loaded.Load();

        Assert.Equal("https://custom-registry.example.com", loaded.GetEndpoint("npm"));
        Assert.Equal("https://maven.example.com", loaded.GetEndpoint("maven"));
        Assert.Null(loaded.GetEndpoint("jsr"));
    }

    [Fact]
    public void CreateRegistry_Npm_CreatesNpmRegistry()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        var registry = manager.CreateRegistry("npm");

        Assert.IsType<NpmRegistry>(registry);
        Assert.Equal("https://registry.npmjs.org", registry.Endpoint);
    }

    [Fact]
    public void CreateRegistry_WithCustomEndpoint_UsesCustomEndpoint()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);
        manager.SetEndpoint("npm", "https://custom.registry.io");

        var registry = manager.CreateRegistry("npm");

        Assert.Equal("https://custom.registry.io", registry.Endpoint);
    }

    [Fact]
    public void CreateRegistry_Unknown_Throws()
    {
        string configDir = GetTempDir();
        var manager = new RegistrySourceManager(configDir);

        Assert.Throws<System.ArgumentException>(() => manager.CreateRegistry("unknown"));
    }
}