using System.Xml.Linq;
using Legion.Auth;

namespace Legion.CredentialProviders;

/// <summary>
/// 从 NuGet.Config 读取 NuGet API 密钥
/// </summary>
public class NuGetConfigProvider : ICredentialProvider
{
    public string ProviderName => "nuget (NuGet.Config)";
    public string VendorName => "nuget";
    public int Priority => 10;

    public bool IsAvailable()
    {
        string configPath = GetNuGetConfigPath();

        if (!File.Exists(configPath))
        {
            return false;
        }

        try
        {
            string content = File.ReadAllText(configPath);
            return content.Contains("packageSourceCredentials") || content.Contains("apikeys");
        }
        catch
        {
            return false;
        }
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        string configPath = GetNuGetConfigPath();

        if (!File.Exists(configPath))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        try
        {
            var doc = XDocument.Load(configPath);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var credentials = doc.Descendants(ns + "packageSourceCredentials")
                .Concat(doc.Descendants("packageSourceCredentials"));

            foreach (var credential in credentials)
            {
                foreach (var source in credential.Elements())
                {
                    string? username = source.Element(ns + "add")?.Attribute("key")
                        ?.Value == "Username" ? source.Element(ns + "add")?.Attribute("value")?.Value
                        : source.Element("add")?.Attribute("key")?.Value == "Username"
                            ? source.Element("add")?.Attribute("value")?.Value
                            : null;

                    string? password = null;

                    foreach (var add in source.Elements(ns + "add").Concat(source.Elements("add")))
                    {
                        string? key = add.Attribute("key")?.Value;

                        if (key == "ClearTextPassword" || key == "Password")
                        {
                            password = add.Attribute("value")?.Value;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(password))
                    {
                        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                        {
                            Token = password,
                            Username = username,
                            Source = $"NuGet.Config [{source.Name.LocalName}]"
                        });
                    }
                }
            }

            var apiKeys = doc.Descendants(ns + "apikeys")
                .Concat(doc.Descendants("apikeys"));

            foreach (var apiKey in apiKeys)
            {
                foreach (var add in apiKey.Elements(ns + "add").Concat(apiKey.Elements("add")))
                {
                    string? value = add.Attribute("value")?.Value;

                    if (!string.IsNullOrEmpty(value))
                    {
                        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                        {
                            Token = value,
                            Source = $"NuGet.Config [apikeys/{add.Attribute("key")?.Value}]"
                        });
                    }
                }
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string GetNuGetConfigPath()
    {
        string appData = Environment.GetEnvironmentVariable("APPDATA")
                         ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                         ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "NuGet", "NuGet.Config");
    }
}