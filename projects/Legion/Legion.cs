using System.Reflection;
using Legion.Auth;
using Legion.Config;
using Legion.Dependency;
using Legion.Package;
using Legion.Registry;
using Legion.Registry.Conda;
using Legion.Registry.Jsr;
using Legion.Registry.Maven;
using Legion.Registry.Npm;
using Legion.Registry.Nuget;
using Legion.Scripts;
using Legion.Security;
using Legion.Tools;
using Legion.Version;
using Legion.Workspace;

namespace Legion;

public class Legion
{
    #region 字段

    private readonly Dictionary<string, IRegistry> _registries = new();
    private readonly string _vendorsDirectory;
    private readonly string _baseDirectory;
    private readonly PackageCache _cache;
    private readonly LockFile _lockFile;
    private readonly LegionConfig _config;
    private readonly RegistrySourceManager _sourceManager;
    private readonly SecurityAudit _securityAudit;
    private readonly PackagePublisher _publisher;
    private readonly ScriptRunner _scriptRunner;
    private readonly VendorAuthStore _authStore;
    private readonly VendorManager _vendorManager;

    private LegionManifest? _manifest;
    private LegionsWorkspace? _workspace;
    private VoaConfig? _voaConfig;
    private LegionIgnore? _ignore;
    private LegionConfigDirectory? _configDir;

    #endregion

    #region 属性

    public LegionConfig Config => _config;
    public PackageCache Cache => _cache;
    public LockFile LockFile => _lockFile;
    public RegistrySourceManager SourceManager => _sourceManager;
    public SecurityAudit SecurityAudit => _securityAudit;
    public PackagePublisher Publisher => _publisher;
    public ScriptRunner ScriptRunner => _scriptRunner;
    public VendorManager VendorManager => _vendorManager;
    public string VendorsDirectory => _vendorsDirectory;
    public string BaseDirectory => _baseDirectory;

    public LegionManifest? Manifest => _manifest;
    public LegionsWorkspace? Workspace => _workspace;
    public VoaConfig? VoaConfig => _voaConfig;
    public LegionIgnore? Ignore => _ignore;
    public LegionConfigDirectory? ConfigDir => _configDir;

    public bool IsWorkspace => _workspace is not null;
    public bool HasManifest => _manifest is not null;
    public bool IsStandalone => !IsWorkspace && !HasManifest;

    /// <summary>
    /// 是否为 CI 环境（禁用交互式提示）
    /// </summary>
    public bool IsCiMode { get; set; }

    /// <summary>
    /// 是否冻结锁文件（锁文件不匹配时直接报错，不更新）
    /// </summary>
    public bool IsFrozenLockfile { get; set; }

    /// <summary>
    /// 是否为 CI 环境（别名，兼容现有约定）
    /// </summary>
    public bool NoInteractive => IsCiMode;

    #endregion

    #region 构造函数

    public Legion()
    {
        _baseDirectory = GetBaseDirectory();
        _vendorsDirectory = ResolveVendorsDirectory();

        if (!Directory.Exists(_vendorsDirectory))
        {
            Directory.CreateDirectory(_vendorsDirectory);
        }

        _config = new LegionConfig();
        _config.Load();

        _cache = new PackageCache(_baseDirectory);
        _lockFile = new LockFile(_baseDirectory);
        _sourceManager = new RegistrySourceManager(_baseDirectory);
        _authStore = new VendorAuthStore(_baseDirectory);
        _authStore.Load();

        _securityAudit = new SecurityAudit();
        _scriptRunner = new ScriptRunner(_baseDirectory);

        RegisterDefaultRegistries();

        _vendorManager = new VendorManager(_sourceManager, _authStore, _registries);
        _publisher = new PackagePublisher(_registries);

        LoadProjectFiles();

        _voaConfig = new VoaConfig(_baseDirectory);
        if (_voaConfig.Exists())
        {
            _voaConfig.Load();
        }

        if (_lockFile.Exists())
        {
            _lockFile.Load();
        }
    }

    #endregion

    #region 项目文件加载

    private void LoadProjectFiles()
    {
        // 优先检测工作区
        string workspacePath = Path.Combine(_baseDirectory, "voa.workspace.v");
        if (File.Exists(workspacePath))
        {
            try
            {
                _workspace = new LegionsWorkspace(_baseDirectory);
                _workspace.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载工作区失败: {ex.Message}");
            }
        }

        // 检测包清单
        string manifestPath = Path.Combine(_baseDirectory, "legion.von");
        if (File.Exists(manifestPath))
        {
            try
            {
                _manifest = new LegionManifest(_baseDirectory);
                _manifest.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载清单失败: {ex.Message}");
            }
        }

        // 加载 ignore 文件
        _ignore = new LegionIgnore(_baseDirectory);

        // 加载配置目录
        _configDir = new LegionConfigDirectory(_baseDirectory);
    }

    public void ReloadManifest()
    {
        LoadProjectFiles();
    }

    #endregion

    #region 注册器管理

    public void RegisterRegistry(IRegistry registry)
    {
        _registries[registry.Name] = registry;
    }

    public void RegisterRegistryEndpoint(string registryName, string endpointUrl)
    {
        IRegistry registry = registryName switch
        {
            "npm" => new NpmRegistry { Endpoint = endpointUrl },
            "jsr" => new JsrRegistry { Endpoint = endpointUrl },
            "conda" => new CondaRegistry { Endpoint = endpointUrl },
            "maven" => new MavenRegistry { Endpoint = endpointUrl },
            "nuget" => new NuGetRegistry { Endpoint = endpointUrl },
            "valhalla" => CreateValhallaRegistry(endpointUrl),
            _ => throw new ArgumentException($"不支持的注册器类型: {registryName}")
        };

        RegisterRegistry(registry);
        Console.WriteLine($"已注册注册器 {registryName}，端点 {endpointUrl}");
    }

    public IRegistry? GetRegistry(string registryName)
    {
        return _registries.TryGetValue(registryName, out var registry) ? registry : null;
    }

    public Dictionary<string, IRegistry> GetAllRegistries()
    {
        return new Dictionary<string, IRegistry>(_registries);
    }

    public void RemoveRegistry(string registryName)
    {
        if (_registries.Remove(registryName))
        {
            Console.WriteLine($"已移除注册表：{registryName}");
        }
        else
        {
            Console.WriteLine($"注册表 '{registryName}' 不存在");
        }

        _sourceManager.RemoveSource(registryName);
    }

    public string GetRegistryEndpoint(string registryName)
    {
        return _registries.TryGetValue(registryName, out var registry)
            ? registry.Endpoint
            : string.Empty;
    }

    public async Task<List<PackageInfo>> SearchPackages(string query, string? registryName = null)
    {
        var results = new List<PackageInfo>();

        if (registryName is not null)
        {
            if (_registries.TryGetValue(registryName, out var registry))
            {
                return ToPackageInfoList(await registry.SearchPackagesAsync(query));
            }
            return results;
        }

        foreach (var registry in _registries.Values)
        {
            results.AddRange(ToPackageInfoList(await registry.SearchPackagesAsync(query)));
        }

        return results;
    }

    #endregion

    #region 包安装

    public async Task<PackageInfo> InstallAsync(string packageName, string version = "latest", string registryName = "npm")
    {
        if (version.StartsWith("workspace:", StringComparison.OrdinalIgnoreCase))
        {
            return await InstallWorkspacePackageAsync(packageName, version);
        }

        if (!_registries.TryGetValue(registryName, out var registry))
        {
            throw new ArgumentException($"注册器 {registryName} 未找到");
        }

        if (_config.OfflineMode)
        {
            if (_cache.HasPackage(packageName, version))
            {
                Console.WriteLine($"[离线模式] 从缓存安装 {packageName}@{version}");
                return new PackageInfo { Name = packageName, Version = version };
            }

            throw new InvalidOperationException($"离线模式下缓存中没有 {packageName}@{version}");
        }

        var package = await registry.GetPackageAsync(packageName, version);
        Console.WriteLine($"正在安装 {package.Name}@{package.Version}（来源: {registryName}）");

        string registryEndpoint = ResolveRegistryEndpoint(registryName);
        string orgName = ExtractOrgName(packageName);
        string packagePath = BuildPackagePath(registryName, registryEndpoint, orgName, package.Name, package.Version);

        Directory.CreateDirectory(packagePath);

        try
        {
            Console.WriteLine($"正在下载 {package.Name}@{package.Version}...");
            string extractedPath = await registry.DownloadPackageAsync(package, packagePath);
            Console.WriteLine($"下载完成：{extractedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败：{ex.Message}，仅记录元数据");
        }

        _cache.AddPackage(package.Name, package.Version, packagePath);
        _lockFile.AddPackage(package, registryName, registry.Endpoint);
        _lockFile.Save();

        // 更新 legion.von 中的依赖
        if (_manifest is not null)
        {
            _manifest.AddDependency(packageName, package.Version);
            _manifest.Save();
        }

        Console.WriteLine($"已安装到: {packagePath}");
        return ToPackageInfo(package);
    }

    /// <summary>
    /// 安装工作区内部依赖（创建软链接而非下载）
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="versionSpec">版本规范，如 workspace:* 或 workspace:^1.0.0</param>
    /// <returns>包元数据</returns>
    private async Task<PackageInfo> InstallWorkspacePackageAsync(string packageName, string versionSpec)
    {
        var workspaceMembers = GetWorkspaceMemberMap();

        if (!workspaceMembers.TryGetValue(packageName, out var memberPath))
        {
            throw new InvalidOperationException(
                $"工作区中未找到成员包 '{packageName}'。" +
                " 请在 voa.workspace.v 的 members 中添加此包。");
        }

        var memberManifest = new LegionManifest(memberPath);
        if (!memberManifest.Exists())
        {
            throw new InvalidOperationException($"成员包 '{packageName}' 目录中未找到 legion.von：{memberPath}");
        }

        memberManifest.Load();

        string linkPath = Path.Combine(_vendorsDirectory, packageName);
        string sourcePath = Path.GetFullPath(memberPath);

        if (Directory.Exists(linkPath))
        {
            Directory.Delete(linkPath, recursive: true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);

        try
        {
            Directory.CreateSymbolicLink(linkPath, sourcePath);
            Console.WriteLine($"已创建符号链接：{linkPath} → {sourcePath}");
        }
        catch (UnauthorizedAccessException)
        {
            CopyDirectory(sourcePath, linkPath);
            Console.WriteLine($"已复制（无符号链接权限）：{sourcePath} → {linkPath}");
        }

        var package = new PackageInfo
        {
            Name = memberManifest.Name,
            Version = memberManifest.Version,
            Dependencies = memberManifest.Dependencies.Keys.ToList(),
            DependencyVersions = memberManifest.Dependencies
        };

        _lockFile.Packages[$"{packageName}@{memberManifest.Version}"] = new LockEntry
        {
            Name = packageName,
            Version = memberManifest.Version,
            Registry = "workspace",
            Resolved = sourcePath,
            Integrity = string.Empty,
            IsWorkspace = true,
            InstallPath = packageName
        };
        _lockFile.Save();

        return package;
    }

    /// <summary>
    /// 递归复制目录
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destDir, fileName), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(destDir, dirName));
        }
    }

    public async Task<List<PackageInfo>> InstallAsync(string type = "dependencies")
    {
        if (IsWorkspace)
        {
            Console.WriteLine("[Workspace 模式] 安装所有工作区成员的依赖");
            return await InstallWorkspaceAsync(type);
        }

        if (HasManifest)
        {
            Console.WriteLine("[Package 模式] 安装当前包的依赖");
            return await InstallPackageAsync(type);
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法安装依赖");
    }

    private async Task<List<PackageInfo>> InstallWorkspaceAsync(string type)
    {
        var installed = new List<PackageInfo>();

        if (_workspace is null)
        {
            return installed;
        }

        // 安装工作区级依赖
        var workspaceDeps = type switch
        {
            "dev" or "devDependencies" => _workspace.DevDependencies,
            _ => _workspace.Dependencies
        };

        if (workspaceDeps.Count > 0)
        {
            var workspaceInstalled = await InstallDependenciesAsync(workspaceDeps);
            installed.AddRange(workspaceInstalled);
        }

        // 安装各成员包的依赖
        var manifests = _workspace.GetMemberManifests();
        foreach (var manifest in manifests)
        {
            Console.WriteLine($"  安装成员包: {manifest.Name}");
            var memberDeps = type switch
            {
                "dev" or "devDependencies" => manifest.DevDependencies,
                "peer" or "peerDependencies" => manifest.PeerDependencies,
                "optional" or "optionalDependencies" => manifest.OptionalDependencies,
                _ => manifest.Dependencies
            };

            if (memberDeps.Count > 0)
            {
                var memberInstalled = await InstallDependenciesAsync(memberDeps);
                installed.AddRange(memberInstalled);
            }
        }

        await RunHookIfPresent(LegionManifest.HookNames.PostInstall);

        return installed;
    }

    private async Task<List<PackageInfo>> InstallPackageAsync(string type)
    {
        if (_manifest is null)
        {
            throw new InvalidOperationException("当前目录没有 legion.von");
        }

        await RunHookIfPresent(LegionManifest.HookNames.PreInstall);

        var deps = type switch
        {
            "dev" or "devDependencies" => _manifest.DevDependencies,
            "peer" or "peerDependencies" => _manifest.PeerDependencies,
            "optional" or "optionalDependencies" => _manifest.OptionalDependencies,
            _ => _manifest.Dependencies
        };

        var result = await InstallDependenciesAsync(deps);

        await RunHookIfPresent(LegionManifest.HookNames.PostInstall);

        return result;
    }

    /// <summary>
    /// 如果指定名称的钩子存在则执行，否则静默跳过
    /// </summary>
    private async Task RunHookIfPresent(string hookName)
    {
        if (_manifest?.HasHook(hookName) == true)
        {
            var result = await RunHookAsync(hookName);
            if (!result.Success)
            {
                Console.WriteLine($"钩子 '{hookName}' 执行失败：{result.Error}");
            }
        }
    }

    public async Task<List<PackageInfo>> InstallDependenciesAsync(Dictionary<string, string> dependencies, string registryName = "npm")
    {
        var overrides = _manifest?.Overrides ?? new Dictionary<string, string>();
        var workspaceMembers = GetWorkspaceMemberMap();
        var resolver = new DependencyResolver(_registries, overrides, workspaceMembers);

        if (overrides.Count > 0)
        {
            Console.WriteLine($"应用 {overrides.Count} 个版本覆盖：{string.Join(", ", overrides.Select(kv => $"{kv.Key}→{kv.Value}"))}");
        }

        if (workspaceMembers.Count > 0)
        {
            Console.WriteLine($"工作区成员：{workspaceMembers.Count} 个包");
        }

        var rootNodes = await resolver.ResolveAllAsync(dependencies, registryName);

        var packagesToInstall = new List<Registry.Package>();
        var lockedPackages = new List<Registry.Package>();

        foreach (var node in rootNodes)
        {
            var flatList = resolver.GetFlatDependencyList(node);
            foreach (var package in flatList)
            {
                if (!_lockFile.IsPackageLocked(package.Name, package.Version))
                {
                    if (IsFrozenLockfile)
                    {
                        throw new InvalidOperationException(
                            $"锁文件已冻结：{package.Name}@{package.Version} 不在锁文件中。" +
                            " 请运行 'legion install' 更新锁文件后重试。");
                    }

                    packagesToInstall.Add(package);
                }
                else
                {
                    if (!IsCiMode)
                    {
                        Console.WriteLine($"已锁定: {package.Name}@{package.Version}，跳过安装");
                    }

                    lockedPackages.Add(package);
                }
            }
        }

        var installed = new List<PackageInfo>(lockedPackages.Select(ToPackageInfo));

        if (packagesToInstall.Count > 0)
        {
            Console.WriteLine($"并行安装 {packagesToInstall.Count} 个包...");
            var parallelResults = await InstallPackagesParallelAsync(
                packagesToInstall.Select(ToPackageInfo).ToList(), registryName);
            installed.AddRange(parallelResults);
        }

        var conflicts = resolver.DetectConflicts();
        if (conflicts.Count > 0)
        {
            Console.WriteLine("\n检测到版本冲突:");
            foreach (var conflict in conflicts)
            {
                Console.WriteLine($"  {conflict.PackageName}: 请求版本 [{string.Join(", ", conflict.RequestedVersions)}]");
            }
        }

        await CheckPeerDependenciesAsync(installed);

        return installed;
    }

    /// <summary>
    ///     并行安装多个包，使用信号量控制并发度
    /// </summary>
    /// <param name="packages">待安装的包列表</param>
    /// <param name="registryName">注册表名称</param>
    /// <param name="maxParallelism">最大并行度，默认 8</param>
    private async Task<List<PackageInfo>> InstallPackagesParallelAsync(
        List<PackageInfo> packages, string registryName, int maxParallelism = 8)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<PackageInfo>();
        var semaphore = new SemaphoreSlim(maxParallelism);

        var tasks = packages.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                var installedPackage = await InstallAsync(package.Name, package.Version, registryName);
                results.Add(installedPackage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安装失败: {package.Name}@{package.Version} - {ex.Message}");
                results.Add(package);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    ///     获取工作区成员映射：包名 → 目录路径
    /// </summary>
    private Dictionary<string, string> GetWorkspaceMemberMap()
    {
        var map = new Dictionary<string, string>();

        if (_workspace is null)
        {
            return map;
        }

        foreach (var memberPath in _workspace.Members)
        {
            var fullPath = Path.Combine(_baseDirectory, memberPath);
            if (!Directory.Exists(fullPath))
            {
                continue;
            }

            var memberManifest = new LegionManifest(fullPath);
            if (memberManifest.Exists())
            {
                memberManifest.Load();
                if (!string.IsNullOrEmpty(memberManifest.Name))
                {
                    map[memberManifest.Name] = fullPath;
                }
            }
        }

        return map;
    }

    /// <summary>
    ///     检查所有已安装包的同伴依赖是否满足
    /// </summary>
    private async Task CheckPeerDependenciesAsync(List<PackageInfo> installed)
    {
        var installedNames = new HashSet<string>(installed.Select(p => p.Name));
        var missingPeers = new List<string>();

        foreach (var package in installed)
        {
            if (package.PeerDependencies is null || package.PeerDependencies.Count == 0)
            {
                continue;
            }

            foreach (var (peerName, peerVersionSpec) in package.PeerDependencies)
            {
                if (!installedNames.Contains(peerName))
                {
                    missingPeers.Add($"  {package.Name}@{package.Version} 需要 {peerName}@{peerVersionSpec}");
                }
            }
        }

        if (missingPeers.Count > 0)
        {
            Console.WriteLine("\n⚠ peerDependencies 警告：");
            foreach (var warning in missingPeers)
            {
                Console.WriteLine(warning);
            }

            Console.WriteLine("请手动安装以上同伴依赖");
        }
    }

    #endregion

    #region 包卸载

    public async Task<PackageInfo> UninstallAsync(string packageName)
    {
        Console.WriteLine($"正在卸载 {packageName}");

        var lockedPackage = _lockFile.GetPackage(packageName);
        if (lockedPackage is not null)
        {
            string registryEndpoint = ResolveRegistryEndpoint(lockedPackage.Registry);
            string orgName = ExtractOrgName(packageName);
            string packagePath = BuildPackagePath(lockedPackage.Registry, registryEndpoint, orgName, lockedPackage.Name, lockedPackage.Version);

            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);
            }

            _lockFile.RemovePackage(packageName);
            _lockFile.Save();
        }

        await _cache.RemovePackageAsync(packageName, lockedPackage?.Version ?? "0.0.0");

        // 更新 legion.von 中的依赖
        if (_manifest is not null)
        {
            _manifest.RemoveDependency(packageName);
            _manifest.Save();
        }

        return new PackageInfo { Name = packageName, Version = "0.0.0" };
    }

    /// <summary>
    /// 添加依赖到 legion.von 并安装
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="version">版本约束</param>
    /// <param name="registryName">注册表名</param>
    /// <param name="isDev">是否为开发依赖</param>
    public async Task<PackageInfo> AddDependencyAsync(string packageName, string version = "latest", string registryName = "npm", bool isDev = false)
    {
        var installedPackage = await InstallAsync(packageName, version, registryName);

        if (_manifest is not null)
        {
            var depType = isDev ? "dev" : "dependencies";
            var versionConstraint = version == "latest" ? $"^{installedPackage.Version}" : version;
            _manifest.AddDependency(packageName, versionConstraint, depType);
            _manifest.Save();

            Console.WriteLine($"已添加 {packageName}@{versionConstraint} 到 legion.von [{depType}]");
        }

        return installedPackage;
    }

    /// <summary>
    /// 从 legion.von 移除依赖并卸载
    /// </summary>
    /// <param name="packageName">包名</param>
    public async Task<PackageInfo> RemoveDependencyAsync(string packageName)
    {
        var removed = await UninstallAsync(packageName);

        if (_manifest is not null)
        {
            _manifest.RemoveDependency(packageName);
            _manifest.Save();

            Console.WriteLine($"已从 legion.von 移除 {packageName}");
        }

        return removed;
    }

    #endregion

    #region 包更新

    public async Task<PackageInfo> UpdateAsync(string packageName, string version = "latest", string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            throw new ArgumentException($"注册器 {registryName} 未找到");
        }

        var package = await registry.GetPackageAsync(packageName, version);
        Console.WriteLine($"正在更新 {package.Name} 到 {package.Version}（来源: {registryName}）");

        var lockedPackage = _lockFile.GetPackage(packageName);
        if (lockedPackage is not null)
        {
            string oldRegistryEndpoint = ResolveRegistryEndpoint(lockedPackage.Registry);
            string oldOrgName = ExtractOrgName(packageName);
            string oldPackagePath = BuildPackagePath(lockedPackage.Registry, oldRegistryEndpoint, oldOrgName, lockedPackage.Name, lockedPackage.Version);

            if (Directory.Exists(oldPackagePath))
            {
                Directory.Delete(oldPackagePath, true);
            }
        }

        string registryEndpoint = ResolveRegistryEndpoint(registryName);
        string orgName = ExtractOrgName(packageName);
        string packagePath = BuildPackagePath(registryName, registryEndpoint, orgName, package.Name, package.Version);

        Directory.CreateDirectory(packagePath);

        _lockFile.RemovePackage(packageName);
        _lockFile.AddPackage(package, registryName, registry.Endpoint);
        _lockFile.Save();

        // 更新 legion.von 中的依赖
        if (_manifest is not null)
        {
            _manifest.AddDependency(packageName, package.Version);
            _manifest.Save();
        }

        Console.WriteLine($"已更新到: {packagePath}");
        return package;
    }

    public async Task<List<PackageInfo>> UpdateAsync()
    {
        if (IsWorkspace)
        {
            Console.WriteLine("[Workspace 模式] 更新所有工作区成员的依赖");
            return await UpdateWorkspaceAsync();
        }

        if (HasManifest)
        {
            Console.WriteLine("[Package 模式] 更新当前包的依赖");
            return await UpdatePackageAsync();
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法更新依赖");
    }

    private async Task<List<PackageInfo>> UpdateWorkspaceAsync()
    {
        var updated = new List<PackageInfo>();

        if (_workspace is null)
        {
            return updated;
        }

        // 更新工作区级依赖
        foreach (var dep in _workspace.Dependencies)
        {
            try
            {
                var package = await UpdateAsync(dep.Key, "latest");
                updated.Add(package);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新工作区依赖 {dep.Key} 失败: {ex.Message}");
            }
        }

        // 更新各成员包的依赖
        var manifests = _workspace.GetMemberManifests();
        foreach (var manifest in manifests)
        {
            Console.WriteLine($"  更新成员包: {manifest.Name}");
            foreach (var dep in manifest.Dependencies)
            {
                try
                {
                    var package = await UpdateAsync(dep.Key, "latest");
                    updated.Add(package);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  更新 {dep.Key} 失败: {ex.Message}");
                }
            }
        }

        return updated;
    }

    private async Task<List<PackageInfo>> UpdatePackageAsync()
    {
        var updated = new List<PackageInfo>();
        var lockedPackages = _lockFile.GetAllPackages();

        foreach (var locked in lockedPackages)
        {
            try
            {
                var package = await UpdateAsync(locked.Name, "latest", locked.Registry);
                updated.Add(package);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新 {locked.Name} 失败: {ex.Message}");
            }
        }

        return updated;
    }

    #endregion

    #region 包搜索

    public async Task<List<PackageInfo>> SearchAsync(string query, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            throw new ArgumentException($"注册器 {registryName} 未找到");
        }

        var packages = await registry.SearchPackagesAsync(query);
        Console.WriteLine($"在 {registryName} 中找到 {packages.Count} 个匹配 '{query}' 的包");
        return ToPackageInfoList(packages);
    }

    #endregion

    #region 依赖解析

    public async Task<DependencyNode> ResolveDependenciesAsync(string packageName, string version, string registryName = "npm")
    {
        var resolver = new DependencyResolver(_registries);
        return await resolver.ResolveAsync(packageName, version, registryName);
    }

    public string PrintDependencyTree(DependencyNode node)
    {
        var resolver = new DependencyResolver(_registries);
        return resolver.PrintDependencyTree(node);
    }

    #endregion

    #region 脚本执行

    public async Task<ScriptResult> RunAsync(string scriptName)
    {
        if (IsStandalone)
        {
            // Script 模式：尝试直接执行 vcc 命令
            Console.WriteLine("[Script 模式] 直接执行脚本");
            return await _scriptRunner.RunAsync(scriptName);
        }

        if (_workspace is not null && _workspace.HasScript(scriptName))
        {
            Console.WriteLine("[Workspace 模式] 执行工作区脚本");
            return await _scriptRunner.RunScriptAsync(_workspace, scriptName);
        }

        if (_manifest is not null && _manifest.HasScript(scriptName))
        {
            Console.WriteLine("[Package 模式] 执行包脚本");
            return await _scriptRunner.RunScriptAsync(_manifest, scriptName);
        }

        return new ScriptResult
        {
            Success = false,
            ExitCode = -1,
            Error = $"脚本 '{scriptName}' 未在 legion.von 或 voa.workspace.v 中定义"
        };
    }

    public async Task<ScriptResult> RunScriptAsync(string scriptName)
    {
        return await RunAsync(scriptName);
    }

    /// <summary>
    /// 执行生命周期钩子，支持条件判断和 Shell 选择
    /// </summary>
    /// <param name="hookName">钩子名称</param>
    public async Task<ScriptResult> RunHookAsync(string hookName)
    {
        var hook = _manifest?.GetHook(hookName);
        if (hook is null)
        {
            return new ScriptResult
            {
                Success = true,
                ExitCode = 0,
                Output = $"钩子 '{hookName}' 未定义，跳过"
            };
        }

        if (!EvaluateCondition(hook.Condition))
        {
            return new ScriptResult
            {
                Success = true,
                ExitCode = 0,
                Output = $"钩子 '{hookName}' 条件不满足（{hook.Condition}），跳过"
            };
        }

        Console.WriteLine($"> 执行钩子: {hookName}" +
                          (hook.Description is not null ? $" — {hook.Description}" : ""));

        var env = new Dictionary<string, string>
        {
            ["LEGION_HOOK_NAME"] = hookName,
            ["LEGION_HOOK_SHELL"] = hook.Shell ?? string.Empty
        };

        var runner = hook.Shell is not null
            ? new ScriptRunner(_baseDirectory, env)
            : _scriptRunner;

        var result = await runner.RunAsync(hook.Command);

        if (!result.Success && hook.FailOnError)
        {
            Console.WriteLine($"钩子 '{hookName}' 执行失败（FailOnError=true）：{result.Error}");
        }

        return result;
    }

    /// <summary>
    /// 评估平台条件表达式
    /// </summary>
    /// <param name="condition">条件表达式，如 "windows"、"!linux"、"macos"</param>
    private static bool EvaluateCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        var cond = condition.Trim();

        if (cond.StartsWith('!'))
        {
            var negated = cond[1..];
            return !EvaluatePlatformCondition(negated);
        }

        return EvaluatePlatformCondition(cond);
    }

    private static bool EvaluatePlatformCondition(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "windows" or "win" => OperatingSystem.IsWindows(),
            "linux" or "unix" => OperatingSystem.IsLinux(),
            "macos" or "mac" or "osx" => OperatingSystem.IsMacOS(),
            _ => true
        };
    }

    /// <summary>
    /// 获取当前包中所有标记为 [main] 的 micro 函数
    /// </summary>
    public Dictionary<string, (string FilePath, string Line)> GetMicroFunctions()
    {
        if (_manifest is null)
        {
            return new Dictionary<string, (string, string)>();
        }

        return _manifest.GetMicroFunctions();
    }

    public List<string> ListScripts()
    {
        var scripts = new List<string>();

        if (_workspace is not null)
        {
            scripts.AddRange(_scriptRunner.ListScripts(_workspace));
        }

        if (_manifest is not null)
        {
            foreach (var script in _scriptRunner.ListScripts(_manifest))
            {
                if (!scripts.Contains(script))
                {
                    scripts.Add(script);
                }
            }
        }

        return scripts;
    }

    #endregion

    #region 安全审计

    public async Task<SecurityAuditResult> AuditAsync()
    {
        if (IsWorkspace)
        {
            Console.WriteLine("[Workspace 模式] 审计所有工作区成员");
            return await AuditWorkspaceAsync();
        }

        if (HasManifest)
        {
            Console.WriteLine("[Package 模式] 审计当前包");
            return await AuditPackageAsync();
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法审计");
    }

    private async Task<SecurityAuditResult> AuditWorkspaceAsync()
    {
        var result = new SecurityAuditResult();

        if (_workspace is null)
        {
            return result;
        }

        // 审计工作区级依赖
        var workspacePackages = new List<PackageInfo>();
        foreach (var dep in _workspace.Dependencies)
        {
            workspacePackages.Add(new PackageInfo { Name = dep.Key, Version = dep.Value });
        }

        var workspaceResult = await _securityAudit.AuditDependenciesAsync(
            workspacePackages.Select(p => p.ToRegistryPackage()).ToList());
        result.Vulnerabilities.AddRange(workspaceResult.Vulnerabilities);
        result.Licenses.AddRange(workspaceResult.Licenses);

        // 审计各成员包
        var manifests = _workspace.GetMemberManifests();
        foreach (var manifest in manifests)
        {
            var memberPackages = new List<PackageInfo>();
            foreach (var dep in manifest.Dependencies)
            {
                memberPackages.Add(new PackageInfo { Name = dep.Key, Version = dep.Value });
            }

            var memberResult = await _securityAudit.AuditDependenciesAsync(
                memberPackages.Select(p => p.ToRegistryPackage()).ToList());
            result.Vulnerabilities.AddRange(memberResult.Vulnerabilities);
            result.Licenses.AddRange(memberResult.Licenses);
        }

        return result;
    }

    private async Task<SecurityAuditResult> AuditPackageAsync()
    {
        var lockedPackages = _lockFile.GetAllPackages();
        var packages = new List<PackageInfo>();

        foreach (var locked in lockedPackages)
        {
            packages.Add(new PackageInfo
            {
                Name = locked.Name,
                Version = locked.Version,
                Dependencies = locked.Dependencies
            });
        }

        return await _securityAudit.AuditDependenciesAsync(
            packages.Select(p => p.ToRegistryPackage()).ToList());
    }

    public async Task<SecurityAuditResult> AuditPackageAsync(PackageInfo package)
    {
        return await _securityAudit.AuditPackageAsync(package.ToRegistryPackage());
    }

    #endregion

    #region 包发布

    public async Task<PublishResult> PublishAsync(PublishOptions? options = null, string registryName = "npm")
    {
        if (IsWorkspace)
        {
            Console.WriteLine("[Workspace 模式] 发布所有工作区成员");
            return await PublishWorkspaceAsync(options, registryName);
        }

        if (HasManifest)
        {
            Console.WriteLine("[Package 模式] 发布当前包");
            return await PublishPackageAsync(options, registryName);
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法发布");
    }

    private async Task<PublishResult> PublishWorkspaceAsync(PublishOptions? options, string registryName = "npm")
    {
        if (_workspace is null || _manifest is null)
        {
            throw new InvalidOperationException("工作区未加载");
        }

        await RunHookIfPresent(LegionManifest.HookNames.PrePublish);

        var rootOptions = options ?? new PublishOptions
        {
            PackageName = _manifest.Name,
            Version = _manifest.Version,
            PackagePath = _baseDirectory
        };

        rootOptions.RegistryName = registryName;
        InjectAuthToken(rootOptions);
        var result = await _publisher.PublishAsync(rootOptions);

        await RunHookIfPresent(LegionManifest.HookNames.PostPublish);

        return result;
    }

    private async Task<PublishResult> PublishPackageAsync(PublishOptions? options, string registryName = "npm")
    {
        if (_manifest is null)
        {
            throw new InvalidOperationException("包清单未加载");
        }

        await RunHookIfPresent(LegionManifest.HookNames.PrePublish);

        var packageOptions = options ?? new PublishOptions
        {
            PackageName = _manifest.Name,
            Version = _manifest.Version,
            PackagePath = _baseDirectory
        };

        packageOptions.RegistryName = registryName;
        InjectAuthToken(packageOptions);
        var result = await _publisher.PublishAsync(packageOptions);

        await RunHookIfPresent(LegionManifest.HookNames.PostPublish);

        return result;
    }

    /// <summary>
    /// 自动注入已存储的认证令牌到发布选项
    /// </summary>
    private void InjectAuthToken(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AuthToken))
        {
            string? storedToken = _authStore.GetToken(options.RegistryName);

            if (storedToken is not null)
            {
                options.AuthToken = storedToken;
                Console.WriteLine($"已自动使用 {options.RegistryName} 的已存储认证令牌");
            }
        }
    }

    #endregion

    #region 缓存管理

    public async Task CleanCacheAsync()
    {
        await _cache.CleanAsync();
    }

    public async Task ClearCacheAsync()
    {
        await _cache.ClearAsync();
    }

    #endregion

    #region 清单操作

    /// <summary>
    /// 创建新的包清单文件 legion.von，同时生成 voa.config.v
    /// </summary>
    /// <param name="name">包名</param>
    /// <param name="version">初始版本</param>
    /// <param name="description">包描述</param>
    /// <param name="author">作者</param>
    /// <param name="license">许可证</param>
    /// <param name="projectType">项目类型：library / application / sdk</param>
    /// <param name="targetArch">目标架构</param>
    public LegionManifest CreateManifest(
        string name,
        string version = "0.0.0",
        string? description = null,
        string? author = null,
        string? license = null,
        string projectType = "application",
        string targetArch = "wasm")
    {
        var manifest = new LegionManifest(_baseDirectory)
        {
            Name = name,
            Version = version,
            Description = description ?? string.Empty,
            Author = author ?? string.Empty,
            License = license ?? "MIT"
        };

        manifest.Save();
        _manifest = manifest;

        _voaConfig = new VoaConfig(_baseDirectory)
        {
            ProjectType = projectType
        };

        _voaConfig.Target.Arch = targetArch;
        _voaConfig.Save();

        return manifest;
    }

    public LegionsWorkspace CreateWorkspace()
    {
        var workspace = new LegionsWorkspace(_baseDirectory);
        workspace.Save();
        _workspace = workspace;

        return workspace;
    }

    public LegionIgnore CreateIgnore()
    {
        var ignore = LegionIgnore.CreateDefault(_baseDirectory);
        _ignore = ignore;

        return ignore;
    }

    #endregion

    #region 过期检查

    /// <summary>
    /// 验证所有已安装包的完整性，返回校验结果
    /// </summary>
    /// <returns>校验失败的包列表（空列表表示全部通过）</returns>
    public List<IntegrityCheckResult> VerifyIntegrity()
    {
        if (!_lockFile.Exists())
        {
            throw new InvalidOperationException("未找到 legion-lock.von，请先运行 legion install");
        }

        return _lockFile.VerifyIntegrity(_vendorsDirectory);
    }

    /// <summary>
    /// 检测锁文件与 manifest 之间的版本漂移
    /// </summary>
    /// <returns>漂移的依赖名称列表</returns>
    public List<string> DetectDrift()
    {
        if (_manifest is null)
        {
            throw new InvalidOperationException("未加载 legion.von");
        }

        if (!_lockFile.Exists())
        {
            throw new InvalidOperationException("未找到 legion-lock.von");
        }

        return _lockFile.DetectDrift(_manifest);
    }

    /// <summary>
    /// 检查所有依赖是否过期，返回可更新的依赖列表
    /// </summary>
    public async Task<List<OutdatedDependency>> CheckOutdatedAsync()
    {
        var result = new List<OutdatedDependency>();

        var dependencies = new Dictionary<string, string>();
        if (_manifest is not null)
        {
            foreach (var dep in _manifest.Dependencies)
            {
                dependencies[dep.Key] = dep.Value;
            }

            foreach (var dep in _manifest.DevDependencies)
            {
                dependencies[dep.Key] = dep.Value;
            }
        }

        if (_workspace is not null)
        {
            foreach (var dep in _workspace.Dependencies)
            {
                dependencies[dep.Key] = dep.Value;
            }
        }

        var lockedPackages = _lockFile.GetAllPackages();
        var lockedMap = new Dictionary<string, string>();
        foreach (var locked in lockedPackages)
        {
            lockedMap[locked.Name] = locked.Version;
        }

        foreach (var dep in dependencies)
        {
            string currentVersion = lockedMap.TryGetValue(dep.Key, out var locked) ? locked : dep.Value;
            string? latestVersion = await GetLatestVersionAsync(dep.Key);

            if (latestVersion is null)
            {
                continue;
            }

            if (currentVersion == latestVersion)
            {
                continue;
            }

            var currentSemVer = SemanticVersion.Parse(currentVersion.TrimStart('^', '~', '>', '<', '='));
            var latestSemVer = SemanticVersion.Parse(latestVersion);

            if (currentSemVer < latestSemVer)
            {
                result.Add(new OutdatedDependency
                {
                    PackageName = dep.Key,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    Constraint = dep.Value
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定包的最新版本号
    /// </summary>
    private async Task<string?> GetLatestVersionAsync(string packageName)
    {
        foreach (var registry in _registries.Values)
        {
            try
            {
                var packages = await registry.SearchPackagesAsync(packageName);
                var match = packages.FirstOrDefault(p => p.Name == packageName);
                if (match is not null)
                {
                    return match.Version;
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    #endregion

    #region 私有方法

    private void RegisterDefaultRegistries()
    {
        RegisterRegistry(new NpmRegistry());
        RegisterRegistry(new JsrRegistry());
        RegisterRegistry(new CondaRegistry());
        RegisterRegistry(new MavenRegistry());
        RegisterRegistry(new NuGetRegistry());
        RegisterValhallaRegistry();

        foreach (var endpoint in _config.RegistryEndpoints)
        {
            RegisterRegistryEndpoint(endpoint.Key, endpoint.Value);
        }
    }

    private string GetBaseDirectory()
    {
        string? valkyrieHome = Environment.GetEnvironmentVariable("VALKYRIE_HOME");
        if (!string.IsNullOrEmpty(valkyrieHome))
        {
            return valkyrieHome;
        }

        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "legion.von")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "voa.workspace.v")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "valkyrie.von")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "project.von")))
        {
            return Environment.CurrentDirectory;
        }

        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userHome))
        {
            return Path.Combine(userHome, ".valkyrie");
        }

        return Environment.CurrentDirectory;
    }

    private string ResolveRegistryEndpoint(string registryName)
    {
        if (_registries.TryGetValue(registryName, out var registry))
        {
            return registry.Endpoint;
        }

        return registryName switch
        {
            "npm" => "registry.npmjs.org",
            "jsr" => "jsr.io",
            "conda" => "api.anaconda.org",
            "maven" => "search.maven.org",
            "nuget" => "api.nuget.org",
            "valhalla" => "valhalla.nyar.dev",
            _ => registryName
        };
    }

    private string ExtractOrgName(string packageName)
    {
        if (packageName.StartsWith("@"))
        {
            int slashIndex = packageName.IndexOf('/');
            if (slashIndex > 0)
            {
                return packageName.Substring(1, slashIndex - 1);
            }
        }

        return "default";
    }

    private string BuildPackagePath(string registryName, string endpoint, string orgName, string packageName, string version)
    {
        string safePackageName = packageName.Replace("@", "").Replace("/", "-");
        return Path.Combine(_vendorsDirectory, $"{registryName}@{endpoint}", $"{orgName}@{safePackageName}@{version}");
    }

    private string ResolveVendorsDirectory()
    {
        // 1. 检查当前目录是否有 vendors（项目级，优先级最高）
        string localVendors = Path.Combine(Environment.CurrentDirectory, "vendors");
        if (Directory.Exists(localVendors))
        {
            return localVendors;
        }

        // 2. 如果在工作区成员中，检查工作区根目录的 vendors
        string? workspaceRoot = FindWorkspaceRoot(Environment.CurrentDirectory);
        if (workspaceRoot is not null)
        {
            string workspaceVendors = Path.Combine(workspaceRoot, "vendors");
            if (Directory.Exists(workspaceVendors))
            {
                return workspaceVendors;
            }
        }

        // 3. 回退到全局 vendors
        return Path.Combine(_baseDirectory, "vendors");
    }

    private string? FindWorkspaceRoot(string startDirectory)
    {
        string currentDir = startDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            if (File.Exists(Path.Combine(currentDir, "voa.workspace.v")))
            {
                return currentDir;
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

    /// <summary>
    /// 通过反射创建 ValhallaRegistry 实例（避免循环依赖）
    /// </summary>
    private static IRegistry CreateValhallaRegistry(string endpointUrl)
    {
        var registry = LoadValhallaRegistryPlugin();
        if (registry is null)
        {
            throw new InvalidOperationException("无法加载 ValhallaRegistry 插件，请确保 Legion.Registry.Valhalla.dll 存在");
        }

        registry.Endpoint = endpointUrl;
        return registry;
    }

    /// <summary>
    /// 注册默认的 Valhalla 注册表
    /// </summary>
    private void RegisterValhallaRegistry()
    {
        var registry = LoadValhallaRegistryPlugin();
        if (registry is not null)
        {
            RegisterRegistry(registry);
        }
    }

    /// <summary>
    /// 从插件目录加载 ValhallaRegistry 类型
    /// </summary>
    private static IRegistry? LoadValhallaRegistryPlugin()
    {
        const string typeFullName = "Legion.ValhallaRegistry";
        const string assemblyName = "Legion.Registry.Valhalla";

        try
        {
            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType(typeFullName);
            if (type is not null && Activator.CreateInstance(type) is IRegistry registry)
            {
                return registry;
            }
        }
        catch (FileNotFoundException)
        {
            var assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"{assemblyName}.dll");

            if (File.Exists(assemblyPath))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var type = assembly.GetType(typeFullName);
                    if (type is not null && Activator.CreateInstance(type) is IRegistry registry)
                    {
                        return registry;
                    }
                }
                catch
                {
                    // 无法加载插件注册器
                }
            }
        }
        catch
        {
            // 无法加载插件注册器，Valhalla 功能不可用
        }

        return null;
    }

    /// <summary>
    /// 获取远程包信息
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="registryName">注册表名</param>
    public async Task<PackageInfo?> GetPackageInfoAsync(string packageName, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            Console.WriteLine($"未找到注册器：{registryName}");
            return null;
        }

        try
        {
            var packages = await registry.SearchPackagesAsync(packageName);
            var match = packages.FirstOrDefault(p => p.Name == packageName);

            if (match is null)
            {
                return null;
            }

            var info = new PackageInfo
            {
                Name = match.Name,
                LatestVersion = match.Version,
                License = match.License ?? "unknown",
                Description = match.Description ?? string.Empty,
                Author = match.Author ?? string.Empty
            };

            if (_manifest is not null)
            {
                info.IsInstalled = _lockFile.GetAllPackages()
                    .Any(p => p.Name == packageName);
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 类型转换

    private static PackageInfo ToPackageInfo(Registry.Package pkg)
    {
        return new PackageInfo
        {
            Name = pkg.Name,
            Version = pkg.Version,
            LatestVersion = pkg.Version,
            License = pkg.License ?? "unknown",
            Description = pkg.Description ?? string.Empty,
            Author = pkg.Author ?? string.Empty,
            Dependencies = pkg.Dependencies,
            DependencyVersions = pkg.DependencyVersions,
            PeerDependencies = pkg.PeerDependencies
        };
    }

    private static List<PackageInfo> ToPackageInfoList(List<Registry.Package> packages)
    {
        return packages.Select(ToPackageInfo).ToList();
    }

    #endregion
}