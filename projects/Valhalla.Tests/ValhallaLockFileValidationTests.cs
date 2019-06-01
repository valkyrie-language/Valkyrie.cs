using Valhalla.Client;
using Xunit;

namespace Valhalla.Tests;

public class ValhallaLockFileValidationTests
{
    private const string TestPackageName = "test.pkg";
    private const string TestVersion = "1.0.0";
    private const string PublisherA = "aaaa1111bbbb2222cccc3333dddd4444eeee5555ffff6666gggg7777hhhh";
    private const string PublisherB = "zzzz9999yyyy8888xxxx7777wwww6666vvvv5555uuuu4444tttt3333ssss";

    private static (byte[] PackageData, ValhallaDigest Digest) CreateTestPackageData()
    {
        byte[] data = new byte[256];
        new Random(42).NextBytes(data);
        var digest = ValhallaDigest.Compute(data);
        return (data, digest);
    }

    private static ValhallaLockFile CreateLockFile(string publisher, int incarnation, string sha256)
    {
        var lockFile = new ValhallaLockFile("/test/project");
        lockFile.AddOrUpdate(TestPackageName, new ValhallaLockEntry
        {
            Publisher = publisher,
            Incarnation = incarnation,
            Version = TestVersion,
            Sha256 = sha256,
            Registry = "https://valhalla.example.com"
        });
        return lockFile;
    }

    private static PackageManifest CreateManifest(string publisher, int incarnation)
    {
        return new PackageManifest
        {
            Name = TestPackageName,
            Incarnation = incarnation,
            Publisher = publisher
        };
    }

    [Fact]
    public void Incarnation不匹配_远程更大_校验失败()
    {
        var (packageData, _) = CreateTestPackageData();
        var lockFile = CreateLockFile(PublisherA, 1, "any");
        var manifest = CreateManifest(PublisherA, 3);

        var downloadResult = new ValhallaDownloadResult
        {
            PackageName = TestPackageName,
            Version = TestVersion,
            PackageData = packageData
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.ValidateAgainstLock(
            lockFile, manifest, downloadResult, TestPackageName, TestVersion);

        Assert.False(passed);
        Assert.Contains("PURGE", error);
    }

    [Fact]
    public void Publisher不匹配_校验失败()
    {
        var (packageData, _) = CreateTestPackageData();
        var lockFile = CreateLockFile(PublisherA, 1, "any");

        // 远程 manifest 中 publisher 与锁文件不同
        var manifest = CreateManifest(PublisherB, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            PackageName = TestPackageName,
            Version = TestVersion,
            PackageData = packageData
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.ValidateAgainstLock(
            lockFile, manifest, downloadResult, TestPackageName, TestVersion);

        Assert.False(passed);
        Assert.Contains("发布者", error);
    }

    [Fact]
    public void SHA256不匹配_校验失败()
    {
        var (packageData, digest) = CreateTestPackageData();
        var wrongSha256 = "0000000000000000000000000000000000000000000000000000000000000000";
        var lockFile = CreateLockFile(PublisherA, 1, wrongSha256);
        var manifest = CreateManifest(PublisherA, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            PackageName = TestPackageName,
            Version = TestVersion,
            PackageData = packageData,
            PackageSha256 = digest.HexString
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.ValidateAgainstLock(
            lockFile, manifest, downloadResult, TestPackageName, TestVersion);

        Assert.False(passed);
        Assert.Contains("SHA-256", error);
        Assert.Contains("锁文件", error);
    }

    [Fact]
    public void 所有匹配_校验通过()
    {
        var (packageData, digest) = CreateTestPackageData();
        var lockFile = CreateLockFile(PublisherA, 1, digest.HexString);
        var manifest = CreateManifest(PublisherA, 1);

        var downloadResult = new ValhallaDownloadResult
        {
            PackageName = TestPackageName,
            Version = TestVersion,
            PackageData = packageData,
            PackageSha256 = digest.HexString
        };

        var installer = new ValhallaInstaller(new ValhallaClient("https://valhalla.test"));

        var (passed, error) = installer.ValidateAgainstLock(
            lockFile, manifest, downloadResult, TestPackageName, TestVersion);

        Assert.True(passed);
        Assert.Null(error);
    }
}