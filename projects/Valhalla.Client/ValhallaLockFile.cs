using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Valhalla.Client;

/// <summary>
/// protoswap.lock 文件读写，作为客户端本地信任锚点
/// </summary>
public class ValhallaLockFile
{
    private readonly string _projectDirectory;
    private ValhallaLockContent? _content;

    /// <summary>
    /// 创建锁文件实例
    /// </summary>
    /// <param name="projectDirectory">项目目录，lock 文件位于此目录下</param>
    public ValhallaLockFile(string projectDirectory)
    {
        _projectDirectory = projectDirectory;
    }

    /// <summary>
    /// lock 文件完整路径
    /// </summary>
    public string FilePath => Path.Combine(_projectDirectory, "protoswap.lock");

    /// <summary>
    /// lock 文件是否存在
    /// </summary>
    public bool Exists()
    {
        return File.Exists(FilePath);
    }

    /// <summary>
    /// 从磁盘加载 lock 文件
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(FilePath))
        {
            _content = new ValhallaLockContent();
            return;
        }

        string json = await File.ReadAllTextAsync(FilePath);
        _content = JsonSerializer.Deserialize<ValhallaLockContent>(json)
                   ?? new ValhallaLockContent();
    }

    /// <summary>
    /// 保存 lock 到磁盘
    /// </summary>
    public async Task SaveAsync()
    {
        _content ??= new ValhallaLockContent();
        _content.UpdatedAt = DateTime.UtcNow;

        string json = JsonSerializer.Serialize(_content,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, json);
    }

    /// <summary>
    /// 删除 lock 文件
    /// </summary>
    public void Delete()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        _content = new ValhallaLockContent();
    }

    /// <summary>
    /// 添加或更新一个包条目
    /// </summary>
    public void AddOrUpdate(string packageName, ValhallaLockEntry entry)
    {
        _content ??= new ValhallaLockContent();
        _content.Entries[packageName] = entry;
        _content.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 删除指定包的 lock 条目
    /// </summary>
    public bool Remove(string packageName)
    {
        if (_content is null)
        {
            return false;
        }

        bool removed = _content.Entries.Remove(packageName);
        if (removed)
        {
            _content.UpdatedAt = DateTime.UtcNow;
        }

        return removed;
    }

    /// <summary>
    /// 获取指定包的 lock 条目
    /// </summary>
    public ValhallaLockEntry? GetEntry(string packageName)
    {
        if (_content?.Entries.TryGetValue(packageName, out var entry) == true)
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// 获取所有 lock 条目
    /// </summary>
    public Dictionary<string, ValhallaLockEntry> GetAllEntries()
    {
        return _content?.Entries ?? new Dictionary<string, ValhallaLockEntry>();
    }

    /// <summary>
    /// 检查指定包的指定版本是否已被 lock
    /// </summary>
    public bool IsVersionLocked(string packageName, string version)
    {
        var entry = GetEntry(packageName);
        return entry is not null && entry.Version == version;
    }

    /// <summary>
    /// 获取锁文件中记录的 SHA-256（若有）
    /// </summary>
    public string? GetLockedSha256(string packageName, string version)
    {
        var entry = GetEntry(packageName);
        if (entry is not null && entry.Version == version)
        {
            return entry.Sha256;
        }

        return null;
    }

    /// <summary>
    /// 获取锁文件中记录的 incarnation（若有）
    /// </summary>
    public int? GetLockedIncarnation(string packageName)
    {
        var entry = GetEntry(packageName);
        return entry?.Incarnation;
    }

    /// <summary>
    /// 获取锁文件中记录的 publisher 指纹（若有）
    /// </summary>
    public string? GetLockedPublisher(string packageName)
    {
        var entry = GetEntry(packageName);
        return entry?.Publisher;
    }

    /// <summary>
    /// 获取 lock 内容（不包括 Entries 字典，用于外部访问）
    /// </summary>
    public ValhallaLockContent? GetContent()
    {
        return _content;
    }
}