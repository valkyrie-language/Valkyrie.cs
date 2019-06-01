namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     模块编译结果
/// </summary>
public sealed class ModuleCompileResult
{
    /// <summary>
    ///     模块名称
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    ///     编译是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     编译耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; init; }

    /// <summary>
    ///     是否使用缓存
    /// </summary>
    public bool UsedCache { get; init; }

    /// <summary>
    ///     诊断信息
    /// </summary>
    public List<string> Errors { get; init; } = [];
}
