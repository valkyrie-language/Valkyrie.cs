using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Valhalla;

namespace Valhalla.Client;

/// <summary>
/// 瓦尓哈拉安装器，负责下载、SHA-256 验证、写入磁盘
/// </summary>
public class ValhallaInstaller
{
    private readonly ValhallaClient _client;

    /// <summary>
    /// 创建安装器
    /// </summary>
    /// <param name="client">瓦尓哈拉 HTTP 客户端</param>
    public ValhallaInstaller(ValhallaClient client)
    {
        _client = client;
    }

    /// <summary>
    /// 对照锁文件校验下载结果的安全属性
    /// </summary>
    /// <param name="lockFile">锁文件</param>
    /// <param name="manifest">远程包清单</param>
    /// <param name="downloadResult">下载结果</param>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <returns>校验结果，若校验失败则包含错误信息</returns>
    public (bool Passed, string? Error) ValidateAgainstLock(
        ValhallaLockFile lockFile,
        PackageManifest manifest,
        ValhallaDownloadResult downloadResult,
        string packageName,
        string version)
    {
        var lockedEntry = lockFile.GetEntry(packageName);

        if (lockedEntry is not null)
        {
            if (manifest.Incarnation > lockedEntry.Incarnation)
            {
                return (false, "包已被 PURGE 后重新注册，请检查安全通告");
            }

            string manifestPublisher = manifest.Publisher.ToLowerInvariant();
            string lockedPublisher = lockedEntry.Publisher.ToLowerInvariant();
            if (!string.Equals(manifestPublisher, lockedPublisher, StringComparison.Ordinal))
            {
                return (false, "发布者身份已变更，可能存在安全风险");
            }
        }

        ValhallaDigest actualDigest = ValhallaDigest.Compute(downloadResult.PackageData);

        if (lockedEntry is not null && lockedEntry.Version == version)
        {
            string lockedShaLower = lockedEntry.Sha256.ToLowerInvariant();
            if (actualDigest.HexString != lockedShaLower)
            {
                return (false, $"SHA-256 与锁文件不匹配：锁记录 {lockedShaLower}，实际 {actualDigest.HexString}");
            }
        }

        if (!string.IsNullOrEmpty(downloadResult.PackageSha256))
        {
            string serverShaLower = downloadResult.PackageSha256.ToLowerInvariant();
            if (actualDigest.HexString != serverShaLower)
            {
                return (false, $"SHA-256 与服务端声明不匹配：服务端声明 {serverShaLower}，实际 {actualDigest.HexString}");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 下载并验证包，写入目标目录
    /// </summary>
    /// <param name="packageName">规范包名</param>
    /// <param name="version">版本号</param>
    /// <param name="targetDirectory">目标安装目录</param>
    /// <param name="ct">取消令牌</param>
    public async Task<InstallResult> InstallAsync(
        string packageName,
        string version,
        string targetDirectory,
        CancellationToken ct = default)
    {
        try
        {
            var download = await _client.DownloadAsync(packageName, version, ct);

            ValhallaDigest actualDigest = ValhallaDigest.Compute(download.PackageData);

            if (!string.IsNullOrEmpty(download.PackageSha256))
            {
                string expectedLower = download.PackageSha256.ToLowerInvariant();
                if (actualDigest.HexString != expectedLower)
                {
                    return InstallResult.Fail(packageName, version,
                        $"SHA-256 不匹配：期望 {expectedLower}，实际 {actualDigest.HexString}");
                }
            }

            // 验证源码 SHA-256（如果有）
            string? actualSourceSha256 = null;
            if (download.SourceData is not null && !string.IsNullOrEmpty(download.SourceSha256))
            {
                var sourceDigest = ValhallaDigest.Compute(download.SourceData);
                actualSourceSha256 = sourceDigest.HexString;

                string expectedSourceLower = download.SourceSha256.ToLowerInvariant();
                if (sourceDigest.HexString != expectedSourceLower)
                {
                    return InstallResult.Fail(packageName, version,
                        $"源码 SHA-256 不匹配：期望 {expectedSourceLower}，实际 {sourceDigest.HexString}");
                }
            }

            // 对照锁文件校验安全属性
            var lockFile = new ValhallaLockFile(targetDirectory);
            if (lockFile.Exists())
            {
                await lockFile.LoadAsync();

                var manifest = await _client.GetManifestAsync(packageName, ct);
                if (manifest is not null)
                {
                    var (passed, error) = ValidateAgainstLock(lockFile, manifest, download, packageName, version);
                    if (!passed)
                    {
                        return InstallResult.Fail(packageName, version, error!);
                    }
                }
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // 写入 .nyar 文件
            string packagePath = Path.Combine(targetDirectory, $"{packageName}-{version}.nyar");
            await File.WriteAllBytesAsync(packagePath, download.PackageData, ct);

            // 写入源码包（如果有）
            if (download.SourceData is not null)
            {
                string sourcePath = Path.Combine(targetDirectory, $"{packageName}-{version}.src.tar.gz");
                await File.WriteAllBytesAsync(sourcePath, download.SourceData, ct);
            }

            // 写入 SHA-256 承诺文件
            string digestPath = Path.Combine(targetDirectory, $"{packageName}-{version}.sha256");
            await File.WriteAllTextAsync(digestPath,
                $"{actualDigest.HexString}  .nyar\n{actualSourceSha256 ?? "N/A"}  source\n", ct);

            return InstallResult.Succeed(packageName, version, actualDigest.HexString, actualSourceSha256);
        }
        catch (Exception ex)
        {
            return InstallResult.Fail(packageName, version, ex.Message);
        }
    }
}