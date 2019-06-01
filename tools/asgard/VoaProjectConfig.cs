namespace Asgard.CLI;

/// <summary>
///     VOA 项目配置模型，对应 voa.config.v 中的 VON 配置对象
/// </summary>
public sealed class VoaProjectConfig
{
    public string ProjectType { get; set; } = "frontend";
    public string Target { get; set; } = "wasm";
    public VoaServerConfig? Server { get; set; }
    public VoaBuildConfig Build { get; set; } = new();
    public VoaHotReloadConfig HotReload { get; set; } = new();
    public List<VoaRouteModeConfig>? Routes { get; set; }
    public string? DefaultRenderMode { get; set; }

    public bool IsFrontend => ProjectType == "frontend";
    public bool IsBackend => ProjectType == "backend";
    public bool IsLibrary => ProjectType == "library";

    /// <summary>
    ///     获取指定路径的渲染模式，若无显式配置则返回默认值
    /// </summary>
    public string GetRenderMode(string path)
    {
        if (Routes == null || Routes.Count == 0)
        {
            return DefaultRenderMode ?? "csr";
        }

        foreach (var route in Routes)
        {
            if (MatchRoutePath(route.Path, path))
            {
                return route.Mode ?? DefaultRenderMode ?? "csr";
            }
        }

        return DefaultRenderMode ?? "csr";
    }

    private static bool MatchRoutePath(string pattern, string actual)
    {
        if (pattern == actual) return true;
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return actual.StartsWith(prefix);
        }

        return false;
    }
}