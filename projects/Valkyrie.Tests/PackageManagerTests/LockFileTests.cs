namespace Valkyrie.Tests.PackageManagerTests;

public class LockFileTests
{
    [Fact]
    public void AddPackage_ShouldAddEntry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var lockFile = new LockFile(tempDir);

            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string>() };
            lockFile.AddPackage(package, "npm", "https://registry.npmjs.org");

            Assert.True(lockFile.IsPackageLocked("test-pkg", "1.0.0"));
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
    public void AddPackage_ShouldUseRealSha512Integrity()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var lockFile = new LockFile(tempDir);

            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string>() };
            lockFile.AddPackage(package, "npm", "https://registry.npmjs.org");

            var entry = lockFile.GetPackage("test-pkg", "1.0.0");
            Assert.NotNull(entry);
            Assert.StartsWith("sha512-", entry.Integrity);
            Assert.NotEqual("sha512-", entry.Integrity);
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
    public void AddPackage_ShouldUseDistIntegrityWhenAvailable()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var lockFile = new LockFile(tempDir);

            var package = new Package
            {
                Name = "test-pkg",
                Version = "1.0.0",
                Dependencies = new List<string>(),
                DistIntegrity = "sha512-abc123"
            };
            lockFile.AddPackage(package, "npm", "https://registry.npmjs.org");

            var entry = lockFile.GetPackage("test-pkg", "1.0.0");
            Assert.NotNull(entry);
            Assert.Equal("sha512-abc123", entry.Integrity);
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
    public void RemovePackage_ShouldRemoveEntry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var lockFile = new LockFile(tempDir);

            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string>() };
            lockFile.AddPackage(package, "npm", "https://registry.npmjs.org");
            lockFile.RemovePackage("test-pkg");

            Assert.False(lockFile.IsPackageLocked("test-pkg", "1.0.0"));
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
    public void SaveAndLoad_ShouldPersistData()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var lockFile = new LockFile(tempDir);
            var package = new Package { Name = "test-pkg", Version = "1.0.0", Dependencies = new List<string> { "dep1" } };
            lockFile.AddPackage(package, "npm", "https://registry.npmjs.org");
            lockFile.Save();

            var loadedLockFile = new LockFile(tempDir);
            loadedLockFile.Load();

            Assert.True(loadedLockFile.IsPackageLocked("test-pkg", "1.0.0"));
            var entry = loadedLockFile.GetPackage("test-pkg", "1.0.0");
            Assert.NotNull(entry);
            Assert.Equal("npm", entry.Registry);
            Assert.Contains("dep1", entry.Dependencies);
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