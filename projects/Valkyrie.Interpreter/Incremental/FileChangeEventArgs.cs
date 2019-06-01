namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     文件变化事件参数
/// </summary>
public sealed class FileChangeEventArgs : EventArgs
{
    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     变化类型
    /// </summary>
    public FileChangeType ChangeType { get; }

    public FileChangeEventArgs(string filePath, FileChangeType changeType)
    {
        FilePath = filePath;
        ChangeType = changeType;
    }
}
