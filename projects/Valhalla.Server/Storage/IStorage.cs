using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Valhalla.Server.Storage;

/// <summary>
/// 二进制存储抽象接口，支持 local 和 S3 等后端
/// </summary>
public interface IStorage
{
    /// <summary>
    /// 获取字符串内容
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <param name="ct">取消令牌</param>
    Task<string?> ReadStringAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// 获取二进制内容
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <param name="ct">取消令牌</param>
    Task<byte[]?> ReadBytesAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// 写入字符串内容
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <param name="content">字符串内容</param>
    /// <param name="ct">取消令牌</param>
    Task<StorageResult> WriteStringAsync(string path, string content, CancellationToken ct = default);

    /// <summary>
    /// 写入二进制内容
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <param name="data">二进制数据</param>
    /// <param name="ct">取消令牌</param>
    Task<StorageResult> WriteBytesAsync(string path, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// 检查路径是否存在
    /// </summary>
    /// <param name="path">相对路径</param>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// 删除文件或目录
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <param name="ct">取消令牌</param>
    Task<StorageResult> DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// 列出指定前缀下的所有键
    /// </summary>
    /// <param name="prefix">路径前缀</param>
    /// <param name="ct">取消令牌</param>
    Task<List<string>> ListAsync(string prefix, CancellationToken ct = default);
}