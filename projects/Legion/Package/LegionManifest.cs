using Legion.Tools;
using Legion.Version;
using Oak.Data;
using Oak.Von;

namespace Legion.Package;

/// <summary>
/// 生命周期钩子定义，用于在特定构建阶段执行自定义命令
/// </summary>
public class LifecycleHook
{
    /// <summary>
    /// 要执行的命令
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 执行失败时是否中止构建
    /// </summary>
    public bool FailOnError { get; set; } = true;

    /// <summary>
    /// 使用的 Shell（pwsh / cmd / bash），空则使用系统默认
    /// </summary>
    public string? Shell { get; set; }

    /// <summary>
    /// 平台条件表达式，满足条件时才执行（如 "windows"、"!linux"）
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 钩子描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 包清单 legion.von 的数据模型（类比 package.json）
/// </summary>
public class LegionManifest
{
    /// <summary>
    /// 包名称（必须符合 @scope/name 或 name 格式）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号（遵循 SemVer 2.0）
    /// </summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>
    /// 包描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目主页 URL
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 许可证标识（如 MIT、Apache-2.0）
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// 作者信息
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 贡献者列表
    /// </summary>
    public List<string> Contributors { get; set; } = new();

    /// <summary>
    /// 代码仓库地址
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// Bug 反馈地址
    /// </summary>
    public string? Bugs { get; set; }

    /// <summary>
    /// 资金赞助信息
    /// </summary>
    public string? Funding { get; set; }

    /// <summary>
    /// 主入口文件路径
    /// </summary>
    public string? Main { get; set; }

    /// <summary>
    /// CLI 可执行文件路径
    /// </summary>
    public string? Bin { get; set; }

    /// <summary>
    /// ES Module 入口文件路径
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// TypeScript 类型定义文件路径
    /// </summary>
    public string? Types { get; set; }

    /// <summary>
    /// 导出映射
    /// </summary>
    public Dictionary<string, object?>? Exports { get; set; }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public List<string>? Keywords { get; set; }

    /// <summary>
    /// 是否为私有包（不发布到注册表）
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// 发布配置（指定目标注册表、访问级别等）
    /// </summary>
    public PublishConfig PublishConfig { get; set; } = new();

    /// <summary>
    /// 构建目标列表（legion.von 的 build 字段）
    /// </summary>
    public List<BuildTarget> BuildTargets { get; set; } = new();

    /// <summary>
    /// 运行时依赖
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>
    /// 开发依赖
    /// </summary>
    public Dictionary<string, string> DevDependencies { get; set; } = new();

    /// <summary>
    /// 对等依赖
    /// </summary>
    public Dictionary<string, string> PeerDependencies { get; set; } = new();

    /// <summary>
    /// 可选依赖
    /// </summary>
    public Dictionary<string, string> OptionalDependencies { get; set; } = new();

    /// <summary>
    /// 版本覆盖规则
    /// </summary>
    public Dictionary<string, string> Overrides { get; set; } = new();

    /// <summary>
    /// 脚本定义
    /// </summary>
    public Dictionary<string, string> Scripts { get; set; } = new();

    /// <summary>
    /// 生命周期钩子定义
    /// </summary>
    public Dictionary<string, LifecycleHook> Hooks { get; set; } = new();

    /// <summary>
    /// 包含文件列表（发布时仅包含这些文件）
    /// </summary>
    public List<string> Files { get; set; } = new();

    /// <summary>
    /// 引擎版本要求（如 valkyrie: ">=1.0.0"）
    /// </summary>
    public Dictionary<string, string> Engines { get; set; } = new();

    /// <summary>
    /// 操作系统兼容性
    /// </summary>
    public Dictionary<string, string> Os { get; set; } = new();

    /// <summary>
    /// CPU 架构兼容性
    /// </summary>
    public Dictionary<string, string> Cpu { get; set; } = new();

    private readonly string _filePath;
    private readonly GonParser _parser = new();

    public LegionManifest(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "legion.von");
    }

    public static LegionManifest Load(string directoryPath)
    {
        var manifest = new LegionManifest(directoryPath);
        manifest.Load();
        return manifest;
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"legion.von 未找到: {_filePath}");
        }

        string content = File.ReadAllText(_filePath);
        var root = _parser.Deserialize(content);

        if (root.Type != SerdeValueType.Object)
        {
            throw new FormatException("legion.von 根元素必须是对象");
        }

        Name = root.GetField("name")?.GetString() ?? string.Empty;
        Version = root.GetField("version")?.GetString() ?? "0.0.0";
        Description = root.GetField("description")?.GetString();
        Homepage = root.GetField("homepage")?.GetString();
        License = root.GetField("license")?.GetString();
        Author = root.GetField("author")?.GetString();
        Repository = root.GetField("repository")?.GetString();
        Main = root.GetField("main")?.GetString();
        Bin = root.GetField("bin")?.GetString();
        Module = root.GetField("module")?.GetString();
        Types = root.GetField("types")?.GetString();

        var exportsField = root.GetField("exports");
        if (exportsField?.Type == SerdeValueType.Object && exportsField.Fields is not null)
        {
            Exports = exportsField.Fields.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value.GetString());
        }

        var keywordsField = root.GetField("keywords");
        if (keywordsField?.Type == SerdeValueType.Array && keywordsField.Elements is not null)
        {
            Keywords = keywordsField.Elements
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        Private = root.GetField("private")?.GetBoolean() ?? false;

        var contributorsField = root.GetField("contributors");
        if (contributorsField?.Type == SerdeValueType.Array && contributorsField.Elements is not null)
        {
            Contributors = contributorsField.Elements
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        var publishConfigField = root.GetField("publishConfig");
        if (publishConfigField?.Type == SerdeValueType.Object && publishConfigField.Fields is not null)
        {
            PublishConfig = new PublishConfig
            {
                Registry = publishConfigField.GetField("registry")?.GetString(),
                Access = publishConfigField.GetField("access")?.GetString() ?? "public",
                Tag = publishConfigField.GetField("tag")?.GetString() ?? "latest"
            };
        }

        var buildField = root.GetField("build");
        if (buildField?.Type == SerdeValueType.Array && buildField.Elements is not null)
        {
            BuildTargets = buildField.Elements
                .Where(e => e.Type == SerdeValueType.Object && e.Fields is not null)
                .Select(e => new BuildTarget
                {
                    Target = e.Fields!["target"].GetString() ?? string.Empty,
                    SourceMap = e.Fields!.TryGetValue("source_map", out var sm) && sm.GetBoolean(),
                    TypeScript = e.Fields!.TryGetValue("typescript", out var ts) && ts.GetBoolean(),
                    Wat = e.Fields!.TryGetValue("wat", out var wat) && wat.GetBoolean(),
                    Msil = e.Fields!.TryGetValue("msil", out var msil) && msil.GetBoolean()
                })
                .Where(bt => !string.IsNullOrEmpty(bt.Target))
                .ToList();
        }

        LoadDictionary(root, "dependencies", Dependencies);
        LoadDictionary(root, "devDependencies", DevDependencies);
        LoadDictionary(root, "peerDependencies", PeerDependencies);
        LoadDictionary(root, "optionalDependencies", OptionalDependencies);
        LoadDictionary(root, "scripts", Scripts);
        LoadHooks(root);
        LoadDictionary(root, "engines", Engines);
        LoadDictionary(root, "os", Os);
        LoadDictionary(root, "cpu", Cpu);
        LoadDictionary(root, "overrides", Overrides);

        var filesField = root.GetField("files");
        if (filesField?.Type == SerdeValueType.Array && filesField.Elements is not null)
        {
            Files = filesField.Elements
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        var baseDir = Path.GetDirectoryName(_filePath);
        if (baseDir is not null)
        {
            var scriptDir = Path.Combine(baseDir, "script");
            if (Directory.Exists(scriptDir))
            {
                foreach (var scriptFile in Directory.GetFiles(scriptDir, "*.v"))
                {
                    var scriptName = Path.GetFileNameWithoutExtension(scriptFile);
                    if (!Scripts.ContainsKey(scriptName))
                    {
                        Scripts[scriptName] = $"run {scriptFile}";
                    }
                }
            }
        }
    }

    public void Save()
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.String(Name),
            ["version"] = SerdeValue.String(Version)
        };

        if (Description is not null) fields["description"] = SerdeValue.String(Description);
        if (Homepage is not null) fields["homepage"] = SerdeValue.String(Homepage);
        if (License is not null) fields["license"] = SerdeValue.String(License);
        if (Author is not null) fields["author"] = SerdeValue.String(Author);
        if (Repository is not null) fields["repository"] = SerdeValue.String(Repository);
        if (Bugs is not null) fields["bugs"] = SerdeValue.String(Bugs);
        if (Funding is not null) fields["funding"] = SerdeValue.String(Funding);
        if (Main is not null) fields["main"] = SerdeValue.String(Main);
        if (Bin is not null) fields["bin"] = SerdeValue.String(Bin);
        if (Module is not null) fields["module"] = SerdeValue.String(Module);
        if (Types is not null) fields["types"] = SerdeValue.String(Types);
        if (Private) fields["private"] = SerdeValue.Boolean(Private);

        if (Contributors.Count > 0)
        {
            fields["contributors"] = SerdeValue.Array(Contributors.Select(c => SerdeValue.String(c)).ToList());
        }

        if (PublishConfig.Registry is not null || PublishConfig.Access != "public" || PublishConfig.Tag != "latest")
        {
            var publishConfigFields = new Dictionary<string, SerdeValue>();
            if (PublishConfig.Registry is not null)
            {
                publishConfigFields["registry"] = SerdeValue.String(PublishConfig.Registry);
            }

            if (PublishConfig.Access != "public")
            {
                publishConfigFields["access"] = SerdeValue.String(PublishConfig.Access);
            }

            if (PublishConfig.Tag != "latest")
            {
                publishConfigFields["tag"] = SerdeValue.String(PublishConfig.Tag);
            }

            fields["publishConfig"] = SerdeValue.Object(publishConfigFields);
        }

        if (Exports is not null && Exports.Count > 0)
        {
            var exportFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Exports)
            {
                exportFields[kvp.Key] = SerdeValue.String(kvp.Value?.ToString() ?? string.Empty);
            }

            fields["exports"] = SerdeValue.Object(exportFields);
        }

        if (Keywords is not null && Keywords.Count > 0)
        {
            fields["keywords"] = SerdeValue.Array(Keywords.Select(k => SerdeValue.String(k)).ToList());
        }

        if (BuildTargets.Count > 0)
        {
            var buildElements = BuildTargets.Select(bt =>
            {
                var btFields = new Dictionary<string, SerdeValue>
                {
                    ["target"] = SerdeValue.String(bt.Target)
                };
                if (bt.SourceMap)
                {
                    btFields["source_map"] = SerdeValue.Boolean(true);
                }

                if (bt.TypeScript)
                {
                    btFields["typescript"] = SerdeValue.Boolean(true);
                }

                if (bt.Wat)
                {
                    btFields["wat"] = SerdeValue.Boolean(true);
                }

                if (bt.Msil)
                {
                    btFields["msil"] = SerdeValue.Boolean(true);
                }
                return SerdeValue.Object(btFields);
            }).ToList();

            fields["build"] = SerdeValue.Array(buildElements);
        }

        SaveDictionary(fields, "dependencies", Dependencies);
        SaveDictionary(fields, "devDependencies", DevDependencies);
        SaveDictionary(fields, "peerDependencies", PeerDependencies);
        SaveDictionary(fields, "optionalDependencies", OptionalDependencies);
        SaveDictionary(fields, "scripts", Scripts);
        SaveHooks(fields);
        SaveDictionary(fields, "engines", Engines);
        SaveDictionary(fields, "os", Os);
        SaveDictionary(fields, "cpu", Cpu);
        SaveDictionary(fields, "overrides", Overrides);

        if (Files.Count > 0)
        {
            fields["files"] = SerdeValue.Array(Files.Select(f => SerdeValue.String(f)).ToList());
        }

        var root = SerdeValue.Object(fields);
        File.WriteAllText(_filePath, VonFormatter.Format(root));
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return false;
        }

        if (!SemanticVersion.TryParse(Version, out _))
        {
            return false;
        }

        if (Name.StartsWith('@') && !Name.Contains('/'))
        {
            return false;
        }

        if (Name.Any(c => char.IsUpper(c)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 详细验证，返回所有验证错误列表
    /// </summary>
    /// <returns>验证错误列表（空列表表示通过）</returns>
    public List<string> ValidateDetailed()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("包名称不能为空");
        }
        else
        {
            if (Name.Any(c => char.IsUpper(c)))
            {
                errors.Add("包名称不能包含大写字母");
            }

            if (Name.StartsWith('@') && !Name.Contains('/'))
            {
                errors.Add("作用域包名称格式应为 @scope/name");
            }

            if (Name.Length > 214)
            {
                errors.Add("包名称长度不能超过 214 个字符");
            }
        }

        if (!SemanticVersion.TryParse(Version, out _))
        {
            errors.Add($"版本号 '{Version}' 不是有效的 SemVer 2.0 格式");
        }

        if (Private && PublishConfig.Access == "public")
        {
            errors.Add("私有包不能设置 publishConfig.access 为 public");
        }

        if (Engines.TryGetValue("valkyrie", out var engineVersion))
        {
            if (!SemanticVersion.TryParse(engineVersion.TrimStart('^', '~', '>', '<', '='), out _))
            {
                errors.Add($"引擎版本约束 '{engineVersion}' 格式无效");
            }
        }

        foreach (var dep in Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dep.Key))
            {
                errors.Add("依赖名称不能为空");
            }
        }

        return errors;
    }

    public string GetScript(string name)
    {
        return Scripts.TryGetValue(name, out var script) ? script : string.Empty;
    }

    public bool HasScript(string name)
    {
        return Scripts.ContainsKey(name);
    }

    /// <summary>
    /// 获取指定名称的钩子定义
    /// </summary>
    /// <param name="name">钩子名称</param>
    public LifecycleHook? GetHook(string name)
    {
        return Hooks.TryGetValue(name, out var hook) ? hook : null;
    }

    /// <summary>
    /// 检查指定钩子是否存在
    /// </summary>
    /// <param name="name">钩子名称</param>
    public bool HasHook(string name)
    {
        return Hooks.ContainsKey(name);
    }

    /// <summary>
    /// 获取源码中所有标记为 [main] 的 micro 函数名称及其所属文件
    /// </summary>
    /// <returns>micro 函数名 → (文件路径, 行内容) 的映射</returns>
    public Dictionary<string, (string FilePath, string Line)> GetMicroFunctions()
    {
        var result = new Dictionary<string, (string, string)>();

        var baseDir = Path.GetDirectoryName(_filePath);
        if (baseDir is null)
        {
            return result;
        }

        var sourceDir = Path.Combine(baseDir, "source");
        if (!Directory.Exists(sourceDir))
        {
            return result;
        }

        foreach (var vFile in Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(vFile);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();

                if (line.StartsWith("micro ") && i >= 1)
                {
                    var prevLine = lines[i - 1].Trim();
                    if (prevLine == "[main]")
                    {
                        var funcName = line["micro ".Length..].Trim();
                        var parenIdx = funcName.IndexOf('(');
                        if (parenIdx > 0)
                        {
                            funcName = funcName[..parenIdx].Trim();
                        }

                        var braceIdx = funcName.IndexOf('{');
                        if (braceIdx > 0)
                        {
                            funcName = funcName[..braceIdx].Trim();
                        }

                        result[funcName] = (vFile, line);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 标准生命周期钩子名称
    /// </summary>
    public static class HookNames
    {
        public const string PreBuild = "preBuild";
        public const string PostBuild = "postBuild";
        public const string PrePublish = "prePublish";
        public const string PostPublish = "postPublish";
        public const string PreInstall = "preInstall";
        public const string PostInstall = "postInstall";
        public const string PrePack = "prePack";
        public const string PostPack = "postPack";
        public const string PreTest = "preTest";
        public const string PostTest = "postTest";
        public const string PreClean = "preClean";
        public const string PostClean = "postClean";
    }

    public void AddDependency(string packageName, string version, string type = "dependencies")
    {
        var dict = type switch
        {
            "dev" or "devDependencies" => DevDependencies,
            "peer" or "peerDependencies" => PeerDependencies,
            "optional" or "optionalDependencies" => OptionalDependencies,
            _ => Dependencies
        };

        dict[packageName] = version;
    }

    public void RemoveDependency(string packageName, string type = "dependencies")
    {
        var dict = type switch
        {
            "dev" or "devDependencies" => DevDependencies,
            "peer" or "peerDependencies" => PeerDependencies,
            "optional" or "optionalDependencies" => OptionalDependencies,
            _ => Dependencies
        };

        dict.Remove(packageName);
    }

    private void LoadDictionary(SerdeValue root, string key, Dictionary<string, string> target)
    {
        target.Clear();
        var field = root.GetField(key);
        if (field?.Type == SerdeValueType.Object && field.Fields is not null)
        {
            foreach (var kvp in field.Fields)
            {
                target[kvp.Key] = kvp.Value.GetString() ?? kvp.Value.ToString();
            }
        }
    }

    private void SaveDictionary(Dictionary<string, SerdeValue> fields, string key, Dictionary<string, string> source)
    {
        if (source.Count > 0)
        {
            var dict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in source.OrderBy(p => p.Key))
            {
                dict[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields[key] = SerdeValue.Object(dict);
        }
    }

    private void LoadHooks(SerdeValue root)
    {
        Hooks.Clear();
        var hooksField = root.GetField("hooks");
        if (hooksField?.Type != SerdeValueType.Object || hooksField.Fields is null)
        {
            return;
        }

        foreach (var kvp in hooksField.Fields)
        {
            var hookValue = kvp.Value;
            var hook = new LifecycleHook();

            if (hookValue.Type == SerdeValueType.String)
            {
                hook.Command = hookValue.GetString() ?? string.Empty;
            }
            else if (hookValue.Type == SerdeValueType.Object && hookValue.Fields is not null)
            {
                if (hookValue.Fields.TryGetValue("command", out var cmd))
                {
                    hook.Command = cmd.GetString() ?? string.Empty;
                }

                if (hookValue.Fields.TryGetValue("failOnError", out var foe))
                {
                    hook.FailOnError = foe.GetBoolean();
                }

                if (hookValue.Fields.TryGetValue("shell", out var sh))
                {
                    hook.Shell = sh.GetString();
                }

                if (hookValue.Fields.TryGetValue("condition", out var cond))
                {
                    hook.Condition = cond.GetString();
                }

                if (hookValue.Fields.TryGetValue("description", out var desc))
                {
                    hook.Description = desc.GetString();
                }
            }

            Hooks[kvp.Key] = hook;
        }
    }

    private void SaveHooks(Dictionary<string, SerdeValue> fields)
    {
        if (Hooks.Count > 0)
        {
            var hookFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Hooks.OrderBy(p => p.Key))
            {
                var hookDict = new Dictionary<string, SerdeValue>
                {
                    ["command"] = SerdeValue.String(kvp.Value.Command),
                    ["failOnError"] = SerdeValue.Boolean(kvp.Value.FailOnError)
                };

                if (kvp.Value.Shell is not null)
                {
                    hookDict["shell"] = SerdeValue.String(kvp.Value.Shell);
                }

                if (kvp.Value.Condition is not null)
                {
                    hookDict["condition"] = SerdeValue.String(kvp.Value.Condition);
                }

                if (kvp.Value.Description is not null)
                {
                    hookDict["description"] = SerdeValue.String(kvp.Value.Description);
                }

                hookFields[kvp.Key] = SerdeValue.Object(hookDict);
            }

            fields["hooks"] = SerdeValue.Object(hookFields);
        }
    }
}

/// <summary>
/// 发布配置，控制包发布到注册表时的行为
/// </summary>
public class PublishConfig
{
    /// <summary>
    /// 目标注册表名称（覆盖默认注册表）
    /// </summary>
    public string? Registry { get; set; }

    /// <summary>
    /// 访问级别：public 或 restricted
    /// </summary>
    public string Access { get; set; } = "public";

    /// <summary>
    /// 发布标签（默认 latest，可设为 next/beta/rc 等）
    /// </summary>
    public string Tag { get; set; } = "latest";
}

/// <summary>
///     构建目标定义，支持短别名和完整目标三元组
/// </summary>
public class BuildTarget
{
    /// <summary>
    ///     目标标识，支持短别名（如 "nyar"）或完整三元组（如 "clr-unity-windows-il2cpp"）
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    ///     是否生成 Source Map（.wasm.map）
    /// </summary>
    public bool SourceMap { get; set; }

    /// <summary>
    ///     是否生成 TypeScript 声明文件（.d.ts）
    /// </summary>
    public bool TypeScript { get; set; }

    /// <summary>
    ///     是否生成 WAT（WebAssembly Text Format）文本输出
    /// </summary>
    public bool Wat { get; set; }

    /// <summary>
    ///     是否生成 MSIL 文本输出
    /// </summary>
    public bool Msil { get; set; }

    /// <summary>
    ///     解析目标为 CompilationTarget
    /// </summary>
    /// <param name="target">目标字符串</param>
    /// <returns>解析后的编译目标，解析失败返回 null</returns>
    public static Nyar.Types.CompilationTarget? Resolve(string target)
    {
        if (Nyar.Types.TargetTriple.TryParse(target, out var triple))
        {
            return triple.ToCompilationTarget();
        }

        return null;
    }
}
