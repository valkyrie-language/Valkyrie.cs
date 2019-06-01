namespace Legion.Registry;

/// <summary>
/// 版本递增类型
/// </summary>
public enum VersionBump
{
    /// <summary>递增补丁版本号（1.0.0 → 1.0.1）</summary>
    Patch,

    /// <summary>递增次版本号（1.0.0 → 1.1.0）</summary>
    Minor,

    /// <summary>递增主版本号（1.0.0 → 2.0.0）</summary>
    Major
}

/// <summary>
/// 发布选项
/// </summary>
public class PublishOptions
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

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
    public string? Homepage { get; set; }

    /// <summary>
    /// 许可证
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// 包本地路径
    /// </summary>
    public string PackagePath { get; set; } = string.Empty;

    /// <summary>
    /// 目标注册表名称
    /// </summary>
    public string RegistryName { get; set; } = "npm";

    /// <summary>
    /// 认证令牌
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// 发布标签
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 访问级别（public/restricted）
    /// </summary>
    public string? Access { get; set; }

    /// <summary>
    /// 版本递增类型（优先级高于手动指定版本）
    /// </summary>
    public VersionBump? Bump { get; set; }

    /// <summary>
    /// 是否自动创建 Git Tag
    /// </summary>
    public bool CreateGitTag { get; set; } = true;

    /// <summary>
    /// Git Tag 前缀（如 "v"，最终 tag 为 "v1.0.0"）
    /// </summary>
    public string? GitTagPrefix { get; set; } = "v";

    /// <summary>
    /// 跳过 Git 工作区清洁检查
    /// </summary>
    public bool SkipGitCheck { get; set; }

    /// <summary>
    /// 发布前执行 prePublish 脚本
    /// </summary>
    public bool RunPrePublishScript { get; set; } = true;
}