using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Legion;
using Legion.Package;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

public class PackageCacheTests
{
    private string GetTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "legion-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void AddPackage_NewPackage_AddedToIndex()
    {
        string cacheDir = GetTempDir();
        var cache = new PackageCache(cacheDir);

        cache.AddPackage("test-pkg", "1.0.0", "/some/path");

        Assert.True(cache.HasPackage("test-pkg", "1.0.0"));
    }

    [Fact]
    public void RemovePackage_RemovesFromIndex()
    {
        string cacheDir = GetTempDir();
        var cache = new PackageCache(cacheDir);

        cache.AddPackage("test-pkg", "1.0.0", cacheDir);
        cache.RemovePackage("test-pkg", "1.0.0");

        Assert.False(cache.HasPackage("test-pkg", "1.0.0"));
    }

    [Fact]
    public async Task SaveAndLoad_PreservesData()
    {
        string cacheDir = GetTempDir();
        var cache = new PackageCache(cacheDir);

        cache.AddPackage("pkg-a", "1.0.0", "/path/to/a");
        cache.AddPackage("pkg-b", "2.0.0", "/path/to/b");
        await cache.SaveAsync();

        var cache2 = new PackageCache(cacheDir);
        cache2.Load();

        Assert.True(cache2.HasPackage("pkg-a", "1.0.0"));
        Assert.True(cache2.HasPackage("pkg-b", "2.0.0"));
    }

    [Fact]
    public void Verify_AllFilesExist_ReturnsTrue()
    {
        string cacheDir = GetTempDir();
        string pkgDir = Path.Combine(cacheDir, "test-pkg");
        Directory.CreateDirectory(pkgDir);
        File.WriteAllText(Path.Combine(pkgDir, "test.txt"), "hello");

        var cache = new PackageCache(cacheDir);
        cache.AddPackage("test-pkg", "1.0.0", pkgDir);

        Assert.True(cache.Verify());
    }

    [Fact]
    public void Verify_FileMissing_ReturnsFalse()
    {
        string cacheDir = GetTempDir();
        var cache = new PackageCache(cacheDir);

        cache.AddPackage("test-pkg", "1.0.0", "/nonexistent/path/12345");

        Assert.False(cache.Verify());
    }

    [Fact]
    public void Clean_RemovesAllEntries()
    {
        string cacheDir = GetTempDir();
        var cache = new PackageCache(cacheDir);

        cache.AddPackage("pkg-a", "1.0.0", "/path/a");
        cache.AddPackage("pkg-b", "2.0.0", "/path/b");
        cache.Clean();

        Assert.False(cache.HasPackage("pkg-a", "1.0.0"));
        Assert.False(cache.HasPackage("pkg-b", "2.0.0"));
    }

    [Fact]
    public void List_ReturnsAllCached()
    {
        string cacheDir = GetTempDir();
        string pkgADir = Path.Combine(cacheDir, "pkg-a");
        string pkgBDir = Path.Combine(cacheDir, "pkg-b");
        Directory.CreateDirectory(pkgADir);
        Directory.CreateDirectory(pkgBDir);
        File.WriteAllText(Path.Combine(pkgADir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(pkgBDir, "b.txt"), "b");

        var cache = new PackageCache(cacheDir);
        cache.AddPackage("pkg-a", "1.0.0", pkgADir);
        cache.AddPackage("pkg-b", "2.0.0", pkgBDir);

        var list = cache.List();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.PackageName == "pkg-a" && e.Version == "1.0.0");
        Assert.Contains(list, e => e.PackageName == "pkg-b" && e.Version == "2.0.0");
    }

    [Fact]
    public void GetCacheDir_ReturnsCachedPath()
    {
        string cacheDir = GetTempDir();
        string pkgDir = Path.Combine(cacheDir, "my-pkg");
        Directory.CreateDirectory(pkgDir);
        File.WriteAllText(Path.Combine(pkgDir, "data.txt"), "data");

        var cache = new PackageCache(cacheDir);
        cache.AddPackage("my-pkg", "3.0.0", pkgDir);

        string? result = cache.GetCacheDir("my-pkg", "3.0.0");

        Assert.Equal(pkgDir, result);
    }
}