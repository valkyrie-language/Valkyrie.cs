namespace Valhalla.Config;

/// <summary>
/// 存储后端类型
/// </summary>
public enum StorageBackend
{
    /// <summary>本地文件系统</summary>
    Local,
    /// <summary>S3 兼容存储</summary>
    S3
}