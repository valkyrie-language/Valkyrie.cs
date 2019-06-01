using Legion.Tools;
using Oak.Data;
using Oak.Von;

namespace Legion.Sdk;

/// <summary>
/// 平台 SDK 包规范 — 定义 SDK 包的入口点、FFI 绑定声明和平台要求
/// </summary>
public class PlatformSdk
{
    /// <summary>
    /// SDK 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SDK 版本
    /// </summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>
    /// 目标平台：wasm / clr / jvm / native
    /// </summary>
    public string Platform { get; set; } = "wasm";

    /// <summary>
    /// SDK 入口点模块列表
    /// </summary>
    public List<SdkEntryPoint> EntryPoints { get; set; } = new();

    /// <summary>
    /// FFI 绑定声明列表
    /// </summary>
    public List<FfiBinding> FfiBindings { get; set; } = new();

    /// <summary>
    /// 平台要求
    /// </summary>
    public SdkPlatformRequirement PlatformRequirement { get; set; } = new();

    /// <summary>
    /// SDK 提供的类型声明
    /// </summary>
    public List<SdkTypeDeclaration> TypeDeclarations { get; set; } = new();

    private readonly string _filePath;
    private readonly GonParser _parser = new();

    /// <summary>
    /// 创建 PlatformSdk 实例
    /// </summary>
    /// <param name="directoryPath">项目目录路径</param>
    public PlatformSdk(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "sdk.von");
    }

    /// <summary>
    /// 从目录加载 sdk.von
    /// </summary>
    public static PlatformSdk Load(string directoryPath)
    {
        var sdk = new PlatformSdk(directoryPath);
        sdk.Load();
        return sdk;
    }

    /// <summary>
    /// 解析 sdk.von 文件
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

        Name = root.GetField("name")?.GetString() ?? string.Empty;
        Version = root.GetField("version")?.GetString() ?? "0.0.0";
        Platform = root.GetField("platform")?.GetString() ?? "wasm";

        LoadEntryPoints(root);
        LoadFfiBindings(root);
        LoadPlatformRequirement(root);
        LoadTypeDeclarations(root);
    }

    /// <summary>
    /// 序列化并保存到 sdk.von
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
            ["name"] = SerdeValue.String(Name),
            ["version"] = SerdeValue.String(Version),
            ["platform"] = SerdeValue.String(Platform)
        };

        SaveEntryPoints(fields);
        SaveFfiBindings(fields);
        SavePlatformRequirement(fields);
        SaveTypeDeclarations(fields);

        var root = SerdeValue.Object(fields);
        File.WriteAllText(_filePath, VonFormatter.Format(root));
    }

    /// <summary>
    /// 验证 SDK 规范完整性
    /// </summary>
    public SdkValidationResult Validate()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("SDK 名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            errors.Add("SDK 版本不能为空");
        }

        if (!IsValidPlatform(Platform))
        {
            errors.Add($"不支持的平台：{Platform}，应为 wasm / clr / jvm / native");
        }

        if (EntryPoints.Count == 0)
        {
            warnings.Add("未定义入口点，SDK 可能无法被正确加载");
        }

        foreach (var entry in EntryPoints)
        {
            if (string.IsNullOrWhiteSpace(entry.Module))
            {
                errors.Add($"入口点 '{entry.Name}' 缺少 module 字段");
            }
        }

        foreach (var binding in FfiBindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Name))
            {
                errors.Add("FFI 绑定缺少 name 字段");
            }

            if (string.IsNullOrWhiteSpace(binding.Signature))
            {
                errors.Add($"FFI 绑定 '{binding.Name}' 缺少 signature 字段");
            }
        }

        return new SdkValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// 检查 sdk.von 是否存在
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    #region 私有方法

    private static bool IsValidPlatform(string platform)
    {
        return platform is "wasm" or "clr" or "jvm" or "native";
    }

    private void LoadEntryPoints(SerdeValue root)
    {
        EntryPoints.Clear();
        var field = root.GetField("entry_points");
        if (field?.Type != SerdeValueType.Array || field.Elements is null)
        {
            return;
        }

        foreach (var element in field.Elements)
        {
            if (element.Type != SerdeValueType.Object || element.Fields is null)
            {
                continue;
            }

            var entry = new SdkEntryPoint
            {
                Name = element.Fields.TryGetValue("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty,
                Module = element.Fields.TryGetValue("module", out var m) ? (m.GetString() ?? string.Empty) : string.Empty,
                Description = element.Fields.TryGetValue("description", out var d) ? d.GetString() : null
            };

            EntryPoints.Add(entry);
        }
    }

    private void LoadFfiBindings(SerdeValue root)
    {
        FfiBindings.Clear();
        var field = root.GetField("ffi_bindings");
        if (field?.Type != SerdeValueType.Array || field.Elements is null)
        {
            return;
        }

        foreach (var element in field.Elements)
        {
            if (element.Type != SerdeValueType.Object || element.Fields is null)
            {
                continue;
            }

            var binding = new FfiBinding
            {
                Name = element.Fields.TryGetValue("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty,
                Signature = element.Fields.TryGetValue("signature", out var s) ? (s.GetString() ?? string.Empty) : string.Empty,
                Library = element.Fields.TryGetValue("library", out var l) ? l.GetString() : null,
                Convention = element.Fields.TryGetValue("convention", out var c) ? c.GetString() : "cdecl"
            };

            FfiBindings.Add(binding);
        }
    }

    private void LoadPlatformRequirement(SerdeValue root)
    {
        var field = root.GetField("platform_requirement");
        if (field?.Type != SerdeValueType.Object || field.Fields is null)
        {
            return;
        }

        PlatformRequirement = new SdkPlatformRequirement
        {
            MinVersion = field.Fields.TryGetValue("min_version", out var min) ? min.GetString() : null,
            MaxVersion = field.Fields.TryGetValue("max_version", out var max) ? max.GetString() : null,
            Os = field.Fields.TryGetValue("os", out var os) ? os.GetString() : null,
            Arch = field.Fields.TryGetValue("arch", out var arch) ? arch.GetString() : null
        };
    }

    private void LoadTypeDeclarations(SerdeValue root)
    {
        TypeDeclarations.Clear();
        var field = root.GetField("types");
        if (field?.Type != SerdeValueType.Array || field.Elements is null)
        {
            return;
        }

        foreach (var element in field.Elements)
        {
            if (element.Type != SerdeValueType.Object || element.Fields is null)
            {
                continue;
            }

            var typeDecl = new SdkTypeDeclaration
            {
                Name = element.Fields.TryGetValue("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty,
                Kind = element.Fields.TryGetValue("kind", out var k) ? (k.GetString() ?? "struct") : "struct",
                Description = element.Fields.TryGetValue("description", out var d) ? d.GetString() : null
            };

            TypeDeclarations.Add(typeDecl);
        }
    }

    private void SaveEntryPoints(Dictionary<string, SerdeValue> fields)
    {
        if (EntryPoints.Count == 0)
        {
            return;
        }

        var elements = new List<SerdeValue>();
        foreach (var entry in EntryPoints)
        {
            var entryFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.String(entry.Name),
                ["module"] = SerdeValue.String(entry.Module)
            };

            if (entry.Description is not null)
            {
                entryFields["description"] = SerdeValue.String(entry.Description);
            }

            elements.Add(SerdeValue.Object(entryFields));
        }

        fields["entry_points"] = SerdeValue.Array(elements);
    }

    private void SaveFfiBindings(Dictionary<string, SerdeValue> fields)
    {
        if (FfiBindings.Count == 0)
        {
            return;
        }

        var elements = new List<SerdeValue>();
        foreach (var binding in FfiBindings)
        {
            var bindingFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.String(binding.Name),
                ["signature"] = SerdeValue.String(binding.Signature),
                ["convention"] = SerdeValue.String(binding.Convention ?? "cdecl")
            };

            if (binding.Library is not null)
            {
                bindingFields["library"] = SerdeValue.String(binding.Library);
            }

            elements.Add(SerdeValue.Object(bindingFields));
        }

        fields["ffi_bindings"] = SerdeValue.Array(elements);
    }

    private void SavePlatformRequirement(Dictionary<string, SerdeValue> fields)
    {
        var reqFields = new Dictionary<string, SerdeValue>();

        if (PlatformRequirement.MinVersion is not null)
        {
            reqFields["min_version"] = SerdeValue.String(PlatformRequirement.MinVersion);
        }

        if (PlatformRequirement.MaxVersion is not null)
        {
            reqFields["max_version"] = SerdeValue.String(PlatformRequirement.MaxVersion);
        }

        if (PlatformRequirement.Os is not null)
        {
            reqFields["os"] = SerdeValue.String(PlatformRequirement.Os);
        }

        if (PlatformRequirement.Arch is not null)
        {
            reqFields["arch"] = SerdeValue.String(PlatformRequirement.Arch);
        }

        if (reqFields.Count > 0)
        {
            fields["platform_requirement"] = SerdeValue.Object(reqFields);
        }
    }

    private void SaveTypeDeclarations(Dictionary<string, SerdeValue> fields)
    {
        if (TypeDeclarations.Count == 0)
        {
            return;
        }

        var elements = new List<SerdeValue>();
        foreach (var typeDecl in TypeDeclarations)
        {
            var typeFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.String(typeDecl.Name),
                ["kind"] = SerdeValue.String(typeDecl.Kind ?? "struct")
            };

            if (typeDecl.Description is not null)
            {
                typeFields["description"] = SerdeValue.String(typeDecl.Description);
            }

            elements.Add(SerdeValue.Object(typeFields));
        }

        fields["types"] = SerdeValue.Array(elements);
    }

    #endregion
}

/// <summary>
/// SDK 入口点定义
/// </summary>
public class SdkEntryPoint
{
    /// <summary>
    /// 入口点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 入口模块路径
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 入口点描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// FFI 绑定声明
/// </summary>
public class FfiBinding
{
    /// <summary>
    /// 函数名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 函数签名（类型签名表达式）
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 外部库名
    /// </summary>
    public string? Library { get; set; }

    /// <summary>
    /// 调用约定：cdecl / stdcall / fastcall
    /// </summary>
    public string? Convention { get; set; } = "cdecl";
}

/// <summary>
/// SDK 平台要求
/// </summary>
public class SdkPlatformRequirement
{
    /// <summary>
    /// 最低平台版本
    /// </summary>
    public string? MinVersion { get; set; }

    /// <summary>
    /// 最高平台版本
    /// </summary>
    public string? MaxVersion { get; set; }

    /// <summary>
    /// 操作系统要求
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 架构要求
    /// </summary>
    public string? Arch { get; set; }
}

/// <summary>
/// SDK 类型声明
/// </summary>
public class SdkTypeDeclaration
{
    /// <summary>
    /// 类型名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型种类：struct / enum / interface / trait
    /// </summary>
    public string? Kind { get; set; } = "struct";

    /// <summary>
    /// 类型描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// SDK 规范验证结果
/// </summary>
public class SdkValidationResult
{
    /// <summary>
    /// 是否通过验证
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 验证警告列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}