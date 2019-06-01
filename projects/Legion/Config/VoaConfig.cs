using Legion.Package;
using Legion.Tools;
using Oak.Data;
using Oak.Von;

namespace Legion.Config;

/// <summary>
/// 条件文件匹配模式
/// </summary>
public class ConditionalFilePattern
{
    /// <summary>
    /// 文件匹配模式（glob 格式）
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// 平台条件表达式，如 "windows"、"!linux"、"arch==wasm"
    /// 空则始终生效
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// 条件编译配置
/// </summary>
public class ConditionalConfig
{
    /// <summary>
    /// 全局宏定义常量（key = 宏名, value = 值或 null）
    /// </summary>
    public Dictionary<string, string?> DefineConstants { get; set; } = new();

    /// <summary>
    /// 条件宏定义（key = 宏名, value = 条件表达式如 "platform==windows"）
    /// </summary>
    public Dictionary<string, string> ConditionDefines { get; set; } = new();

    /// <summary>
    /// 条件排除的文件列表
    /// </summary>
    public List<ConditionalFilePattern> ExcludeFiles { get; set; } = new();

    /// <summary>
    /// 条件排除的目录列表
    /// </summary>
    public List<ConditionalFilePattern> ExcludeDirectories { get; set; } = new();
}

/// <summary>
/// VOA 项目构建配置，对应 voa.config.v 文件（类比 vite.config.ts）
/// 与 <see cref="LegionManifest"/>（legion.von，类比 package.json）独立共存
/// </summary>
public class VoaConfig
{
    /// <summary>
    /// 项目类型：library / application / sdk
    /// </summary>
    public string ProjectType { get; set; } = "application";

    /// <summary>
    /// 主构建目标（向后兼容单目标场景）
    /// </summary>
    public TargetConfig Target { get; set; } = new();

    /// <summary>
    /// 多目标编译列表，为空则仅使用 <see cref="Target"/>
    /// </summary>
    public List<TargetConfig> Targets { get; set; } = new();

    /// <summary>
    /// 编译选项
    /// </summary>
    public BuildConfig Build { get; set; } = new();

    /// <summary>
    /// 资源与静态文件配置
    /// </summary>
    public AssetsConfig Assets { get; set; } = new();

    /// <summary>
    /// 条件编译配置
    /// </summary>
    public ConditionalConfig Conditional { get; set; } = new();

    /// <summary>
    /// 条件编译宏定义（向后兼容旧格式）
    /// </summary>
    public Dictionary<string, string> Defines { get; set; } = new();

    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    private readonly string _filePath;
    private readonly GonParser _parser = new();

    /// <summary>
    /// 创建 VoaConfig 实例
    /// </summary>
    /// <param name="directoryPath">项目目录路径</param>
    public VoaConfig(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "voa.config.v");
    }

    /// <summary>
    /// 从目录加载 voa.config.v
    /// </summary>
    public static VoaConfig Load(string directoryPath)
    {
        var config = new VoaConfig(directoryPath);
        config.Load();
        return config;
    }

    /// <summary>
    /// 解析 voa.config.v 文件，文件不存在时使用默认配置
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var content = File.ReadAllText(_filePath);
        var root = _parser.Deserialize(content);

        if (root.Type != SerdeValueType.Object)
        {
            return;
        }

        ProjectType = root.GetField("project_type")?.GetString() ?? "application";

        LoadTargetConfig(root);
        LoadTargetsList(root);
        LoadBuildConfig(root);
        LoadAssetsConfig(root);
        LoadConditionalConfig(root);
        LoadDictionary(root, "defines", Defines);
        LoadDictionary(root, "environment", Environment);
    }

    /// <summary>
    /// 序列化并保存到 voa.config.v
    /// </summary>
    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var fields = new Dictionary<string, SerdeValue>
        {
            ["project_type"] = SerdeValue.String(ProjectType),
            ["target"] = SerdeValue.Object(new Dictionary<string, SerdeValue>
            {
                ["arch"] = SerdeValue.String(Target.Arch),
                ["os"] = SerdeValue.String(Target.OS)
            }),
            ["build"] = SerdeValue.Object(new Dictionary<string, SerdeValue>
            {
                ["optimize"] = SerdeValue.String(Build.Optimize),
                ["debug_symbols"] = SerdeValue.Boolean(Build.DebugSymbols),
                ["output_dir"] = SerdeValue.String(Build.OutputDir),
                ["sourcemap"] = SerdeValue.Boolean(Build.Sourcemap)
            }),
            ["assets"] = SerdeValue.Object(new Dictionary<string, SerdeValue>
            {
                ["public_dir"] = SerdeValue.String(Assets.PublicDir),
                ["assets_dir"] = SerdeValue.String(Assets.AssetsDir)
            })
        };

        if (Target.ABI is not null)
        {
            fields["target"].Fields!["abi"] = SerdeValue.String(Target.ABI);
        }

        SaveTargetsList(fields);
        SaveConditionalConfig(fields);

        if (Defines.Count > 0)
        {
            var defineFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Defines)
            {
                defineFields[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields["defines"] = SerdeValue.Object(defineFields);
        }

        if (Environment.Count > 0)
        {
            var envFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Environment)
            {
                envFields[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            fields["environment"] = SerdeValue.Object(envFields);
        }

        var root = SerdeValue.Object(fields);
        File.WriteAllText(_filePath, VonFormatter.Format(root));
    }

    /// <summary>
    /// 检查 voa.config.v 是否存在
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    /// <summary>
    /// 获取所有有效编译目标（Target + Targets 合并）
    /// </summary>
    public List<TargetConfig> GetAllTargets()
    {
        if (Targets.Count > 0)
        {
            return new List<TargetConfig>(Targets);
        }

        return new List<TargetConfig> { Target };
    }

    private void LoadTargetConfig(SerdeValue root)
    {
        var targetField = root.GetField("target");
        if (targetField?.Type != SerdeValueType.Object || targetField.Fields is null)
        {
            return;
        }

        Target.Arch = targetField.Fields.TryGetValue("arch", out var arch) ? (arch.GetString() ?? "wasm") : "wasm";
        Target.OS = targetField.Fields.TryGetValue("os", out var os) ? (os.GetString() ?? "web") : "web";
        Target.ABI = targetField.Fields.TryGetValue("abi", out var abi) ? abi.GetString() : null;
    }

    private void LoadTargetsList(SerdeValue root)
    {
        Targets.Clear();
        var targetsField = root.GetField("targets");
        if (targetsField?.Type != SerdeValueType.Array || targetsField.Elements is null)
        {
            return;
        }

        foreach (var element in targetsField.Elements)
        {
            if (element.Type != SerdeValueType.Object || element.Fields is null)
            {
                continue;
            }

            var target = new TargetConfig
            {
                Arch = element.Fields.TryGetValue("arch", out var arch) ? (arch.GetString() ?? "wasm") : "wasm",
                OS = element.Fields.TryGetValue("os", out var os) ? (os.GetString() ?? "web") : "web",
                ABI = element.Fields.TryGetValue("abi", out var abi) ? abi.GetString() : null
            };

            Targets.Add(target);
        }
    }

    private void SaveTargetsList(Dictionary<string, SerdeValue> fields)
    {
        if (Targets.Count > 0)
        {
            var targetElements = new List<SerdeValue>();
            foreach (var target in Targets)
            {
                var targetFields = new Dictionary<string, SerdeValue>
                {
                    ["arch"] = SerdeValue.String(target.Arch),
                    ["os"] = SerdeValue.String(target.OS)
                };

                if (target.ABI is not null)
                {
                    targetFields["abi"] = SerdeValue.String(target.ABI);
                }

                targetElements.Add(SerdeValue.Object(targetFields));
            }

            fields["targets"] = SerdeValue.Array(targetElements);
        }
    }

    private void LoadBuildConfig(SerdeValue root)
    {
        var buildField = root.GetField("build");
        if (buildField?.Type != SerdeValueType.Object || buildField.Fields is null)
        {
            return;
        }

        if (buildField.Fields.TryGetValue("optimize", out var optimize))
        {
            Build.Optimize = optimize.GetString() ?? "debug";
        }

        if (buildField.Fields.TryGetValue("debug_symbols", out var debug))
        {
            Build.DebugSymbols = debug.GetBoolean();
        }

        if (buildField.Fields.TryGetValue("output_dir", out var output))
        {
            Build.OutputDir = output.GetString() ?? "dist";
        }

        if (buildField.Fields.TryGetValue("sourcemap", out var sourcemap))
        {
            Build.Sourcemap = sourcemap.GetBoolean();
        }
    }

    private void LoadAssetsConfig(SerdeValue root)
    {
        var assetsField = root.GetField("assets");
        if (assetsField?.Type != SerdeValueType.Object || assetsField.Fields is null)
        {
            return;
        }

        if (assetsField.Fields.TryGetValue("public_dir", out var publicDir))
        {
            Assets.PublicDir = publicDir.GetString() ?? "public";
        }

        if (assetsField.Fields.TryGetValue("assets_dir", out var assetsDir))
        {
            Assets.AssetsDir = assetsDir.GetString() ?? "assets";
        }
    }

    private void LoadConditionalConfig(SerdeValue root)
    {
        var conditionalField = root.GetField("conditional");
        if (conditionalField?.Type != SerdeValueType.Object || conditionalField.Fields is null)
        {
            return;
        }

        Conditional = new ConditionalConfig();

        if (conditionalField.Fields.TryGetValue("defines", out var cd))
        {
            var dict = new Dictionary<string, string?>();
            if (cd.Type == SerdeValueType.Object && cd.Fields is not null)
            {
                foreach (var kvp in cd.Fields)
                {
                    dict[kvp.Key] = kvp.Value.GetString();
                }
            }

            Conditional.DefineConstants = dict;
        }

        if (conditionalField.Fields.TryGetValue("condition_defines", out var condDef))
        {
            var dict = new Dictionary<string, string>();
            if (condDef.Type == SerdeValueType.Object && condDef.Fields is not null)
            {
                foreach (var kvp in condDef.Fields)
                {
                    dict[kvp.Key] = kvp.Value.GetString() ?? string.Empty;
                }
            }

            Conditional.ConditionDefines = dict;
        }

        LoadConditionalFilePatterns(conditionalField, "exclude_files", Conditional.ExcludeFiles);
        LoadConditionalFilePatterns(conditionalField, "exclude_directories", Conditional.ExcludeDirectories);
    }

    private static void LoadConditionalFilePatterns(
        SerdeValue parent,
        string fieldName,
        List<ConditionalFilePattern> targetList)
    {
        targetList.Clear();
        var field = parent.Fields?.TryGetValue(fieldName, out var f) == true ? f : null;
        if (field?.Type != SerdeValueType.Array || field.Elements is null)
        {
            return;
        }

        foreach (var element in field.Elements)
        {
            var pattern = new ConditionalFilePattern();

            if (element.Type == SerdeValueType.String)
            {
                pattern.Pattern = element.GetString() ?? string.Empty;
            }
            else if (element.Type == SerdeValueType.Object && element.Fields is not null)
            {
                pattern.Pattern = element.Fields.TryGetValue("pattern", out var p)
                    ? (p.GetString() ?? string.Empty)
                    : string.Empty;

                if (element.Fields.TryGetValue("condition", out var cond))
                {
                    pattern.Condition = cond.GetString();
                }
            }

            if (!string.IsNullOrEmpty(pattern.Pattern))
            {
                targetList.Add(pattern);
            }
        }
    }

    private void SaveConditionalConfig(Dictionary<string, SerdeValue> fields)
    {
        if (Conditional.DefineConstants.Count == 0
            && Conditional.ConditionDefines.Count == 0
            && Conditional.ExcludeFiles.Count == 0
            && Conditional.ExcludeDirectories.Count == 0)
        {
            return;
        }

        var conditionalFields = new Dictionary<string, SerdeValue>();

        if (Conditional.DefineConstants.Count > 0)
        {
            var defineDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Conditional.DefineConstants)
            {
                defineDict[kvp.Key] = kvp.Value is not null
                    ? SerdeValue.String(kvp.Value)
                    : SerdeValue.String(string.Empty);
            }

            conditionalFields["defines"] = SerdeValue.Object(defineDict);
        }

        if (Conditional.ConditionDefines.Count > 0)
        {
            var condDefineDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in Conditional.ConditionDefines)
            {
                condDefineDict[kvp.Key] = SerdeValue.String(kvp.Value);
            }

            conditionalFields["condition_defines"] = SerdeValue.Object(condDefineDict);
        }

        SaveConditionalFilePatterns(conditionalFields, "exclude_files", Conditional.ExcludeFiles);
        SaveConditionalFilePatterns(conditionalFields, "exclude_directories", Conditional.ExcludeDirectories);

        fields["conditional"] = SerdeValue.Object(conditionalFields);
    }

    private static void SaveConditionalFilePatterns(
        Dictionary<string, SerdeValue> fields,
        string fieldName,
        List<ConditionalFilePattern> sourceList)
    {
        if (sourceList.Count == 0)
        {
            return;
        }

        var elements = new List<SerdeValue>();
        foreach (var item in sourceList)
        {
            if (item.Condition is null)
            {
                elements.Add(SerdeValue.String(item.Pattern));
            }
            else
            {
                var itemFields = new Dictionary<string, SerdeValue>
                {
                    ["pattern"] = SerdeValue.String(item.Pattern),
                    ["condition"] = SerdeValue.String(item.Condition)
                };

                elements.Add(SerdeValue.Object(itemFields));
            }
        }

        fields[fieldName] = SerdeValue.Array(elements);
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
}