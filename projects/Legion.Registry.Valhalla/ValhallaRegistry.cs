using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Legion.Registry;
using Valhalla;
using Valhalla.Client;

namespace Valkyrie.PackageManager;

/// <summary>
/// 瓦尓哈拉注册表适配器，将瓦尓哈拉 API 适配为 Legion 的 IRegistry 接口
/// </summary>
public class ValhallaRegistry : IRegistry
{
    private ValhallaClient _client;
    private string _endpoint;

    /// <summary>
    /// 创建瓦尓哈拉注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表基础 URL，如 https://valhalla.example.com</param>
    /// <param name="httpClient">可选的 HttpClient</param>
    public ValhallaRegistry(string endpoint = "https://valhalla.nyar.dev", HttpClient? httpClient = null)
    {
        _endpoint = endpoint.TrimEnd('/');
        _client = new ValhallaClient(_endpoint, httpClient);
    }

    /// <inheritdoc />
    public string Name => "valhalla";

    /// <inheritdoc />
    public string Endpoint
    {
        get => _endpoint;
        set
        {
            _endpoint = value.TrimEnd('/');
            _client.Dispose();
            _client = new ValhallaClient(_endpoint);
        }
    }

    /// <inheritdoc />
    public async Task<Package> GetPackageAsync(string packageName, string version)
    {
        var name = new global::Valhalla.PackageName(packageName);

        if (version == "latest")
        {
            var manifest = await _client.GetManifestAsync(name.Canonical);
            if (manifest is null)
            {
                throw new RegistryException($"包 {name.Canonical} 不存在", 404);
            }

            var latestEntry = manifest.Versions.Values
                .Where(v => v.Status == global::Valhalla.VersionStatus.Active)
                .MaxBy(v => v.PublishedAt);

            if (latestEntry is null)
            {
                throw new RegistryException($"包 {name.Canonical} 没有可用版本", 404);
            }

            return ConvertToPackage(manifest, latestEntry);
        }

        // 获取指定版本
        var versionEntry = await _client.GetVersionAsync(name.Canonical, version);
        if (versionEntry is null)
        {
            throw new RegistryException($"包 {name.Canonical}@{version} 不存在", 404);
        }

        var fullManifest = await _client.GetManifestAsync(name.Canonical);
        return ConvertToPackage(fullManifest ?? new PackageManifest { Name = name.Canonical }, versionEntry);
    }

    /// <inheritdoc />
    public async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var response = await _client.ListPackagesAsync(query: query, size: 50);
        var result = new List<Package>();

        foreach (var summary in response.Packages)
        {
            result.Add(new Package
            {
                Name = summary.Name,
                Version = summary.LatestVersion,
                Description = summary.Description,
                Author = summary.Publisher
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData)
    {
        // 瓦尓哈拉发布使用自己的 API 格式，此处为简化适配
        return new PublishResult
        {
            Success = false,
            PackageName = options.PackageName,
            Version = options.Version,
            Message = "瓦尓哈拉发布请使用 valhalla publish 命令，支持 .nyar + source.tar.gz 格式"
        };
    }

    /// <inheritdoc />
    public async Task<string> DownloadPackageAsync(Package package, string targetDirectory)
    {
        var name = new global::Valhalla.PackageName(package.Name);

        var installer = new ValhallaInstaller(_client);
        var result = await installer.InstallAsync(
            name.Canonical, package.Version, targetDirectory);

        if (!result.Success)
        {
            throw new RegistryException(
                $"下载 {package.Name}@{package.Version} 失败: {result.Error}", 500);
        }

        return targetDirectory;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetPackageVersionsAsync(string packageName)
    {
        var name = new global::Valhalla.PackageName(packageName);
        var manifest = await _client.GetManifestAsync(name.Canonical);

        if (manifest is null)
        {
            return new List<string>();
        }

        return manifest.Versions.Keys.ToList();
    }

    /// <inheritdoc />
    public async Task<TokenVerifyResult> VerifyTokenAsync(string token)
    {
        // 瓦尓哈拉使用 Ed25519 密钥对认证，而非令牌
        // 此处返回一个存根结果
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenVerifyResult.Failure("瓦尓哈拉使用 Ed25519 密钥对认证，请提供有效的密钥指纹");
        }

        return TokenVerifyResult.Success(token, DateTime.UtcNow.AddHours(1));
    }

    #region 私有方法

    /// <summary>
    /// 将瓦尓哈拉数据模型转换为 Legion 的 Package 模型
    /// </summary>
    private static Package ConvertToPackage(PackageManifest manifest, VersionEntry entry)
    {
        return new Package
        {
            Name = manifest.Name,
            Version = entry.Version,
            Description = $"化身 {manifest.Incarnation}，发布者 {manifest.Publisher}",
            Author = manifest.Publisher,
            DistTarball = $"{manifest.Name}/{entry.Version}/download",
            DistIntegrity = $"sha256-{entry.PackageDigest}"
        };
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}