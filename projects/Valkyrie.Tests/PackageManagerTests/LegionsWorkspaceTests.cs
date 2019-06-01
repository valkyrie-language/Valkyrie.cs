namespace Valkyrie.Tests.PackageManagerTests;

public class LegionsWorkspaceTests
{
    [Fact]
    public void CreateAndSave_ShouldPersistWorkspace()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var workspace = new LegionsWorkspace(tempDir);
            workspace.AddMember("packages/core");
            workspace.AddMember("packages/ui");
            workspace.Scripts["build"] = "legion run build --all";
            workspace.Save();

            Assert.True(workspace.Exists());

            var loaded = new LegionsWorkspace(tempDir);
            loaded.Load();

            Assert.Contains("packages/core", loaded.Members);
            Assert.Contains("packages/ui", loaded.Members);
            Assert.Equal("legion run build --all", loaded.GetScript("build"));
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
    public void AddMember_ShouldNotDuplicate()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var workspace = new LegionsWorkspace(tempDir);
            workspace.AddMember("packages/core");
            workspace.AddMember("packages/core");

            Assert.Single(workspace.Members);
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