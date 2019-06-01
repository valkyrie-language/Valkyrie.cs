using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从环境变量读取 jsr 令牌
/// </summary>
public class JsrEnvProvider : ICredentialProvider
{
    public string ProviderName => "jsr (环境变量)";
    public string VendorName => "jsr";
    public int Priority => 20;

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JSR_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DENO_AUTH_TOKENS"));
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? token = Environment.GetEnvironmentVariable("JSR_TOKEN");

        if (!string.IsNullOrEmpty(token))
        {
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                Token = token,
                Source = "JSR_TOKEN 环境变量"
            });
        }

        string? denoAuthTokens = Environment.GetEnvironmentVariable("DENO_AUTH_TOKENS");

        if (!string.IsNullOrEmpty(denoAuthTokens))
        {
            var parts = denoAuthTokens.Split(';');

            foreach (string part in parts)
            {
                if (part.StartsWith("jsr@"))
                {
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        Token = part.Substring(4),
                        Source = "DENO_AUTH_TOKENS 环境变量"
                    });
                }
            }
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }
}