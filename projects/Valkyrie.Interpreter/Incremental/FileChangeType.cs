namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     文件变化类型
/// </summary>
public enum FileChangeType
{
    /// <summary>
    ///     文件被创建
    /// </summary>
    Created,

    /// <summary>
    ///     文件被修改
    /// </summary>
    Changed,

    /// <summary>
    ///     文件被删除
    /// </summary>
    Deleted
}
