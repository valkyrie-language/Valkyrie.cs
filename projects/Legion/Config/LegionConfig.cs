using System.Text;
using Legion.Tools;
using Oak.Data;
using Oak.Von;

namespace Legion.Config;

public class LegionConfig
{
    /// <summary>
    /// 默认注册表名称
    /// </summary>
    public string Registry { get; set; } = "npm";

    /// <summary>
    /// HTTP 代理地址
    /// </summary>
    public string? Proxy { get; set; }

    /// <summary>
    /// 代理认证用户名
    /// </summary>
    public string? ProxyUsername { get; set; }

    /// <summary>
    /// 代理认证密码
    /// </summary>
    public string? ProxyPassword { get; set; }

    /// <summary>
    /// 是否启用离线模式（仅使用缓存和本地文件）
    /// </summary>
    public bool OfflineMode { get; set; }

    /// <summary>
    /// 网络请求超时秒数
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 下载失败最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 是否验证包完整性哈希
    /// </summary>
    public bool VerifyIntegrity { get; set; } = true;

    /// <summary>
    /// 是否严格验证 SSL 证书
    /// </summary>
    public bool StrictSsl { get; set; } = true;

    /// <summary>
    /// 注册表端点地址映射（名称 → URL）
    /// </summary>
    public Dictionary<string, string> RegistryEndpoints { get; set; } = new();

    /// <summary>
    /// 注册表镜像源映射（原始域名 → 镜像域名）
    /// </summary>
    public Dictionary<string, string> RegistryMirrors { get; set; } = new();

    /// <summary>
    /// 自定义环境变量
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// 全局缓存目录路径（null 表示使用默认路径）
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// 最大并行下载数
    /// </summary>
    public int MaxParallelDownloads { get; set; } = 8;

    private readonly string _globalConfigPath;
    private readonly string? _projectConfigPath;
    private readonly GonParser _parser = new();

    public LegionConfig()
    {
        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string globalDir = Path.Combine(userHome, ".valkyrie");

        if (!Directory.Exists(globalDir))
        {
            Directory.CreateDirectory(globalDir);
        }

        _globalConfigPath = Path.Combine(globalDir, "config.von");
        _projectConfigPath = FindProjectConfig();
    }

    public void Load()
    {
        LoadGlobalConfig();

        if (_projectConfigPath is not null)
        {
            LoadProjectConfig(_projectConfigPath);
        }
    }

    public void SaveGlobal()
    {
        SaveConfig(_globalConfigPath);
    }

    public void SaveProject(string projectDirectory)
    {
        string projectConfigPath = Path.Combine(projectDirectory, "valkyrie.von");
        SaveConfig(projectConfigPath);
    }

    public string? Get(string key)
    {
        return key switch
        {
            "registry" => Registry,
            "proxy" => Proxy,
            "proxyUsername" => ProxyUsername,
            "proxyPassword" => ProxyPassword,
            "offline" => OfflineMode.ToString().ToLower(),
            "timeout" => TimeoutSeconds.ToString(),
            "maxRetries" => MaxRetries.ToString(),
            "verifyIntegrity" => VerifyIntegrity.ToString().ToLower(),
            "strictSsl" => StrictSsl.ToString().ToLower(),
            "cacheDirectory" => CacheDirectory,
            "maxParallelDownloads" => MaxParallelDownloads.ToString(),
            _ => EnvironmentVariables.TryGetValue(key, out var value) ? value : null
        };
    }

    public void Set(string key, string value)
    {
        switch (key)
        {
            case "registry":
                Registry = value;
                break;
            case "proxy":
                Proxy = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "proxyUsername":
                ProxyUsername = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "proxyPassword":
                ProxyPassword = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "offline":
                OfflineMode = bool.TryParse(value, out var offline) && offline;
                break;
            case "timeout":
                TimeoutSeconds = int.TryParse(value, out var timeout) ? timeout : 30;
                break;
            case "maxRetries":
                MaxRetries = int.TryParse(value, out var retries) ? retries : 3;
                break;
            case "verifyIntegrity":
                VerifyIntegrity = !bool.TryParse(value, out var verify) || verify;
                break;
            case "strictSsl":
                StrictSsl = !bool.TryParse(value, out var ssl) || ssl;
                break;
            case "cacheDirectory":
                CacheDirectory = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "maxParallelDownloads":
                MaxParallelDownloads = int.TryParse(value, out var parallel) ? parallel : 8;
                break;
            default:
                EnvironmentVariables[key] = value;
                break;
        }
    }

    /// <summary>
    /// 解析镜像源 URL，如果配置了镜像则返回镜像地址
    /// </summary>
    /// <param name="originalUrl">原始注册表 URL</param>
    /// <returns>替换后的 URL</returns>
    public string ResolveMirror(string originalUrl)
    {
        foreach (var mirror in RegistryMirrors)
        {
            if (originalUrl.Contains(mirror.Key))
            {
                return originalUrl.Replace(mirror.Key, mirror.Value);
            }
        }

        return originalUrl;
    }

    public bool Validate()
    {
        if (TimeoutSeconds <= 0)
        {
            return false;
        }

        if (MaxRetries < 0)
        {
            return false;
        }

        return true;
    }

    private void LoadGlobalConfig()
    {
        if (!File.Exists(_globalConfigPath))
        {
            return;
        }

        LoadConfigFile(_globalConfigPath, isGlobal: true);
    }

    private void LoadProjectConfig(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        LoadConfigFile(path, isGlobal: false);
    }

    private void LoadConfigFile(string path, bool isGlobal)
    {
        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            var root = _parser.Deserialize(content);

            if (root.Type != SerdeValueType.Object)
            {
                return;
            }

            var registryField = root.GetField("registry");
            if (registryField is not null)
            {
                Registry = registryField.GetString() ?? Registry;
            }

            var proxyField = root.GetField("proxy");
            if (proxyField is not null)
            {
                Proxy = proxyField.GetString();
            }

            var offlineField = root.GetField("offline");
            if (offlineField is not null)
            {
                OfflineMode = offlineField.Type == SerdeValueType.Boolean
                    ? offlineField.GetBoolean()
                    : bool.TryParse(offlineField.GetString(), out var offline) && offline;
            }

            var timeoutField = root.GetField("timeout");
            if (timeoutField is not null)
            {
                TimeoutSeconds = timeoutField.Type == SerdeValueType.Integer
                    ? int.Parse(timeoutField.GetIntegerString() ?? "30")
                    : int.TryParse(timeoutField.GetString(), out var timeout) ? timeout : 30;
            }

            var maxRetriesField = root.GetField("maxRetries");
            if (maxRetriesField is not null)
            {
                MaxRetries = maxRetriesField.Type == SerdeValueType.Integer
                    ? int.Parse(maxRetriesField.GetIntegerString() ?? "3")
                    : int.TryParse(maxRetriesField.GetString(), out var retries) ? retries : 3;
            }

            var verifyField = root.GetField("verifyIntegrity");
            if (verifyField is not null)
            {
                VerifyIntegrity = verifyField.Type == SerdeValueType.Boolean
                    ? verifyField.GetBoolean()
                    : !bool.TryParse(verifyField.GetString(), out var verify) || verify;
            }

            var strictSslField = root.GetField("strictSsl");
            if (strictSslField is not null)
            {
                StrictSsl = strictSslField.Type == SerdeValueType.Boolean
                    ? strictSslField.GetBoolean()
                    : !bool.TryParse(strictSslField.GetString(), out var ssl) || ssl;
            }

            var proxyUsernameField = root.GetField("proxyUsername");
            if (proxyUsernameField is not null)
            {
                ProxyUsername = proxyUsernameField.GetString();
            }

            var proxyPasswordField = root.GetField("proxyPassword");
            if (proxyPasswordField is not null)
            {
                ProxyPassword = proxyPasswordField.GetString();
            }

            var cacheDirField = root.GetField("cacheDirectory");
            if (cacheDirField is not null)
            {
                CacheDirectory = cacheDirField.GetString();
            }

            var maxParallelField = root.GetField("maxParallelDownloads");
            if (maxParallelField is not null)
            {
                MaxParallelDownloads = maxParallelField.Type == SerdeValueType.Integer
                    ? int.Parse(maxParallelField.GetIntegerString() ?? "8")
                    : int.TryParse(maxParallelField.GetString(), out var parallel) ? parallel : 8;
            }

            var endpointsField = root.GetField("registryEndpoints");
            if (endpointsField?.Type == SerdeValueType.Object && endpointsField.Fields is not null)
            {
                foreach (var kvp in endpointsField.Fields)
                {
                    RegistryEndpoints[kvp.Key] = kvp.Value.GetString() ?? kvp.Value.ToString();
                }
            }

            var mirrorsField = root.GetField("registryMirrors");
            if (mirrorsField?.Type == SerdeValueType.Object && mirrorsField.Fields is not null)
            {
                foreach (var kvp in mirrorsField.Fields)
                {
                    RegistryMirrors[kvp.Key] = kvp.Value.GetString() ?? kvp.Value.ToString();
                }
            }

            var envVarsField = root.GetField("environment");
            if (envVarsField?.Type == SerdeValueType.Object && envVarsField.Fields is not null)
            {
                foreach (var kvp in envVarsField.Fields)
                {
                    EnvironmentVariables[kvp.Key] = kvp.Value.GetString() ?? kvp.Value.ToString();
                }
            }
        }
        catch
        {
            // 配置文件解析失败时使用默认值
        }
    }

    private void SaveConfig(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var fields = new Dictionary<string, SerdeValue>
        {
            ["registry"] = SerdeValue.String(Registry),
            ["offline"] = SerdeValue.Boolean(OfflineMode),
            ["timeout"] = SerdeValue.Integer(TimeoutSeconds.ToString()),
            ["maxRetries"] = SerdeValue.Integer(MaxRetries.ToString()),
            ["verifyIntegrity"] = SerdeValue.Boolean(VerifyIntegrity),
            ["strictSsl"] = SerdeValue.Boolean(StrictSsl),
            ["maxParallelDownloads"] = SerdeValue.Integer(MaxParallelDownloads.ToString())
        };

        if (Proxy is not null)
        {
            fields["proxy"] = SerdeValue.String(Proxy);
        }

        if (ProxyUsername is not null)
        {
            fields["proxyUsername"] = SerdeValue.String(ProxyUsername);
        }

        if (ProxyPassword is not null)
        {
            fields["proxyPassword"] = SerdeValue.String(ProxyPassword);
        }

        if (CacheDirectory is not null)
        {
            fields["cacheDirectory"] = SerdeValue.String(CacheDirectory);
        }

        if (RegistryEndpoints.Count > 0)
        {
            var endpointsDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in RegistryEndpoints)
            {
                endpointsDict[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields["registryEndpoints"] = SerdeValue.Object(endpointsDict);
        }

        if (RegistryMirrors.Count > 0)
        {
            var mirrorsDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in RegistryMirrors)
            {
                mirrorsDict[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields["registryMirrors"] = SerdeValue.Object(mirrorsDict);
        }

        if (EnvironmentVariables.Count > 0)
        {
            var envDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in EnvironmentVariables)
            {
                envDict[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields["environment"] = SerdeValue.Object(envDict);
        }

        var root = SerdeValue.Object(fields);
        string content = VonFormatter.Format(root);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private string? FindProjectConfig()
    {
        string currentDir = Environment.CurrentDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            string configPath = Path.Combine(currentDir, "valkyrie.von");
            if (File.Exists(configPath))
            {
                return configPath;
            }

            string? parent = Path.GetDirectoryName(currentDir);
            if (parent == currentDir)
            {
                break;
            }

            currentDir = parent ?? string.Empty;
        }

        return null;
    }
}