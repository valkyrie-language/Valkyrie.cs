using System.Text.Json;

namespace Asgard.CLI.Effect;

/// <summary>
///     Effect 副作用结果
/// </summary>
public sealed class EffectResult
{
    /// <summary>当前状态</summary>
    public EffectStatus Status { get; set; } = EffectStatus.Pending;

    /// <summary>解决结果数据</summary>
    public JsonElement? Data { get; set; }

    /// <summary>拒绝原因</summary>
    public string? Error { get; set; }

    /// <summary>Effect 唯一标识</summary>
    public string EntryId { get; set; } = string.Empty;
}
