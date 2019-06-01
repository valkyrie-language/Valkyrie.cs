using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Legion.Registry.Npm;

/// <summary>
/// npm 注册表适配器，对接 npm registry API
/// </summary>
public class NpmRegistry : RegistryBase
{
    /// <summary>
    /// npm 默认注册表端点
    /// </summary>
    public const string DefaultEndpoint = "https://registry.npmjs.org";

    /// <summary>
    /// 创建 npm 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://registry.npmjs.org</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public NpmRegistry(string endpoint = DefaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("npm", endpoint, httpClient, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package> GetPackageAsync(string packageName, string version)
    {
        if (version == "latest")
        {
            var url = BuildUrl($"{packageName}/latest");
            var versionData = await GetAsync<NpmVersionData>(url);
            return ConvertToPackage(packageName, versionData);
        }

        var packageUrl = BuildUrl($"{packageName}/{version}");
        var data = await GetAsync<NpmVersionData>(packageUrl);
        return ConvertToPackage(packageName, data);
    }

    /// <summary>
    /// npm 注册表的重试配置：5 次重试，指数退避，应对速率限制
    /// </summary>
    protected override RetryConfig RetryConfig { get; } = new()
    {
        MaxRetries = 5,
        InitialDelay = TimeSpan.FromMilliseconds(500),
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(15)
    };

    /// <inheritdoc />
    public override async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var url = BuildUrl($"-/v1/search?text={Uri.EscapeDataString(query)}&size=20");
        var response = await GetAsync<NpmSearchResponse>(url);

        var result = new List<Package>();

        if (response.Objects is not null)
        {
            foreach (var obj in response.Objects)
            {
                if (obj.Package is not null)
                {
                    result.Add(new Package
                    {
                        Name = obj.Package.Name,
                        Version = obj.Package.Version,
                        Description = obj.Package.Description ?? string.Empty,
                        Author = obj.Package.Author?.Name ?? string.Empty
                    });
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData)
    {
        var url = BuildUrl(options.PackageName);

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new ByteArrayContent(tarballData);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        if (!string.IsNullOrWhiteSpace(options.AuthToken))
        {
            request.Headers.Add("Authorization", $"Bearer {options.AuthToken}");
        }

        var response = await HttpClient.SendAsync(request);

        return response.IsSuccessStatusCode
            ? new PublishResult
            {
                Success = true,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = "发布成功",
                PublishedUrl = url
            }
            : new PublishResult
            {
                Success = false,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = $"发布失败，HTTP {(int)response.StatusCode}"
            };
    }

    /// <inheritdoc />
    public override async Task<string> DownloadPackageAsync(Package package, string targetDirectory)
    {
        string tarballUrl;

        if (!string.IsNullOrWhiteSpace(package.DistTarball))
        {
            tarballUrl = package.DistTarball;
        }
        else
        {
            var url = BuildUrl($"{package.Name}/{package.Version}");
            var data = await GetAsync<NpmVersionData>(url);

            if (string.IsNullOrWhiteSpace(data.Dist?.Tarball))
            {
                throw new RegistryException($"获取 {package.Name}@{package.Version} 的下载地址失败", 404);
            }

            tarballUrl = data.Dist.Tarball;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-npm-{package.Name}-{package.Version}.tgz");

        try
        {
            var response = await HttpClient.GetAsync(tarballUrl);
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
        var url = BuildUrl(packageName);
        var data = await GetAsync<NpmPackageMetadata>(url);

        return data.Versions?.Keys.ToList() ?? new List<string>();
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> VerifyTokenAsync(string token)
    {
        var url = BuildUrl("-/whoami");

        try
        {
            var response = await GetAuthenticatedAsync(url, token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var whoami = System.Text.Json.JsonSerializer.Deserialize<NpmWhoAmIResponse>(content, JsonOptions);

                return TokenVerifyResult.Success(
                    whoami?.Username ?? "unknown");
            }

            return TokenVerifyResult.Failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.Failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private static Package ConvertToPackage(string packageName, NpmVersionData data)
    {
        var deps = new List<string>();
        var depVersions = new Dictionary<string, string>();

        if (data.Dependencies is not null)
        {
            foreach (var dep in data.Dependencies)
            {
                deps.Add($"{dep.Key}@{dep.Value}");
                depVersions[dep.Key] = dep.Value;
            }
        }

        return new Package
        {
            Name = data.Name ?? packageName,
            Version = data.Version ?? string.Empty,
            Description = data.Description ?? string.Empty,
            Author = data.Author?.Name ?? string.Empty,
            License = data.License ?? string.Empty,
            DistTarball = data.Dist?.Tarball,
            DistIntegrity = data.Dist?.Integrity,
            Dependencies = deps,
            DependencyVersions = depVersions,
            PeerDependencies = data.PeerDependencies
        };
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

    private class NpmPackageMetadata
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("versions")]
        public Dictionary<string, NpmVersionData>? Versions { get; set; }

        [JsonPropertyName("dist-tags")]
        public Dictionary<string, string>? DistTags { get; set; }
    }

    private class NpmVersionData
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public NpmAuthor? Author { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("dist")]
        public NpmDist? Dist { get; set; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; set; }

        [JsonPropertyName("peerDependencies")]
        public Dictionary<string, string>? PeerDependencies { get; set; }
    }

    private class NpmAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class NpmDist
    {
        [JsonPropertyName("tarball")]
        public string? Tarball { get; set; }

        [JsonPropertyName("integrity")]
        public string? Integrity { get; set; }

        [JsonPropertyName("shasum")]
        public string? Shasum { get; set; }
    }

    private class NpmSearchResponse
    {
        [JsonPropertyName("objects")]
        public List<NpmSearchObject>? Objects { get; set; }
    }

    private class NpmSearchObject
    {
        [JsonPropertyName("package")]
        public NpmSearchPackage? Package { get; set; }
    }

    private class NpmSearchPackage
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public NpmAuthor? Author { get; set; }
    }

    private class NpmWhoAmIResponse
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    #endregion
}