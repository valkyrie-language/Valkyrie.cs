using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Valhalla.Audit;

namespace Valhalla;

/// <summary>
/// 包注册中心 API 端点
/// </summary>
public class RegistryApi
{
    private readonly string _registryRoot;
    private readonly AuditLog _auditLog;

    public RegistryApi(string registryRoot, AuditLog auditLog)
    {
        _registryRoot = registryRoot;
        _auditLog = auditLog;
    }

    /// <summary>
    ///     获取所有包列表
    /// </summary>
    public List<string> ListPackages()
    {
        var manifestDir = Path.Combine(_registryRoot, "manifests");
        if (!Directory.Exists(manifestDir))
        {
            return new List<string>();
        }

        return Directory.GetFiles(manifestDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .ToList();
    }

    /// <summary>
    ///     获取包清单
    /// </summary>
    public PackageManifest? GetPackage(string name)
    {
        var path = Path.Combine(_registryRoot, "manifests", $"{name}.json");
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PackageManifest>(json);
    }

    /// <summary>
    ///     注册新包
    /// </summary>
    public bool RegisterPackage(PackageManifest manifest, string actor)
    {
        var existing = GetPackage(manifest.Name);
        if (existing is not null)
        {
            return false;
        }

        var path = Path.Combine(_registryRoot, "manifests", $"{manifest.Name}.json");
        manifest.RegisteredAt = DateTime.UtcNow;
        manifest.Incarnation = 1;

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _auditLog.Append(new AuditEntry
        {
            Operation = AuditOperation.RegisterPackage,
            PackageName = manifest.Name,
            Actor = actor,
            Details = new Dictionary<string, string> { ["publisher"] = manifest.Publisher }
        });

        return true;
    }

    /// <summary>
    ///     发布包版本
    /// </summary>
    public bool PublishVersion(string packageName, VersionEntry version, string actor,
        string? signature = null)
    {
        var manifest = GetPackage(packageName);
        if (manifest is null)
        {
            return false;
        }

        if (manifest.Versions.ContainsKey(version.Version))
        {
            return false;
        }

        manifest.Versions[version.Version] = version;
        manifest.Incarnation++;

        var path = Path.Combine(_registryRoot, "manifests", $"{packageName}.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _auditLog.LogPublish(packageName, version.Version, actor,
            version.PackageDigest);

        return true;
    }

    /// <summary>
    ///     查询包的审计日志
    /// </summary>
    public List<AuditEntry> GetAuditLog(string packageName)
    {
        return _auditLog.QueryByPackage(packageName);
    }

    /// <summary>
    ///     搜索包（按名称前缀）
    /// </summary>
    public List<PackageSearchResult> Search(string query)
    {
        var results = new List<PackageSearchResult>();
        foreach (var name in ListPackages())
        {
            if (!name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var manifest = GetPackage(name);
            if (manifest is null || manifest.Status == PackageStatus.Purged)
            {
                continue;
            }

            var latest = manifest.Versions.Values
                .OrderByDescending(v => v.PublishedAt)
                .FirstOrDefault();

            results.Add(new PackageSearchResult
            {
                Name = name,
                Description = latest?.PackageDigest ?? string.Empty,
                LatestVersion = latest?.Version ?? "0.0.0",
                PublishedAt = latest?.PublishedAt ?? DateTime.MinValue
            });
        }

        return results;
    }

    /// <summary>
    ///     屏蔽指定版本
    /// </summary>
    public bool ShieldVersion(string packageName, string version, string reason, string actor)
    {
        var manifest = GetPackage(packageName);
        if (manifest is null || !manifest.Versions.TryGetValue(version, out var entry))
        {
            return false;
        }

        entry.Status = VersionStatus.Shielded;
        entry.ShieldReason = reason;
        entry.ShieldedAt = DateTime.UtcNow;

        var path = Path.Combine(_registryRoot, "manifests", $"{packageName}.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        _auditLog.LogShield(packageName, version, actor, reason);
        return true;
    }
}