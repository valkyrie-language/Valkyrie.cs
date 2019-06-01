using Legion.Registry;
using Legion.Registry.Conda;
using Legion.Registry.Jsr;
using Legion.Registry.Maven;
using Legion.Registry.Npm;
using Legion.Registry.Nuget;
using Oak.Data;
using Oak.Von;

namespace Legion.Auth;

/// <summary>
/// 注册表源管理器，持久化管理注册表端点配置
/// </summary>
public class RegistrySourceManager
{
    private readonly string _sourcesFilePath;
    private readonly Dictionary<string, string> _sources = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 创建注册表源管理器
    /// </summary>
    /// <param name="configDirectory">配置目录路径（通常为 ~/.valkyrie/）</param>
    public RegistrySourceManager(string configDirectory)
    {
        _sourcesFilePath = Path.Combine(configDirectory, "registry-sources.von");
        EnsureDefaultSources();
    }

    /// <summary>
    /// 获取所有已配置的注册表源
    /// </summary>
    public IReadOnlyDictionary<string, string> Sources => _sources;

    public void RemoveSource(string registryName)
    {
        _sources.Remove(registryName.ToLowerInvariant());
    }

    /// <summary>
    /// 获取指定注册表的端点
    /// </summary>
    public string? GetEndpoint(string registryName)
    {
        return _sources.TryGetValue(registryName.ToLowerInvariant(), out var endpoint) ? endpoint : null;
    }

    /// <summary>
    /// 设置注册表端点
    /// </summary>
    public void SetEndpoint(string registryName, string endpoint)
    {
        _sources[registryName.ToLowerInvariant()] = endpoint;
    }

    /// <summary>
    /// 移除注册表源配置
    /// </summary>
    /// <param name="registryName">注册表名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveEndpoint(string registryName)
    {
        return _sources.Remove(registryName.ToLowerInvariant());
    }

    /// <summary>
    /// 从文件加载注册表源配置
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_sourcesFilePath))
        {
            EnsureDefaultSources();
            return;
        }

        try
        {
            string content = File.ReadAllText(_sourcesFilePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                EnsureDefaultSources();
                return;
            }

            _sources.Clear();

            var parser = new GonParser();
            var value = parser.Deserialize(content);

            if (value.Type == SerdeValueType.Object && value.Fields is not null)
            {
                foreach (var field in value.Fields)
                {
                    _sources[field.Key.ToLowerInvariant()] = field.Value.GetString() ?? field.Value.ToString();
                }
            }
        }
        catch
        {
            EnsureDefaultSources();
        }
    }

    /// <summary>
    /// 保存注册表源配置到文件
    /// </summary>
    public async Task SaveAsync()
    {
        string? dir = Path.GetDirectoryName(_sourcesFilePath);

        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var entries = _sources.Select(kv => $"{kv.Key}: \"{kv.Value}\"").ToList();
        string content = "{\n" + string.Join(",\n", entries.Select(e => "    " + e)) + "\n}\n";

        await File.WriteAllTextAsync(_sourcesFilePath, content);
    }

    /// <summary>
    /// 创建对应注册表的实例
    /// </summary>
    /// <param name="registryName">注册表名称</param>
    /// <returns>注册表实例</returns>
    public IRegistry CreateRegistry(string registryName)
    {
        string endpoint = GetEndpoint(registryName)
                          ?? GetDefaultEndpoint(registryName);

        return registryName.ToLowerInvariant() switch
        {
            "npm" => new NpmRegistry { Endpoint = endpoint },
            "jsr" => new JsrRegistry { Endpoint = endpoint },
            "conda" => new CondaRegistry { Endpoint = endpoint },
            "maven" => new MavenRegistry { Endpoint = endpoint },
            "nuget" => new NuGetRegistry { Endpoint = endpoint },
            _ => throw new ArgumentException($"不支持的注册表类型: {registryName}")
        };
    }

    private void EnsureDefaultSources()
    {
        foreach (var (name, endpoint) in s_defaultSources)
        {
            if (!_sources.ContainsKey(name))
            {
                _sources[name] = endpoint;
            }
        }
    }

    private static string GetDefaultEndpoint(string registryName)
    {
        return s_defaultSources.TryGetValue(registryName.ToLowerInvariant(), out var endpoint)
            ? endpoint
            : throw new ArgumentException($"未知的注册表类型: {registryName}");
    }

    private static readonly Dictionary<string, string> s_defaultSources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["npm"] = "https://registry.npmjs.org",
        ["jsr"] = "https://jsr.io",
        ["conda"] = "https://api.anaconda.org",
        ["maven"] = "https://search.maven.org",
        ["nuget"] = "https://api.nuget.org/v3"
    };
}