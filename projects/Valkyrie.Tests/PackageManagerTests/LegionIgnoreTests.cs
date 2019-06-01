namespace Valkyrie.Tests.PackageManagerTests;

public class LegionIgnoreTests
{
    [Fact]
    public void IsIgnored_ShouldMatchPattern()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var ignore = new LegionIgnore(tempDir);
            ignore.AddPattern("*.log");
            ignore.AddPattern("build/");
            ignore.Save();

            Assert.True(ignore.IsIgnored("debug.log"));
            Assert.True(ignore.IsIgnored("build/output.js"));
            Assert.False(ignore.IsIgnored("src/main.js"));
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
    public void CreateDefault_ShouldCreateStandardPatterns()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var ignore = LegionIgnore.CreateDefault(tempDir);

            Assert.True(ignore.Exists());
            Assert.True(ignore.IsIgnored("build/"));
            Assert.True(ignore.IsIgnored("vendors/"));
            Assert.True(ignore.IsIgnored(".cache/"));
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