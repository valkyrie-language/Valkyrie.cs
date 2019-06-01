using Oak.Von;

namespace Asgard.CLI;

/// <summary>
///     VOA 项目配置加载器，使用 Oak.Von（GonParser）解析 VON 格式的 voa.config.v
///     配置文件使用 VON 格式：{ key: value, nested: { ... } }
/// </summary>
public sealed class VoaConfigLoader
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly GonParser _parser;

    public VoaConfigLoader()
    {
        _parser = new GonParser(_diagnostics);
    }

    /// <summary>
    ///     从项目目录加载配置
    /// </summary>
    public VoaProjectConfig Load(string projectDir)
    {
        var configPath = Path.Combine(projectDir, "voa.config.v");

        if (!File.Exists(configPath))
        {
            return new VoaProjectConfig();
        }

        var content = File.ReadAllText(configPath);
        return ParseConfig(content);
    }

    /// <summary>
    ///     查找工作区根目录（包含 voa.workspace.v 的目录）
    /// </summary>
    public string? FindWorkspaceRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "voa.workspace.v")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     查找项目目录（包含 voa.config.v 的目录）
    /// </summary>
    public string? FindProjectDir(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "voa.config.v")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     解析项目目录路径
    ///     先检查直接路径是否存在，再查找工作区子目录，最后回退到向上搜索
    /// </summary>
    /// <param name="project">项目名或路径</param>
    /// <param name="configLoader">配置加载器实例</param>
    /// <returns>项目绝对路径，找不到返回 <see langword="null" /></returns>
    public static string? ResolveProjectDir(string project, VoaConfigLoader configLoader)
    {
        if (Directory.Exists(project))
        {
            return project;
        }

        var workspaceRoot = configLoader.FindWorkspaceRoot(Directory.GetCurrentDirectory());
        if (workspaceRoot is not null)
        {
            var candidate = Path.Combine(workspaceRoot, "projects", project);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return configLoader.FindProjectDir(Directory.GetCurrentDirectory());
    }

    /// <summary>
    ///     解析 voa.config.v 内容（VON 格式）
    ///     语法：{ key: value, nested: { ... } }
    ///     GonParser 直接解析 VON 对象，无需 define_config(voa) 包装
    /// </summary>
    private VoaProjectConfig ParseConfig(string content)
    {
        var config = new VoaProjectConfig();

        try
        {
            var root = _parser.Parse(content);
            ExtractConfigFromGonValue(root, config);
        }
        catch (Exception)
        {
            // 解析失败时使用默认配置
        }

        return config;
    }

    /// <summary>
    ///     从 GonValue 对象中提取配置
    /// </summary>
    private void ExtractConfigFromGonValue(GonValue root, VoaProjectConfig config)
    {
        if (root.Fields is null) return;

        if (root.GetField("project_type") is { } projectType)
        {
            config.ProjectType = projectType.GetString() ?? "frontend";
        }

        if (root.GetField("target") is { } target)
        {
            config.Target = target.GetString() ?? "wasm";
        }

        if (root.GetField("server") is { } server)
        {
            config.Server = ExtractServerConfig(server);
        }

        if (root.GetField("build") is { } build)
        {
            ExtractBuildConfig(build, config.Build);
        }

        if (root.GetField("hot_reload") is { } hotReload)
        {
            ExtractHotReloadConfig(hotReload, config.HotReload);
        }
    }

    private static VoaServerConfig ExtractServerConfig(GonValue server)
    {
        var result = new VoaServerConfig();

        if (server.GetField("host") is { } host)
        {
            result.Host = host.GetString() ?? "localhost";
        }

        if (server.GetField("port") is { } port)
        {
            var portStr = port.GetIntegerString();
            if (portStr is not null && int.TryParse(portStr, out var portVal) && portVal > 0)
            {
                result.Port = portVal;
            }
        }

        if (server.GetField("workers") is { } workers)
        {
            var workersStr = workers.GetIntegerString();
            if (workersStr is not null && int.TryParse(workersStr, out var workersVal))
            {
                result.Workers = workersVal;
            }
        }

        if (server.GetField("timeout") is { } timeout)
        {
            var timeoutStr = timeout.GetIntegerString();
            if (timeoutStr is not null && int.TryParse(timeoutStr, out var timeoutVal) && timeoutVal > 0)
            {
                result.Timeout = timeoutVal;
            }
        }

        return result;
    }

    private static void ExtractBuildConfig(GonValue build, VoaBuildConfig result)
    {
        if (build.GetField("output") is { } output)
        {
            result.Output = output.GetString() ?? "dist";
        }

        if (build.GetField("minify") is { } minify)
        {
            result.Minify = minify.GetBoolean();
        }

        if (build.GetField("sourcemap") is { } sourcemap)
        {
            result.Sourcemap = sourcemap.GetBoolean();
        }

        if (build.GetField("generate_wat") is { } generateWat)
        {
            result.GenerateWat = generateWat.GetBoolean();
        }
    }

    private static void ExtractHotReloadConfig(GonValue hotReload, VoaHotReloadConfig result)
    {
        if (hotReload.GetField("enabled") is { } enabled)
        {
            result.Enabled = enabled.GetBoolean();
        }

        if (hotReload.GetField("debounce") is { } debounce)
        {
            var debounceStr = debounce.GetIntegerString();
            if (debounceStr is not null && int.TryParse(debounceStr, out var debounceVal) && debounceVal > 0)
            {
                result.Debounce = debounceVal;
            }
        }

        if (hotReload.GetField("watch") is { } watch && watch.Elements is not null)
        {
            result.Watch = [];
            foreach (var elem in watch.Elements)
            {
                var val = elem.GetString();
                if (val is not null)
                {
                    result.Watch.Add(val);
                }
            }
        }

        if (hotReload.GetField("ignore") is { } ignore && ignore.Elements is not null)
        {
            result.Ignore = [];
            foreach (var elem in ignore.Elements)
            {
                var val = elem.GetString();
                if (val is not null)
                {
                    result.Ignore.Add(val);
                }
            }
        }
    }
}