namespace Asgard.CLI.DevServer;

/// <summary>
///     健康检查报告
/// </summary>
public readonly struct HealthReport
{
    public TimeSpan Uptime { get; init; }
    public string UptimeFormatted { get; init; }
    public long TotalRequests { get; init; }
    public long TotalErrors { get; init; }
    public double ErrorRate { get; init; }
    public long ActiveConnections { get; init; }
    public long PeakConnections { get; init; }
    public long CurrentMemoryBytes { get; init; }
    public double AverageRequestsPerMinute { get; init; }
    public bool IsHealthy { get; init; }
}