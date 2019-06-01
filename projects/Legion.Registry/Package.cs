namespace Legion.Registry;

/// <summary>
/// 包信息，描述从注册表获取的包元数据
/// </summary>
public class Package
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 包描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 项目主页
    /// </summary>
    public string Homepage { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 许可证
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// 依赖项列表，格式 "name@version"
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 依赖版本字典，键为包名，值为版本约束
    /// </summary>
    public Dictionary<string, string> DependencyVersions { get; set; } = new();

    /// <summary>
    /// 目标条件依赖，按目标平台过滤的依赖
    /// 键为条件标识（如 <c>"target.wasm"</c>、<c>"target.wasip1"</c>、<c>"target.web"</c>）
    /// 值中列出该目标下有效的额外依赖
    /// </summary>
    public Dictionary<string, List<string>> TargetConditions { get; set; } = new();

    /// <summary>
    /// 分发 Tarball/Jar 等二进制文件的下载地址
    /// </summary>
    public string? DistTarball { get; set; }

    /// <summary>
    /// 分发文件完整性校验值（SRI 格式）
    /// </summary>
    public string? DistIntegrity { get; set; }

    /// <summary>
    /// 是否为 SDK 包（如 std.adaptor.wasm、std.adaptor.dotnet）
    /// </summary>
    public bool IsSdkPackage { get; set; }

    /// <summary>
    /// SDK 包对应的标准库模块名
    /// </summary>
    public string? SdkModuleName { get; set; }

    /// <summary>
    /// 同伴依赖（peerDependencies），键为包名，值为版本约束
    /// </summary>
    public Dictionary<string, string>? PeerDependencies { get; set; }
}