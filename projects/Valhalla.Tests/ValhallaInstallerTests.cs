using System.Net;
using Xunit;
using Valhalla.Client;

namespace Valhalla.Tests;

public class ValhallaInstallerTests
{
    private const string BaseUrl = "https://valhalla.test";

    [Fact]
    public async Task InstallAsync_成功下载验证并写入()
    {
        byte[] packageData = new byte[512];
        new Random(42).NextBytes(packageData);
        var packageDigest = ValhallaDigest.Compute(packageData);

        // 构建下载响应：4B长度 + 包数据 + 4B零长度(无源码)
        var responseData = new MemoryStream();
        responseData.Write(BitConverter.GetBytes(packageData.Length));
        responseData.Write(packageData);
        responseData.Write(BitConverter.GetBytes(0));
        byte[] responseBytes = responseData.ToArray();

        var handler = new TestHttpMessageHandler();
        handler.AddGetBytes("/api/packages/test.pkg/versions/1.0.0/download",
            HttpStatusCode.OK, responseBytes, new Dictionary<string, string>
            {
                ["X-Content-SHA256"] = packageDigest.HexString,
                ["Content-Type"] = "application/octet-stream"
            });

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(BaseUrl, http);

        string tempDir = Path.Combine(Path.GetTempPath(), $"valhalla-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new ValhallaInstaller(client);
            var result = await installer.InstallAsync("test.pkg", "1.0.0", tempDir);

            Assert.True(result.Success, result.Error ?? "安装应成功");
            Assert.Equal(packageDigest.HexString, result.Sha256);

            string packagePath = Path.Combine(tempDir, "test.pkg-1.0.0.nyar");
            Assert.True(File.Exists(packagePath));

            byte[] writtenData = await File.ReadAllBytesAsync(packagePath);
            Assert.Equal(packageData, writtenData);

            string digestPath = Path.Combine(tempDir, "test.pkg-1.0.0.sha256");
            Assert.True(File.Exists(digestPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstallAsync_SHA256不匹配_失败()
    {
        byte[] packageData = new byte[256];
        new Random(42).NextBytes(packageData);

        var responseData = new MemoryStream();
        responseData.Write(BitConverter.GetBytes(packageData.Length));
        responseData.Write(packageData);
        responseData.Write(BitConverter.GetBytes(0));
        byte[] responseBytes = responseData.ToArray();

        var handler = new TestHttpMessageHandler();
        handler.AddGetBytes("/api/packages/bad.pkg/versions/1.0.0/download",
            HttpStatusCode.OK, responseBytes, new Dictionary<string, string>
            {
                ["X-Content-SHA256"] = "0000000000000000000000000000000000000000000000000000000000000000",
                ["Content-Type"] = "application/octet-stream"
            });

        using var http = new HttpClient(handler);
        var client = new ValhallaClient(BaseUrl, http);

        string tempDir = Path.Combine(Path.GetTempPath(), $"valhalla-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new ValhallaInstaller(client);
            var result = await installer.InstallAsync("bad.pkg", "1.0.0", tempDir);

            Assert.False(result.Success);
            Assert.Contains("SHA-256 不匹配", result.Error);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}