namespace Valkyrie.Tests.PackageManagerTests;

public class LegionManifestTests
{
    [Fact]
    public void CreateAndSave_ShouldPersistManifest()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var manifest = new LegionManifest(tempDir)
            {
                Name = "test-pkg",
                Version = "1.0.0",
                Description = "A test package",
                License = "MIT"
            };
            manifest.AddDependency("lodash", "^4.17.0");
            manifest.AddDependency("react", "^18.0.0", "dev");
            manifest.Save();

            Assert.True(manifest.Exists());

            var loaded = new LegionManifest(tempDir);
            loaded.Load();

            Assert.Equal("test-pkg", loaded.Name);
            Assert.Equal("1.0.0", loaded.Version);
            Assert.Equal("MIT", loaded.License);
            Assert.True(loaded.Dependencies.ContainsKey("lodash"));
            Assert.True(loaded.DevDependencies.ContainsKey("react"));
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
    public void Validate_ShouldReturnTrueForValidManifest()
    {
        var manifest = new LegionManifest(Path.GetTempPath())
        {
            Name = "test-pkg",
            Version = "1.0.0"
        };

        Assert.True(manifest.Validate());
    }

    [Fact]
    public void Validate_ShouldReturnFalseForEmptyName()
    {
        var manifest = new LegionManifest(Path.GetTempPath())
        {
            Name = "",
            Version = "1.0.0"
        };

        Assert.False(manifest.Validate());
    }

    [Fact]
    public void Validate_ShouldReturnFalseForInvalidVersion()
    {
        var manifest = new LegionManifest(Path.GetTempPath())
        {
            Name = "test-pkg",
            Version = "not-a-version"
        };

        Assert.False(manifest.Validate());
    }

    [Fact]
    public void GetScript_ShouldReturnScriptContent()
    {
        var manifest = new LegionManifest(Path.GetTempPath())
        {
            Name = "test-pkg",
            Version = "1.0.0"
        };
        manifest.Scripts["build"] = "vcc build";

        Assert.Equal("vcc build", manifest.GetScript("build"));
    }

    [Fact]
    public void HasScript_ShouldReturnFalseForMissingScript()
    {
        var manifest = new LegionManifest(Path.GetTempPath())
        {
            Name = "test-pkg",
            Version = "1.0.0"
        };

        Assert.False(manifest.HasScript("nonexistent"));
    }
}