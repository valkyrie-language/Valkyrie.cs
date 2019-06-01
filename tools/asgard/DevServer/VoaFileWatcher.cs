namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA 文件监听器，监视项目文件变更并触发增量编译
/// </summary>
public sealed class VoaFileWatcher : IDisposable
{
    private readonly string _projectDir;
    private readonly List<string> _watchPatterns;
    private readonly List<string> _ignorePatterns;
    private readonly int _debounceMs;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly object _lock = new();
    private Timer? _debounceTimer;
    private readonly HashSet<string> _changedFiles = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public event Action<string>? OnFileChanged;
    public event Action<string, string?>? OnError;

    public VoaFileWatcher(string projectDir, List<string> watchPatterns, List<string> ignorePatterns, int debounceMs)
    {
        _projectDir = projectDir;
        _watchPatterns = watchPatterns;
        _ignorePatterns = ignorePatterns;
        _debounceMs = debounceMs;
    }

    public void Start()
    {
        var sourceDir = Path.Combine(_projectDir, "source");
        var assetsDir = Path.Combine(_projectDir, "assets");

        var directories = new List<string>();

        if (Directory.Exists(sourceDir))
        {
            directories.Add(sourceDir);
        }

        if (Directory.Exists(assetsDir))
        {
            directories.Add(assetsDir);
        }

        if (directories.Count == 0)
        {
            directories.Add(_projectDir);
        }

        foreach (var dir in directories)
        {
            var watcher = new FileSystemWatcher(dir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += OnFileSystemEvent;
            watcher.Created += OnFileSystemEvent;
            watcher.Renamed += OnRenamedEvent;
            watcher.Deleted += OnDeletedEvent;

            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
    }

    public void Stop()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
        }

        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _changedFiles.Clear();
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsRelevantFile(e.FullPath)) return;

        if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
        {
            ScheduleDebounce(e.FullPath);
        }
    }

    private void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        if (!IsRelevantFile(e.FullPath)) return;
        ScheduleDebounce(e.FullPath);
    }

    private void OnDeletedEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsRelevantFile(e.FullPath)) return;
        ScheduleDebounce(e.FullPath);
    }

    private void ScheduleDebounce(string filePath)
    {
        lock (_lock)
        {
            _changedFiles.Add(filePath);

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(OnDebounceElapsed, null, _debounceMs, Timeout.Infinite);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        List<string> files;

        lock (_lock)
        {
            files = new List<string>(_changedFiles);
            _changedFiles.Clear();
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        foreach (var file in files)
        {
            try
            {
                OnFileChanged?.Invoke(file);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"文件变更处理失败：{ex.Message}", file);
            }
        }
    }

    private bool IsRelevantFile(string filePath)
    {
        var relativePath = Path.GetRelativePath(_projectDir, filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension is not (".v" or ".awsl" or ".css" or ".js" or ".html" or ".json" or ".svg" or ".png" or ".jpg"))
        {
            return false;
        }

        foreach (var ignore in _ignorePatterns)
        {
            var normalizedIgnore = ignore.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);

            if (relativePath.StartsWith(normalizedIgnore, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();

        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
    }
}