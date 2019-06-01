namespace Legion.Config;

/// <summary>
/// 平台目标配置
/// </summary>
public class TargetConfig
{
    /// <summary>
    /// 目标架构：wasm / jvm / clr / native
    /// </summary>
    public string Arch { get; set; } = "wasm";

    /// <summary>
    /// 目标操作系统：web / windows / linux / macos
    /// </summary>
    public string OS { get; set; } = "web";

    /// <summary>
    /// 目标 ABI：wasip1 / wasip2 / gnu / msvc（可选）
    /// </summary>
    public string? ABI { get; set; }
}