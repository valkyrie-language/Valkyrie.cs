using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 文件监控器
/// 监控文件变化，触发增量重编译
/// </summary>
public sealed class FileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, FileChangeInfo> _changeCache;
    private readonly TimeSpan _debounceInterval;
    private readonly Timer _debounceTimer;
    private readonly object _timerLock = new();
    private readonly List<string> _pendingChanges = new();
    private readonly Channel<string> _changeChannel;
    private bool _disposed;

    /// <summary>
    ///     文件变化事件
    /// </summary>
    public event EventHandler<FileChangeEventArgs>? FileChanged;

    /// <summary>
    ///     创建文件监控器
    /// </summary>
    /// <param name="watchPath">监控路径</param>
    /// <param name="filter">文件过滤（默认 *.v）</param>
    /// <param name="debounceInterval">防抖间隔（默认 100ms）</param>
    public FileWatcher(string watchPath, string filter = "*.v", TimeSpan? debounceInterval = null)
    {
        _watcher = new FileSystemWatcher(watchPath)
        {
            Filter = filter,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _changeCache = new ConcurrentDictionary<string, FileChangeInfo>(StringComparer.OrdinalIgnoreCase);
        _debounceInterval = debounceInterval ?? TimeSpan.FromMilliseconds(100);
        _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        _changeChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _watcher.Changed += OnFileSystemEvent;
        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Renamed += OnFileRenamedEvent;
    }

    /// <summary>
    ///     获取文件变化通道
    /// </summary>
    public ChannelReader<string> ChangeChannel => _changeChannel.Reader;

    /// <summary>
    ///     获取缓存的文件变化信息
    /// </summary>
    public FileChangeInfo? GetChangeInfo(string filePath)
    {
        return _changeCache.TryGetValue(filePath, out var info) ? info : null;
    }

    /// <summary>
    ///     手动触发文件变化检测
    /// </summary>
    public void CheckForChanges()
    {
        foreach (var kvp in _changeCache)
        {
            CheckFileChange(kvp.Key);
        }
    }

    /// <summary>
    ///     获取所有监控的文件路径
    /// </summary>
    public IEnumerable<string> GetWatchedFiles()
    {
        return _changeCache.Keys;
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        EnqueueChange(e.FullPath, GetChangeType(e.ChangeType));
    }

    private void OnFileRenamedEvent(object sender, RenamedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        EnqueueChange(e.OldFullPath, FileChangeType.Deleted);
        EnqueueChange(e.FullPath, FileChangeType.Created);
    }

    private void EnqueueChange(string filePath, FileChangeType changeType)
    {
        lock (_timerLock)
        {
            _pendingChanges.Add(filePath);
            _debounceTimer.Change(_debounceInterval, Timeout.InfiniteTimeSpan);
        }

        var changeInfo = new FileChangeInfo(filePath, changeType, DateTime.UtcNow);
        _changeCache[filePath] = changeInfo;

        FileChanged?.Invoke(this, new FileChangeEventArgs(filePath, changeType));
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        List<string> changes;

        lock (_timerLock)
        {
            if (_pendingChanges.Count == 0)
            {
                return;
            }

            changes = new List<string>(_pendingChanges);
            _pendingChanges.Clear();
        }

        foreach (var filePath in changes.Distinct())
        {
            _changeChannel.Writer.TryWrite(filePath);
        }
    }

    private void CheckFileChange(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        if (_changeCache.TryGetValue(filePath, out var cached) &&
            cached.LastWriteTime == lastWriteTime)
        {
            return;
        }

        var changeInfo = new FileChangeInfo(filePath, FileChangeType.Changed, lastWriteTime);
        _changeCache[filePath] = changeInfo;

        _changeChannel.Writer.TryWrite(filePath);
        FileChanged?.Invoke(this, new FileChangeEventArgs(filePath, FileChangeType.Changed));
    }

    /// <summary>
    ///     获取文件变化类型
    /// </summary>
    private static FileChangeType GetChangeType(WatcherChangeTypes type)
    {
        return type switch
        {
            WatcherChangeTypes.Changed => FileChangeType.Changed,
            WatcherChangeTypes.Created => FileChangeType.Created,
            WatcherChangeTypes.Deleted => FileChangeType.Deleted,
            WatcherChangeTypes.Renamed => FileChangeType.Changed,
            _ => FileChangeType.Changed
        };
    }

    /// <summary>
    ///     添加监控文件
    /// </summary>
    public void AddWatchFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
        var changeInfo = new FileChangeInfo(filePath, FileChangeType.Created, lastWriteTime);
        _changeCache[filePath] = changeInfo;
    }

    /// <summary>
    ///     移除监控文件
    /// </summary>
    public void RemoveWatchFile(string filePath)
    {
        _changeCache.TryRemove(filePath, out _);
    }

    /// <summary>
    ///     开始监控
    /// </summary>
    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    ///     停止监控
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileSystemEvent;
        _watcher.Created -= OnFileSystemEvent;
        _watcher.Deleted -= OnFileSystemEvent;
        _watcher.Renamed -= OnFileRenamedEvent;
        _watcher.Dispose();

        _debounceTimer.Dispose();
        _changeChannel.Writer.Complete();
    }
}