namespace Valhalla.Client;

/// <summary>
/// 安装结果
/// </summary>
public class InstallResult
{
    /// <summary>是否安装成功</summary>
    public bool Success { get; set; }

    /// <summary>安装的包名</summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>安装的版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>.nyar 的 SHA-256</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>源码的 SHA-256</summary>
    public string? SourceSha256 { get; set; }

    /// <summary>错误消息</summary>
    public string? Error { get; set; }

    /// <summary>完整性校验是否通过</summary>
    public bool IntegrityVerified { get; set; }

    /// <summary>发布者身份是否已验证</summary>
    public bool PublisherVerified { get; set; }

    /// <summary>化身编号是否匹配</summary>
    public bool IncarnationMatched { get; set; }

    /// <summary>创建成功的安装结果</summary>
    public static InstallResult Succeed(string packageName, string version, string sha256, string? sourceSha256 = null)
    {
        return new InstallResult
        {
            Success = true,
            PackageName = packageName,
            Version = version,
            Sha256 = sha256,
            SourceSha256 = sourceSha256
        };
    }

    /// <summary>创建失败的安装结果</summary>
    public static InstallResult Fail(string packageName, string version, string error)
    {
        return new InstallResult
        {
            Success = false,
            PackageName = packageName,
            Version = version,
            Error = error
        };
    }
}