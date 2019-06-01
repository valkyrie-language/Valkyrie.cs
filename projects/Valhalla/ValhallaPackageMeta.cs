namespace Valhalla;

/// <summary>
/// 包级元信息（不参与校验，可随时修改）
/// </summary>
public class ValhallaPackageMeta
{
    /// <summary>包描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>标签列表</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>分类列表</summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>项目主页</summary>
    public string? Homepage { get; set; }

    /// <summary>代码仓库 URL</summary>
    public string? Repository { get; set; }

    /// <summary>许可证</summary>
    public string? License { get; set; }

    /// <summary>图标 URL</summary>
    public string? Icon { get; set; }

    /// <summary>自定义链接</summary>
    public Dictionary<string, string> Links { get; set; } = new();

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后修改时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}