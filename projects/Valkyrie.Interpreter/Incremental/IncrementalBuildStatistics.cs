namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     增量编译性能统计
/// </summary>
public sealed class IncrementalBuildStatistics
{
    /// <summary>
    ///     总文件数
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    ///     缓存命中数
    /// </summary>
    public int CacheHits { get; set; }

    /// <summary>
    ///     实际编译数
    /// </summary>
    public int ActualCompilations => TotalFiles - CacheHits;

    /// <summary>
    ///     缓存命中率
    /// </summary>
    public double CacheHitRatio => TotalFiles == 0 ? 0 : (double)CacheHits / TotalFiles;

    /// <summary>
    ///     总耗时（毫秒）
    /// </summary>
    public long TotalElapsedMs { get; set; }

    /// <summary>
    ///     编译阶段耗时（毫秒）
    /// </summary>
    public long CompilationElapsedMs { get; set; }

    /// <summary>
    ///     解析阶段耗时（毫秒）
    /// </summary>
    public long ParsingElapsedMs { get; set; }

    /// <summary>
    ///     优化阶段耗时（毫秒）
    /// </summary>
    public long OptimizationElapsedMs { get; set; }

    /// <summary>
    ///     平均文件编译耗时（毫秒）
    /// </summary>
    public double AverageCompilationTimeMs => ActualCompilations == 0
        ? 0
        : (double)CompilationElapsedMs / ActualCompilations;

    /// <summary>
    ///     预估 10 万行代码编译时间（毫秒）
    ///     基于实际编译性能线性外推
    /// </summary>
    public long Estimated100KLinesMs
    {
        get
        {
            if (AverageCompilationTimeMs <= 0)
            {
                return 0;
            }

            var avgLinesPerFile = 500;
            var filesFor100k = (int)Math.Ceiling(100000.0 / avgLinesPerFile);
            return (long)(filesFor100k * AverageCompilationTimeMs / Environment.ProcessorCount);
        }
    }

    /// <summary>
    ///     是否满足性能目标（10万行 < 5秒）
    /// </summary>
    public bool MeetsPerformanceTarget => Estimated100KLinesMs < 5000;
}