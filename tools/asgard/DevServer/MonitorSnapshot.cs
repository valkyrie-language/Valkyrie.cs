namespace Asgard.CLI.DevServer;

/// <summary>
///     监控快照数据点
/// </summary>
public readonly struct MonitorSnapshot
{
    /// <summary>快照时间</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>累计请求数</summary>
    public long TotalRequests { get; init; }

    /// <summary>累计错误数</summary>
    public long TotalErrors { get; init; }

    /// <summary>当前活跃连接数</summary>
    public long ActiveConnections { get; init; }

    /// <summary>当前内存占用（字节）</summary>
    public long MemoryBytes { get; init; }

    /// <summary>线程池线程数</summary>
    public int ThreadCount { get; init; }
}