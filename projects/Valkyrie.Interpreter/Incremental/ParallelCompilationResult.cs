using Nyar.Assembler;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     并行编译结果
/// </summary>
public sealed class ParallelCompilationResult
{
    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     编译耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    ///     是否使用缓存
    /// </summary>
    public bool UsedCache { get; set; }

    /// <summary>
    ///     编译后的模块
    /// </summary>
    public CompilationUnit? CompilationUnit { get; set; }

    /// <summary>
    ///     创建空编译结果
    /// </summary>
    public ParallelCompilationResult()
    {
    }

    private ParallelCompilationResult(
        string filePath,
        bool success,
        string? errorMessage)
    {
        FilePath = filePath;
        Success = success;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    public static ParallelCompilationResult FromSuccess(
        string filePath,
        CompilationUnit unit,
        long elapsedMs,
        bool usedCache)
    {
        return new ParallelCompilationResult(filePath, true, null)
        {
            CompilationUnit = unit,
            ElapsedMs = elapsedMs,
            UsedCache = usedCache
        };
    }

    /// <summary>
    ///     创建错误结果
    /// </summary>
    public static ParallelCompilationResult FromError(string filePath, string errorMessage)
    {
        return new ParallelCompilationResult(filePath, false, errorMessage);
    }
}
