using System;
using System.IO;
using System.Threading.Tasks;
using Legion;
using Legion.Auth;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class VendorAuthStoreTests
{
    private string GetTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "legion-tests", "auth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void IsLoggedIn_NoToken_ReturnsFalse()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        Assert.False(store.IsLoggedIn("npm"));
    }

    [Fact]
    public void SaveToken_ThenIsLoggedIn_ReturnsTrue()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "test-token-12345");

        Assert.True(store.IsLoggedIn("npm"));
    }

    [Fact]
    public void GetToken_ReturnsStoredToken()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "my-secret-token");

        string? token = store.GetToken("npm");
        Assert.Equal("my-secret-token", token);
    }

    [Fact]
    public void GetToken_NoToken_ReturnsNull()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        string? token = store.GetToken("npm");
        Assert.Null(token);
    }

    [Fact]
    public void RemoveToken_RemovesAuthInfo()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "token");
        bool removed = store.RemoveToken("npm");

        Assert.True(removed);
        Assert.False(store.IsLoggedIn("npm"));
        Assert.Null(store.GetToken("npm"));
    }

    [Fact]
    public void GetAuthInfo_StoresUserAndExpiry()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);
        DateTime expiry = DateTime.UtcNow.AddDays(30);

        store.SaveToken("npm", "https://registry.npmjs.org", "token", "testuser", expiry);

        var info = store.GetAuthInfo("npm");
        Assert.NotNull(info);
        Assert.Equal("testuser", info.CurrentUser);
        Assert.Equal(expiry, info.ExpiresAt);
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        var info = new VendorAuthInfo
        {
            VendorName = "npm",
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(info.IsExpired);
        Assert.False(info.IsLoggedIn);
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        var info = new VendorAuthInfo
        {
            VendorName = "npm",
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        Assert.False(info.IsExpired);
        Assert.True(info.IsLoggedIn);
    }

    [Fact]
    public void ObfuscateDeobfuscate_RoundTrip()
    {
        string original = "npm_abc123def456ghi789";

        string obfuscated = VendorAuthStore.ObfuscateToken(original);
        string recovered = VendorAuthStore.DeobfuscateToken(obfuscated);

        Assert.NotEqual(original, obfuscated);
        Assert.Equal(original, recovered);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesTokens()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "npm-token-123");
        store.SaveToken("jsr", "https://jsr.io", "jsr-token-456");
        await store.SaveAsync();

        var loaded = new VendorAuthStore(dir);
        loaded.Load();

        Assert.True(loaded.IsLoggedIn("npm"));
        Assert.True(loaded.IsLoggedIn("jsr"));
        Assert.Equal("npm-token-123", loaded.GetToken("npm"));
        Assert.Equal("jsr-token-456", loaded.GetToken("jsr"));
    }

    [Fact]
    public void GetToken_ExpiredToken_ReturnsNull()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "old-token", null, DateTime.UtcNow.AddDays(-5));

        Assert.False(store.IsLoggedIn("npm"));
        Assert.Null(store.GetToken("npm"));
    }

    [Fact]
    public void MultipleVendors_ManagedIndependently()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "n-token");
        store.SaveToken("nuget", "https://api.nuget.org/v3", "g-token");

        Assert.True(store.IsLoggedIn("npm"));
        Assert.True(store.IsLoggedIn("nuget"));

        store.RemoveToken("npm");

        Assert.False(store.IsLoggedIn("npm"));
        Assert.True(store.IsLoggedIn("nuget"));
    }

    [Fact]
    public void CaseInsensitive_VendorName()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("NPM", "https://registry.npmjs.org", "token");

        Assert.True(store.IsLoggedIn("npm"));
        Assert.Equal("token", store.GetToken("NPM"));
    }

    [Fact]
    public async Task SaveAsync_OnlyWritesLoggedInVendors()
    {
        string dir = GetTempDir();
        var store = new VendorAuthStore(dir);

        store.SaveToken("npm", "https://registry.npmjs.org", "active-token");
        store.SaveToken("jsr", "https://jsr.io", "expired-token", null, DateTime.UtcNow.AddDays(-1));
        await store.SaveAsync();

        var loaded = new VendorAuthStore(dir);
        loaded.Load();

        Assert.True(loaded.IsLoggedIn("npm"));
        Assert.False(loaded.IsLoggedIn("jsr"));
    }
}