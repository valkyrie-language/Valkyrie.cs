using Legion.CredentialProviders;

namespace Legion.Auth;

/// <summary>
/// 凭据发现管理器，按优先级尝试所有提供器
/// </summary>
public class CredentialDiscoveryManager
{
    private readonly List<ICredentialProvider> _providers = new();

    /// <summary>
    /// 注册凭据提供器
    /// </summary>
    public void RegisterProvider(ICredentialProvider provider)
    {
        _providers.Add(provider);
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// 注册默认的内置凭据提供器
    /// </summary>
    public void RegisterBuiltinProviders()
    {
        _providers.Add(new NpmNpmrcProvider());
        _providers.Add(new NpmEnvProvider());
        _providers.Add(new JsrDenoConfigProvider());
        _providers.Add(new JsrEnvProvider());
        _providers.Add(new CondaAnacondaProvider());
        _providers.Add(new CondaEnvProvider());
        _providers.Add(new MavenSettingsProvider());
        _providers.Add(new MavenEnvProvider());
        _providers.Add(new NuGetConfigProvider());
        _providers.Add(new NuGetEnvProvider());

        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// 尝试发现指定 Vendor 的凭据
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>发现的凭据，未找到返回 null</returns>
    public async Task<DiscoveredCredential?> DiscoverAsync(string vendorName)
    {
        var candidates = _providers
            .Where(p => p.VendorName.Equals(vendorName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var provider in candidates)
        {
            if (!provider.IsAvailable())
            {
                continue;
            }

            try
            {
                var credential = await provider.DiscoverAsync();

                if (credential is not null && !string.IsNullOrEmpty(credential.Token))
                {
                    credential.Source = $"{provider.ProviderName} → {credential.Source}";
                    return credential;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    /// <summary>
    /// 获取指定 Vendor 的所有可用提供器（用于列表显示）
    /// </summary>
    public List<ICredentialProvider> GetAvailableProviders(string vendorName)
    {
        return _providers
            .Where(p => p.VendorName.Equals(vendorName, StringComparison.OrdinalIgnoreCase) && p.IsAvailable())
            .ToList();
    }
}