using Legion.Package;
using Legion.Tools;
using Oak.Data;
using Oak.Von;

namespace Legion.Workspace;

public class LegionsWorkspace
{
    #region 属性

    public string Version { get; set; } = "1";
    public string Name { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    /// <summary>
    ///     脚本映射
    /// </summary>
    public Dictionary<string, string> Scripts { get; set; } = new();

    /// <summary>
    ///     共享运行时依赖
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>
    ///     共享开发依赖
    /// </summary>
    public Dictionary<string, string> DevDependencies { get; set; } = new();

    /// <summary>
    ///     是否启用依赖提升（hoisting）：将共享依赖提升到工作区根目录
    /// </summary>
    public bool Hoisting { get; set; } = true;

    /// <summary>
    ///     提升策略：all（全部提升）/ shared（仅多包共享的依赖）/ none（不提升）
    /// </summary>
    public string HoistingStrategy { get; set; } = "shared";

    /// <summary>
    ///     共享依赖的最小引用次数（达到此次数才提升，默认 2）
    /// </summary>
    public int MinHoistReferences { get; set; } = 2;

    private readonly string _filePath;
    private readonly string _directoryPath;
    private readonly GonParser _parser = new();

    #endregion

    #region 构造与加载

    public LegionsWorkspace(string directoryPath)
    {
        _directoryPath = directoryPath;
        _filePath = System.IO.Path.Combine(directoryPath, "voa.workspace.v");
    }

    public static LegionsWorkspace Load(string directoryPath)
    {
        var workspace = new LegionsWorkspace(directoryPath);
        workspace.Load();
        return workspace;
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"voa.workspace.v 未找到: {_filePath}");
        }

        string content = File.ReadAllText(_filePath);
        var root = _parser.Deserialize(content);

        if (root.Type != SerdeValueType.Object)
        {
            throw new FormatException("voa.workspace.v 根元素必须是对象");
        }

        Version = root.GetField("version")?.GetString() ?? "1";
        Name = root.GetField("name")?.GetString() ?? string.Empty;

        var membersField = root.GetField("members");
        if (membersField?.Type == SerdeValueType.Array && membersField.Elements is not null)
        {
            Members = membersField.Elements
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        LoadDictionary(root, "scripts", Scripts);
        LoadDictionary(root, "dependencies", Dependencies);
        LoadDictionary(root, "devDependencies", DevDependencies);

        AutoDiscoverMembers();
    }

    public void Save()
    {
        string? dir = System.IO.Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var fields = new Dictionary<string, SerdeValue>
        {
            ["version"] = SerdeValue.String(Version)
        };

        if (!string.IsNullOrEmpty(Name))
        {
            fields["name"] = SerdeValue.String(Name);
        }

        if (Members.Count > 0)
        {
            fields["members"] = SerdeValue.Array(Members.Select(m => SerdeValue.String(m)).ToList());
        }

        SaveDictionary(fields, "scripts", Scripts);
        SaveDictionary(fields, "dependencies", Dependencies);
        SaveDictionary(fields, "devDependencies", DevDependencies);

        var root = SerdeValue.Object(fields);
        File.WriteAllText(_filePath, VonFormatter.Format(root));
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    #endregion

    #region 成员管理

    public List<LegionManifest> GetMemberManifests()
    {
        var manifests = new List<LegionManifest>();

        foreach (var memberPath in Members)
        {
            string fullPath = System.IO.Path.Combine(_directoryPath, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath);
                if (manifest.Exists())
                {
                    manifest.Load();
                    manifests.Add(manifest);
                }
            }
        }

        return manifests;
    }

    public LegionManifest? GetMemberManifest(string memberName)
    {
        foreach (var memberPath in Members)
        {
            string fullPath = System.IO.Path.Combine(_directoryPath, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath);
                if (manifest.Exists())
                {
                    manifest.Load();
                    if (manifest.Name == memberName)
                    {
                        return manifest;
                    }
                }
            }
        }

        return null;
    }

    public void AddMember(string path)
    {
        if (!Members.Contains(path))
        {
            Members.Add(path);
        }
    }

    public void RemoveMember(string path)
    {
        Members.Remove(path);
    }

    public string? FindMemberPath(string memberName)
    {
        foreach (var memberPath in Members)
        {
            string fullPath = System.IO.Path.Combine(_directoryPath, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath);
                if (manifest.Exists())
                {
                    manifest.Load();
                    if (manifest.Name == memberName)
                    {
                        return fullPath;
                    }
                }
            }
        }

        return null;
    }

    #endregion

    #region 工作区依赖图

    public WorkspaceDependencyGraph BuildDependencyGraph()
    {
        var graph = new WorkspaceDependencyGraph();
        var manifests = GetMemberManifests();
        var memberNames = manifests.Select(m => m.Name).ToHashSet();

        foreach (var manifest in manifests)
        {
            var internalDeps = new List<string>();
            var externalDeps = new List<string>();

            foreach (var dep in manifest.Dependencies)
            {
                if (memberNames.Contains(dep.Key))
                {
                    internalDeps.Add(dep.Key);
                }
                else
                {
                    externalDeps.Add($"{dep.Key}@{dep.Value}");
                }
            }

            graph.InternalDependencies[manifest.Name] = internalDeps;
            graph.ExternalDependencies[manifest.Name] = externalDeps;
        }

        return graph;
    }

    public List<string> GetBuildOrder()
    {
        var graph = BuildDependencyGraph();
        return graph.GetTopologicalOrder();
    }

    public Dictionary<string, string> CollectAllExternalDependencies()
    {
        var allDeps = new Dictionary<string, string>();
        var manifests = GetMemberManifests();
        var memberNames = manifests.Select(m => m.Name).ToHashSet();

        foreach (var dep in Dependencies)
        {
            if (!memberNames.Contains(dep.Key))
            {
                allDeps[dep.Key] = dep.Value;
            }
        }

        foreach (var manifest in manifests)
        {
            foreach (var dep in manifest.Dependencies)
            {
                if (!memberNames.Contains(dep.Key) && !allDeps.ContainsKey(dep.Key))
                {
                    allDeps[dep.Key] = dep.Value;
                }
            }
        }

        return allDeps;
    }

    public bool IsWorkspaceDependency(string packageName)
    {
        var manifests = GetMemberManifests();
        return manifests.Any(m => m.Name == packageName);
    }

    #endregion

    #region 脚本管理

    public string GetScript(string name)
    {
        return Scripts.TryGetValue(name, out var script) ? script : string.Empty;
    }

    public bool HasScript(string name)
    {
        return Scripts.ContainsKey(name);
    }

    #endregion

    #region 自动发现

    private void AutoDiscoverMembers()
    {
        var discovered = new List<string>();

        foreach (var memberPath in Members.ToList())
        {
            string fullPath = System.IO.Path.Combine(_directoryPath, memberPath);
            if (Directory.Exists(fullPath))
            {
                discovered.Add(memberPath);
            }
        }

        string packagesDir = System.IO.Path.Combine(_directoryPath, "packages");
        if (Directory.Exists(packagesDir))
        {
            foreach (var dir in Directory.GetDirectories(packagesDir))
            {
                string relativePath = System.IO.Path.GetRelativePath(_directoryPath, dir);
                string legionPath = System.IO.Path.Combine(dir, "legion.von");
                if (File.Exists(legionPath) && !discovered.Contains(relativePath))
                {
                    discovered.Add(relativePath);
                }
            }
        }

        string projectsDir = System.IO.Path.Combine(_directoryPath, "projects");
        if (Directory.Exists(projectsDir))
        {
            foreach (var dir in Directory.GetDirectories(projectsDir))
            {
                string relativePath = System.IO.Path.GetRelativePath(_directoryPath, dir);
                string legionPath = System.IO.Path.Combine(dir, "legion.von");
                if (File.Exists(legionPath) && !discovered.Contains(relativePath))
                {
                    discovered.Add(relativePath);
                }
            }
        }

        Members = discovered;
    }

    #endregion

    #region 私有方法

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

    #endregion
}