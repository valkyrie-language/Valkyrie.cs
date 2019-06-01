namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     并行编译选项
/// </summary>
public sealed class ParallelCompilationOptions
{
    /// <summary>
    ///     最大并行度，0 表示使用 CPU 核心数
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; }

    /// <summary>
    ///     是否保留依赖顺序（牺牲部分并行性保证正确性）
    /// </summary>
    public bool PreserveDependencyOrder { get; init; }

    /// <summary>
    ///     是否使用数据流块进行编译（支持背压）
    /// </summary>
    public bool UseDataflowBlocks { get; init; }

    /// <summary>
    ///     编译超时时间（毫秒）
    /// </summary>
    public int CompilationTimeoutMs { get; init; } = 60000;

    /// <summary>
    ///     是否启用增量编译优化
    /// </summary>
    public bool EnableIncrementalOptimization { get; init; } = true;

    /// <summary>
    ///     创建默认选项
    /// </summary>
    public static ParallelCompilationOptions Default => new()
    {
        MaxDegreeOfParallelism = 0,
        PreserveDependencyOrder = true,
        UseDataflowBlocks = false,
        CompilationTimeoutMs = 60000,
        EnableIncrementalOptimization = true
    };

    /// <summary>
    ///     创建最大性能选项（不保留依赖顺序）
    /// </summary>
    public static ParallelCompilationOptions MaxPerformance => new()
    {
        MaxDegreeOfParallelism = 0,
        PreserveDependencyOrder = false,
        UseDataflowBlocks = true,
        CompilationTimeoutMs = 120000,
        EnableIncrementalOptimization = true
    };
}
