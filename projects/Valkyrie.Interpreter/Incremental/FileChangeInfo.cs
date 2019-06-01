namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     文件变化信息
/// </summary>
public sealed class FileChangeInfo
{
    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     变化类型
    /// </summary>
    public FileChangeType ChangeType { get; }

    /// <summary>
    ///     最后修改时间
    /// </summary>
    public DateTime LastWriteTime { get; }

    /// <summary>
    ///     检测时间
    /// </summary>
    public DateTime DetectedAt { get; }

    public FileChangeInfo(string filePath, FileChangeType changeType, DateTime lastWriteTime)
    {
        FilePath = filePath;
        ChangeType = changeType;
        LastWriteTime = lastWriteTime;
        DetectedAt = DateTime.UtcNow;
    }
}
