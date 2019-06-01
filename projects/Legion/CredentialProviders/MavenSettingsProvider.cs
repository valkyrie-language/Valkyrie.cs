using System.Xml.Linq;
using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从 ~/.m2/settings.xml 读取 Maven 认证凭据
/// </summary>
public class MavenSettingsProvider : ICredentialProvider
{
    public string ProviderName => "maven (settings.xml)";
    public string VendorName => "maven";
    public int Priority => 10;

    public bool IsAvailable()
    {
        string settingsPath = GetMavenSettingsPath();

        if (!File.Exists(settingsPath))
        {
            return false;
        }

        try
        {
            string content = File.ReadAllText(settingsPath);
            return content.Contains("<server>") && content.Contains("ossrh");
        }
        catch
        {
            return false;
        }
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string settingsPath = GetMavenSettingsPath();

        if (!File.Exists(settingsPath))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        try
        {
            var doc = XDocument.Load(settingsPath);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var servers = doc.Descendants(ns + "server")
                .Concat(doc.Descendants("server"));

            foreach (var server in servers)
            {
                string? id = server.Element(ns + "id")?.Value
                             ?? server.Element("id")?.Value;

                if (id is null || (!id.Contains("ossrh", StringComparison.OrdinalIgnoreCase)
                                   && !id.Contains("central", StringComparison.OrdinalIgnoreCase)
                                   && !id.Contains("sonatype", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string? username = server.Element(ns + "username")?.Value
                                   ?? server.Element("username")?.Value;
                string? password = server.Element(ns + "password")?.Value
                                   ?? server.Element("password")?.Value;

                string? token = null;

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    token = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                }
                else
                {
                    string? privateKey = server.Element(ns + "privateKey")?.Value
                                         ?? server.Element("privateKey")?.Value;

                    if (!string.IsNullOrEmpty(privateKey))
                    {
                        token = privateKey;
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        Token = token,
                        Username = username,
                        Source = $"~/.m2/settings.xml [server id={id}]"
                    });
                }
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string GetMavenSettingsPath()
    {
        string home = Environment.GetEnvironmentVariable("USERPROFILE")
                      ?? Environment.GetEnvironmentVariable("HOME")
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".m2", "settings.xml");
    }
}