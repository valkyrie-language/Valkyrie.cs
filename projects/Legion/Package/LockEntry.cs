namespace Legion.Package;

public class LockEntry
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
    /// 来源注册表名称
    /// </summary>
    public string Registry { get; set; } = string.Empty;

    /// <summary>
    /// 解析后的下载地址
    /// </summary>
    public string Resolved { get; set; } = string.Empty;

    /// <summary>
    /// 完整性校验哈希（格式：sha512-Base64）
    /// </summary>
    public string Integrity { get; set; } = string.Empty;

    /// <summary>
    /// 包的直接依赖列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 包的许可协议
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// 是否为开发依赖
    /// </summary>
    public bool IsDev { get; set; }

    /// <summary>
    /// 是否为工作区内部依赖（workspace:* 协议）
    /// </summary>
    public bool IsWorkspace { get; set; }

    /// <summary>
    /// 下载后的本地安装路径（相对于项目根目录）
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;
}