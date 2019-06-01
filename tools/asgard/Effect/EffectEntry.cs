using System.Text.Json;

namespace Asgard.CLI.Effect;

/// <summary>
///     Effect 条目，记录一次副作用的完整生命周期
/// </summary>
public sealed record EffectEntry
{
    /// <summary>Effect 唯一标识</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>副作用函数名</summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>函数参数</summary>
    public string[] Args { get; set; } = [];

    /// <summary>当前状态</summary>
    public EffectStatus Status { get; set; } = EffectStatus.Pending;

    /// <summary>解决结果（仅 Resolved 状态有效）</summary>
    public JsonElement? Result { get; set; }

    /// <summary>拒绝原因（仅 Rejected 状态有效）</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>创建时间戳（毫秒）</summary>
    public long CreatedAt { get; set; }

    /// <summary>最后更新时间戳（毫秒）</summary>
    public long UpdatedAt { get; set; }
}
