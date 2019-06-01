using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从环境变量读取 NuGet API 密钥
/// </summary>
public class NuGetEnvProvider : ICredentialProvider
{
    public string ProviderName => "nuget (环境变量)";
    public string VendorName => "nuget";
    public int Priority => 20;

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NUGET_API_KEY"));
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? token = Environment.GetEnvironmentVariable("NUGET_API_KEY");

        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            Token = token,
            Source = "NUGET_API_KEY 环境变量"
        });
    }
}