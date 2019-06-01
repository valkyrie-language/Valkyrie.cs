using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从环境变量读取 npm 令牌
/// </summary>
public class NpmEnvProvider : ICredentialProvider
{
    public string ProviderName => "npm (环境变量)";
    public string VendorName => "npm";
    public int Priority => 20;

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NPM_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NODE_AUTH_TOKEN"));
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? token = Environment.GetEnvironmentVariable("NPM_TOKEN")
                        ?? Environment.GetEnvironmentVariable("NODE_AUTH_TOKEN");

        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            Token = token,
            Source = "NODE_AUTH_TOKEN 环境变量"
        });
    }
}