using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Legion.Tools;

namespace Legion.Package;

/// <summary>
/// 包打包器，将项目目录打包为发布用的 tarball
/// </summary>
public class PackagePacker
{
    /// <summary>
    /// 将项目目录打包为 tarball 字节数组
    /// </summary>
    /// <param name="packageDirectory">项目根目录</param>
    /// <param name="ignore">忽略规则（可选）</param>
    /// <returns>tarball 字节数组和 SHA-256 哈希</returns>
    public static PackResult Pack(string packageDirectory, LegionIgnore? ignore = null)
    {
        using var ms = new MemoryStream();
        string manifestContent = string.Empty;

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var files = GetFilesToPack(packageDirectory, ignore);

            foreach (var file in files)
            {
                var relativePath = file[(packageDirectory.Length + 1)..];
                var entry = zip.CreateEntry(relativePath, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                fileStream.CopyTo(entryStream);

                if (relativePath.Equals("legion.von", StringComparison.OrdinalIgnoreCase))
                {
                    manifestContent = File.ReadAllText(file, Encoding.UTF8);
                }
            }

            var metaEntry = zip.CreateEntry("package.json", CompressionLevel.Optimal);
            using var metaStream = metaEntry.Open();
            var meta = new PackageMeta
            {
                PackedAt = DateTime.UtcNow.ToString("O"),
                PackerVersion = "1.0.0"
            };

            if (!string.IsNullOrEmpty(manifestContent))
            {
                meta.HasManifest = true;
            }

            var metaJson = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            var metaBytes = Encoding.UTF8.GetBytes(metaJson);
            metaStream.Write(metaBytes, 0, metaBytes.Length);
        }

        var tarballData = ms.ToArray();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(tarballData);

        return new PackResult
        {
            TarballData = tarballData,
            Sha256 = $"sha256-{Convert.ToHexString(hash).ToLowerInvariant()}",
            Size = tarballData.Length,
            FileCount = GetFilesToPack(packageDirectory, ignore).Count
        };
    }

    /// <summary>
    /// 将 tarball 解包到指定目录
    /// </summary>
    /// <param name="tarballData">tarball 字节数组</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <returns>解包的文件数量</returns>
    public static int Unpack(byte[] tarballData, string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        using var ms = new MemoryStream(tarballData);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        int fileCount = 0;

        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var targetPath = Path.Combine(targetDirectory, entry.FullName);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            using var entryStream = entry.Open();
            using var fileStream = File.Create(targetPath);
            entryStream.CopyTo(fileStream);

            fileCount++;
        }

        return fileCount;
    }

    /// <summary>
    /// 验证 tarball 的完整性哈希
    /// </summary>
    /// <param name="tarballData">tarball 字节数组</param>
    /// <param name="expectedSha256">期望的 SHA-256 哈希（格式：sha256-hex）</param>
    /// <returns>是否匹配</returns>
    public static bool VerifyIntegrity(byte[] tarballData, string expectedSha256)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(tarballData);
        var actualHash = $"sha256-{Convert.ToHexString(hash).ToLowerInvariant()}";

        return string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取需要打包的文件列表
    /// </summary>
    private static List<string> GetFilesToPack(string directory, LegionIgnore? ignore)
    {
        var result = new List<string>();

        var alwaysExclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vendors",
            "node_modules",
            ".git",
            ".svn",
            ".hg",
            "dist",
            "build",
            "bin",
            "obj",
            ".idea",
            ".vs",
            ".vscode",
            "__pycache__",
            ".DS_Store",
            "Thumbs.db"
        };

        var alwaysExcludeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "legion-lock.von"
        };

        EnumerateFiles(directory, directory, alwaysExclude, alwaysExcludeFiles, ignore, result);

        return result;
    }

    private static void EnumerateFiles(
        string rootDir,
        string currentDir,
        HashSet<string> excludedDirs,
        HashSet<string> excludedFiles,
        LegionIgnore? ignore,
        List<string> result)
    {
        try
        {
            foreach (var file in Directory.GetFiles(currentDir))
            {
                var fileName = Path.GetFileName(file);
                if (excludedFiles.Contains(fileName))
                {
                    continue;
                }

                if (ignore?.IsIgnored(file[(rootDir.Length + 1)..]) == true)
                {
                    continue;
                }

                result.Add(file);
            }

            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                var dirName = Path.GetFileName(dir);
                if (excludedDirs.Contains(dirName))
                {
                    continue;
                }

                if (dirName.StartsWith('.'))
                {
                    continue;
                }

                if (ignore?.IsIgnored(dir[(rootDir.Length + 1)..]) == true)
                {
                    continue;
                }

                EnumerateFiles(rootDir, dir, excludedDirs, excludedFiles, ignore, result);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 跳过无权限的目录
        }
    }
}

/// <summary>
/// 打包结果
/// </summary>
public class PackResult
{
    /// <summary>
    /// tarball 字节数据
    /// </summary>
    public byte[] TarballData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// SHA-256 完整性哈希
    /// </summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>
    /// tarball 大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 包含的文件数量
    /// </summary>
    public int FileCount { get; set; }
}

/// <summary>
/// 包元数据（写入 tarball 中的 package.json）
/// </summary>
internal class PackageMeta
{
    /// <summary>
    /// 打包时间
    /// </summary>
    public string PackedAt { get; set; } = string.Empty;

    /// <summary>
    /// 打包器版本
    /// </summary>
    public string PackerVersion { get; set; } = string.Empty;

    /// <summary>
    /// 是否包含 legion.von 清单
    /// </summary>
    public bool HasManifest { get; set; }
}