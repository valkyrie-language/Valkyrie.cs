namespace Valhalla.Client;

/// <summary>
/// 下载结果
/// </summary>
public class ValhallaDownloadResult
{
    /// <summary>包名</summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>.nyar 字节码数据</summary>
    public byte[] PackageData { get; set; } = Array.Empty<byte>();

    /// <summary>源码包数据</summary>
    public byte[]? SourceData { get; set; }

    /// <summary>服务端声明的 .nyar SHA-256</summary>
    public string PackageSha256 { get; set; } = string.Empty;

    /// <summary>服务端声明的源码 SHA-256</summary>
    public string? SourceSha256 { get; set; }

    /// <summary>错误信息（下载失败时设置，如 404）</summary>
    public string? Error { get; set; }
}