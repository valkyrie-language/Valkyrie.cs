using Legion.Config;
using Legion.Registry;
using Legion.Version;
using IRegistry = Legion.Registry.IRegistry;

namespace Legion.Dependency;

public class DependencyResolver
{
    private const int MaxRecursionDepth = 100;
    private const string WorkspacePrefix = "workspace:";

    private readonly Dictionary<string, IRegistry> _registries;
    private readonly Dictionary<string, List<VersionRequest>> _versionRequests = new();
    private readonly Dictionary<string, Registry.Package> _resolvedPackages = new();
    private readonly Dictionary<string, string> _overrides;
    private readonly TargetCondition _currentTarget;
    private readonly List<string> _autoResolvedSdks = new();
    private readonly Dictionary<string, string> _workspaceMembers;
    private int _recursionDepth;

    /// <summary>
    ///     创建依赖解析器
    /// </summary>
    /// <param name="registries">注册表映射</param>
    /// <param name="overrides">版本覆盖（来自 legion.von overrides）</param>
    /// <param name="workspaceMembers">工作区成员映射：包名 → 目录路径（来自 voa.workspace.v）</param>
    public DependencyResolver(Dictionary<string, IRegistry> registries, Dictionary<string, string>? overrides = null, Dictionary<string, string>? workspaceMembers = null)
    {
        _registries = registries;
        _overrides = overrides ?? new Dictionary<string, string>();
        _workspaceMembers = workspaceMembers ?? new Dictionary<string, string>();
        _currentTarget = new TargetCondition();
    }

    public void SetTarget(string arch, string? os = null, string? abi = null, string? channel = null)
    {
        _currentTarget.Arch = arch;
        _currentTarget.OS = os;
        _currentTarget.Abi = abi;
        _currentTarget.Channel = channel;
    }

    public async Task<DependencyNode> ResolveAsync(string packageName, string version, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            throw new ArgumentException($"注册器 {registryName} 未找到");
        }

        var visited = new HashSet<string>();
        return await ResolveRecursiveAsync(packageName, version, registryName, registry, visited, null);
    }

    public async Task<List<DependencyNode>> ResolveAllAsync(Dictionary<string, string> dependencies, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            throw new ArgumentException($"注册器 {registryName} 未找到");
        }

        var results = new List<DependencyNode>();
        var visited = new HashSet<string>();

        foreach (var dep in dependencies)
        {
            var node = await ResolveRecursiveAsync(dep.Key, dep.Value, registryName, registry, visited, null);
            results.Add(node);
        }

        return results;
    }

    private async Task<DependencyNode> ResolveRecursiveAsync(
        string packageName,
        string versionSpec,
        string registryName,
        IRegistry registry,
        HashSet<string> visited,
        string? targetCondition)
    {
        _recursionDepth++;
        if (_recursionDepth > MaxRecursionDepth)
        {
            throw new DependencyResolutionException(
                $"依赖解析超出最大递归深度 {MaxRecursionDepth}，可能存在循环依赖：{packageName}@{versionSpec}");
        }

        // 检查 overrides：用户强制指定的版本优先
        if (_overrides.TryGetValue(packageName, out var overrideVersion))
        {
            versionSpec = overrideVersion;
        }

        string resolvedVersion = await ResolveVersionSpecAsync(packageName, versionSpec, registry);

        string key = $"{packageName}@{resolvedVersion}";

        if (visited.Contains(key))
        {
            return new DependencyNode
            {
                PackageName = packageName,
                Version = resolvedVersion,
                RegistryName = registryName
            };
        }

        visited.Add(key);

        RecordVersionRequest(packageName, versionSpec, resolvedVersion);

        var package = await registry.GetPackageAsync(packageName, resolvedVersion);
        _resolvedPackages[packageName] = package;

        var node = new DependencyNode
        {
            PackageName = package.Name,
            Version = package.Version,
            RegistryName = registryName,
            TargetCondition = targetCondition,
            IsSdk = package.IsSdkPackage,
            SdkModuleName = package.SdkModuleName
        };

        if (package.Dependencies is not null && package.Dependencies.Count > 0)
        {
            foreach (var dep in package.Dependencies)
            {
                var parts = dep.Split('@');
                string depName = parts.Length > 1 ? parts[0] : dep;
                string depVersion = parts.Length > 1 ? parts[1] : "latest";

                var depNode = await ResolveRecursiveAsync(depName, depVersion, registryName, registry, visited, targetCondition);
                node.Dependencies.Add(depNode);
            }
        }

        if (package.DependencyVersions is not null && package.DependencyVersions.Count > 0)
        {
            foreach (var dep in package.DependencyVersions)
            {
                var depNode = await ResolveRecursiveAsync(dep.Key, dep.Value, registryName, registry, visited, targetCondition);
                node.Dependencies.Add(depNode);
            }
        }

        if (package.TargetConditions.Count > 0)
        {
            foreach (var condition in package.TargetConditions)
            {
                if (!MatchesTarget(condition.Key))
                {
                    continue;
                }

                foreach (var dep in condition.Value)
                {
                    var parts = dep.Split('@');
                    string depName = parts.Length > 1 ? parts[0] : dep;
                    string depVersion = parts.Length > 1 ? parts[1] : "latest";

                    var depNode = await ResolveRecursiveAsync(depName, depVersion, registryName, registry, visited, condition.Key);
                    node.Dependencies.Add(depNode);
                }
            }
        }

        if (package.IsSdkPackage && !_autoResolvedSdks.Contains(package.Name))
        {
            await AutoResolveSdkDependenciesAsync(package, registryName, registry, visited);
        }

        return node;
    }

    /// <summary>
    ///     检查目标条件是否匹配当前编译目标
    /// </summary>
    private bool MatchesTarget(string conditionKey)
    {
        var parts = conditionKey.Split('.');

        if (parts.Length < 2 || parts[0] != "target")
        {
            return false;
        }

        var targetValue = parts[1];

        return targetValue switch
        {
            "web" => _currentTarget.Arch == "wasm",
            "wasip1" => _currentTarget.Abi == "wasip1",
            "wasip2" => _currentTarget.Abi == "wasip2",
            "jvm" => _currentTarget.Arch == "jvm",
            "clr" => _currentTarget.Arch == "clr",
            "native" => _currentTarget.Arch == "native",
            "linux" => _currentTarget.OS == "linux",
            "windows" => _currentTarget.OS == "windows",
            "macos" => _currentTarget.OS == "macos",
            _ => _currentTarget.Arch == targetValue || _currentTarget.OS == targetValue
        };
    }

    /// <summary>
    ///     自动解析 SDK 包的平台相关依赖
    /// </summary>
    private async Task AutoResolveSdkDependenciesAsync(
        Registry.Package sdkPackage,
        string registryName,
        IRegistry registry,
        HashSet<string> visited)
    {
        if (!_registries.TryGetValue(registryName, out var reg))
        {
            return;
        }

        _autoResolvedSdks.Add(sdkPackage.Name);

        var sdkMap = new Dictionary<string, (string Arch, string? Os, string? Abi, string? Channel)>
        {
            ["std.adaptor.wasm"] = ("wasm", null, null, null),
            ["std.adaptor.wasip1"] = ("wasm", null, "wasip1", null),
            ["std.adaptor.wasip2"] = ("wasm", null, "wasip2", null),
            ["std.adaptor.dotnet"] = ("clr", null, null, null),
            ["std.adaptor.jvm"] = ("jvm", null, null, null),
            ["std.adaptor.windows"] = ("native", "windows", null, null),
            ["std.adaptor.linux"] = ("native", "linux", null, null),
            ["std.adaptor.macos"] = ("native", "macos", null, null),
        };

        if (sdkMap.TryGetValue(sdkPackage.Name, out var sdkTarget))
        {
            var prevTarget = new TargetCondition
            {
                Arch = _currentTarget.Arch,
                OS = _currentTarget.OS,
                Abi = _currentTarget.Abi,
                Channel = _currentTarget.Channel
            };

            _currentTarget.Arch = sdkTarget.Arch;
            _currentTarget.OS = sdkTarget.Os;
            _currentTarget.Abi = sdkTarget.Abi;
            _currentTarget.Channel = sdkTarget.Channel;

            if (sdkPackage.Dependencies is not null)
            {
                foreach (var dep in sdkPackage.Dependencies)
                {
                    var parts = dep.Split('@');
                    string depName = parts.Length > 1 ? parts[0] : dep;
                    string depVersion = parts.Length > 1 ? parts[1] : "latest";
                    await ResolveRecursiveAsync(depName, depVersion, registryName, reg, visited, null);
                }
            }

            _currentTarget.Arch = prevTarget.Arch;
            _currentTarget.OS = prevTarget.OS;
            _currentTarget.Abi = prevTarget.Abi;
            _currentTarget.Channel = prevTarget.Channel;
        }
    }

    public List<DependencyConflict> DetectConflicts()
    {
        var conflicts = new List<DependencyConflict>();

        foreach (var kvp in _versionRequests)
        {
            if (kvp.Value.Count > 1)
            {
                var conflict = new DependencyConflict
                {
                    PackageName = kvp.Key,
                    RequestedVersions = kvp.Value.Select(v => v.RawSpec).Distinct().ToList()
                };

                if (_resolvedPackages.TryGetValue(kvp.Key, out var package))
                {
                    conflict.ResolvedVersion = package.Version;
                    conflict.ResolutionStrategy = ConflictResolutionStrategy.HighestCompatible;
                }

                if (_overrides.ContainsKey(kvp.Key))
                {
                    conflict.ResolutionStrategy = ConflictResolutionStrategy.Override;
                }
                else if (conflict.IsSevere)
                {
                    conflict.ResolutionStrategy = ConflictResolutionStrategy.Manual;
                }

                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    /// <summary>
    ///     自动解决所有可解决的冲突，返回仍需手动解决的冲突列表
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <returns>仍需手动解决的冲突</returns>
    public List<DependencyConflict> AutoResolveConflicts(List<DependencyConflict> conflicts)
    {
        var unresolved = new List<DependencyConflict>();

        foreach (var conflict in conflicts)
        {
            if (conflict.ResolutionStrategy == ConflictResolutionStrategy.Manual)
            {
                unresolved.Add(conflict);
                continue;
            }

            if (conflict.ResolutionStrategy == ConflictResolutionStrategy.HighestCompatible &&
                conflict.ResolvedVersion is not null)
            {
                Console.WriteLine($"  自动解决冲突：{conflict.PackageName} → {conflict.ResolvedVersion}");
            }
            else if (conflict.ResolutionStrategy == ConflictResolutionStrategy.Override)
            {
                var overrideVersion = _overrides[conflict.PackageName];
                conflict.ResolvedVersion = overrideVersion;
                Console.WriteLine($"  覆盖解决冲突：{conflict.PackageName} → {overrideVersion}（override）");
            }
        }

        return unresolved;
    }

    public string PrintDependencyTree(DependencyNode node, int indent = 0)
    {
        string indentStr = new string(' ', indent * 2);
        string output = $"{indentStr}{node.PackageName}@{node.Version}";

        foreach (var dep in node.Dependencies)
        {
            output += "\n" + PrintDependencyTree(dep, indent + 1);
        }

        return output;
    }

    /// <summary>
    ///     扁平化依赖树，对 SemVer 兼容范围内的版本自动去重
    /// </summary>
    public List<Registry.Package> GetFlatDependencyList(DependencyNode root)
    {
        var packages = new List<Registry.Package>();
        var dedupMap = new Dictionary<string, (Registry.Package Package, SemanticVersion Version)>();

        FlattenDependencies(root, dedupMap);

        return dedupMap.Values.Select(v => v.Package).ToList();
    }

    private void FlattenDependencies(DependencyNode node, Dictionary<string, (Registry.Package, SemanticVersion)> dedupMap)
    {
        string key = node.PackageName;

        SemanticVersion? nodeVersion = SemanticVersion.TryParse(node.Version, out var sv) ? sv : null;

        if (dedupMap.TryGetValue(key, out var existing))
        {
            // 同一包已存在：选更高版本（SemVer 兼容范围内去重）
            if (nodeVersion is not null && existing.Item2 is not null &&
                nodeVersion.CompareTo(existing.Item2) > 0)
            {
                if (_resolvedPackages.TryGetValue(node.PackageName, out var pkg))
                {
                    dedupMap[key] = (pkg, nodeVersion);
                }
            }

            return;
        }

        if (_resolvedPackages.TryGetValue(node.PackageName, out var package))
        {
            dedupMap[key] = (package, nodeVersion ?? new SemanticVersion(0, 0, 0));
        }

        foreach (var dep in node.Dependencies)
        {
            FlattenDependencies(dep, dedupMap);
        }
    }

    #region 版本范围解析

    /// <summary>
    ///     异步解析版本范围，对范围约束（^/~）查询注册表并选择最优版本
    /// </summary>
    private async Task<string> ResolveVersionSpecAsync(string packageName, string versionSpec, IRegistry registry)
    {
        // workspace:* / workspace:^ → 工作区内部依赖
        if (versionSpec.StartsWith(WorkspacePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (_workspaceMembers.TryGetValue(packageName, out var memberPath))
            {
                return versionSpec; // 保留 workspace: 前缀，后续由安装器处理
            }

            throw new DependencyResolutionException(
                $"workspace 协议但未找到成员包 '{packageName}'。请在 voa.workspace.v 的 members 中添加此包。");
        }

        // latest / * / 精确 SemVer / 精确 YearlyVersion → 直接返回
        if (versionSpec == "latest" || versionSpec == "*")
        {
            return versionSpec;
        }

        if (SemanticVersion.TryParse(versionSpec, out _) || YearlyVersion.TryParse(versionSpec, out _))
        {
            return versionSpec;
        }

        // 版本范围 → 查询注册表，选择最优版本
        try
        {
            var range = VersionRange.Parse(versionSpec);

            // 精确版本（无运算符，如 "1.2.3"）→ 直接使用
            if (range.MinVersion is not null && range.MaxVersion is not null &&
                range.MinInclusive && range.MaxInclusive &&
                range.MinVersion.Equals(range.MaxVersion))
            {
                return range.MinVersion.ToString();
            }

            // 范围约束 → 查询可用版本列表
            var availableVersions = await registry.GetPackageVersionsAsync(packageName);
            if (availableVersions.Count == 0)
            {
                return versionSpec;
            }

            var bestVersion = FindBestVersion(packageName, versionSpec, availableVersions);
            return bestVersion ?? versionSpec;
        }
        catch (FormatException)
        {
            return versionSpec;
        }
        catch (RegistryException)
        {
            return versionSpec;
        }
    }

    /// <summary>
    ///     判断版本范围是否可被某个版本满足（同步版本，用于已解析后的检查）
    /// </summary>
    public bool IsVersionSatisfied(string versionSpec, string availableVersion)
    {
        if (versionSpec == "latest" || versionSpec == "*")
        {
            return true;
        }

        if (string.Equals(versionSpec, availableVersion, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (SemanticVersion.TryParse(availableVersion, out var available) && available is not null)
        {
            try
            {
                var range = VersionRange.Parse(versionSpec);
                return range.Satisfies(available);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        if (YearlyVersion.TryParse(availableVersion, out var yearlyAvailable) && yearlyAvailable is not null)
        {
            try
            {
                var range = YearlyVersionRange.Parse(versionSpec);
                return range.Satisfies(yearlyAvailable);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return false;
    }

    public string? FindBestVersion(string packageName, string versionSpec, List<string> availableVersions)
    {
        if (versionSpec == "latest" || versionSpec == "*")
        {
            return availableVersions
                .Where(v => SemanticVersion.TryParse(v, out _))
                .Select(v => SemanticVersion.Parse(v))
                .OrderByDescending(v => v)
                .FirstOrDefault()?.ToString();
        }

        var matchingVersions = availableVersions
            .Where(v => IsVersionSatisfied(versionSpec, v))
            .ToList();

        if (matchingVersions.Count == 0)
        {
            return null;
        }

        return matchingVersions
            .Where(v => SemanticVersion.TryParse(v, out _))
            .Select(v => SemanticVersion.Parse(v))
            .OrderByDescending(v => v)
            .FirstOrDefault()?.ToString() ?? matchingVersions.First();
    }

    #endregion

    #region 版本请求记录

    private void RecordVersionRequest(string packageName, string rawSpec, string resolvedVersion)
    {
        if (!_versionRequests.ContainsKey(packageName))
        {
            _versionRequests[packageName] = new List<VersionRequest>();
        }

        bool alreadyRecorded = _versionRequests[packageName].Any(v => v.RawSpec == rawSpec);
        if (!alreadyRecorded)
        {
            _versionRequests[packageName].Add(new VersionRequest
            {
                RawSpec = rawSpec,
                ResolvedVersion = resolvedVersion
            });
        }
    }

    #endregion
}