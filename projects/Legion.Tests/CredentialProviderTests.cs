using System;
using System.IO;
using System.Threading.Tasks;
using Legion;
using Legion.Auth;
using Legion.CredentialProviders;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class CredentialProviderTests
{
    private string GetTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "legion-tests", "cred-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    #region NpmNpmrcProvider

    [Fact]
    public async Task NpmNpmrc_AuthToken_ParsedCorrectly()
    {
        string dir = GetTempDir();
        string npmrcPath = Path.Combine(dir, ".npmrc");
        File.WriteAllText(npmrcPath, "//registry.npmjs.org/:_authToken=npm_test_token_123\n");

        string originalHome = Environment.GetEnvironmentVariable("USERPROFILE")
                              ?? Environment.GetEnvironmentVariable("HOME") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var provider = new NpmNpmrcProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("npm_test_token_123", credential.Token);
            Assert.Contains(".npmrc", credential.Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            File.Delete(npmrcPath);
        }
    }

    [Fact]
    public async Task NpmNpmrc_NoFile_ReturnsNull()
    {
        string dir = GetTempDir();
        string originalHome = Environment.GetEnvironmentVariable("USERPROFILE")
                              ?? Environment.GetEnvironmentVariable("HOME") ?? "";
        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var provider = new NpmNpmrcProvider();
            Assert.False(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
        }
    }

    #endregion

    #region NpmEnvProvider

    [Fact]
    public async Task NpmEnv_NodeAuthToken_Found()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", "env-npm-token-abc");

        try
        {
            var provider = new NpmEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("env-npm-token-abc", credential.Token);
            Assert.Contains("NODE_AUTH_TOKEN", credential.Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public async Task NpmEnv_NoToken_FallsThrough()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);

        var provider = new NpmEnvProvider();
        Assert.False(provider.IsAvailable());

        var credential = await provider.DiscoverAsync();
        Assert.Null(credential);
    }

    #endregion

    #region JsrDenoConfigProvider

    [Fact]
    public async Task JsrDenoConfig_TokenFound()
    {
        string dir = GetTempDir();
        string denoDir = Path.Combine(dir, "deno");
        Directory.CreateDirectory(denoDir);
        string configPath = Path.Combine(denoDir, "config.json");
        File.WriteAllText(configPath, @"{ ""https://jsr.io"": ""jsr-config-token-xyz"", ""net.jsr"": ""alt-token"" }");

        string originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var provider = new JsrDenoConfigProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("alt-token", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    [Fact]
    public async Task JsrDenoConfig_NoFile_ReturnsNull()
    {
        string dir = GetTempDir();
        string originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var provider = new JsrDenoConfigProvider();
            Assert.False(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    #endregion

    #region JsrEnvProvider

    [Fact]
    public async Task JsrEnv_JsrToken_Found()
    {
        Environment.SetEnvironmentVariable("JSR_TOKEN", "jsr-env-token");

        try
        {
            var provider = new JsrEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("jsr-env-token", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("JSR_TOKEN", null);
        }
    }

    [Fact]
    public async Task JsrEnv_DenoAuthTokens_Found()
    {
        Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", "npm@token1;jsr@jsr-from-deno-token;other@token3");

        try
        {
            var provider = new JsrEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("jsr-from-deno-token", credential.Token);
            Assert.Contains("DENO_AUTH_TOKENS", credential.Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", null);
        }
    }

    #endregion

    #region CondaEnvProvider

    [Fact]
    public async Task CondaEnv_AnacondaApiToken_Found()
    {
        Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", "conda-api-token");

        try
        {
            var provider = new CondaEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("conda-api-token", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", null);
        }
    }

    #endregion

    #region MavenEnvProvider

    [Fact]
    public async Task MavenEnv_MavenToken_Found()
    {
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", "maven-env-token");

        try
        {
            var provider = new MavenEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("maven-env-token", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);
        }
    }

    #endregion

    #region NuGetEnvProvider

    [Fact]
    public async Task NuGetEnv_NuGetApiKey_Found()
    {
        Environment.SetEnvironmentVariable("NUGET_API_KEY", "nuget-api-key-123");

        try
        {
            var provider = new NuGetEnvProvider();
            Assert.True(provider.IsAvailable());

            var credential = await provider.DiscoverAsync();
            Assert.NotNull(credential);
            Assert.Equal("nuget-api-key-123", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_API_KEY", null);
        }
    }

    #endregion

    #region CredentialDiscoveryManager

    [Fact]
    public async Task DiscoveryManager_WithEnvToken_DiscoversNpm()
    {
        Environment.SetEnvironmentVariable("DISCOVERY_NPM_TOKEN", "discovery-npm-token");

        var provider = new FakeNpmEnvProvider("discovery-npm-token");
        var manager = new CredentialDiscoveryManager();
        manager.RegisterProvider(provider);

        try
        {
            var credential = await manager.DiscoverAsync("npm");

            Assert.NotNull(credential);
            Assert.Equal("discovery-npm-token", credential.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DISCOVERY_NPM_TOKEN", null);
        }
    }

    [Fact]
    public async Task DiscoveryManager_NothingAvailable_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);
        Environment.SetEnvironmentVariable("JSR_TOKEN", null);
        Environment.SetEnvironmentVariable("DENO_AUTH_TOKENS", null);
        Environment.SetEnvironmentVariable("ANACONDA_API_TOKEN", null);
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);
        Environment.SetEnvironmentVariable("NUGET_API_KEY", null);

        string dir = GetTempDir();
        string originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        string originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";

        Environment.SetEnvironmentVariable("USERPROFILE", dir);
        Environment.SetEnvironmentVariable("APPDATA", dir);

        try
        {
            var manager = new CredentialDiscoveryManager();
            manager.RegisterBuiltinProviders();

            var credential = await manager.DiscoverAsync("npm");

            Assert.Null(credential);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
        }
    }

    [Fact]
    public void DiscoveryManager_GetAvailableProviders_EmptyWhenNone()
    {
        Environment.SetEnvironmentVariable("NODE_AUTH_TOKEN", null);
        Environment.SetEnvironmentVariable("NPM_TOKEN", null);
        Environment.SetEnvironmentVariable("MAVEN_TOKEN", null);

        string dir = GetTempDir();
        string originalHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";

        Environment.SetEnvironmentVariable("USERPROFILE", dir);

        try
        {
            var manager = new CredentialDiscoveryManager();
            manager.RegisterBuiltinProviders();

            var available = manager.GetAvailableProviders("maven");

            Assert.Empty(available);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", originalHome);
        }
    }

    [Fact]
    public void DiscoveryManager_ProviderPriority_OrderedByPriority()
    {
        var manager = new CredentialDiscoveryManager();
        manager.RegisterBuiltinProviders();

        var npmProviders = manager.GetAvailableProviders("npm");

        if (npmProviders.Count >= 2)
        {
            for (int i = 1; i < npmProviders.Count; i++)
            {
                Assert.True(npmProviders[i - 1].Priority <= npmProviders[i].Priority);
            }
        }
    }

    #endregion
}