using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从环境变量读取 Maven 凭据
/// </summary>
public class MavenEnvProvider : ICredentialProvider
{
    public string ProviderName => "maven (环境变量)";
    public string VendorName => "maven";
    public int Priority => 20;

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MAVEN_TOKEN"))
               || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONATYPE_USERNAME"))
                   && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONATYPE_PASSWORD")));
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? token = Environment.GetEnvironmentVariable("MAVEN_TOKEN");

        if (!string.IsNullOrEmpty(token))
        {
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                Token = token,
                Source = "MAVEN_TOKEN 环境变量"
            });
        }

        string? username = Environment.GetEnvironmentVariable("SONATYPE_USERNAME");
        string? password = Environment.GetEnvironmentVariable("SONATYPE_PASSWORD");

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                Token = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{username}:{password}")),
                Username = username,
                Source = "SONATYPE_USERNAME/SONATYPE_PASSWORD 环境变量"
            });
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }
}