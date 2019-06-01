using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Legion.Registry.Jsr;

/// <summary>
/// JSR 注册表适配器，对接 jsr.io API
/// </summary>
public class JsrRegistry : RegistryBase
{
    /// <summary>
    /// JSR 默认注册表端点
    /// </summary>
    public const string DefaultEndpoint = "https://jsr.io";

    /// <summary>
    /// JSR npm 兼容层端点
    /// </summary>
    private const string NpmCompatEndpoint = "https://npm.jsr.io";

    /// <summary>
    /// 创建 JSR 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://jsr.io</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public JsrRegistry(string endpoint = DefaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("jsr", endpoint, httpClient, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package> GetPackageAsync(string packageName, string version)
    {
        var (scope, name) = ParsePackageName(packageName);

        if (version == "latest")
        {
            var metaUrl = BuildUrl($"api/scopes/{scope}/packages/{name}/versions");
            var versions = await GetAsync<JsrVersionListResponse>(metaUrl);

            var latestVersion = versions.Versions?
                .OrderByDescending(v => ParseVersionForComparison(v.Version))
                .FirstOrDefault();

            if (latestVersion is null)
            {
                throw new RegistryException($"包 {packageName} 没有可用版本", 404);
            }

            return await GetPackageVersionAsync(scope, name, latestVersion.Version);
        }

        return await GetPackageVersionAsync(scope, name, version);
    }

    /// <summary>
    /// JSR 注册表的重试配置：3 次重试，较快退避
    /// </summary>
    protected override RetryConfig RetryConfig { get; } = new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromMilliseconds(300),
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(8)
    };

    /// <inheritdoc />
    public override async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var url = BuildUrl($"api/packages?query={Uri.EscapeDataString(query)}&limit=20");
        var response = await GetAsync<JsrSearchResponse>(url);

        var result = new List<Package>();

        if (response.Items is not null)
        {
            foreach (var item in response.Items)
            {
                result.Add(new Package
                {
                    Name = $"@{item.Scope}/{item.Name}",
                    Version = item.LatestStableVersion ?? item.LatestVersion ?? "0.0.0",
                    Description = item.Description ?? string.Empty
                });
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData)
    {
        return Task.FromResult(new PublishResult
        {
            Success = false,
            PackageName = options.PackageName,
            Version = options.Version,
            Message = "JSR 发布请使用 deno publish 或 jsr publish 命令"
        });
    }

    /// <inheritdoc />
    public override async Task<string> DownloadPackageAsync(Package package, string targetDirectory)
    {
        var (scope, name) = ParsePackageName(package.Name);
        var npmCompatUrl = $"{NpmCompatEndpoint}/~/packages/@{scope}/{name}/{package.Version}/";

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-jsr-{scope}-{name}-{package.Version}.tgz");

        try
        {
            var response = await HttpClient.GetAsync(npmCompatUrl);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            VerifyDownloadIntegrity(tempFile, package.DistIntegrity);

            await using var tarballStream = File.OpenRead(tempFile);
            await ExtractTarballAsync(tarballStream, targetDirectory);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> GetPackageVersionsAsync(string packageName)
    {
        var (scope, name) = ParsePackageName(packageName);
        var url = BuildUrl($"api/scopes/{scope}/packages/{name}/versions");

        try
        {
            var response = await GetAsync<JsrVersionListResponse>(url);
            return response.Versions?
                .Select(v => v.Version)
                .Where(v => v is not null)
                .Select(v => v!)
                .ToList() ?? new List<string>();
        }
        catch (RegistryException)
        {
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> VerifyTokenAsync(string token)
    {
        var url = BuildUrl("api/user");

        try
        {
            var response = await GetAuthenticatedAsync(url, token);

            if (response.IsSuccessStatusCode)
            {
                var userData = await response.Content.ReadFromJsonAsync<JsrUserResponse>(JsonOptions);
                return TokenVerifyResult.Success(userData?.User?.Name ?? "unknown");
            }

            return TokenVerifyResult.Failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.Failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private static (string Scope, string Name) ParsePackageName(string packageName)
    {
        var clean = packageName.StartsWith('@') ? packageName[1..] : packageName;
        var slashIndex = clean.IndexOf('/');

        if (slashIndex > 0)
        {
            return (clean[..slashIndex], clean[(slashIndex + 1)..]);
        }

        return ("std", clean);
    }

    private async Task<Package> GetPackageVersionAsync(string scope, string name, string version)
    {
        var url = BuildUrl($"api/scopes/{scope}/packages/{name}/versions/{version}");
        var versionData = await GetAsync<JsrVersionData>(url);

        return new Package
        {
            Name = $"@{scope}/{name}",
            Version = versionData.Version ?? version,
            Description = versionData.Description ?? string.Empty,
            DistTarball = $"{NpmCompatEndpoint}/~/packages/@{scope}/{name}/{version}/",
            DistIntegrity = versionData.Checksum is not null ? $"sha256-{versionData.Checksum}" : null
        };
    }

    private static Version? ParseVersionForComparison(string version)
    {
        return Version.TryParse(version.Split('-')[0], out var v) ? v : null;
    }

    private static async Task ExtractTarballAsync(Stream tarballStream, string targetDirectory)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await using (var fileStream = File.Create(tempFile))
            {
                await tarballStream.CopyToAsync(fileStream);
            }

            Directory.CreateDirectory(targetDirectory);

            using var archive = ZipFile.OpenRead(tempFile);

            foreach (var entry in archive.Entries)
            {
                var relativePath = entry.FullName;

                if (relativePath.StartsWith("package/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath["package/".Length..];
                }

                if (string.IsNullOrEmpty(relativePath))
                {
                    continue;
                }

                var destPath = Path.Combine(targetDirectory, relativePath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
                {
                    Directory.CreateDirectory(destPath);
                    continue;
                }

                entry.ExtractToFile(destPath, true);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    #endregion

    #region JSON 数据模型

    private class JsrSearchResponse
    {
        [JsonPropertyName("items")]
        public List<JsrSearchItem>? Items { get; set; }
    }

    private class JsrSearchItem
    {
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("latestVersion")]
        public string? LatestVersion { get; set; }

        [JsonPropertyName("latestStableVersion")]
        public string? LatestStableVersion { get; set; }
    }

    private class JsrVersionListResponse
    {
        [JsonPropertyName("versions")]
        public List<JsrVersionEntry>? Versions { get; set; }
    }

    private class JsrVersionEntry
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    private class JsrVersionData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }
    }

    private class JsrUserResponse
    {
        [JsonPropertyName("user")]
        public JsrUser? User { get; set; }
    }

    private class JsrUser
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}