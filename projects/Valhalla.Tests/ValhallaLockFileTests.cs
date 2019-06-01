using Xunit;
using Valhalla.Client;

namespace Valhalla.Tests;

public class ValhallaLockFileTests
{
    private string GetTempProjectDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"valhalla-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Exists_新实例_返回假()
    {
        string dir = GetTempProjectDir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            Assert.False(lockFile.Exists());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FilePath_正确拼接()
    {
        var lockFile = new ValhallaLockFile("/project");
        string expected = Path.Combine("/project", "protoswap.lock");
        Assert.Equal(expected, lockFile.FilePath);
    }

    [Fact]
    public async Task 添加条目后保存_可重新加载()
    {
        string dir = GetTempProjectDir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry
            {
                Incarnation = 3,
                Publisher = "abc123",
                Version = "1.2.0",
                Sha256 = "abcdef",
                Registry = "https://valhalla.example.com"
            });

            await lockFile.SaveAsync();
            Assert.True(lockFile.Exists());

            var reloaded = new ValhallaLockFile(dir);
            await reloaded.LoadAsync();

            var entry = reloaded.GetEntry("test.pkg");
            Assert.NotNull(entry);
            Assert.Equal(3, entry!.Incarnation);
            Assert.Equal("abc123", entry.Publisher);
            Assert.Equal("1.2.0", entry.Version);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task 删除条目_报不满足存在()
    {
        string dir = GetTempProjectDir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry { Version = "1.0.0" });
            await lockFile.SaveAsync();

            lockFile.Remove("test.pkg");
            await lockFile.SaveAsync();

            var reloaded = new ValhallaLockFile(dir);
            await reloaded.LoadAsync();

            Assert.Null(reloaded.GetEntry("test.pkg"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void IsVersionLocked_版本匹配_返回真()
    {
        var lockFile = new ValhallaLockFile("/project");
        lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry { Version = "2.0.0" });

        Assert.True(lockFile.IsVersionLocked("test.pkg", "2.0.0"));
    }

    [Fact]
    public void IsVersionLocked_版本不匹配_返回假()
    {
        var lockFile = new ValhallaLockFile("/project");
        lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry { Version = "2.0.0" });

        Assert.False(lockFile.IsVersionLocked("test.pkg", "1.0.0"));
    }

    [Fact]
    public void 删除不存在条目_返回假()
    {
        var lockFile = new ValhallaLockFile("/project");
        Assert.False(lockFile.Remove("nonexistent"));
    }

    [Fact]
    public async Task 多次保存_UpdatedAt更新()
    {
        string dir = GetTempProjectDir();
        try
        {
            var lockFile = new ValhallaLockFile(dir);
            lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry { Version = "1.0.0" });
            await lockFile.SaveAsync();

            var first = new ValhallaLockFile(dir);
            await first.LoadAsync();
            DateTime firstTime = first.GetContent()!.UpdatedAt;

            await Task.Delay(10);

            lockFile.AddOrUpdate("test.pkg", new ValhallaLockEntry { Version = "2.0.0" });
            await lockFile.SaveAsync();

            var second = new ValhallaLockFile(dir);
            await second.LoadAsync();
            DateTime secondTime = second.GetContent()!.UpdatedAt;

            Assert.True(secondTime > firstTime);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}