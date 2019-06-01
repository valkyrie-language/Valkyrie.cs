using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Legion;
using Legion.Auth;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class VendorManagerTests
{
    private string GetTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "legion-tests", "vendor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private VendorManager CreateManager(string configDir)
    {
        var sourceManager = new RegistrySourceManager(configDir);
        var authStore = new VendorAuthStore(configDir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(),
            ["jsr"] = new JsrRegistry(),
            ["conda"] = new CondaRegistry(),
            ["maven"] = new MavenRegistry(),
            ["nuget"] = new NuGetRegistry()
        };

        return new VendorManager(sourceManager, authStore, registries);
    }

    [Fact]
    public void AddVendor_AddsNewVendor()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        manager.AddVendor("custom", "https://custom.registry.io");

        var status = manager.GetVendorStatus("custom");
        Assert.NotNull(status);
        Assert.Equal("https://custom.registry.io", status.Endpoint);
    }

    [Fact]
    public void RemoveVendor_RemovesExistingVendor()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        bool removed = manager.RemoveVendor("jsr");

        Assert.True(removed);
        Assert.Null(manager.GetVendorStatus("jsr"));
    }

    [Fact]
    public void ListVendors_ReturnsAllConfiguredVendors()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        var vendors = manager.ListVendors();

        Assert.Equal(5, vendors.Count);

        foreach (var v in vendors)
        {
            Assert.False(v.IsLoggedIn);
        }
    }

    [Fact]
    public async Task LoginAsync_WithValidToken_ReturnsSuccess()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""testuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        var result = await manager.LoginAsync("npm", "valid-test-token");

        Assert.True(result.Success);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("手动输入", result.CredentialSource);
        Assert.True(manager.IsLoggedIn("npm"));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidToken_ReturnsFailure()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.Unauthorized, @"{}");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        var result = await manager.LoginAsync("npm", "invalid-token");

        Assert.False(result.Success);
        Assert.Contains("验证失败", result.ErrorMessage);
        Assert.False(manager.IsLoggedIn("npm"));
    }

    [Fact]
    public async Task LoginAsync_WithoutToken_AttemptsAutoDiscovery()
    {
        string dir = GetTempDir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""auto-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "auto-token", "mock-npmrc");
        manager.CredentialDiscovery.RegisterProvider(fakeProvider);

        var result = await manager.LoginAsync("npm");

        Assert.True(result.Success);
        Assert.Equal("auto-user", result.Username);
        Assert.Contains("mock-npmrc", result.CredentialSource);
    }

    [Fact]
    public async Task LoginAsync_UnknownVendor_ReturnsFailure()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        var result = await manager.LoginAsync("unknown", "token");

        Assert.False(result.Success);
        Assert.Contains("未配置", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_EmptyToken_AutoDiscoveryFallback()
    {
        string dir = GetTempDir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""fallback-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "fallback-token", "mock-fallback-npmrc");
        manager.CredentialDiscovery.RegisterProvider(fakeProvider);

        var result = await manager.LoginAsync("npm", "");

        Assert.True(result.Success);
        Assert.Equal("fallback-user", result.Username);
        Assert.Contains("mock-fallback-npmrc", result.CredentialSource);
    }

    [Fact]
    public async Task LogoutAsync_LoggedInVendor_Success()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.LoginAsync("npm", "token");

        var result = await manager.LogoutAsync("npm");

        Assert.True(result.Success);
        Assert.False(manager.IsLoggedIn("npm"));
    }

    [Fact]
    public async Task LogoutAsync_NotLoggedIn_ReturnsFailure()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        var result = await manager.LogoutAsync("npm");

        Assert.False(result.Success);
        Assert.Contains("未登录", result.ErrorMessage);
    }

    [Fact]
    public async Task WhoAmI_LoggedIn_ReturnsUserInfo()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""myuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.LoginAsync("npm", "token");

        var result = manager.WhoAmI("npm");

        Assert.True(result.Success);
        Assert.Equal("myuser", result.Username);
    }

    [Fact]
    public void WhoAmI_NotLoggedIn_ReturnsFailure()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        var result = manager.WhoAmI("npm");

        Assert.False(result.Success);
        Assert.Contains("未登录", result.ErrorMessage);
    }

    [Fact]
    public void GetVendorStatus_ReturnsCorrectStatus()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        var status = manager.GetVendorStatus("npm");

        Assert.NotNull(status);
        Assert.Equal("npm", status.Name);
        Assert.Equal("https://registry.npmjs.org", status.Endpoint);
        Assert.False(status.IsLoggedIn);
    }

    [Fact]
    public async Task GetToken_ReturnsStoredTokenForPublish()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""pubuser"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.LoginAsync("npm", "publish-token-xyz");

        Assert.Equal("publish-token-xyz", manager.GetToken("npm"));
    }

    [Fact]
    public void IsLoggedIn_NoLogin_ReturnsFalse()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        Assert.False(manager.IsLoggedIn("npm"));
    }

    [Fact]
    public async Task RemoveVendor_AlsoRemovesAuthToken()
    {
        string dir = GetTempDir();
        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""u"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);
        await manager.LoginAsync("npm", "token");
        Assert.True(manager.IsLoggedIn("npm"));

        manager.RemoveVendor("npm");

        Assert.False(manager.IsLoggedIn("npm"));
        Assert.Null(manager.GetVendorStatus("npm"));
    }

    [Fact]
    public void CredentialDiscovery_IsInitialized()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        Assert.NotNull(manager.CredentialDiscovery);
    }

    [Fact]
    public async Task RefreshFromOfficialTool_WithNpmrc_ReturnsSuccess()
    {
        string dir = GetTempDir();

        var handler = new RoutingFakeHttpMessageHandler();
        handler.AddRoute("https://registry.npmjs.org/-/whoami",
            HttpStatusCode.OK, @"{ ""username"": ""refresh-user"" }");

        var httpClient = new HttpClient(handler);
        var sourceManager = new RegistrySourceManager(dir);
        var authStore = new VendorAuthStore(dir);
        var registries = new Dictionary<string, IRegistry>
        {
            ["npm"] = new NpmRegistry(httpClient)
        };

        var manager = new VendorManager(sourceManager, authStore, registries);

        var fakeProvider = new FakeCredentialProvider("npm", "refresh-token", "npmrc-mock");
        manager.CredentialDiscovery.RegisterProvider(fakeProvider);

        var result = await manager.RefreshFromOfficialToolAsync("npm");

        Assert.True(result.Success);
        Assert.Equal("refresh-user", result.Username);
        Assert.Contains("npmrc-mock", result.CredentialSource);
    }

    [Fact]
    public void VendorStatus_IncludesCredentialsInfo()
    {
        string dir = GetTempDir();
        var manager = CreateManager(dir);

        string originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        string originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var status = manager.GetVendorStatus("npm");

            Assert.NotNull(status);
            Assert.Equal("npm", status.Name);
            Assert.False(status.HasOfficialCredentials);
            Assert.Empty(status.AvailableCredentialSources);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }
}