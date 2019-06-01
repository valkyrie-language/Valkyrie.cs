namespace Legion.CLI.Compiler;

/// <summary>
/// Legion 构建结果
/// </summary>
public sealed class LegionBuildResult
{
    /// <summary>
    /// 构建是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 输出目录路径
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 所有产出文件名列表
    /// </summary>
    public List<string> OutputFiles { get; set; } = [];

    /// <summary>
    /// 主产物完整路径
    /// </summary>
    public string MainArtifact { get; set; } = string.Empty;
}

/// <summary>
/// Legion 清理结果
/// </summary>
public sealed class LegionCleanResult
{
    /// <summary>
    /// 清理是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 已删除的文件列表
    /// </summary>
    public List<string> RemovedFiles { get; set; } = [];

    /// <summary>
    /// 已删除的目录列表
    /// </summary>
    public List<string> RemovedDirs { get; set; } = [];
}
