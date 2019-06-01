namespace Asgard.CLI.DevServer;

/// <summary>
///     HMR 延迟测量记录，用于跟踪热重载全链路性能
/// </summary>
internal readonly struct HmrTiming
{
    /// <summary>文件变更检测时间</summary>
    public DateTime FileChangedAt { get; init; }

    /// <summary>编译开始时间</summary>
    public DateTime? CompileStartAt { get; init; }

    /// <summary>编译完成时间</summary>
    public DateTime? CompileEndAt { get; init; }

    /// <summary>广播发送完成时间</summary>
    public DateTime? BroadcastAt { get; init; }

    /// <summary>变更的文件路径</summary>
    public string FilePath { get; init; }

    /// <summary>从文件变更到编译开始的延迟</summary>
    public double DetectMs => CompileStartAt.HasValue
        ? (CompileStartAt.Value - FileChangedAt).TotalMilliseconds
        : 0;

    /// <summary>编译耗时</summary>
    public double CompileMs => CompileEndAt.HasValue && CompileStartAt.HasValue
        ? (CompileEndAt.Value - CompileStartAt.Value).TotalMilliseconds
        : 0;

    /// <summary>从文件变更到广播完成的总延迟</summary>
    public double TotalMs => BroadcastAt.HasValue
        ? (BroadcastAt.Value - FileChangedAt).TotalMilliseconds
        : 0;
}