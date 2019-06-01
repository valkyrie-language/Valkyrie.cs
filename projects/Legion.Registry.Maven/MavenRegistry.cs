using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Legion.Registry.Maven;

/// <summary>
/// Maven Central 注册表适配器，对接 Maven Central REST API
/// </summary>
public class MavenRegistry : RegistryBase
{
    /// <summary>
    /// Maven 搜索端点
    /// </summary>
    public const string DefaultEndpoint = "https://search.maven.org";

    /// <summary>
    /// Maven 仓库下载端点
    /// </summary>
    public const string DefaultRepositoryUrl = "https://repo1.maven.org/maven2";

    private string _repositoryUrl;

    /// <summary>
    /// 创建 Maven 注册表适配器
    /// </summary>
    /// <param name="endpoint">搜索端点，默认为 https://search.maven.org</param>
    /// <param name="repositoryUrl">仓库下载地址，默认为 https://repo1.maven.org/maven2</param>
    /// <param name="publishUrl">发布地址，可选</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public MavenRegistry(
        string endpoint = DefaultEndpoint,
        string repositoryUrl = DefaultRepositoryUrl,
        string? publishUrl = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
        : base("maven", endpoint, httpClient, logger)
    {
        _repositoryUrl = repositoryUrl.TrimEnd('/');
    }

    /// <summary>
    /// 仓库下载地址
    /// </summary>
    public string RepositoryUrl
    {
        get => _repositoryUrl;
        set => _repositoryUrl = value.TrimEnd('/');
    }

    /// <summary>
    /// 发布地址（Sonatype OSSRH 等）
    /// </summary>
    public string? PublishUrl { get; set; }

    /// <inheritdoc />
    public override async Task<Package> GetPackageAsync(string packageName, string version)
    {
        ParseMavenCoordinates(packageName, out var groupId, out var artifactId);

        var searchUrl = BuildUrl(
            $"solrsearch/select?q=g:{Uri.EscapeDataString(groupId)}+AND+a:{Uri.EscapeDataString(artifactId)}&core=gav&rows=20&wt=json");

        var searchJson = await GetAsync<MavenSearchResponse>(searchUrl);
        var docs = searchJson.Response?.Docs;

        if (docs is null || docs.Count == 0)
        {
            throw new RegistryException($"包 '{packageName}' 在 Maven Central 中未找到", 404);
        }

        var resolvedVersion = version == "latest"
            ? docs[0].Version ?? "0.0.0"
            : version;

        var groupPath = groupId.Replace('.', '/');
        var pomUrl = $"{RepositoryUrl}/{groupPath}/{artifactId}/{resolvedVersion}/{artifactId}-{resolvedVersion}.pom";

        var dependencyVersions = new Dictionary<string, string>();
        var dependencies = new List<string>();

        try
        {
            var pomResponse = await HttpClient.GetAsync(pomUrl);

            if (pomResponse.IsSuccessStatusCode)
            {
                var pomContent = await pomResponse.Content.ReadAsStringAsync();
                ParsePomDependencies(pomContent, dependencies, dependencyVersions);
            }
        }
        catch
        {
            Logger.LogWarning("解析 POM 文件失败: {PomUrl}", pomUrl);
        }

        return new Package
        {
            Name = $"{groupId}:{artifactId}",
            Version = resolvedVersion,
            Description = string.Empty,
            Homepage = string.Empty,
            Author = groupId,
            License = string.Empty,
            Dependencies = dependencies,
            DependencyVersions = dependencyVersions,
            DistTarball = $"{RepositoryUrl}/{groupPath}/{artifactId}/{resolvedVersion}/{artifactId}-{resolvedVersion}.jar"
        };
    }

    /// <summary>
    /// Maven 注册表的重试配置：4 次重试，Solr 搜索慢时使用较长退避
    /// </summary>
    protected override RetryConfig RetryConfig { get; } = new()
    {
        MaxRetries = 4,
        InitialDelay = TimeSpan.FromMilliseconds(600),
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(12)
    };

    /// <inheritdoc />
    public override async Task<List<Package>> SearchPackagesAsync(string query)
    {
        var url = BuildUrl($"solrsearch/select?q={Uri.EscapeDataString(query)}&rows=20&wt=json");
        var json = await GetAsync<MavenSearchResponse>(url);

        var docs = json.Response?.Docs;
        var packages = new List<Package>();

        if (docs is not null)
        {
            foreach (var doc in docs)
            {
                var g = doc.GroupId;
                var a = doc.ArtifactId;

                if (g is null || a is null)
                {
                    continue;
                }

                packages.Add(new Package
                {
                    Name = $"{g}:{a}",
                    Version = doc.Version ?? "0.0.0",
                    Description = string.Empty,
                    Homepage = string.Empty,
                    Author = g,
                    License = string.Empty
                });
            }
        }

        return packages;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData)
    {
        if (string.IsNullOrWhiteSpace(options.AuthToken))
        {
            throw new RegistryException("发布到 Maven 仓库需要认证令牌");
        }

        var publishEndpoint = PublishUrl ?? "https://s01.oss.sonatype.org/service/local/staging/deploy/maven2";

        ParseMavenCoordinates(options.PackageName, out var groupId, out var artifactId);
        var groupPath = groupId.Replace('.', '/');
        var url = $"{publishEndpoint}/{groupPath}/{artifactId}/{options.Version}/{artifactId}-{options.Version}.jar";

        var content = new ByteArrayContent(tarballData);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/java-archive");

        var response = await SendAuthenticatedAsync(HttpMethod.Put, url, options.AuthToken, content);

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
            Message = $"成功发布 {options.PackageName}:{options.Version} 到 Maven 仓库"
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

        var fileName = Path.GetFileName(new Uri(package.DistTarball).AbsolutePath);
        var targetFile = Path.Combine(targetDirectory, fileName);

        using var response = await HttpClient.GetAsync(package.DistTarball);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(targetFile);
        await stream.CopyToAsync(fileStream);

        VerifyDownloadIntegrity(targetFile, package.DistIntegrity);

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> GetPackageVersionsAsync(string packageName)
    {
        ParseMavenCoordinates(packageName, out var groupId, out var artifactId);

        var url = BuildUrl(
            $"solrsearch/select?q=g:{Uri.EscapeDataString(groupId)}+AND+a:{Uri.EscapeDataString(artifactId)}&core=gav&rows=200&wt=json");

        try
        {
            var json = await GetAsync<MavenSearchResponse>(url);
            var docs = json.Response?.Docs;

            return docs?.Select(d => d.Version ?? string.Empty)
                .Where(v => !string.IsNullOrEmpty(v))
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
        var url = BuildUrl("solrsearch/select?q=g:com&rows=0");

        try
        {
            var response = await GetAuthenticatedAsync(url, token);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return TokenVerifyResult.Failure("认证令牌无效或已过期");
            }

            if (response.IsSuccessStatusCode)
            {
                return TokenVerifyResult.Success("maven-user");
            }

            return TokenVerifyResult.Failure($"验证失败：{(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.Failure($"验证请求失败：{ex.Message}");
        }
    }

    #region 私有方法

    /// <summary>
    /// 解析 Maven 坐标 groupId:artifactId 格式
    /// </summary>
    public static void ParseMavenCoordinates(string packageName, out string groupId, out string artifactId)
    {
        var colonIndex = packageName.IndexOf(':');

        if (colonIndex > 0 && colonIndex < packageName.Length - 1)
        {
            groupId = packageName[..colonIndex];
            artifactId = packageName[(colonIndex + 1)..];
        }
        else
        {
            groupId = packageName;
            artifactId = packageName;
        }
    }

    private static void ParsePomDependencies(string pomContent, List<string> dependencies, Dictionary<string, string> dependencyVersions)
    {
        var depsStart = pomContent.IndexOf("<dependencies>", StringComparison.Ordinal);

        if (depsStart < 0)
        {
            return;
        }

        var depsEnd = pomContent.IndexOf("</dependencies>", StringComparison.Ordinal);

        if (depsEnd < 0)
        {
            return;
        }

        var depsSection = pomContent[depsStart..(depsEnd + "</dependencies>".Length)];
        var searchStart = 0;

        while (true)
        {
            var depStart = depsSection.IndexOf("<dependency>", searchStart, StringComparison.Ordinal);

            if (depStart < 0)
            {
                break;
            }

            var depEnd = depsSection.IndexOf("</dependency>", depStart, StringComparison.Ordinal);

            if (depEnd < 0)
            {
                break;
            }

            var depXml = depsSection[depStart..(depEnd + "</dependency>".Length)];
            searchStart = depEnd + 1;

            var depGroup = ExtractXmlElement(depXml, "groupId");
            var depArtifact = ExtractXmlElement(depXml, "artifactId");
            var depVersion = ExtractXmlElement(depXml, "version");

            if (depGroup is not null && depArtifact is not null)
            {
                var coord = $"{depGroup}:{depArtifact}";
                var ver = depVersion ?? "0.0.0";

                if (!ver.StartsWith('$'))
                {
                    dependencyVersions[coord] = ver;
                    dependencies.Add($"{coord}:{ver}");
                }
            }
        }
    }

    private static string? ExtractXmlElement(string xml, string elementName)
    {
        var openTag = $"<{elementName}>";
        var closeTag = $"</{elementName}>";

        var start = xml.IndexOf(openTag, StringComparison.Ordinal);

        if (start < 0)
        {
            return null;
        }

        start += openTag.Length;
        var end = xml.IndexOf(closeTag, start, StringComparison.Ordinal);

        if (end < 0)
        {
            return null;
        }

        return xml[start..end].Trim();
    }

    #endregion

    #region JSON 数据模型

    private class MavenSearchResponse
    {
        [JsonPropertyName("response")]
        public MavenResponseBody? Response { get; set; }
    }

    private class MavenResponseBody
    {
        [JsonPropertyName("docs")]
        public List<MavenDoc>? Docs { get; set; }
    }

    private class MavenDoc
    {
        [JsonPropertyName("g")]
        public string? GroupId { get; set; }

        [JsonPropertyName("a")]
        public string? ArtifactId { get; set; }

        [JsonPropertyName("v")]
        public string? Version { get; set; }
    }

    #endregion
}