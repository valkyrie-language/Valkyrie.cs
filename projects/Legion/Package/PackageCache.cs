using System.Collections.Concurrent;
using System.Security.Cryptography;
using Oak.Data;
using Oak.Von;

namespace Legion.Package;

/// <summary>
///     全局包缓存（Content-Addressable Store），类比 pnpm store。
///     包按内容哈希存储，支持硬链接安装以减少磁盘占用。
/// </summary>
public class PackageCache
{
    private const string StoreDirName = "store";
    private const string IndexFileName = "cache-index.von";

    private readonly string _rootDirectory;
    private readonly string _storeDirectory;
    private readonly string _indexFilePath;
    private readonly Dictionary<string, List<CachedPackage>> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    private long _hits;
    private long _misses;
    private long _bytesSavedByLinks;

    /// <summary>
    ///     创建包缓存实例
    /// </summary>
    /// <param name="rootDirectory">缓存根目录（通常为 ~/.valkyrie/）</param>
    public PackageCache(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        _storeDirectory = Path.Combine(rootDirectory, StoreDirName);
        _indexFilePath = Path.Combine(rootDirectory, IndexFileName);
        Directory.CreateDirectory(_storeDirectory);
    }

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    public long Hits => _hits;

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    public long Misses => _misses;

    /// <summary>
    ///     缓存命中率
    /// </summary>
    public double HitRate => (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) : 0;

    /// <summary>
    ///     通过硬链接节省的总字节数
    /// </summary>
    public long BytesSavedByLinks => _bytesSavedByLinks;

    #region 加载与持久化

    /// <summary>
    ///     从索引文件加载缓存
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(_indexFilePath))
        {
            return;
        }

        await _indexLock.WaitAsync();

        try
        {
            var content = await File.ReadAllTextAsync(_indexFilePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var parser = new GonParser();
            var value = parser.Deserialize(content);

            if (value.Type == SerdeValueType.Object && value.Fields is not null)
            {
                foreach (var field in value.Fields)
                {
                    var parts = field.Key.Split('/', 2);

                    if (parts.Length == 2)
                    {
                        var pkgName = parts[0];
                        var version = parts[1];
                        var localPath = field.Value.GetString() ?? string.Empty;

                        if (!_index.TryGetValue(pkgName, out var entries))
                        {
                            entries = new List<CachedPackage>();
                            _index[pkgName] = entries;
                        }

                        entries.Add(new CachedPackage
                        {
                            PackageName = pkgName,
                            Version = version,
                            LocalPath = localPath
                        });
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    ///     保存缓存索引到文件
    /// </summary>
    public async Task SaveAsync()
    {
        await _indexLock.WaitAsync();

        try
        {
            var lines = new List<string>();

            foreach (var (name, entries) in _index)
            {
                foreach (var entry in entries)
                {
                    lines.Add($"    \"{name}/{entry.Version}\": \"{entry.LocalPath}\"");
                }
            }

            var content = "{\n" + string.Join(",\n", lines) + "\n}\n";
            await File.WriteAllTextAsync(_indexFilePath, content);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    #endregion

    #region 内容寻址存储（CAS）

    /// <summary>
    ///     将包文件存入全局 Store，返回内容哈希和存储路径
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="version">版本号</param>
    /// <param name="sourcePath">源文件/目录路径</param>
    /// <returns>存储路径和内容哈希</returns>
    public async Task<(string StorePath, string ContentHash)> StoreToStoreAsync(string packageName, string version, string sourcePath)
    {
        var contentHash = await ComputeContentHashAsync(sourcePath);
        var storePath = GetStorePath(contentHash);

        if (!Directory.Exists(storePath))
        {
            Directory.CreateDirectory(storePath);

            if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
            {
                CopyDirectoryRecursive(sourcePath, storePath);
            }
            else
            {
                File.Copy(sourcePath, Path.Combine(storePath, Path.GetFileName(sourcePath)), true);
            }
        }

        var localDir = Path.Combine(_storeDirectory, $"{SanitizeName(packageName)}@{version}");

        await _indexLock.WaitAsync();

        try
        {
            if (!_index.TryGetValue(packageName, out var entries))
            {
                entries = new List<CachedPackage>();
                _index[packageName] = entries;
            }

            entries.RemoveAll(e => e.Version == version);
            entries.Add(new CachedPackage
            {
                PackageName = packageName,
                Version = version,
                LocalPath = localDir
            });
        }
        finally
        {
            _indexLock.Release();
        }

        return (storePath, contentHash);
    }

    /// <summary>
    ///     通过硬链接安装包：从 Store 创建一个到目标目录的硬链接
    /// </summary>
    /// <param name="sourceDir">Store 中的源目录</param>
    /// <param name="targetDir">目标安装目录</param>
    /// <returns>通过硬链接节省的字节数</returns>
    public long InstallWithHardLinks(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            return 0;
        }

        var savedBytes = 0L;

        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);
            var targetFileDir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetFileDir))
            {
                Directory.CreateDirectory(targetFileDir);
            }

            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }

            try
            {
                File.CreateSymbolicLink(targetFile, file);
                savedBytes += new FileInfo(file).Length;
            }
            catch
            {
                // 符号链接失败时回退到复制
                File.Copy(file, targetFile, true);
            }
        }

        _bytesSavedByLinks += savedBytes;
        return savedBytes;
    }

    /// <summary>
    ///     获取内容寻址的 Store 路径
    /// </summary>
    private string GetStorePath(string contentHash)
    {
        // 两层前缀目录减少单目录文件数
        var prefix = contentHash.Substring(0, 2);
        return Path.Combine(_storeDirectory, prefix, contentHash);
    }

    /// <summary>
    ///     计算目录/文件的 SHA256 内容哈希
    /// </summary>
    private static async Task<string> ComputeContentHashAsync(string path)
    {
        using var sha256 = SHA256.Create();

        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(path, file);
                var pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
                sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

                var fileBytes = await File.ReadAllBytesAsync(file);
                sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
            }
        }
        else
        {
            var fileBytes = await File.ReadAllBytesAsync(path);
            sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexStringLower(sha256.Hash!);
    }

    #endregion

    #region 批量并行下载与解压

    /// <summary>
    ///     并行下载多个包的工件并存入 Store
    /// </summary>
    /// <param name="packages">包列表（包名 → 下载URL或本地路径）</param>
    /// <param name="downloadFunc">下载函数：包名 → 本地临时路径</param>
    /// <param name="parallelism">并行度，默认 8</param>
    public async Task<List<(string PackageName, string Version, string StorePath, string ContentHash)>>
        DownloadAndStoreBatchAsync(
            List<(string PackageName, string Version, string Source)> packages,
            Func<string, string, Task<string>> downloadFunc,
            int parallelism = 8)
    {
        var results = new ConcurrentBag<(string, string, string, string)>();
        var semaphore = new SemaphoreSlim(parallelism);

        var tasks = packages.Select(async pkg =>
        {
            await semaphore.WaitAsync();

            try
            {
                var localPath = await downloadFunc(pkg.PackageName, pkg.Source);
                var (storePath, hash) = await StoreToStoreAsync(pkg.PackageName, pkg.Version, localPath);
                results.Add((pkg.PackageName, pkg.Version, storePath, hash));
            }
            catch
            {
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    #endregion

    #region 查询

    /// <summary>
    ///     检查指定包是否在缓存中
    /// </summary>
    public bool HasPackage(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
        {
            if (entries.Any(e => e.Version == version))
            {
                Interlocked.Increment(ref _hits);
                return true;
            }
        }

        Interlocked.Increment(ref _misses);
        return false;
    }

    /// <summary>
    ///     将包注册到缓存索引
    /// </summary>
    public void AddPackage(string packageName, string version, string localDir)
    {
        _indexLock.Wait();

        try
        {
            if (!_index.TryGetValue(packageName, out var entries))
            {
                entries = new List<CachedPackage>();
                _index[packageName] = entries;
            }

            entries.RemoveAll(e => e.Version == version);
            entries.Add(new CachedPackage
            {
                PackageName = packageName,
                Version = version,
                LocalPath = localDir
            });
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    ///     获取包在缓存中的本地目录
    /// </summary>
    public string? GetCacheDir(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
        {
            return entries.FirstOrDefault(e => e.Version == version)?.LocalPath;
        }

        return null;
    }

    /// <summary>
    ///     列出缓存中的所有包
    /// </summary>
    public List<CachedPackage> List()
    {
        return _index.SelectMany(kvp => kvp.Value).ToList();
    }

    /// <summary>
    ///     为包构建缓存存储路径
    /// </summary>
    public string GetPackageCachePath(string packageName, string version)
    {
        return Path.Combine(_storeDirectory, $"{SanitizeName(packageName)}@{version}");
    }

    #endregion

    #region 清理

    /// <summary>
    ///     清理过期缓存：删除磁盘上不存在但索引中仍有的条目
    /// </summary>
    public async Task CleanAsync()
    {
        await _indexLock.WaitAsync();

        try
        {
            var toRemove = new List<(string Name, string Version)>();

            foreach (var (name, entries) in _index)
            {
                foreach (var entry in entries)
                {
                    if (!Directory.Exists(entry.LocalPath) && !File.Exists(entry.LocalPath))
                    {
                        toRemove.Add((name, entry.Version));
                    }
                }
            }

            foreach (var (name, version) in toRemove)
            {
                RemoveFromIndex(name, version);
            }
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    ///     全量清空所有缓存（Store + 索引）
    /// </summary>
    public async Task ClearAsync()
    {
        await _indexLock.WaitAsync();

        try
        {
            if (Directory.Exists(_storeDirectory))
            {
                Directory.Delete(_storeDirectory, true);
                Directory.CreateDirectory(_storeDirectory);
            }

            if (File.Exists(_indexFilePath))
            {
                File.Delete(_indexFilePath);
            }

            _index.Clear();
            _hits = 0;
            _misses = 0;
            _bytesSavedByLinks = 0;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    ///     从缓存中移除指定包（包括磁盘文件）
    /// </summary>
    public async Task RemovePackageAsync(string packageName, string version)
    {
        await _indexLock.WaitAsync();

        try
        {
            if (_index.TryGetValue(packageName, out var entries))
            {
                var entry = entries.FirstOrDefault(e => e.Version == version);

                if (entry is not null)
                {
                    try
                    {
                        if (Directory.Exists(entry.LocalPath))
                        {
                            Directory.Delete(entry.LocalPath, true);
                        }
                        else if (File.Exists(entry.LocalPath))
                        {
                            File.Delete(entry.LocalPath);
                        }
                    }
                    catch
                    {
                    }
                }

                entries.RemoveAll(e => e.Version == version);

                if (entries.Count == 0)
                {
                    _index.Remove(packageName);
                }
            }
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private void RemoveFromIndex(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
        {
            entries.RemoveAll(e => e.Version == version);

            if (entries.Count == 0)
            {
                _index.Remove(packageName);
            }
        }
    }

    #endregion

    #region 统计与诊断

    /// <summary>
    ///     获取缓存统计信息
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var totalPackages = _index.Sum(kvp => kvp.Value.Count);
        var totalSize = 0L;

        foreach (var (_, entries) in _index)
        {
            foreach (var entry in entries)
            {
                if (Directory.Exists(entry.LocalPath))
                {
                    totalSize += GetDirectorySize(entry.LocalPath);
                }
                else if (File.Exists(entry.LocalPath))
                {
                    totalSize += new FileInfo(entry.LocalPath).Length;
                }
            }
        }

        return new CacheStatistics
        {
            TotalPackages = totalPackages,
            TotalSizeBytes = totalSize,
            Hits = _hits,
            Misses = _misses,
            HitRate = HitRate,
            BytesSavedByLinks = _bytesSavedByLinks
        };
    }

    /// <summary>
    ///     验证 Store 完整性
    /// </summary>
    public bool VerifyStore()
    {
        if (!Directory.Exists(_storeDirectory))
        {
            return _index.Count == 0;
        }

        var valid = true;

        foreach (var (_, entries) in _index)
        {
            foreach (var entry in entries)
            {
                if (!Directory.Exists(entry.LocalPath) && !File.Exists(entry.LocalPath))
                {
                    valid = false;
                }
            }
        }

        return valid;
    }

    #endregion

    #region 工具方法

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }

    private static string SanitizeName(string name)
    {
        return name.Replace('/', '_').Replace('@', '_').Replace(':', '_');
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    #endregion
}

/// <summary>
///     缓存统计信息
/// </summary>
public class CacheStatistics
{
    /// <summary>
    ///     缓存中的包总数
    /// </summary>
    public int TotalPackages { get; set; }

    /// <summary>
    ///     缓存占用的总字节数
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    ///     缓存命中率
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    ///     通过硬链接/符号链接节省的字节数
    /// </summary>
    public long BytesSavedByLinks { get; set; }

    /// <summary>
    ///     格式化总大小
    /// </summary>
    public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

    /// <summary>
    ///     格式化节省字节数
    /// </summary>
    public string BytesSavedFormatted => FormatBytes(BytesSavedByLinks);

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes / 1_024.0:F2} KB",
            _ => $"{bytes} B"
        };
    }
}