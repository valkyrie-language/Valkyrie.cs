using System.Text.RegularExpressions;
using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从 Anaconda nucleus 读取 conda 认证令牌
/// </summary>
public class CondaAnacondaProvider : ICredentialProvider
{
    public string ProviderName => "conda (anaconda nucleus)";
    public string VendorName => "conda";
    public int Priority => 10;

    public bool IsAvailable()
    {
        return TryFindTokenFile() is not null;
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string? tokenPath = TryFindTokenFile();

        if (tokenPath is null || !File.Exists(tokenPath))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        try
        {
            string content = File.ReadAllText(tokenPath);

            var match = Regex.Match(content, @"""token""\s*:\s*""([^""]+)""");

            if (!match.Success)
            {
                match = Regex.Match(content, @"""access_token""\s*:\s*""([^""]+)""");
            }

            if (match.Success)
            {
                string? username = null;
                var userMatch = Regex.Match(content, @"""login""\s*:\s*""([^""]+)""");

                if (userMatch.Success)
                {
                    username = userMatch.Groups[1].Value;
                }

                return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                {
                    Token = match.Groups[1].Value,
                    Username = username,
                    Source = "Anaconda Nucleus 令牌"
                });
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string? TryFindTokenFile()
    {
        string home = Environment.GetEnvironmentVariable("USERPROFILE")
                      ?? Environment.GetEnvironmentVariable("HOME")
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string nucleusDir = Path.Combine(home, ".anaconda", "nucleus");

        if (!Directory.Exists(nucleusDir))
        {
            return null;
        }

        foreach (string userDir in Directory.GetDirectories(nucleusDir))
        {
            string tokensFile = Path.Combine(userDir, "tokens.json");

            if (File.Exists(tokensFile))
            {
                return tokensFile;
            }
        }

        return null;
    }
}