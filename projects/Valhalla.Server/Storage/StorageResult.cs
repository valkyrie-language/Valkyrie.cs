namespace Valhalla.Server.Storage;

/// <summary>
/// 二进制存储操作结果
/// </summary>
public class StorageResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>错误消息</summary>
    public string? Error { get; set; }

    /// <summary>创建成功结果</summary>
    public static StorageResult Succeed()
    {
        return new StorageResult { Success = true };
    }

    /// <summary>创建失败结果</summary>
    public static StorageResult Fail(string error)
    {
        return new StorageResult { Success = false, Error = error };
    }
}