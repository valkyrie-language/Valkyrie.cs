namespace Valhalla.Config;

/// <summary>
/// CORS 配置
/// </summary>
public class CorsConfig
{
    /// <summary>允许的来源列表</summary>
    public List<string> Origins { get; set; } = new();
}