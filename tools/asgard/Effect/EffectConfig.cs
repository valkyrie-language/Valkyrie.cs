namespace Asgard.CLI.Effect;

/// <summary>
///     Effect 副作用配置
/// </summary>
public sealed class EffectConfig
{
    /// <summary>重试次数</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>重试间隔延迟（毫秒）</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>超时时间（毫秒）</summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>缓存 TTL（毫秒），0 表示不缓存</summary>
    public int CacheTtlMs { get; set; }

    /// <summary>是否去重</summary>
    public bool Dedupe { get; set; } = true;

    /// <summary>默认配置</summary>
    public static EffectConfig Default => new();
}
