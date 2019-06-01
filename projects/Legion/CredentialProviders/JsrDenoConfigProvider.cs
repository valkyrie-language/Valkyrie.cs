using System.Text.RegularExpressions;
using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从 Deno CLI 配置文件读取 jsr 认证令牌
/// </summary>
public class JsrDenoConfigProvider : ICredentialProvider
{
    public string ProviderName => "jsr (deno config)";
    public string VendorName => "jsr";
    public int Priority => 10;

    public bool IsAvailable()
    {
        string configPath = GetDenoConfigPath();

        if (!File.Exists(configPath))
        {
            return false;
        }

        string content = File.ReadAllText(configPath);
        return content.Contains("jsr.io");
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string configPath = GetDenoConfigPath();

        if (!File.Exists(configPath))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        try
        {
            string content = File.ReadAllText(configPath);

            var match = Regex.Match(content,
                @"""net\.jsr""\s*:\s*""([^""]+)""");

            if (!match.Success)
            {
                match = Regex.Match(content,
                    @"""https://jsr\.io""\s*:\s*""([^""]+)""");
            }

            if (match.Success)
            {
                return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                {
                    Token = match.Groups[1].Value,
                    Source = "Deno 配置文件"
                });
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string GetDenoConfigPath()
    {
        string appData = Environment.GetEnvironmentVariable("APPDATA")
                         ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                         ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "deno", "config.json");
    }
}
