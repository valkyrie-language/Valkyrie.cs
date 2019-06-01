namespace Asgard.CLI.Effect;

/// <summary>
///     Effect 状态枚举
/// </summary>
public enum EffectStatus
{
    /// <summary>等待中</summary>
    Pending,

    /// <summary>已解决</summary>
    Resolved,

    /// <summary>已拒绝</summary>
    Rejected
}
