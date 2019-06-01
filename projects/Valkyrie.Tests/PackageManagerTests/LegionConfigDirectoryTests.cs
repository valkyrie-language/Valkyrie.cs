namespace Valkyrie.Tests.PackageManagerTests;

public class LegionConfigDirectoryTests
{
    [Fact]
    public void EnsureExists_ShouldCreateDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var configDir = new LegionConfigDirectory(tempDir);

            Assert.True(Directory.Exists(configDir.ConfigDirectory));
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
    public void WriteAndReadLegionConfig_ShouldPersist()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var configDir = new LegionConfigDirectory(tempDir);
            configDir.WriteLegionConfig("{ \"key\": \"value\" }");

            var content = configDir.ReadLegionConfig();

            Assert.NotNull(content);
            Assert.Contains("value", content);
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