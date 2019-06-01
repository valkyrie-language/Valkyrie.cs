namespace Legion.Config;

/// <summary>
/// 资源与静态文件配置
/// </summary>
public class AssetsConfig
{
    /// <summary>
    /// 公共资源目录
    /// </summary>
    public string PublicDir { get; set; } = "public";

    /// <summary>
    /// 静态资源目录
    /// </summary>
    public string AssetsDir { get; set; } = "assets";
}