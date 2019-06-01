namespace Asgard.CLI;

/// <summary>
///     路由渲染模式配置
/// </summary>
public sealed class VoaRouteModeConfig
{
    public string Path { get; set; } = "/";
    public string? Mode { get; set; }
    public int? Revalidate { get; set; }
}