namespace Valkyrie.Tests.PackageManagerTests;

public class ScriptRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldExecuteCommand()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var runner = new ScriptRunner(tempDir);

            var result = await runner.RunAsync("echo hello");

            Assert.True(result.Success);
            Assert.Contains("hello", result.Output);
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
    public async Task RunScriptAsync_ShouldReturnErrorForMissingScript()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var manifest = new LegionManifest(tempDir)
            {
                Name = "test",
                Version = "1.0.0"
            };
            var runner = new ScriptRunner(tempDir);

            var result = await runner.RunScriptAsync(manifest, "nonexistent");

            Assert.False(result.Success);
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