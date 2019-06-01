using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valhalla;
using Valhalla.Client;
using Valhalla.Config;
using Valhalla.Server;
using Xunit;

namespace Valhalla.Tests;

/// <summary>
/// 瓦尓哈拉端到端集成测试，启动真实 HTTP 服务器进行全链路测试
/// </summary>
public class ValhallaE2ETests
{
    /// <summary>
    /// 验证依赖注入完整性
    /// </summary>
    [Fact]
    public void Constructor_验证依赖注入完整性()
    {
        Assert.True(true);
    }

    /// <summary>
    /// 发布→检索→下载全链路测试
    /// </summary>
    [Fact]
    public async Task 发布检索下载全链路_验证完整流程()
    {
        string tempDir = CreateTempDir();
        try
        {
            int port = GetFreePort();
            var config = CreateTestConfig(tempDir, port);
            string baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = StartServerInBackground(server);
            await WaitForServerReady(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            string packageName = "test.e2e.pkg";
            string version = "1.0.0";
            string publisher = "test-fingerprint-abc123";

            byte[] testPackageData = GenerateTestData(4096);
            string expectedDigest = ComputeSha256Hex(testPackageData);

            using var httpClient = new HttpClient();

            // 注册包
            await RegisterPackageAsync(httpClient, baseUrl, packageName, publisher);

            // 上传版本
            await UploadVersionAsync(httpClient, baseUrl, packageName, version, testPackageData);

            // 确认包在列表中
            var listResponse = await client.ListPackagesAsync();
            Assert.NotNull(listResponse);
            Assert.Contains(listResponse.Packages, p => p.Name == packageName);

            // 获取 manifest
            var manifest = await client.GetManifestAsync(packageName);
            Assert.NotNull(manifest);
            Assert.Equal(packageName, manifest!.Name);
            Assert.True(manifest.Versions.ContainsKey(version));

            // 获取版本详情
            var versionEntry = await client.GetVersionAsync(packageName, version);
            Assert.NotNull(versionEntry);
            Assert.Equal(expectedDigest, versionEntry!.PackageDigest);

            // 下载二进制
            var downloadResult = await client.DownloadAsync(packageName, version);
            Assert.NotNull(downloadResult);
            Assert.True(downloadResult.PackageData.Length > 0);
            Assert.Null(downloadResult.Error);

            // 验证下载的 SHA-256 与服务端报告的一致
            string actualDownloadDigest = ComputeSha256Hex(downloadResult.PackageData);
            Assert.Equal(downloadResult.PackageSha256, actualDownloadDigest);

            // 安装到临时目录
            string installDir = Path.Combine(tempDir, "installed");
            var installer = new ValhallaInstaller(client);
            var installResult = await installer.InstallAsync(packageName, version, installDir);
            Assert.True(installResult.Success, installResult.Error ?? "安装应成功");
            Assert.Equal(packageName, installResult.PackageName);
            Assert.Equal(version, installResult.Version);

            // 验证安装成功，文件存在
            string installedNyarPath = Path.Combine(installDir, $"{packageName}-{version}.nyar");
            Assert.True(File.Exists(installedNyarPath));
            string installedDigestPath = Path.Combine(installDir, $"{packageName}-{version}.sha256");
            Assert.True(File.Exists(installedDigestPath));

            byte[] installedData = await File.ReadAllBytesAsync(installedNyarPath);
            string installedDigest = ComputeSha256Hex(installedData);
            Assert.Equal(expectedDigest, installedDigest);

            await StopServerAsync(server, serverTask);
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    /// <summary>
    /// shield/unshield 生命周期测试
    /// </summary>
    [Fact]
    public async Task ShieldUnshield生命周期_验证状态变迁()
    {
        string tempDir = CreateTempDir();
        try
        {
            int port = GetFreePort();
            var config = CreateTestConfig(tempDir, port);
            string baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = StartServerInBackground(server);
            await WaitForServerReady(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            string packageName = "test.shield.pkg";
            string version = "1.0.0";
            string publisher = "test-fingerprint-shield";

            byte[] testPackageData = GenerateTestData(2048);

            using var httpClient = new HttpClient();

            await RegisterPackageAsync(httpClient, baseUrl, packageName, publisher);
            await UploadVersionAsync(httpClient, baseUrl, packageName, version, testPackageData);

            // 确认版本状态为 active
            var versionEntry = await client.GetVersionAsync(packageName, version);
            Assert.NotNull(versionEntry);
            Assert.Equal(VersionStatus.Active, versionEntry!.Status);

            // 发送 shield 请求
            string shieldUrl = $"{baseUrl}/api/packages/{packageName}/shield/{version}";
            var shieldBody = new StringContent(
                @"{""reason"":""安全漏洞 CVE-2025-0001""}",
                Encoding.UTF8,
                "application/json");
            var shieldResponse = await httpClient.PostAsync(shieldUrl, shieldBody);
            Assert.Equal(HttpStatusCode.OK, shieldResponse.StatusCode);

            // 验证版本状态变为 shielded
            var shieldedEntry = await client.GetVersionAsync(packageName, version);
            Assert.NotNull(shieldedEntry);
            Assert.Equal(VersionStatus.Shielded, shieldedEntry!.Status);
            Assert.Equal("安全漏洞 CVE-2025-0001", shieldedEntry.ShieldReason);

            // 发送 unshield 请求
            string unshieldUrl = $"{baseUrl}/api/packages/{packageName}/unshield/{version}";
            var unshieldResponse = await httpClient.PostAsync(unshieldUrl, null);
            Assert.Equal(HttpStatusCode.OK, unshieldResponse.StatusCode);

            // 验证版本状态恢复为 active
            var unshieldedEntry = await client.GetVersionAsync(packageName, version);
            Assert.NotNull(unshieldedEntry);
            Assert.Equal(VersionStatus.Active, unshieldedEntry!.Status);
            Assert.Null(unshieldedEntry.ShieldReason);

            await StopServerAsync(server, serverTask);
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    /// <summary>
    /// purge 包操作测试
    /// </summary>
    [Fact]
    public async Task Purge包操作_验证状态变为Purged()
    {
        string tempDir = CreateTempDir();
        try
        {
            int port = GetFreePort();
            var config = CreateTestConfig(tempDir, port);
            string baseUrl = $"http://localhost:{port}";

            var server = new ValhallaServer(config);
            var serverTask = StartServerInBackground(server);
            await WaitForServerReady(baseUrl);

            var client = new ValhallaClient(baseUrl, maxRetries: 2, retryBaseDelayMs: 100);

            string packageName = "test.purge.pkg";
            string purgeReason = "违反内容政策";

            using var httpClient = new HttpClient();

            await RegisterPackageAsync(httpClient, baseUrl, packageName, "test-fingerprint-purge");

            // 发送 DELETE 请求 purge 包
            string purgeUrl = $"{baseUrl}/api/packages/{packageName}";
            var purgeRequest = new HttpRequestMessage(HttpMethod.Delete, purgeUrl)
            {
                Content = new StringContent(
                    $"{{\"reason\":\"{purgeReason}\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
            var purgeResponse = await httpClient.SendAsync(purgeRequest);
            Assert.Equal(HttpStatusCode.OK, purgeResponse.StatusCode);

            // 验证包状态变为 purged
            var manifest = await client.GetManifestAsync(packageName);
            Assert.NotNull(manifest);
            Assert.Equal(PackageStatus.Purged, manifest!.Status);
            Assert.Equal(purgeReason, manifest.PurgeReason);
            Assert.NotNull(manifest.PurgedAt);

            await StopServerAsync(server, serverTask);
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    #region 辅助方法

    /// <summary>
    /// 获取一个空闲的 TCP 端口
    /// </summary>
    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// 创建用于测试的临时目录
    /// </summary>
    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ValhallaE2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// 删除临时目录及其所有内容
    /// </summary>
    private static void DeleteTempDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }

    /// <summary>
    /// 创建用于测试的服务端配置
    /// </summary>
    private static ValhallaConfig CreateTestConfig(string storagePath, int port)
    {
        return new ValhallaConfig
        {
            Name = "E2E 测试实例",
            Port = port,
            StoragePath = storagePath,
            Storage = StorageBackend.Local,
            PubkeyRequired = false,
            Public = true,
            Registration = RegistrationMode.Open,
            EnableAuditPublicAccess = true
        };
    }

    /// <summary>
    /// 在后台任务中启动服务端
    /// </summary>
    private static Task StartServerInBackground(ValhallaServer server)
    {
        return Task.Run(() => server.StartAsync());
    }

    /// <summary>
    /// 等待服务端就绪，轮询健康检查端点
    /// </summary>
    private static async Task WaitForServerReady(string baseUrl, int timeoutMs = 15000)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync($"{baseUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // 服务端尚未就绪，继续等待
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("服务端未能在超时时间内就绪");
    }

    /// <summary>
    /// 停止服务端并等待后台任务完成
    /// </summary>
    private static async Task StopServerAsync(ValhallaServer server, Task serverTask)
    {
        try
        {
            await server.StopAsync();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // 忽略停止错误
        }
    }

    /// <summary>
    /// 通过 HTTP API 注册包
    /// </summary>
    private static async Task RegisterPackageAsync(
        HttpClient http,
        string baseUrl,
        string packageName,
        string publisher)
    {
        string url = $"{baseUrl}/api/packages";
        var body = new StringContent(
            $"{{\"name\":\"{packageName}\",\"publisher\":\"{publisher}\"}}",
            Encoding.UTF8,
            "application/json");
        var response = await http.PostAsync(url, body);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 通过 HTTP API 上传版本
    /// </summary>
    private static async Task UploadVersionAsync(
        HttpClient http,
        string baseUrl,
        string packageName,
        string version,
        byte[] packageData)
    {
        string url = $"{baseUrl}/api/packages/{packageName}/versions";

        using var form = new MultipartFormDataContent();

        var manifestContent = new StringContent(
            $"{{\"version\":\"{version}\"}}",
            Encoding.UTF8,
            "application/json");
        form.Add(manifestContent, "manifest");

        var packageContent = new ByteArrayContent(packageData);
        packageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(packageContent, "package", $"{packageName}-{version}.nyar");

        var response = await http.PostAsync(url, form);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 生成指定大小的测试数据
    /// </summary>
    private static byte[] GenerateTestData(int size)
    {
        var data = new byte[size];
        Random.Shared.NextBytes(data);
        return data;
    }

    /// <summary>
    /// 计算字节数组的 SHA-256 hex 字符串
    /// </summary>
    private static string ComputeSha256Hex(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}