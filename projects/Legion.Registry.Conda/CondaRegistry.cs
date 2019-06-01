using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Legion.Registry.Conda;

/// <summary>
/// conda 注册表适配器（Anaconda），对接 conda-forge 频道
/// </summary>
public class CondaRegistry : RegistryBase
{
    /// <summary>
    /// conda 默认注册表端点
    /// </summary>
    public const string DefaultEndpoint = "https://api.anaconda.org";

    /// <summary>
    /// conda 默认频道
    /// </summary>
    public const string DefaultChannel = "conda-forge";

    /// <summary>
    /// 创建 conda 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://api.anaconda.org</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public CondaRegistry(string endpoint = DefaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("conda", endpoint, httpClient, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package> GetPackageAsync(string packageName, string version)
    {
        var url = BuildUrl($"package/{DefaultChannel}/{Uri.EscapeDataString(packageName)}");
        var json = await GetAsync<CondaPackageResponse>(url);

        var resolvedVersion = version == "latest"
            ? json.LatestVersion ?? "0.0.0"
            : version;

        string? tarball = json.Files?.FirstOrDefault()?.DownloadUrl;

        var dependencyVersions = new Dictionary<string, string>();
        var dependencies = new List<string>();

        if (json.Dependencies is not null)
        {
            foreach (var (name, ver) in json.Dependencies)
            {
                dependencyVersions[name] = ver;
                dependencies.Add($"{name}@{ver}");
            }
        }

        return new Package
        {
            Name = json.Name ?? packageName,
            Version = resolvedVersion,
            Description = json.Summary ?? json.Description ?? string.Empty,
            Homepage = json.Homepage
                       ?? json.DevUrl
                       ?? $"https://anaconda.org/{DefaultChannel}/{packageName}",
            Author = json.Author ?? string.Empty,
            License = json.License ?? string.Empty,
            Dependencies = dependencies,
            DependencyVersions = dependencyVersions,
            DistTarball = tarball
        };
    }

    /// <summary>
    /// conda 注册表的重试配置：3 次重试，标准退避
    /// </summary>
    protected override RetryConfig RetryConfig { get; } = new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromMilliseconds(500),
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(10)
    };

    /// <inheritdoc />
    public override async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var url = BuildUrl($"search?name={Uri.EscapeDataString(query)}&limit=20");
        var jsonList = await GetAsync<List<CondaSearchItem>>(url);

        var packages = new List<Package>();

        foreach (var item in jsonList)
        {
            packages.Add(new Package
            {
                Name = item.Name ?? string.Empty,
                Version = item.LatestVersion ?? "0.0.0",
                Description = item.Summary ?? string.Empty,
                Homepage = item.Homepage
                           ?? $"https://anaconda.org/{DefaultChannel}/{item.Name}",
                Author = item.Author ?? string.Empty,
                License = item.License ?? string.Empty
            });
        }

        return packages;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData)
    {
        if (string.IsNullOrWhiteSpace(options.AuthToken))
        {
            throw new RegistryException("发布到 conda 注册表需要认证令牌");
        }

        var url = BuildUrl($"upload/{DefaultChannel}/{Uri.EscapeDataString(options.PackageName)}/{options.Version}");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(tarballData), "package", $"{options.PackageName}-{options.Version}.tar.bz2");

        var response = await SendAuthenticatedAsync(HttpMethod.Post, url, options.AuthToken, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new PublishResult
            {
                Success = false,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = "认证失败：无效的认证令牌"
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new PublishResult
            {
                Success = false,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = $"版本 {options.Version} 已存在于 conda 注册表中"
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new PublishResult
            {
                Success = false,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = $"发布失败：{(int)response.StatusCode} - {errorContent}"
            };
        }

        return new PublishResult
        {
            Success = true,
            PackageName = options.PackageName,
            Version = options.Version,
            Message = $"成功发布 {options.PackageName}@{options.Version} 到 conda 注册表",
            PublishedUrl = url
        };
    }

    /// <inheritdoc />
    public override async Task<string> DownloadPackageAsync(Package package, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(package.DistTarball))
        {
            throw new RegistryException($"包 '{package.Name}' 没有可用的下载地址");
        }

        Directory.CreateDirectory(targetDirectory);

        var ext = package.DistTarball.EndsWith(".tar.bz2") ? ".tar.bz2" : ".tar.gz";
        var tempFile = Path.Combine(Path.GetTempPath(), $"{package.Name}@{package.Version}{ext}");

        try
        {
            using var response = await HttpClient.GetAsync(package.DistTarball);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(tempFile);
            await stream.CopyToAsync(fileStream);

            VerifyDownloadIntegrity(tempFile, package.DistIntegrity);

            ExtractTarArchive(tempFile, targetDirectory);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
            }
        }

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> GetPackageVersionsAsync(string packageName)
    {
        var url = BuildUrl($"package/{DefaultChannel}/{Uri.EscapeDataString(packageName)}");

        try
        {
            var json = await GetAsync<CondaPackageResponse>(url);
            var fileVersionMap = new Dictionary<string, string?>();

            if (json.Files is not null)
            {
                foreach (var file in json.Files)
                {
                    if (file.Version is not null)
                    {
                        fileVersionMap[file.Version] = file.Version;
                    }
                }
            }

            return fileVersionMap.Keys.ToList();
        }
        catch (RegistryException)
        {
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> VerifyTokenAsync(string token)
    {
        var url = BuildUrl("user");

        try
        {
            var response = await GetAuthenticatedAsync(url, token);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return TokenVerifyResult.Failure("认证令牌无效或已过期");
            }

            if (!response.IsSuccessStatusCode)
            {
                return TokenVerifyResult.Failure($"验证失败：{(int)response.StatusCode}");
            }

            var userJson = await response.Content.ReadFromJsonAsync<CondaUserResponse>(JsonOptions);
            var username = userJson?.User?.Login
                           ?? userJson?.Login
                           ?? "未知用户";

            return TokenVerifyResult.Success(username);
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.Failure($"验证请求失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            return TokenVerifyResult.Failure($"验证请求失败：{ex.Message}");
        }
    }

    #region JSON 数据模型

    private class CondaPackageResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("latest_version")]
        public string? LatestVersion { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("home")]
        public string? Homepage { get; set; }

        [JsonPropertyName("dev_url")]
        public string? DevUrl { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("files")]
        public List<CondaFileItem>? Files { get; set; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; set; }
    }

    private class CondaFileItem
    {
        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    private class CondaSearchItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("latest_version")]
        public string? LatestVersion { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("home")]
        public string? Homepage { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }
    }

    private class CondaUserResponse
    {
        [JsonPropertyName("user")]
        public CondaUserInfo? User { get; set; }

        [JsonPropertyName("login")]
        public string? Login { get; set; }
    }

    private class CondaUserInfo
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }

    #endregion

    #region TAR 解压

    /// <summary>
    /// 解压 .tar.gz / .tar.bz2 归档到目标目录
    /// </summary>
    private static void ExtractTarArchive(string archivePath, string targetDirectory)
    {
        using var fileStream = File.OpenRead(archivePath);
        Stream dataStream;

        if (archivePath.EndsWith(".gz"))
        {
            dataStream = new GZipStream(fileStream, CompressionMode.Decompress);
        }
        else
        {
            // .tar.bz2 需要 BZip2 支持，.NET BCL 不原生支持
            // 此处暂回退为直接读取原始流，后续可通过引入 NuGet 包来支持
            dataStream = fileStream;
        }

        using var memoryStream = new MemoryStream();
        dataStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var buffer = new byte[4096];
        var position = 0L;
        var length = memoryStream.Length;

        while (position < length)
        {
            memoryStream.Position = position;
            memoryStream.ReadExactly(buffer, 0, 512);

            var name = System.Text.Encoding.ASCII.GetString(buffer, 0, 100).TrimEnd('\0');
            var sizeStr = System.Text.Encoding.ASCII.GetString(buffer, 124, 12).TrimEnd('\0', ' ');
            var fileSize = Convert.ToInt64(sizeStr, 8);

            position += 512;

            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            if (fileSize > 0)
            {
                var filePath = Path.Combine(targetDirectory, name);
                var fileDir = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }

                var fileData = new byte[fileSize];
                memoryStream.Position = position;
                memoryStream.ReadExactly(fileData, 0, (int)fileSize);
                File.WriteAllBytes(filePath, fileData);
            }

            var paddedSize = ((fileSize + 511) / 512) * 512;
            position += paddedSize;
        }
    }

    #endregion
}