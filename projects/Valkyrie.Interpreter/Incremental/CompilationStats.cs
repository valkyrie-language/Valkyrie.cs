namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     编译统计信息，记录增量编译的性能数据
/// </summary>
public sealed class CompilationStats
{
    private readonly object _lock = new();

    /// <summary>
    ///     统计开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///     统计结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    ///     总耗时（毫秒）
    /// </summary>
    public long TotalElapsedMs { get; set; }

    /// <summary>
    ///     处理的总文件数
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    ///     缓存命中的文件数
    /// </summary>
    public int CacheHits { get; set; }

    /// <summary>
    ///     缓存未命中的文件数
    /// </summary>
    public int CacheMisses { get; set; }

    /// <summary>
    ///     缓存命中率
    /// </summary>
    public double CacheHitRatio => TotalFiles == 0 ? 0 : (double)CacheHits / TotalFiles;

    /// <summary>
    ///     各阶段耗时（毫秒）
    /// </summary>
    public Dictionary<string, long> PhaseElapsedMs { get; set; } = new();

    /// <summary>
    ///     线程安全递增缓存命中数
    /// </summary>
    public void IncrementHits()
    {
        lock (_lock)
        {
            CacheHits++;
        }
    }

    /// <summary>
    ///     线程安全递增缓存未命中数
    /// </summary>
    public void IncrementMisses()
    {
        lock (_lock)
        {
            CacheMisses++;
        }
    }
}
