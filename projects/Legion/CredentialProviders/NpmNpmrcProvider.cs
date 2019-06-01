using System.Text.RegularExpressions;
using Legion.Auth;

namespace Legion.CredentialProviders;

#region npm 凭据提供器

/// <summary>
/// 从 ~/.npmrc 读取 npm 认证令牌
/// </summary>
public class NpmNpmrcProvider : ICredentialProvider
{
    public string ProviderName => "npm-cli (.npmrc)";
    public string VendorName => "npm";
    public int Priority => 10;

    public bool IsAvailable()
    {
        return File.Exists(GetGlobalNpmrcPath());
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string npmrcPath = GetGlobalNpmrcPath();

        if (!File.Exists(npmrcPath))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        string content = File.ReadAllText(npmrcPath);
        string? token = ParseNpmrcToken(content, "//registry.npmjs.org/");

        if (token is null)
        {
            string configuredRegistry = ParseNpmrcRegistry(content);

            if (!string.IsNullOrEmpty(configuredRegistry))
            {
                token = ParseNpmrcToken(content, configuredRegistry);
            }
        }

        if (token is null)
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            Token = token,
            Source = $"~/.npmrc (用户级)"
        });
    }

    private static string GetGlobalNpmrcPath()
    {
        string home = Environment.GetEnvironmentVariable("USERPROFILE")
                      ?? Environment.GetEnvironmentVariable("HOME")
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".npmrc");
    }

    private static string? ParseNpmrcToken(string content, string registryPrefix)
    {
        string escapedPrefix = Regex.Escape(registryPrefix);
        var match = Regex.Match(content,
            $@"^{escapedPrefix}:_authToken\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        match = Regex.Match(content,
            $@"^{escapedPrefix}:_auth\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(match.Groups[1].Value.Trim());
                string auth = System.Text.Encoding.UTF8.GetString(bytes);
                int colonIndex = auth.IndexOf(':');

                if (colonIndex >= 0)
                {
                    return auth.Substring(colonIndex + 1);
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? ParseNpmrcRegistry(string content)
    {
        var match = Regex.Match(content,
            @"^registry\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success)
        {
            string registry = match.Groups[1].Value.Trim();

            if (!registry.EndsWith("/"))
            {
                registry += "/";
            }

            return registry;
        }

        return null;
    }
}

#endregion

#region jsr 凭据提供器

#endregion

#region conda 凭据提供器

#endregion

#region Maven Central 凭据提供器

#endregion

#region NuGet 凭据提供器

#endregion

#region 凭据发现管理器

#endregion