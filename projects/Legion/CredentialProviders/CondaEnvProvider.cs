using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从环境变量读取 conda 令牌
/// </summary>
public class CondaEnvProvider : ICredentialProvider
{
    public string ProviderName => "conda (环境变量)";
    public string VendorName => "conda";
    public int Priority => 20;

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANACONDA_API_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONDA_TOKEN"));
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? token = Environment.GetEnvironmentVariable("ANACONDA_API_TOKEN")
                        ?? Environment.GetEnvironmentVariable("CONDA_TOKEN");

        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            Token = token,
            Source = "ANACONDA_API_TOKEN 环境变量"
        });
    }
}