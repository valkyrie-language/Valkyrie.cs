using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Valhalla.Server.Storage;

/// <summary>
/// 本地文件系统存储后端实现
/// </summary>
public class LocalStorage : IStorage
{
    private readonly string _rootPath;

    /// <summary>
    /// 创建本地存储
    /// </summary>
    /// <param name="rootPath">存储根目录路径</param>
    public LocalStorage(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <inheritdoc />
    public Task<string?> ReadStringAsync(string path, CancellationToken ct = default)
    {
        string fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<string?>(null);
        }

        return File.ReadAllTextAsync(fullPath, ct)!;
    }

    /// <inheritdoc />
    public Task<byte[]?> ReadBytesAsync(string path, CancellationToken ct = default)
    {
        string fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<byte[]?>(null);
        }

        return File.ReadAllBytesAsync(fullPath, ct)!;
    }

    /// <inheritdoc />
    public async Task<StorageResult> WriteStringAsync(string path, string content, CancellationToken ct = default)
    {
        try
        {
            string fullPath = GetFullPath(path);
            string? dir = Path.GetDirectoryName(fullPath);
            if (dir is not null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(fullPath, content, ct);
            return StorageResult.Succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<StorageResult> WriteBytesAsync(string path, byte[] data, CancellationToken ct = default)
    {
        try
        {
            string fullPath = GetFullPath(path);
            string? dir = Path.GetDirectoryName(fullPath);
            if (dir is not null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(fullPath, data, ct);
            return StorageResult.Succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        string fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task<StorageResult> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            string fullPath = GetFullPath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            return Task.FromResult(StorageResult.Succeed());
        }
        catch (Exception ex)
        {
            return Task.FromResult(StorageResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<List<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        string fullPath = GetFullPath(prefix);
        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(new List<string>());
        }

        var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
        var result = new List<string>();
        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(_rootPath, file)
                .Replace('\\', '/');
            result.Add(relativePath);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取绝对路径，防止路径穿越
    /// </summary>
    private string GetFullPath(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/').TrimStart('/');
        string fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"路径穿越检测：{relativePath}");
        }

        return fullPath;
    }
}