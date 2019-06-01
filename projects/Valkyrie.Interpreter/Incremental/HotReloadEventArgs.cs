namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     热重载事件参数
/// </summary>
public sealed class HotReloadEventArgs : EventArgs
{
    /// <summary>
    ///     变更的文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     变更类型
    /// </summary>
    public FileChangeType ChangeType { get; }

    public HotReloadEventArgs(string filePath, FileChangeType changeType = FileChangeType.Changed)
    {
        FilePath = filePath;
        ChangeType = changeType;
    }
}
