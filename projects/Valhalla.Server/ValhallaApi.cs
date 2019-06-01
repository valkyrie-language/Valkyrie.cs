using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Valhalla;
using Valhalla.Audit;
using Valhalla.Authorization;
using Valhalla.Server.Storage;

namespace Valhalla.Server;

/// <summary>
/// 瓦尓哈拉 REST API 路由注册
/// </summary>
public static class ValhallaApi
{
    #region 存储路径常量

    private const string ManifestPrefix = "packages";
    private const string BinaryPrefix = "binaries";
    private const string AuditPrefix = "audit";
    private const string MetaPrefix = "meta";
    private const string OrgsPrefix = "orgs";

    #endregion

    /// <summary>
    /// 注册所有 API 路由
    /// </summary>
    /// <param name="app">Web 应用</param>
    /// <param name="storage">存储后端</param>
    public static void MapApiRoutes(this WebApplication app, IStorage storage)
    {
        #region 包检索

        app.MapGet("/api/packages", async (
            [FromQuery] int page,
            [FromQuery] int size,
            [FromQuery] string? query,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            size = size is <= 0 or > 100 ? 20 : size;

            var allKeys = await storage.ListAsync(ManifestPrefix, ct);
            var manifests = new List<PackageManifest>();
            foreach (string key in allKeys)
            {
                if (!key.EndsWith("/manifest.json"))
                {
                    continue;
                }

                string? content = await storage.ReadStringAsync(key, ct);
                if (content is null)
                {
                    continue;
                }

                try
                {
                    var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                    if (manifest is not null)
                    {
                        manifests.Add(manifest);
                    }
                }
                catch
                {
                    // 跳过损坏的 manifest
                }
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.ToLowerInvariant();
                manifests = manifests
                    .Where(m => m.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int total = manifests.Count;
            var pageItems = manifests
                .Skip((page - 1) * size)
                .Take(size)
                .Select(m =>
                {
                    var latestVersion = m.Versions.Values
                        .Where(v => v.Status == VersionStatus.Active)
                        .MaxBy(v => v.PublishedAt);

                    return new
                    {
                        name = m.Name,
                        description = "",
                        latestVersion = latestVersion?.Version ?? "0.0.0",
                        publisher = m.Publisher,
                        incarnation = m.Incarnation,
                        status = m.Status.ToString().ToLowerInvariant(),
                        downloadCount = 0L,
                        createdAt = m.RegisteredAt,
                        updatedAt = latestVersion?.PublishedAt ?? m.RegisteredAt
                    };
                })
                .ToList();

            return Results.Json(new
            {
                packages = pageItems,
                total,
                page,
                size
            });
        });

        app.MapGet("/api/search", async (
            [FromQuery] string q,
            [FromQuery] string? category,
            [FromQuery] string? author,
            [FromQuery] int page,
            [FromQuery] int size,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            size = size is <= 0 or > 100 ? 20 : size;

            var allKeys = await storage.ListAsync(ManifestPrefix, ct);
            var results = new List<object>();

            foreach (string key in allKeys)
            {
                if (!key.EndsWith("/manifest.json"))
                {
                    continue;
                }

                string? content = await storage.ReadStringAsync(key, ct);
                if (content is null)
                {
                    continue;
                }

                try
                {
                    var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                    if (manifest is null)
                    {
                        continue;
                    }

                    bool matches = manifest.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(category) && matches)
                    {
                        matches = manifest.Name.Contains(category, StringComparison.OrdinalIgnoreCase);
                    }

                    if (!string.IsNullOrWhiteSpace(author) && matches)
                    {
                        matches = manifest.Publisher.Contains(author, StringComparison.OrdinalIgnoreCase);
                    }

                    if (matches)
                    {
                        var latestVersion = manifest.Versions.Values
                            .Where(v => v.Status == VersionStatus.Active)
                            .MaxBy(v => v.PublishedAt);

                        results.Add(new
                        {
                            name = manifest.Name,
                            publisher = manifest.Publisher,
                            latestVersion = latestVersion?.Version ?? "0.0.0",
                            description = "",
                            author = manifest.Publisher,
                            registry = "valhalla"
                        });
                    }
                }
                catch
                {
                    // 跳过损坏的 manifest
                }
            }

            int total = results.Count;
            var pageItems = results.Skip((page - 1) * size).Take(size).ToList();

            return Results.Json(new { packages = pageItems, total, page, size });
        });

        app.MapGet("/api/packages/{name}/manifest", async (
            string name,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 的 manifest 已损坏"));
            }

            return Results.Json(manifest);
        });

        app.MapGet("/api/packages/{name}/versions", async (
            string name,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 的 manifest 已损坏"));
            }

            var versions = manifest.Versions.Values.Select(v => new
            {
                version = v.Version,
                status = v.Status.ToString().ToLowerInvariant(),
                packageDigest = v.PackageDigest,
                packageSize = v.PackageSize,
                publishedAt = v.PublishedAt,
                hasSource = v.SourceDigest is not null
            }).ToList();

            return Results.Json(versions);
        });

        app.MapGet("/api/packages/{name}/versions/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.Versions.TryGetValue(version, out var versionEntry) == true)
            {
                return Results.Json(versionEntry);
            }

            return Results.NotFound(ValhallaErrorResponse.NotFound($"版本 {name}@{version} 不存在"));
        });

        app.MapGet("/api/packages/{name}/versions/{version}/download", async (
            string name,
            string version,
            HttpContext context,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.Versions.TryGetValue(version, out var versionEntry) != true || versionEntry is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"版本 {name}@{version} 不存在"));
            }

            string packagePath = $"{BinaryPrefix}/{name}/{version}/package.nyar";
            byte[]? packageData = await storage.ReadBytesAsync(packagePath, ct);
            if (packageData is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"二进制 {name}@{version} 不存在"));
            }

            string? sourcePathKey = $"{BinaryPrefix}/{name}/{version}/source.tar.gz";
            byte[]? sourceData = await storage.ReadBytesAsync(sourcePathKey, ct);

            var output = new MemoryStream();
            byte[] sizeBytes = BitConverter.GetBytes(packageData.Length);
            output.Write(sizeBytes, 0, 4);
            output.Write(packageData, 0, packageData.Length);

            if (sourceData is not null)
            {
                byte[] srcSizeBytes = BitConverter.GetBytes(sourceData.Length);
                output.Write(srcSizeBytes, 0, 4);
                output.Write(sourceData, 0, sourceData.Length);
            }
            else
            {
                byte[] zero = BitConverter.GetBytes(0);
                output.Write(zero, 0, 4);
            }

            output.Position = 0;

            context.Response.Headers["X-Content-SHA256"] = versionEntry.PackageDigest ?? string.Empty;
            if (!string.IsNullOrEmpty(versionEntry.SourceDigest))
            {
                context.Response.Headers["X-Source-SHA256"] = versionEntry.SourceDigest;
            }

            return Results.File(output, "application/octet-stream");
        });

        #endregion

        #region 发布

        app.MapPost("/api/packages", async (
            HttpContext context,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            if (!body.RootElement.TryGetProperty("name", out var nameElement)
                || string.IsNullOrWhiteSpace(nameElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少包名"));
            }

            if (!body.RootElement.TryGetProperty("publisher", out var publisherElement)
                || string.IsNullOrWhiteSpace(publisherElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少发布者指纹"));
            }

            string? packageName = nameElement.GetString();
            string? publisher = publisherElement.GetString();

            var name = new PackageName(packageName!);
            string manifestPath = $"{ManifestPrefix}/{name.Canonical}/manifest.json";

            if (await storage.ExistsAsync(manifestPath, ct))
            {
                return Results.Conflict(ValhallaErrorResponse.Conflict($"包 {name.Canonical} 已存在"));
            }

            var manifest = new PackageManifest
            {
                Name = name.Canonical,
                Incarnation = 1,
                Publisher = publisher ?? "unknown",
                RegisteredAt = DateTime.UtcNow,
                Status = PackageStatus.Active
            };

            string manifestJson = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(manifestPath, manifestJson, ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.RegisterPackage,
                PackageName = name.Canonical,
                Actor = publisher ?? "unknown",
                ActorRole = "publisher",
                Details = new Dictionary<string, string> { ["action"] = "register" }
            }, ct);

            return Results.Json(manifest, statusCode: 201);
        });

        app.MapPost("/api/packages/{name}/versions", async (
            string name,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("请求必须是 multipart/form-data 格式"));
            }

            var form = await context.Request.ReadFormAsync(ct);

            string? manifestJson = form["manifest"];
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少 manifest 字段"));
            }

            var publishMeta = JsonSerializer.Deserialize<JsonDocument>(manifestJson);
            if (publishMeta is null
                || !publishMeta.RootElement.TryGetProperty("version", out var versionElement)
                || string.IsNullOrWhiteSpace(versionElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少版本号"));
            }

            string version = versionElement.GetString()!;

            var packageFile = form.Files.GetFile("package");
            if (packageFile is null)
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少 package 文件"));
            }

            using var packageMs = new MemoryStream();
            await packageFile.CopyToAsync(packageMs, ct);
            byte[] packageData = packageMs.ToArray();
            var packageDigest = ValhallaDigest.Compute(packageData);

            byte[]? sourceData = null;
            string? sourceDigestHex = null;

            var sourceFile = form.Files.GetFile("source");
            if (sourceFile is not null)
            {
                using var sourceMs = new MemoryStream();
                await sourceFile.CopyToAsync(sourceMs, ct);
                sourceData = sourceMs.ToArray();
                var sourceDigest = ValhallaDigest.Compute(sourceData);
                sourceDigestHex = sourceDigest.HexString;
            }

            // 更新 manifest
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? existingContent = await storage.ReadStringAsync(manifestPath, ct);
            if (existingContent is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(existingContent);
            if (manifest is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 的 manifest 已损坏"));
            }

            manifest.Versions[version] = new VersionEntry
            {
                Version = version,
                Status = VersionStatus.Active,
                PackageDigest = packageDigest.HexString,
                SourceDigest = sourceDigestHex,
                PackageSize = packageData.Length,
                SourceSize = sourceData?.Length,
                PublishedAt = DateTime.UtcNow
            };

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            // 存储二进制文件
            string packageStoragePath = $"{BinaryPrefix}/{name}/{version}/package.nyar";
            await storage.WriteBytesAsync(packageStoragePath, packageData, ct);

            if (sourceData is not null)
            {
                string sourceStoragePath = $"{BinaryPrefix}/{name}/{version}/source.tar.gz";
                await storage.WriteBytesAsync(sourceStoragePath, sourceData, ct);
            }

            string? publisherFingerprint = manifest.Publisher;

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Publish,
                PackageName = name,
                Version = version,
                Sha256 = packageDigest.HexString,
                Actor = publisherFingerprint ?? "unknown",
                ActorRole = "publisher",
                Details = new Dictionary<string, string>
                {
                    ["version"] = version,
                    ["size"] = packageData.Length.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                version,
                sha256 = packageDigest.HexString,
                sourceSha256 = sourceDigestHex,
                size = packageData.Length,
                publishedAt = DateTime.UtcNow
            }, statusCode: 201);
        });

        #endregion

        #region 管理操作

        app.MapPost("/api/packages/{name}/shield/{version}", async (
            string name,
            string version,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.Versions.TryGetValue(version, out var entry) != true || entry is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"版本 {name}@{version} 不存在"));
            }

            entry.Status = VersionStatus.Shielded;
            entry.ShieldReason = body.RootElement.TryGetProperty("reason", out var reason)
                ? reason.GetString() ?? string.Empty
                : string.Empty;
            entry.ShieldedAt = DateTime.UtcNow;

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Shield,
                PackageName = name,
                Version = version,
                Actor = "admin",
                ActorRole = "admin",
                Details = new Dictionary<string, string> { ["reason"] = entry.ShieldReason ?? "" }
            }, ct);

            return Results.Json(new { name, version, status = "shielded" });
        });

        app.MapPost("/api/packages/{name}/unshield/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.Versions.TryGetValue(version, out var unshieldEntry) != true || unshieldEntry is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"版本 {name}@{version} 不存在"));
            }

            unshieldEntry.Status = VersionStatus.Active;
            unshieldEntry.ShieldReason = string.Empty;
            unshieldEntry.ShieldedAt = null;

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Unshield,
                PackageName = name,
                Version = version,
                Actor = "admin",
                ActorRole = "admin"
            }, ct);

            return Results.Json(new { name, version, status = "active" });
        });

        app.MapDelete("/api/packages/{name}", async (
            string name,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 的 manifest 已损坏"));
            }

            manifest.Status = PackageStatus.Purged;
            manifest.PurgeReason = body.RootElement.TryGetProperty("reason", out var purgeReason)
                ? purgeReason.GetString()
                : null;
            manifest.PurgedAt = DateTime.UtcNow;

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Purge,
                PackageName = name,
                Actor = "admin",
                ActorRole = "admin",
                Details = new Dictionary<string, string>
                {
                    ["reason"] = manifest.PurgeReason ?? "",
                    ["incarnation"] = manifest.Incarnation.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                incarnation = manifest.Incarnation,
                status = "purged",
                purgedAt = manifest.PurgedAt,
                purgedBy = "admin"
            });
        });

        #endregion

        #region 审计日志

        app.MapGet("/api/packages/{name}/audit", async (
            string name,
            CancellationToken ct) =>
        {
            string auditPath = $"{AuditPrefix}/{name}/audit.jsonl";
            string? content = await storage.ReadStringAsync(auditPath, ct);
            if (content is null)
            {
                return Results.Json(Array.Empty<AuditEntry>());
            }

            var entries = new List<AuditEntry>();
            string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                    if (entry is not null)
                    {
                        entries.Add(entry);
                    }
                }
                catch
                {
                    // 跳过损坏的行
                }
            }

            return Results.Json(entries);
        });

        #endregion

        #region 组织

        app.MapGet("/api/orgs", async (CancellationToken ct) =>
        {
            var orgKeys = await storage.ListAsync(OrgsPrefix, ct);
            var orgs = new List<object>();
            foreach (string key in orgKeys)
            {
                if (key.EndsWith("/org.json"))
                {
                    string orgName = key.Replace($"{OrgsPrefix}/", "").Replace("/org.json", "");
                    orgs.Add(new { name = orgName });
                }
            }

            return Results.Json(orgs);
        });

        app.MapPost("/api/orgs", async (
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            if (!body.RootElement.TryGetProperty("name", out var nameElement)
                || string.IsNullOrWhiteSpace(nameElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少组织名"));
            }

            if (!body.RootElement.TryGetProperty("publisher", out var publisherElement)
                || string.IsNullOrWhiteSpace(publisherElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少发布者指纹"));
            }

            string? orgName = nameElement.GetString();
            string? publisher = publisherElement.GetString();

            var name = new PackageName(orgName!);
            string orgPath = $"{OrgsPrefix}/{name.Canonical}/org.json";

            if (await storage.ExistsAsync(orgPath, ct))
            {
                return Results.Conflict(ValhallaErrorResponse.Conflict($"组织 {name.Canonical} 已存在"));
            }

            string orgJson = JsonSerializer.Serialize(new
            {
                name = name.Canonical,
                publisher,
                registeredAt = DateTime.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(orgPath, orgJson, ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.RegisterOrg,
                PackageName = name.Canonical,
                Actor = publisher ?? "unknown",
                ActorRole = "publisher",
                Details = new Dictionary<string, string> { ["org"] = name.Canonical }
            }, ct);

            return Results.Json(new
            {
                name = name.Canonical,
                publisher,
                registeredAt = DateTime.UtcNow
            }, statusCode: 201);
        });

        #endregion

        #region 元信息

        app.MapGet("/api/packages/{name}/meta", async (
            string name,
            CancellationToken ct) =>
        {
            string metaPath = $"{MetaPrefix}/{name}/package-meta.json";
            string? content = await storage.ReadStringAsync(metaPath, ct);
            if (content is null)
            {
                return Results.Json(new ValhallaPackageMeta());
            }

            return Results.Json(JsonSerializer.Deserialize<ValhallaPackageMeta>(content));
        });

        app.MapGet("/api/packages/{name}/versions/{version}/meta", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            string metaPath = $"{MetaPrefix}/{name}/{version}/version-meta.json";
            string? content = await storage.ReadStringAsync(metaPath, ct);
            if (content is null)
            {
                return Results.Json(new ValhallaVersionMeta { Version = version });
            }

            return Results.Json(JsonSerializer.Deserialize<ValhallaVersionMeta>(content));
        });

        #endregion

        #region 元信息更新

        app.MapPut("/api/packages/{name}/meta", async (
            string name,
            [FromBody] ValhallaPackageMeta body,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            if (!await storage.ExistsAsync(manifestPath, ct))
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var meta = body;
            meta.UpdatedAt = DateTime.UtcNow;

            string metaPath = $"{MetaPrefix}/{name}/package-meta.json";
            string metaJson = JsonSerializer.Serialize(meta,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(metaPath, metaJson, ct);

            return Results.Json(meta);
        });

        app.MapPut("/api/packages/{name}/versions/{version}/meta", async (
            string name,
            string version,
            [FromBody] ValhallaVersionMeta body,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            if (!await storage.ExistsAsync(manifestPath, ct))
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var meta = body;
            meta.Version = version;

            string metaPath = $"{MetaPrefix}/{name}/{version}/version-meta.json";
            string metaJson = JsonSerializer.Serialize(meta,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(metaPath, metaJson, ct);

            return Results.Json(meta);
        });

        #endregion

        #region 授权管理

        app.MapPost("/api/packages/{name}/auth", async (
            string name,
            [FromBody] AuthorizationGrant grant,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            if (!await storage.ExistsAsync(manifestPath, ct))
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            if (string.IsNullOrWhiteSpace(grant.Namespace))
            {
                grant.Namespace = name;
            }

            if (string.IsNullOrWhiteSpace(grant.Grantee))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少被授权者公钥指纹"));
            }

            grant.IssuedAt = DateTime.UtcNow;

            string authPath = $"{ManifestPrefix}/{name}/auth/{grant.Grantee}/grant.json";
            string grantJson = JsonSerializer.Serialize(grant,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(authPath, grantJson, ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Authorize,
                PackageName = name,
                Actor = grant.Issuer,
                ActorRole = "publisher",
                Details = new Dictionary<string, string>
                {
                    ["grantee"] = grant.Grantee,
                    ["permissions"] = grant.Permissions.ToString(),
                    ["namespace"] = grant.Namespace
                }
            }, ct);

            return Results.Json(grant, statusCode: 201);
        });

        app.MapDelete("/api/packages/{name}/auth", async (
            string name,
            [FromBody] AuthorizationRevocation revocation,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            if (!await storage.ExistsAsync(manifestPath, ct))
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            if (string.IsNullOrWhiteSpace(revocation.Namespace))
            {
                revocation.Namespace = name;
            }

            if (string.IsNullOrWhiteSpace(revocation.Grantee))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少被撤销者公钥指纹"));
            }

            revocation.IssuedAt = DateTime.UtcNow;

            string revokePath = $"{ManifestPrefix}/{name}/auth/{revocation.Grantee}/revocation.json";
            string revokeJson = JsonSerializer.Serialize(revocation,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.WriteStringAsync(revokePath, revokeJson, ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.RevokeAuthorization,
                PackageName = name,
                Actor = revocation.Revoker,
                ActorRole = "publisher",
                Details = new Dictionary<string, string>
                {
                    ["grantee"] = revocation.Grantee,
                    ["namespace"] = revocation.Namespace
                }
            }, ct);

            return Results.Json(new
            {
                name,
                grantee = revocation.Grantee,
                namespace_ = revocation.Namespace,
                revokedAt = revocation.IssuedAt,
                revokedBy = revocation.Revoker
            });
        });

        #endregion

        #region 版本删除

        app.MapDelete("/api/packages/{name}/versions/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null || !manifest.Versions.TryGetValue(version, out _))
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"版本 {name}@{version} 不存在"));
            }

            manifest.Versions.Remove(version);

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            string packageBinaryPath = $"{BinaryPrefix}/{name}/{version}/package.nyar";
            await storage.DeleteAsync(packageBinaryPath, ct);

            string sourcePath = $"{BinaryPrefix}/{name}/{version}/source.tar.gz";
            if (await storage.ExistsAsync(sourcePath, ct))
            {
                await storage.DeleteAsync(sourcePath, ct);
            }

            string versionMetaPath = $"{MetaPrefix}/{name}/{version}/version-meta.json";
            if (await storage.ExistsAsync(versionMetaPath, ct))
            {
                await storage.DeleteAsync(versionMetaPath, ct);
            }

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.Purge,
                PackageName = name,
                Version = version,
                Actor = "admin",
                ActorRole = "admin",
                Details = new Dictionary<string, string>
                {
                    ["action"] = "delete-version",
                    ["version"] = version
                }
            }, ct);

            return Results.Json(new
            {
                name,
                version,
                deletedAt = DateTime.UtcNow,
                remainingVersions = manifest.Versions.Count
            });
        });

        #endregion

        #region 发布者转移

        app.MapPost("/api/packages/{name}/transfer", async (
            string name,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            string manifestPath = $"{ManifestPrefix}/{name}/manifest.json";
            string? content = await storage.ReadStringAsync(manifestPath, ct);
            if (content is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 不存在"));
            }

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null)
            {
                return Results.NotFound(ValhallaErrorResponse.NotFound($"包 {name} 的 manifest 已损坏"));
            }

            if (!body.RootElement.TryGetProperty("newPublisher", out var newPubElement)
                || string.IsNullOrWhiteSpace(newPubElement.GetString()))
            {
                return Results.BadRequest(ValhallaErrorResponse.ValidationError("缺少目标发布者公钥指纹"));
            }

            string oldPublisher = manifest.Publisher;
            string newPublisher = newPubElement.GetString()!;

            manifest.Publisher = newPublisher;
            manifest.Incarnation++;

            await storage.WriteStringAsync(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.TransferPublisher,
                PackageName = name,
                Actor = oldPublisher,
                ActorRole = "previous-publisher",
                Details = new Dictionary<string, string>
                {
                    ["oldPublisher"] = oldPublisher,
                    ["newPublisher"] = newPublisher,
                    ["incarnation"] = manifest.Incarnation.ToString()
                }
            }, ct);

            await WriteAuditAsync(storage, new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Operation = AuditOperation.TransferPublisher,
                PackageName = name,
                Actor = newPublisher,
                ActorRole = "new-publisher",
                Details = new Dictionary<string, string>
                {
                    ["oldPublisher"] = oldPublisher,
                    ["newPublisher"] = newPublisher,
                    ["incarnation"] = manifest.Incarnation.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                incarnation = manifest.Incarnation,
                oldPublisher,
                newPublisher,
                transferredAt = DateTime.UtcNow
            });
        });

        #endregion

        #region 统计

        app.MapGet("/api/stats", async (CancellationToken ct) =>
        {
            var allKeys = await storage.ListAsync("", ct);

            int packageCount = 0;
            int versionCount = 0;
            long totalSize = 0;

            foreach (string key in allKeys)
            {
                if (key.StartsWith($"{ManifestPrefix}/") && key.EndsWith("/manifest.json"))
                {
                    packageCount++;
                    string? content = await storage.ReadStringAsync(key, ct);
                    if (content is not null)
                    {
                        try
                        {
                            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                            if (manifest is not null)
                            {
                                versionCount += manifest.Versions.Count;
                                totalSize += manifest.Versions.Values.Sum(v => (long)v.PackageSize);
                            }
                        }
                        catch
                        {
                            // 跳过损坏的 manifest
                        }
                    }
                }
            }

            return Results.Json(new
            {
                packages = packageCount,
                versions = versionCount,
                totalSize,
                updatedAt = DateTime.UtcNow
            });
        });

        #endregion
    }

    #region 私有辅助

    /// <summary>
    /// 写入审计日志（追加到 JSONL 文件）
    /// </summary>
    private static async Task WriteAuditAsync(
        IStorage storage,
        AuditEntry entry,
        CancellationToken ct)
    {
        string auditPath = $"{AuditPrefix}/{entry.PackageName}/audit.jsonl";
        string? existing = await storage.ReadStringAsync(auditPath, ct);
        string line = JsonSerializer.Serialize(entry);

        string newContent = existing is null
            ? line + "\n"
            : existing + line + "\n";

        await storage.WriteStringAsync(auditPath, newContent, ct);
    }

    #endregion
}