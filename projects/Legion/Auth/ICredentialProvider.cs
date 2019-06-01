namespace Legion.Auth;

/// <summary>
/// 凭据提供器接口，适配各官方 CLI 工具的凭据存储
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// 提供器名称
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 关联的 Vendor 名称
    /// </summary>
    string VendorName { get; }

    /// <summary>
    /// 优先级，数值越小优先级越高
    /// 官方 CLI 工具配置 = 10，环境变量 = 20，项目本地配置 = 30，手动输入 = 100
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 检查凭据源是否可用
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// 尝试从官方工具配置中发现凭据
    /// </summary>
    /// <returns>发现的凭据，未找到返回 null</returns>
    Task<DiscoveredCredential?> DiscoverAsync();
}