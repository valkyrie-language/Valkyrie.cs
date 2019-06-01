using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Legion.Security;

/// <summary>
/// 安全审计引擎 — 漏洞扫描、许可证合规、签名验证、完整性校验
/// </summary>
public class SecurityAudit
{
    #region 常量

    private static readonly HashSet<string> _compatibleLicenses = new(StringComparer.OrdinalIgnoreCase)
    {
        "MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "0BSD",
        "ISC", "Unlicense", "CC0-1.0", "WTFPL", "Zlib"
    };

    private static readonly HashSet<string> _restrictedLicenses = new(StringComparer.OrdinalIgnoreCase)
    {
        "GPL-2.0-only", "GPL-2.0-or-later", "GPL-3.0-only", "GPL-3.0-or-later",
        "AGPL-3.0-only", "AGPL-3.0-or-later", "SSPL-1.0", "BUSL-1.1"
    };

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string OsvApiEndpoint = "https://api.osv.dev/v1/query";
    private const string VulnCacheFileName = "vulnerability-cache.json";

    #endregion

    #region 字段

    private readonly string _cacheDir;
    private readonly Dictionary<string, List<VulnerabilityReport>> _vulnCache;
    private readonly object _cacheLock = new();

    #endregion

    #region 构造函数

    public SecurityAudit()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".valkyrie", "cache");

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        _vulnCache = LoadVulnerabilityCache();
    }

    #endregion

    #region 公开 API

    /// <summary>
    /// 审计单个包
    /// </summary>
    public async Task<SecurityAuditResult> AuditPackageAsync(Registry.Package package)
    {
        var result = new SecurityAuditResult();

        result.Vulnerabilities = await ScanVulnerabilitiesAsync(package);
        result.Licenses = await CheckLicenseAsync(package);

        return result;
    }

    /// <summary>
    /// 审计依赖列表
    /// </summary>
    public async Task<SecurityAuditResult> AuditDependenciesAsync(List<Registry.Package> packages)
    {
        var result = new SecurityAuditResult();

        foreach (var package in packages)
        {
            var vulns = await ScanVulnerabilitiesAsync(package);
            result.Vulnerabilities.AddRange(vulns);

            var licenses = await CheckLicenseAsync(package);
            result.Licenses.AddRange(licenses);
        }

        return result;
    }

    /// <summary>
    /// 审计依赖列表，返回可修复的版本建议
    /// </summary>
    public Dictionary<string, string> GetFixSuggestions(List<VulnerabilityReport> vulnerabilities)
    {
        var suggestions = new Dictionary<string, string>();

        foreach (var vuln in vulnerabilities.Where(v => v.IsFixable))
        {
            string key = $"{vuln.PackageName}@{vuln.Version}";

            if (!suggestions.ContainsKey(key) || IsHigherVersion(vuln.FixedVersion!, suggestions[key]))
            {
                suggestions[key] = vuln.FixedVersion!;
            }
        }

        return suggestions;
    }

    /// <summary>
    /// CRC64 完整性校验
    /// </summary>
    public bool VerifyIntegrity(string packagePath, string expectedHash)
    {
        if (!File.Exists(packagePath) && !Directory.Exists(packagePath))
        {
            return false;
        }

        string actualHash;

        if (File.Exists(packagePath))
        {
            actualHash = ComputeFileHash(packagePath);
        }
        else
        {
            actualHash = ComputeDirectoryHash(packagePath);
        }

        if (expectedHash.StartsWith("sha512-"))
        {
            return actualHash == expectedHash;
        }

        if (expectedHash.StartsWith("sha256-"))
        {
            string sha256Hash = File.Exists(packagePath)
                ? ComputeFileHashSha256(packagePath)
                : ComputeDirectoryHashSha256(packagePath);
            return sha256Hash == expectedHash;
        }

        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// RSA 签名验证
    /// </summary>
    public SignatureVerificationResult VerifySignature(string packagePath, string? publicKeyPath = null)
    {
        var result = new SignatureVerificationResult();

        string signaturePath = packagePath + ".sig";
        if (!File.Exists(signaturePath))
        {
            result.IsValid = false;
            result.Error = "未找到签名文件";
            return result;
        }

        if (publicKeyPath is not null && !File.Exists(publicKeyPath))
        {
            result.IsValid = false;
            result.Error = "未找到公钥文件";
            return result;
        }

        try
        {
            byte[] signatureBytes = File.ReadAllBytes(signaturePath);
            byte[] dataBytes = File.Exists(packagePath)
                ? File.ReadAllBytes(packagePath)
                : ComputeDirectoryHashRaw(packagePath);

            if (publicKeyPath is not null)
            {
                byte[] publicKeyBytes = File.ReadAllBytes(publicKeyPath);

                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                result.IsValid = rsa.VerifyData(
                    dataBytes, signatureBytes, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            else
            {
                result.IsValid = VerifySignatureWithEmbeddedKey(dataBytes, signatureBytes);
            }

            result.Signer = result.IsValid ? "verified" : "unverified";
        }
        catch (CryptographicException ex)
        {
            result.IsValid = false;
            result.Error = $"签名验证失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Ed25519 签名验证（Valhalla 注册中心包签名）
    /// </summary>
    /// <param name="data">待验证的原始数据</param>
    /// <param name="signature">64 字节 Ed25519 签名</param>
    /// <param name="publicKey">32 字节 Ed25519 公钥</param>
    public static SignatureVerificationResult VerifyEd25519Signature(byte[] data, byte[] signature, byte[] publicKey)
    {
        var result = new SignatureVerificationResult();

        if (publicKey.Length != 32)
        {
            result.IsValid = false;
            result.Error = "Ed25519 公钥长度无效，应为 32 字节";
            return result;
        }

        if (signature.Length != 64)
        {
            result.IsValid = false;
            result.Error = "Ed25519 签名长度无效，应为 64 字节";
            return result;
        }

        try
        {
            var nsecType = Type.GetType(
                "NSec.Cryptography.SignatureAlgorithm, NSec.Cryptography");

            if (nsecType is null)
            {
                result.IsValid = false;
                result.Error = "NSec.Cryptography 未安装，无法进行 Ed25519 签名验证。请安装 NSec.Cryptography 包";
                return result;
            }

            var algorithmProp = nsecType.GetProperty("Ed25519",
                BindingFlags.Public | BindingFlags.Static);

            if (algorithmProp is null)
            {
                result.IsValid = false;
                result.Error = "无法获取 Ed25519 算法实例";
                return result;
            }

            var algorithm = algorithmProp.GetValue(null);

            var publicKeyType = Type.GetType(
                "NSec.Cryptography.PublicKey, NSec.Cryptography");

            var importMethod = publicKeyType?.GetMethod("Import",
                BindingFlags.Public | BindingFlags.Static,
                new[]
                {
                    nsecType,
                    typeof(byte[]),
                    Type.GetType("NSec.Cryptography.KeyBlobFormat, NSec.Cryptography")!
                });

            var keyBlobFormat = Type.GetType(
                "NSec.Cryptography.KeyBlobFormat, NSec.Cryptography");

            var rawFormat = keyBlobFormat?.GetField("RawPublicKey",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

            var publicKeyObj = importMethod?.Invoke(null,
                new[] { algorithm, publicKey, rawFormat });

            if (publicKeyObj is null)
            {
                result.IsValid = false;
                result.Error = "无法导入 Ed25519 公钥";
                return result;
            }

            var verifyMethod = nsecType.GetMethod("Verify",
                BindingFlags.Public | BindingFlags.Instance);

            var verifyResult = (bool)verifyMethod!.Invoke(algorithm,
                new[] { publicKeyObj, data, signature })!;

            result.IsValid = verifyResult;
            result.Signer = verifyResult
                ? $"ed25519:{Convert.ToHexStringLower(publicKey)}"
                : "unverified";
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Error = $"Ed25519 签名验证异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 检查许可证是否兼容
    /// </summary>
    public bool IsLicenseCompatible(string license)
    {
        return _compatibleLicenses.Contains(license);
    }

    /// <summary>
    /// 检查许可证是否受限
    /// </summary>
    public bool IsLicenseRestricted(string license)
    {
        return _restrictedLicenses.Contains(license);
    }

    /// <summary>
    /// 计算单个文件的 SHA256 哈希
    /// </summary>
    public static string ComputeSha256Hash(string filePath)
    {
        return ComputeFileHashSha256(filePath);
    }

    /// <summary>
    /// 计算目录的 SHA256 哈希
    /// </summary>
    public static string ComputeSha256HashDirectory(string directoryPath)
    {
        return ComputeDirectoryHashSha256(directoryPath);
    }

    #endregion

    #region 本地漏洞缓存

    /// <summary>
    /// 从本地缓存文件加载漏洞数据
    /// </summary>
    private Dictionary<string, List<VulnerabilityReport>> LoadVulnerabilityCache()
    {
        string cachePath = Path.Combine(_cacheDir, VulnCacheFileName);

        if (!File.Exists(cachePath))
        {
            return new Dictionary<string, List<VulnerabilityReport>>();
        }

        try
        {
            string json = File.ReadAllText(cachePath);
            var cache = JsonSerializer.Deserialize<Dictionary<string, List<VulnerabilityReport>>>(json);

            if (cache is null)
            {
                return new Dictionary<string, List<VulnerabilityReport>>();
            }

            var expiredKeys = new List<string>();
            var cacheMaxAge = TimeSpan.FromDays(1);
            var now = DateTime.UtcNow;

            foreach (var kvp in cache)
            {
                foreach (var vuln in kvp.Value)
                {
                    if (vuln.CacheTimestamp is DateTime ts && now - ts > cacheMaxAge)
                    {
                        expiredKeys.Add(kvp.Key);
                        break;
                    }
                }
            }

            foreach (var key in expiredKeys)
            {
                cache.Remove(key);
            }

            return cache;
        }
        catch
        {
            return new Dictionary<string, List<VulnerabilityReport>>();
        }
    }

    /// <summary>
    /// 保存漏洞数据到本地缓存
    /// </summary>
    private void SaveVulnerabilityCache()
    {
        lock (_cacheLock)
        {
            try
            {
                string cachePath = Path.Combine(_cacheDir, VulnCacheFileName);
                string json = JsonSerializer.Serialize(_vulnCache,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(cachePath, json);
            }
            catch
            {
                // 缓存写入失败不影响主流程
            }
        }
    }

    /// <summary>
    /// 获取缓存中的漏洞数据
    /// </summary>
    private List<VulnerabilityReport>? GetCachedVulnerabilities(string packageName, string version)
    {
        lock (_cacheLock)
        {
            string key = $"{packageName}@{version}";
            return _vulnCache.TryGetValue(key, out var vulns) ? vulns : null;
        }
    }

    /// <summary>
    /// 缓存漏洞数据
    /// </summary>
    private void CacheVulnerabilities(string packageName, string version, List<VulnerabilityReport> vulns)
    {
        lock (_cacheLock)
        {
            string key = $"{packageName}@{version}";

            foreach (var vuln in vulns)
            {
                vuln.CacheTimestamp = DateTime.UtcNow;
            }

            _vulnCache[key] = vulns;
        }

        SaveVulnerabilityCache();
    }

    #endregion

    #region 漏洞扫描

    private async Task<List<VulnerabilityReport>> ScanVulnerabilitiesAsync(Registry.Package package)
    {
        var cached = GetCachedVulnerabilities(package.Name, package.Version);
        if (cached is not null)
        {
            return cached;
        }

        var reports = new List<VulnerabilityReport>();

        try
        {
            var queryPayload = new
            {
                package = new
                {
                    name = package.Name,
                    version = package.Version
                }
            };

            string jsonPayload = JsonSerializer.Serialize(queryPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OsvApiEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                CacheVulnerabilities(package.Name, package.Version, reports);
                return reports;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("vulns", out var vulnsArray))
            {
                CacheVulnerabilities(package.Name, package.Version, reports);
                return reports;
            }

            foreach (var vuln in vulnsArray.EnumerateArray())
            {
                var report = new VulnerabilityReport
                {
                    PackageName = package.Name,
                    Version = package.Version,
                    VulnerabilityId = vuln.TryGetProperty("id", out var id) ? id.GetString() : null
                };

                if (vuln.TryGetProperty("summary", out var summary))
                {
                    report.Title = summary.GetString() ?? string.Empty;
                }

                if (vuln.TryGetProperty("aliases", out var aliases) &&
                    aliases.ValueKind == JsonValueKind.Array)
                {
                    var cveId = aliases.EnumerateArray()
                        .Select(a => a.GetString())
                        .FirstOrDefault(a => a is not null && a.StartsWith("CVE-"));

                    report.VulnerabilityId ??= cveId;
                }

                if (vuln.TryGetProperty("database_specific", out var dbSpecific))
                {
                    if (dbSpecific.TryGetProperty("severity", out var severity))
                    {
                        report.Severity = severity.GetString() ?? "unknown";
                    }

                    if (dbSpecific.TryGetProperty("cwe_id", out var cwe))
                    {
                        report.CweId = cwe.GetString();
                    }
                }

                if (vuln.TryGetProperty("severity", out var severityArray) &&
                    severityArray.ValueKind == JsonValueKind.Array)
                {
                    var firstSeverity = severityArray.EnumerateArray().FirstOrDefault();
                    if (firstSeverity.TryGetProperty("score", out var score))
                    {
                        report.Severity = score.GetString() ?? "unknown";
                    }
                }

                ParseFixedVersion(vuln, report);
                ParseCvssScore(vuln, report);

                if (vuln.TryGetProperty("references", out var refs) &&
                    refs.ValueKind == JsonValueKind.Array)
                {
                    var firstRef = refs.EnumerateArray().FirstOrDefault();
                    if (firstRef.TryGetProperty("url", out var url))
                    {
                        report.Url = url.GetString();
                    }
                }

                reports.Add(report);
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (HttpRequestException)
        {
        }
        catch (JsonException)
        {
        }

        CacheVulnerabilities(package.Name, package.Version, reports);
        return reports;
    }

    /// <summary>
    /// 从 OSV 响应中解析修复版本
    /// </summary>
    private static void ParseFixedVersion(JsonElement vuln, VulnerabilityReport report)
    {
        if (!vuln.TryGetProperty("affected", out var affected) ||
            affected.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in affected.EnumerateArray())
        {
            if (!entry.TryGetProperty("ranges", out var ranges) ||
                ranges.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var range in ranges.EnumerateArray())
            {
                if (!range.TryGetProperty("type", out var rangeType) ||
                    rangeType.GetString() != "ECOSYSTEM")
                {
                    continue;
                }

                if (range.TryGetProperty("events", out var events) &&
                    events.ValueKind == JsonValueKind.Array)
                {
                    foreach (var evt in events.EnumerateArray())
                    {
                        if (evt.TryGetProperty("fixed", out var fixed_))
                        {
                            report.FixedVersion = fixed_.GetString();
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 从 OSV 响应中解析 CVSS 评分
    /// </summary>
    private static void ParseCvssScore(JsonElement vuln, VulnerabilityReport report)
    {
        if (!vuln.TryGetProperty("severity", out var sevs) ||
            sevs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var sev in sevs.EnumerateArray())
        {
            if (!sev.TryGetProperty("type", out var sevType) ||
                sevType.GetString() != "CVSS_V3")
            {
                continue;
            }

            if (sev.TryGetProperty("score", out var score) &&
                score.GetString() is string scoreStr &&
                double.TryParse(scoreStr, out var scoreVal))
            {
                report.CvssScore = scoreVal;
                return;
            }
        }
    }

    #endregion

    #region 许可证检查

    private Task<List<LicenseInfo>> CheckLicenseAsync(Registry.Package package)
    {
        var licenses = new List<LicenseInfo>
        {
            new LicenseInfo
            {
                PackageName = package.Name,
                Version = package.Version,
                License = !string.IsNullOrEmpty(package.License) ? package.License : "unknown",
                IsCompatible = !string.IsNullOrEmpty(package.License) && IsLicenseCompatible(package.License),
                IsRestricted = !string.IsNullOrEmpty(package.License) && IsLicenseRestricted(package.License)
            }
        };

        return Task.FromResult(licenses);
    }

    #endregion

    #region 版本比较辅助

    private static bool IsHigherVersion(string a, string b)
    {
        return string.Compare(a, b, StringComparison.Ordinal) > 0;
    }

    #endregion

    #region 哈希计算

    private static string ComputeFileHash(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha512.ComputeHash(stream);

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    private static string ComputeDirectoryHash(string directoryPath)
    {
        using var sha512 = SHA512.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            string relativePath = Path.GetRelativePath(directoryPath, filePath);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha512.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }

        sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] hash = sha512.Hash!;

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    private static string ComputeFileHashSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);

        return $"sha256-{Convert.ToBase64String(hash)}";
    }

    private static string ComputeDirectoryHashSha256(string directoryPath)
    {
        using var sha256 = SHA256.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            string relativePath = Path.GetRelativePath(directoryPath, filePath);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] hash = sha256.Hash!;

        return $"sha256-{Convert.ToBase64String(hash)}";
    }

    private static byte[] ComputeDirectoryHashRaw(string directoryPath)
    {
        using var sha256 = SHA256.Create();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            string relativePath = Path.GetRelativePath(directoryPath, filePath);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return sha256.Hash!;
    }

    #endregion

    #region 签名验证

    private bool VerifySignatureWithEmbeddedKey(byte[] data, byte[] signature)
    {
        string trustedKeysDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".valkyrie", "trusted-keys");

        if (!Directory.Exists(trustedKeysDir))
        {
            return false;
        }

        foreach (var keyFile in Directory.GetFiles(trustedKeysDir, "*.pub"))
        {
            try
            {
                byte[] publicKeyBytes = File.ReadAllBytes(keyFile);
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                if (rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    return true;
                }
            }
            catch (CryptographicException)
            {
                continue;
            }
        }

        return false;
    }

    #endregion
}