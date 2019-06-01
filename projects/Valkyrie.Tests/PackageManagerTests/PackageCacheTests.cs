namespace Valkyrie.Tests.PackageManagerTests;

public class PackageCacheTests
{
    [Fact]
    public void HasPackage_ShouldReturnFalseForMissingPackage()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var cache = new PackageCache(tempDir);

            Assert.False(cache.HasPackage("nonexistent", "1.0.0"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void AddPackage_ShouldCreateCacheEntry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            string sourcePath = Path.Combine(tempDir, "source");
            Directory.CreateDirectory(sourcePath);
            File.WriteAllText(Path.Combine(sourcePath, "index.js"), "test");

            var cache = new PackageCache(tempDir);
            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string>() };

            cache.AddPackage("test-pkg", "1.0.0", sourcePath);

            Assert.True(cache.HasPackage("test-pkg", "1.0.0"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void RemovePackage_ShouldRemoveCacheEntry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            string sourcePath = Path.Combine(tempDir, "source");
            Directory.CreateDirectory(sourcePath);

            var cache = new PackageCache(tempDir);
            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string>() };
            cache.AddPackage("test-pkg", "1.0.0", sourcePath);

            cache.RemovePackage("test-pkg", "1.0.0");

            Assert.False(cache.HasPackage("test-pkg", "1.0.0"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}