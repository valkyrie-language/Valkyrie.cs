using System.Collections.Concurrent;
using System.Diagnostics;
using Nyar.Types;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 热重载编译器
/// 结合文件监控、并行编译、增量缓存实现亚秒级热重载
/// </summary>
public sealed class HotReloadCompiler : IDisposable
{
    private readonly ValkyrieRuntime _runtime;
    private readonly ParallelIncrementalCompiler _parallelCompiler;
    private readonly FileWatcher _fileWatcher;
    private readonly CompilationTarget _defaultTarget;
    private readonly object _compilationLock = new();
    private readonly ConcurrentDictionary<string, ModuleCompileResult> _moduleCache;
    private readonly IncrementalBuildStatistics _stats;
    private CancellationTokenSource? _cts;
    private Task? _watchTask;
    private bool _disposed;

    /// <summary>
    ///     编译统计信息
    /// </summary>
    public IncrementalBuildStatistics Stats => _stats;

    /// <summary>
    ///     创建热重载编译器
    /// </summary>
    /// <param name="runtime">Valkyrie 运行时</param>
    /// <param name="target">编译目标</param>
    /// <param name="options">并行编译选项</param>
    public HotReloadCompiler(
        ValkyrieRuntime runtime,
        CompilationTarget? target = null,
        ParallelCompilationOptions? options = null)
    {
        _runtime = runtime;
        _defaultTarget = target ?? CompilationTarget.NyarVM;
        _parallelCompiler = new ParallelIncrementalCompiler(options ?? ParallelCompilationOptions.MaxPerformance);
        _fileWatcher = new FileWatcher(string.Empty);
        _moduleCache = new ConcurrentDictionary<string, ModuleCompileResult>(StringComparer.OrdinalIgnoreCase);
        _stats = new IncrementalBuildStatistics();
    }

    /// <summary>
    ///     启动热重载监控
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    public void StartWatching(string projectDir)
    {
        _cts = new CancellationTokenSource();
        _fileWatcher.Start();

        _watchTask = Task.Run(async () =>
        {
            try
            {
                await WatchLoopAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    /// <summary>
    ///     停止热重载监控
    /// </summary>
    public void StopWatching()
    {
        _cts?.Cancel();
        _fileWatcher.Stop();
        _watchTask?.Wait(TimeSpan.FromSeconds(1));
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    ///     执行增量编译
    /// </summary>
    public async Task<IncrementalBuildStatistics> CompileIncrementalAsync(
        string projectDir,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var sourceDir = Path.Combine(projectDir, "source");

        if (!Directory.Exists(sourceDir))
        {
            return _stats;
        }

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories).ToList();
        _stats.TotalFiles = vFiles.Count;
        _stats.CacheHits = 0;

        foreach (var vFile in vFiles)
        {
            var cached = _runtime.IncrementalCompiler.GetCachedModule(vFile);
            if (cached is not null)
            {
                _stats.CacheHits++;
            }
        }

        var parseSw = Stopwatch.StartNew();
        var parseResults = await _parallelCompiler.CompileInParallelAsync(
            vFiles,
            async file =>
            {
                var source = await File.ReadAllTextAsync(file, cancellationToken);
                var currentHash = ComputeSourceHash(source);
                var cached = _runtime.IncrementalCompiler.GetCachedModule(file);

                if (cached is not null && cached.SourceHash == currentHash)
                {
                    return ParallelCompilationResult.FromSuccess(
                        file,
                        cached.CompilationUnit,
                        0,
                        true);
                }

                var result = _runtime.CompileToTarget(source, Path.GetFileNameWithoutExtension(file), _defaultTarget);

                return new ParallelCompilationResult
                {
                    FilePath = file,
                    Success = result.Success,
                    ErrorMessage = result.Diagnostics.Errors.FirstOrDefault()?.Message,
                    CompilationUnit = result.CompilationUnit,
                    UsedCache = false,
                    ElapsedMs = 0
                };
            });

        parseSw.Stop();
        _stats.ParsingElapsedMs = parseSw.ElapsedMilliseconds;

        var optSw = Stopwatch.StartNew();
        optSw.Stop();
        _stats.OptimizationElapsedMs = optSw.ElapsedMilliseconds;

        sw.Stop();
        _stats.TotalElapsedMs = sw.ElapsedMilliseconds;
        _stats.CompilationElapsedMs = sw.ElapsedMilliseconds;

        return _stats;
    }

    /// <summary>
    ///     快速增量编译（仅重新编译变更文件及其依赖）
    /// </summary>
    public async Task<List<string>> QuickIncrementalBuildAsync(
        string projectDir,
        IEnumerable<string> changedFiles,
        CancellationToken cancellationToken = default)
    {
        var affectedFiles = new List<string>();

        foreach (var changedFile in changedFiles)
        {
            var invalidated = _runtime.IncrementalCompiler.Invalidate(changedFile);
            affectedFiles.AddRange(invalidated);
        }

        var sourceDir = Path.Combine(projectDir, "source");
        var filesToRecompile = affectedFiles
            .Where(f => f.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase) && File.Exists(f))
            .ToList();

        await _parallelCompiler.CompileInParallelAsync(
            filesToRecompile,
            file =>
            {
                var source = File.ReadAllText(file);
                var currentHash = ComputeSourceHash(source);
                var result = _runtime.CompileToTarget(source, Path.GetFileNameWithoutExtension(file), _defaultTarget);

                return Task.FromResult(new ParallelCompilationResult
                {
                    FilePath = file,
                    Success = result.Success,
                    CompilationUnit = result.CompilationUnit,
                    UsedCache = false
                });
            });

        return filesToRecompile;
    }

    /// <summary>
    ///     预估编译性能
    /// </summary>
    public (int TotalFiles, double EstimatedSpeedup, bool MeetsTarget) EstimatePerformance(
        int totalFiles,
        int dependencyDepth = 3)
    {
        var speedup = _parallelCompiler.EstimateSpeedup(totalFiles, dependencyDepth);
        var meetsTarget = _stats.Estimated100KLinesMs < 5000;

        return (totalFiles, speedup, meetsTarget);
    }

    /// <summary>
    ///     清除所有缓存
    /// </summary>
    public void ClearCache()
    {
        _runtime.IncrementalCompiler.Clear();
        _moduleCache.Clear();
    }

    private async Task WatchLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var filePath in _fileWatcher.ChangeChannel.ReadAllAsync(cancellationToken))
        {
            await HandleFileChangeAsync(filePath, cancellationToken);
        }
    }

    private async Task HandleFileChangeAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!filePath.EndsWith(".v", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await QuickIncrementalBuildAsync(
            Path.GetDirectoryName(filePath) ?? string.Empty,
            [filePath],
            cancellationToken);

        FileChanged?.Invoke(this, new HotReloadEventArgs(filePath));
    }

    /// <summary>
    ///     文件变化事件
    /// </summary>
    public event EventHandler<HotReloadEventArgs>? FileChanged;

    private static string ComputeSourceHash(string source)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    ///     获取模块缓存状态
    /// </summary>
    public Dictionary<string, bool> GetCacheStatus()
    {
        var status = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var cachedFiles = _runtime.IncrementalCompiler.GetCachedFiles();

        foreach (var file in cachedFiles)
        {
            status[file] = true;
        }

        return status;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopWatching();
        _fileWatcher.Dispose();
        _cts?.Dispose();
    }
}