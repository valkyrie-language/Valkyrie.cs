namespace Valhalla;

/// <summary>
/// 版本状态
/// </summary>
public enum VersionStatus
{
    /// <summary>活跃可用</summary>
    Active,
    /// <summary>已屏蔽（有漏洞）</summary>
    Shielded,
    /// <summary>已弃用</summary>
    Deprecated
}