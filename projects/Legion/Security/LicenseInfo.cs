namespace Legion.Security;

/// <summary>
/// 许可证信息
/// </summary>
public class LicenseInfo
{
    /// <summary>
    /// 包名
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 许可证 SPDX 标识
    /// </summary>
    public string License { get; set; } = "unknown";

    /// <summary>
    /// 是否兼容（MIT/Apache/BSD 等宽松许可）
    /// </summary>
    public bool IsCompatible { get; set; } = true;

    /// <summary>
    /// 是否为限制性许可（GPL/AGPL/SSPL 等）
    /// </summary>
    public bool IsRestricted { get; set; }
}