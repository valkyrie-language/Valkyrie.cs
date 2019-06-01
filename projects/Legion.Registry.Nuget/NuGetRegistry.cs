using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Legion.Registry.Nuget;

/// <summary>
/// NuGet 注册表适配器，对接 NuGet v3 API
/// </summary>
public class NuGetRegistry : RegistryBase
{
    /// <summary>
    /// NuGet 默认注册表端点
    /// </summary>
    public const string DefaultEndpoint = "https://api.nuget.org/v3";

    /// <summary>
    /// 服务索引缓存
    /// </summary>
    private NuGetServiceIndex? _serviceIndex;

    /// <summary>
    /// 创建 NuGet 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://api.nuget.org/v3</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public NuGetRegistry(string endpoint = DefaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("nuget", endpoint, httpClient, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package> GetPackageAsync(string packageName, string version)
    {
        var index = await GetServiceIndexAsync();
        var regUrl = index.GetRegistrationBaseUrl() ?? Endpoint;

        var packageUrl = $"{regUrl}/{packageName.ToLowerInvariant()}/index.json";
        var packageData = await GetAsync<NuGetRegistrationIndex>(packageUrl);

        var items = packageData.Items ?? new List<NuGetRegistrationPage>();

        if (version == "latest")
        {
            var latestItem = items
                .SelectMany(p => p.Items ?? new List<NuGetRegistrationLeaf>())
                .MaxBy(l => ParseNuGetVersion(l.CatalogEntry?.Version));

            if (latestItem?.CatalogEntry is null)
            {
                throw new RegistryException($"包 {packageName} 没有可用版本", 404);
            }

            return ConvertToPackage(latestItem.CatalogEntry);
        }

        var match = items
            .SelectMany(p => p.Items ?? new List<NuGetRegistrationLeaf>())
            .FirstOrDefault(l =>
                string.Equals(l.CatalogEntry?.Version, version, StringComparison.OrdinalIgnoreCase));

        if (match?.CatalogEntry is null)
        {
            throw new RegistryException($"包 {packageName}@{version} 不存在", 404);
        }

        return ConvertToPackage(match.CatalogEntry);
    }

    /// <summary>
    /// NuGet 注册表的重试配置：3 次重试，较长退避，适应大包注册表
    /// </summary>
    protected override RetryConfig RetryConfig { get; } = new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromSeconds(1),
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(15)
    };

    /// <inheritdoc />
    public override async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var index = await GetServiceIndexAsync();
        var searchUrl = index.GetSearchQueryUrl() ?? $"{Endpoint}/query";

        var url = $"{searchUrl}?q={Uri.EscapeDataString(query)}&take=20&prerelease=false";
        var response = await GetAsync<NuGetSearchResponse>(url);

        var result = new List<Package>();

        if (response.Data is not null)
        {
            foreach (var item in response.Data)
            {
                result.Add(new Package
                {
                    Name = item.Id ?? string.Empty,
                    Version = item.Version ?? string.Empty,
                    Description = item.Description ?? string.Empty,
                    Author = string.Join(", ", item.Authors ?? new List<string>())
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
            Message = "NuGet 发布请使用 dotnet nuget push 或 nuget push 命令"
        });
    }

    /// <inheritdoc />
    public override async Task<string> DownloadPackageAsync(Package package, string targetDirectory)
    {
        var index = await GetServiceIndexAsync();
        var flatUrl = index.GetPackageBaseAddressUrl()
                      ?? $"{Endpoint}/flatcontainer";

        var downloadUrl = $"{flatUrl}/{package.Name.ToLowerInvariant()}/{package.Version}/{package.Name.ToLowerInvariant()}.{package.Version}.nupkg";

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-nuget-{package.Name}-{package.Version}.nupkg");

        try
        {
            var response = await HttpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            VerifyDownloadIntegrity(tempFile, package.DistIntegrity);

            await using var nupkgStream = File.OpenRead(tempFile);
            await ExtractNupkgAsync(nupkgStream, targetDirectory);
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
        var index = await GetServiceIndexAsync();
        var regUrl = index.GetRegistrationBaseUrl() ?? Endpoint;

        var packageUrl = $"{regUrl}/{packageName.ToLowerInvariant()}/index.json";

        try
        {
            var packageData = await GetAsync<NuGetRegistrationIndex>(packageUrl);
            var items = packageData.Items ?? new List<NuGetRegistrationPage>();

            return items
                .SelectMany(p => p.Items ?? new List<NuGetRegistrationLeaf>())
                .Select(l => l.CatalogEntry?.Version ?? string.Empty)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .ToList();
        }
        catch (RegistryException)
        {
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> VerifyTokenAsync(string token)
    {
        var index = await GetServiceIndexAsync();
        var searchUrl = index.GetSearchQueryUrl() ?? $"{Endpoint}/query";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{searchUrl}?q=test&take=1");
            request.Headers.Add("X-NuGet-ApiKey", token);

            var response = await HttpClient.SendAsync(request);

            return response.IsSuccessStatusCode
                ? TokenVerifyResult.Success("authenticated")
                : TokenVerifyResult.Failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.Failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private async Task<NuGetServiceIndex> GetServiceIndexAsync()
    {
        if (_serviceIndex is not null)
        {
            return _serviceIndex;
        }

        var url = $"{Endpoint}/index.json";
        _serviceIndex = await GetAsync<NuGetServiceIndex>(url);
        return _serviceIndex;
    }

    private static Package ConvertToPackage(NuGetCatalogEntry entry)
    {
        return new Package
        {
            Name = entry.Id ?? string.Empty,
            Version = entry.Version ?? string.Empty,
            Description = entry.Description ?? string.Empty,
            Author = string.Join(", ", entry.Authors ?? new List<string>()),
            License = entry.LicenseExpression ?? entry.LicenseUrl ?? string.Empty,
            DistTarball = entry.PackageContent
        };
    }

    private static Version? ParseNuGetVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var clean = version.Split('-')[0];
        return Version.TryParse(clean, out var v) ? v : null;
    }

    private static async Task ExtractNupkgAsync(Stream nupkgStream, string targetDirectory)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await using (var fileStream = File.Create(tempFile))
            {
                await nupkgStream.CopyToAsync(fileStream);
            }

            Directory.CreateDirectory(targetDirectory);

            using var archive = ZipFile.OpenRead(tempFile);

            foreach (var entry in archive.Entries)
            {
                var destPath = Path.Combine(targetDirectory, entry.FullName);
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

    #region NuGet v3 JSON 数据模型

    private class NuGetServiceIndex
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("resources")]
        public List<NuGetResource>? Resources { get; set; }

        public string? GetSearchQueryUrl()
        {
            return Resources?
                .FirstOrDefault(r => r.Type == "SearchQueryService")
                ?.Id;
        }

        public string? GetRegistrationBaseUrl()
        {
            return Resources?
                .FirstOrDefault(r => r.Type?
                    .StartsWith("RegistrationsBaseUrl", StringComparison.OrdinalIgnoreCase) == true)
                ?.Id;
        }

        public string? GetPackageBaseAddressUrl()
        {
            return Resources?
                .FirstOrDefault(r => r.Type?
                    .StartsWith("PackageBaseAddress", StringComparison.OrdinalIgnoreCase) == true)
                ?.Id;
        }
    }

    private class NuGetResource
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("@type")]
        public string? Type { get; set; }
    }

    private class NuGetSearchResponse
    {
        [JsonPropertyName("totalHits")]
        public int TotalHits { get; set; }

        [JsonPropertyName("data")]
        public List<NuGetSearchItem>? Data { get; set; }
    }

    private class NuGetSearchItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("authors")]
        public List<string>? Authors { get; set; }
    }

    private class NuGetRegistrationIndex
    {
        [JsonPropertyName("items")]
        public List<NuGetRegistrationPage>? Items { get; set; }
    }

    private class NuGetRegistrationPage
    {
        [JsonPropertyName("items")]
        public List<NuGetRegistrationLeaf>? Items { get; set; }
    }

    private class NuGetRegistrationLeaf
    {
        [JsonPropertyName("catalogEntry")]
        public NuGetCatalogEntry? CatalogEntry { get; set; }
    }

    private class NuGetCatalogEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("authors")]
        public List<string>? Authors { get; set; }

        [JsonPropertyName("licenseExpression")]
        public string? LicenseExpression { get; set; }

        [JsonPropertyName("licenseUrl")]
        public string? LicenseUrl { get; set; }

        [JsonPropertyName("packageContent")]
        public string? PackageContent { get; set; }
    }

    #endregion
}