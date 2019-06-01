namespace Legion.Config;

/// <summary>
/// 编译选项配置
/// </summary>
public class BuildConfig
{
    /// <summary>
    /// 优化等级：debug / release
    /// </summary>
    public string Optimize { get; set; } = "debug";

    /// <summary>
    /// 是否生成调试符号
    /// </summary>
    public bool DebugSymbols { get; set; } = true;

    /// <summary>
    /// 输出目录
    /// </summary>
    public string OutputDir { get; set; } = "dist";

    /// <summary>
    /// 是否生成 Source Map
    /// </summary>
    public bool Sourcemap { get; set; } = true;
}