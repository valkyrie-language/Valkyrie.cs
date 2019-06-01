using RegistryPackage = Legion.Registry.Package;

namespace Legion.Package;

/// <summary>
/// 包信息摘要
/// </summary>
public class PackageInfo
{
    /// <summary>
    /// 包名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 最新版本
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// 许可证
    /// </summary>
    public string License { get; set; } = "unknown";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 是否已安装
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// 依赖列表
    /// </summary>
    public List<string> Dependencies { get; set; } = [];

    /// <summary>
    /// 依赖版本映射
    /// </summary>
    public Dictionary<string, string> DependencyVersions { get; set; } = new();

    /// <summary>
    /// 同伴依赖
    /// </summary>
    public Dictionary<string, string>? PeerDependencies { get; set; }

    /// <summary>
    /// 从注册表包元数据隐式转换为包信息摘要
    /// </summary>
    public static implicit operator PackageInfo?(RegistryPackage? pkg)
    {
        if (pkg is null)
        {
            return null;
        }

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

    /// <summary>
    /// 从注册表包列表转换为包信息列表
    /// </summary>
    public static List<PackageInfo> FromRegistryPackages(List<RegistryPackage> packages)
    {
        return packages.Select(p => (PackageInfo)p).ToList();
    }

    /// <summary>
    /// 转换为注册表包元数据
    /// </summary>
    public RegistryPackage ToRegistryPackage()
    {
        return new RegistryPackage
        {
            Name = Name,
            Version = Version,
            License = License == "unknown" ? null : License,
            Description = Description,
            Author = Author,
            Dependencies = Dependencies,
            DependencyVersions = DependencyVersions,
            PeerDependencies = PeerDependencies
        };
    }
}
