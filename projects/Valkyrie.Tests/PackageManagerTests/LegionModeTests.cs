namespace Valkyrie.Tests.PackageManagerTests;

public class LegionModeTests
{
    [Fact]
    public void Constructor_WithWorkspaceFile_ShouldDetectWorkspaceMode()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var workspace = new LegionsWorkspace(tempDir);
            workspace.Save();

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.True(legion.IsWorkspace);
                Assert.False(legion.HasManifest);
                Assert.False(legion.IsStandalone);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public void Constructor_WithManifestFile_ShouldDetectPackageMode()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var manifest = new LegionManifest(tempDir)
            {
                Name = "test-pkg",
                Version = "1.0.0"
            };
            manifest.Save();

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.False(legion.IsWorkspace);
                Assert.True(legion.HasManifest);
                Assert.False(legion.IsStandalone);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public void Constructor_WithNoManifest_ShouldDetectStandaloneMode()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.False(legion.IsWorkspace);
                Assert.False(legion.HasManifest);
                Assert.True(legion.IsStandalone);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public void InstallAsync_InPackageMode_ShouldUseManifestDependencies()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var manifest = new LegionManifest(tempDir)
            {
                Name = "test-pkg",
                Version = "1.0.0"
            };
            manifest.AddDependency("test-dep", "1.0.0");
            manifest.Save();

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.True(legion.HasManifest);
                Assert.False(legion.IsWorkspace);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public async Task InstallAsync_InWorkspaceMode_ShouldReturnEmptyWithoutMembers()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var workspace = new LegionsWorkspace(tempDir);
            workspace.Save();

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.True(legion.IsWorkspace);

                var result = await legion.InstallAsync();
                Assert.Empty(result);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public async Task RunAsync_InStandaloneMode_ShouldExecuteDirectCommand()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.True(legion.IsStandalone);

                var result = await legion.RunAsync("echo test");
                Assert.True(result.Success);
                Assert.Contains("test", result.Output);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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
    public async Task RunAsync_InPackageMode_ShouldExecuteManifestScript()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var manifest = new LegionManifest(tempDir)
            {
                Name = "test-pkg",
                Version = "1.0.0"
            };
            manifest.Scripts["test"] = "echo hello";
            manifest.Save();

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                var legion = new Legion();
                Assert.True(legion.HasManifest);

                var result = await legion.RunAsync("test");
                Assert.True(result.Success);
                Assert.Contains("hello", result.Output);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
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