namespace Valhalla.Config;

/// <summary>
/// S3 存储配置
/// </summary>
public class S3Config
{
    /// <summary>S3 端点地址</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>存储桶名称</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>访问密钥</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>秘密密钥</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>区域</summary>
    public string Region { get; set; } = "us-east-1";
}