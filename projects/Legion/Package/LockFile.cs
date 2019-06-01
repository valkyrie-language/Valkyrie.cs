using System.Security.Cryptography;
using System.Text;
using Legion.Tools;
using Legion.Version;
using Oak.Data;
using Oak.Von;

namespace Legion.Package;

public class LockFile
{
    public string Version { get; set; } = "1";
    public Dictionary<string, LockEntry> Packages { get; set; } = new();

    private readonly string _filePath;
    private readonly GonParser _parser = new();

    public LockFile(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "legion-lock.von");
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            Packages = new Dictionary<string, LockEntry>();
            return;
        }

        string content = File.ReadAllText(_filePath, Encoding.UTF8);
        var root = _parser.Deserialize(content);

        if (root.Type != SerdeValueType.Object)
        {
            Packages = new Dictionary<string, LockEntry>();
            return;
        }

        Version = root.GetField("version")?.GetString() ?? "1";

        var packagesField = root.GetField("packages");
        if (packagesField?.Type == SerdeValueType.Object && packagesField.Fields is not null)
        {
            Packages = new Dictionary<string, LockEntry>();
            foreach (var kvp in packagesField.Fields)
            {
                var entryObj = kvp.Value;
                if (entryObj.Type != SerdeValueType.Object)
                {
                    continue;
                }

                var entry = new LockEntry
                {
                    Name = entryObj.GetField("name")?.GetString() ?? string.Empty,
                    Version = entryObj.GetField("version")?.GetString() ?? string.Empty,
                    Registry = entryObj.GetField("registry")?.GetString() ?? string.Empty,
                    Resolved = entryObj.GetField("resolved")?.GetString() ?? string.Empty,
                    Integrity = entryObj.GetField("integrity")?.GetString() ?? string.Empty
                };

                var depsField = entryObj.GetField("dependencies");
                if (depsField?.Type == SerdeValueType.Array && depsField.Elements is not null)
                {
                    entry.Dependencies = depsField.Elements
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                Packages[kvp.Key] = entry;
            }
        }
    }

    public void Save()
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var packagesFields = new Dictionary<string, SerdeValue>();
        foreach (var kvp in Packages.OrderBy(p => p.Key))
        {
            var entryFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.String(kvp.Value.Name),
                ["version"] = SerdeValue.String(kvp.Value.Version),
                ["registry"] = SerdeValue.String(kvp.Value.Registry),
                ["resolved"] = SerdeValue.String(kvp.Value.Resolved),
                ["integrity"] = SerdeValue.String(kvp.Value.Integrity)
            };

            if (!string.IsNullOrEmpty(kvp.Value.License))
            {
                entryFields["license"] = SerdeValue.String(kvp.Value.License);
            }

            if (kvp.Value.IsDev)
            {
                entryFields["is_dev"] = SerdeValue.Boolean(true);
            }

            if (kvp.Value.IsWorkspace)
            {
                entryFields["is_workspace"] = SerdeValue.Boolean(true);
            }

            if (!string.IsNullOrEmpty(kvp.Value.InstallPath))
            {
                entryFields["install_path"] = SerdeValue.String(kvp.Value.InstallPath);
            }

            if (kvp.Value.Dependencies is not null && kvp.Value.Dependencies.Count > 0)
            {
                entryFields["dependencies"] = SerdeValue.Array(
                    kvp.Value.Dependencies.Select(d => SerdeValue.String(d)).ToList());
            }

            packagesFields[kvp.Key] = SerdeValue.Object(entryFields);
        }

        var rootFields = new Dictionary<string, SerdeValue>
        {
            ["version"] = SerdeValue.String(Version),
            ["packages"] = SerdeValue.Object(packagesFields)
        };

        var root = SerdeValue.Object(rootFields);
        string content = VonFormatter.Format(root);
        File.WriteAllText(_filePath, content, Encoding.UTF8);
    }

    public void AddPackage(Registry.Package package, string registryName, string resolvedUrl)
    {
        string key = $"{package.Name}@{package.Version}";

        string integrity = package.DistIntegrity ?? ComputePackageIntegrity(package);

        Packages[key] = new LockEntry
        {
            Name = package.Name,
            Version = package.Version,
            Registry = registryName,
            Resolved = resolvedUrl,
            Integrity = integrity,
            Dependencies = package.Dependencies ?? new List<string>()
        };
    }

    public void RemovePackage(string packageName)
    {
        var keysToRemove = Packages.Keys
            .Where(k => k.StartsWith($"{packageName}@"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            Packages.Remove(key);
        }
    }

    public LockEntry? GetPackage(string packageName, string? version = null)
    {
        if (version is not null)
        {
            string key = $"{packageName}@{version}";
            return Packages.TryGetValue(key, out var entry) ? entry : null;
        }

        var matchingKey = Packages.Keys.FirstOrDefault(k => k.StartsWith($"{packageName}@"));
        return matchingKey is not null ? Packages[matchingKey] : null;
    }

    public bool IsPackageLocked(string packageName, string version)
    {
        string key = $"{packageName}@{version}";
        return Packages.ContainsKey(key);
    }

    public List<LockEntry> GetAllPackages()
    {
        return Packages.Values.ToList();
    }

    public void Clear()
    {
        Packages.Clear();
    }

    public static string ComputePackageIntegrity(Registry.Package package)
    {
        using var sha512 = SHA512.Create();

        string data = $"{package.Name}@{package.Version}";
        byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    public static string ComputeFileIntegrity(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha512.ComputeHash(stream);

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// 验证锁文件中所有包的完整性，返回校验结果
    /// </summary>
    /// <param name="vendorsDir">依赖安装目录</param>
    /// <returns>校验失败的包列表（空列表表示全部通过）</returns>
    public List<IntegrityCheckResult> VerifyIntegrity(string vendorsDir)
    {
        var failures = new List<IntegrityCheckResult>();

        foreach (var kvp in Packages)
        {
            var entry = kvp.Value;

            if (entry.IsWorkspace)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.Integrity))
            {
                failures.Add(new IntegrityCheckResult
                {
                    PackageName = entry.Name,
                    Version = entry.Version,
                    Issue = IntegrityIssue.MissingIntegrity,
                    Message = "缺少完整性哈希"
                });
                continue;
            }

            var installPath = string.IsNullOrEmpty(entry.InstallPath)
                ? Path.Combine(vendorsDir, entry.Name)
                : Path.Combine(vendorsDir, entry.InstallPath);

            if (!Directory.Exists(installPath))
            {
                failures.Add(new IntegrityCheckResult
                {
                    PackageName = entry.Name,
                    Version = entry.Version,
                    Issue = IntegrityIssue.MissingPackage,
                    Message = $"安装目录不存在：{installPath}"
                });
                continue;
            }

            var packageHash = ComputeDirectoryIntegrity(installPath);
            var expectedPrefix = ExtractHashPrefix(entry.Integrity);

            if (!string.IsNullOrEmpty(expectedPrefix) && !packageHash.StartsWith(expectedPrefix))
            {
                failures.Add(new IntegrityCheckResult
                {
                    PackageName = entry.Name,
                    Version = entry.Version,
                    Issue = IntegrityIssue.HashMismatch,
                    Message = $"完整性校验失败：期望 {entry.Integrity[..Math.Min(24, entry.Integrity.Length)]}...，实际 {packageHash[..Math.Min(24, packageHash.Length)]}..."
                });
            }
        }

        return failures;
    }

    /// <summary>
    /// 计算目录的完整性哈希（递归所有文件）
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>sha512-Base64 格式的哈希值</returns>
    public static string ComputeDirectoryIntegrity(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return string.Empty;
        }

        using var sha512 = SHA512.Create();

        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            var relativePath = file[(directoryPath.Length + 1)..];
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha512.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var stream = File.OpenRead(file);
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }

        sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return $"sha512-{Convert.ToBase64String(sha512.Hash!)}";
    }

    /// <summary>
    /// 从完整性哈希中提取前缀用于快速比较
    /// </summary>
    private static string ExtractHashPrefix(string integrity)
    {
        var dashIndex = integrity.IndexOf('-');
        if (dashIndex < 0 || dashIndex + 1 >= integrity.Length)
        {
            return string.Empty;
        }

        return integrity[(dashIndex + 1)..];
    }

    /// <summary>
    /// 检查锁文件是否与 manifest 一致（检测是否需要重新安装）
    /// </summary>
    /// <param name="manifest">项目清单</param>
    /// <returns>不一致的依赖列表</returns>
    public List<string> DetectDrift(LegionManifest manifest)
    {
        var drifted = new List<string>();

        foreach (var dep in manifest.Dependencies)
        {
            var locked = GetPackage(dep.Key);
            if (locked is null)
            {
                drifted.Add(dep.Key);
                continue;
            }

            if (!SatisfiesConstraint(locked.Version, dep.Value))
            {
                drifted.Add(dep.Key);
            }
        }

        foreach (var dep in manifest.DevDependencies)
        {
            var locked = GetPackage(dep.Key);
            if (locked is null)
            {
                drifted.Add(dep.Key);
                continue;
            }

            if (!SatisfiesConstraint(locked.Version, dep.Value))
            {
                drifted.Add(dep.Key);
            }
        }

        return drifted;
    }

    /// <summary>
    /// 检查版本号是否满足约束条件
    /// </summary>
    private static bool SatisfiesConstraint(string version, string constraint)
    {
        if (constraint == "*" || constraint == "latest")
        {
            return true;
        }

        if (constraint == version)
        {
            return true;
        }

        if (constraint.StartsWith('^'))
        {
            var minVersion = constraint[1..];
            var minSemVer = SemanticVersion.Parse(minVersion);
            var actualSemVer = SemanticVersion.Parse(version);
            return actualSemVer >= minSemVer &&
                   actualSemVer.Major == minSemVer.Major;
        }

        if (constraint.StartsWith('~'))
        {
            var minVersion = constraint[1..];
            var minSemVer = SemanticVersion.Parse(minVersion);
            var actualSemVer = SemanticVersion.Parse(version);
            return actualSemVer >= minSemVer &&
                   actualSemVer.Major == minSemVer.Major &&
                   actualSemVer.Minor == minSemVer.Minor;
        }

        return version == constraint;
    }
}

/// <summary>
/// 完整性校验结果
/// </summary>
public class IntegrityCheckResult
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 校验问题类型
    /// </summary>
    public IntegrityIssue Issue { get; set; }

    /// <summary>
    /// 问题描述
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 完整性校验问题类型
/// </summary>
public enum IntegrityIssue
{
    /// <summary>
    /// 缺少完整性哈希
    /// </summary>
    MissingIntegrity,

    /// <summary>
    /// 安装目录缺失
    /// </summary>
    MissingPackage,

    /// <summary>
    /// 哈希值不匹配
    /// </summary>
    HashMismatch
}