using VOA.ToolChain.Compiler;
using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class PwaGeneratorTests
{
    [Fact]
    public void GenerateServiceWorker_ContainsCacheName()
    {
        var sw = PwaGenerator.GenerateServiceWorker("test-app", ["index.html", "app.js"]);
        Assert.Contains("test-app-v1", sw);
        Assert.Contains("index.html", sw);
        Assert.Contains("app.js", sw);
    }

    [Fact]
    public void GenerateServiceWorker_HasInstallHandler()
    {
        var sw = PwaGenerator.GenerateServiceWorker("app", []);
        Assert.Contains("install", sw);
        Assert.Contains("activate", sw);
        Assert.Contains("fetch", sw);
    }

    [Fact]
    public void GenerateManifest_ContainsAppName()
    {
        var manifest = PwaGenerator.GenerateManifest("My App", "MyApp");
        Assert.Contains("My App", manifest);
        Assert.Contains("MyApp", manifest);
        Assert.Contains("standalone", manifest);
    }

    [Fact]
    public void GenerateRegistrationScript_ContainsServiceWorkerRegistration()
    {
        var script = PwaGenerator.GenerateRegistrationScript();
        Assert.Contains("serviceWorker", script);
        Assert.Contains("register", script);
    }
}