using System.Diagnostics;

namespace Asgard.CLI.DevServer;

/// <summary>
///     DevServer 性能监视器
///     跟踪内存使用、请求吞吐量、连接数等关键健康指标
/// </summary>
public sealed class VoaDevServerMonitor : IDisposable
{
    private readonly object _lock = new();
    private readonly List<MonitorSnapshot> _snapshots = new();
    private readonly Timer _snapshotTimer;
    private readonly PerformanceCounter? _memoryCounter;
    private long _totalRequests;
    private long _totalErrors;
    private long _totalBytesSent;
    private long _activeConnections;
    private long _peakConnections;
    private DateTime _startTime;

    public VoaDevServerMonitor(TimeSpan snapshotInterval)
    {
        _startTime = DateTime.UtcNow;
        _snapshotTimer = new Timer(_ => TakeSnapshot(), null, snapshotInterval, snapshotInterval);

        try
        {
            _memoryCounter = new PerformanceCounter("Process", "Private Bytes", Process.GetCurrentProcess().ProcessName);
        }
        catch
        {
            _memoryCounter = null;
        }
    }

    /// <summary>
    ///     运行时长
    /// </summary>
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;

    /// <summary>
    ///     总请求数
    /// </summary>
    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    /// <summary>
    ///     总错误数
    /// </summary>
    public long TotalErrors => Interlocked.Read(ref _totalErrors);

    /// <summary>
    ///     总发送字节数
    /// </summary>
    public long TotalBytesSent => Interlocked.Read(ref _totalBytesSent);

    /// <summary>
    ///     当前活跃连接数
    /// </summary>
    public long ActiveConnections => Interlocked.Read(ref _activeConnections);

    /// <summary>
    ///     峰值连接数
    /// </summary>
    public long PeakConnections => Interlocked.Read(ref _peakConnections);

    /// <summary>
    ///     错误率（0-1）
    /// </summary>
    public double ErrorRate => TotalRequests > 0 ? (double)TotalErrors / TotalRequests : 0;

    /// <summary>
    ///     记录一个请求
    /// </summary>
    public void RecordRequest(long bytesSent)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Add(ref _totalBytesSent, bytesSent);
    }

    /// <summary>
    ///     记录一个错误
    /// </summary>
    public void RecordError()
    {
        Interlocked.Increment(ref _totalErrors);
    }

    /// <summary>
    ///     记录连接建立
    /// </summary>
    public void RecordConnectionOpen()
    {
        var current = Interlocked.Increment(ref _activeConnections);
        var peak = Interlocked.Read(ref _peakConnections);
        while (current > peak)
        {
            Interlocked.CompareExchange(ref _peakConnections, current, peak);
            peak = Interlocked.Read(ref _peakConnections);
        }
    }

    /// <summary>
    ///     记录连接关闭
    /// </summary>
    public void RecordConnectionClose()
    {
        Interlocked.Decrement(ref _activeConnections);
    }

    /// <summary>
    ///     获取最近的快照列表
    /// </summary>
    public IReadOnlyList<MonitorSnapshot> GetRecentSnapshots(int count = 60)
    {
        lock (_lock)
        {
            var start = Math.Max(0, _snapshots.Count - count);
            return _snapshots.GetRange(start, _snapshots.Count - start).AsReadOnly();
        }
    }

    /// <summary>
    ///     获取健康报告
    /// </summary>
    public HealthReport GetHealthReport()
    {
        var uptime = Uptime;

        return new HealthReport
        {
            Uptime = uptime,
            UptimeFormatted = FormatUptime(uptime),
            TotalRequests = TotalRequests,
            TotalErrors = TotalErrors,
            ErrorRate = ErrorRate,
            ActiveConnections = ActiveConnections,
            PeakConnections = PeakConnections,
            CurrentMemoryBytes = GetCurrentMemoryBytes(),
            AverageRequestsPerMinute = uptime.TotalMinutes > 0 ? TotalRequests / uptime.TotalMinutes : 0,
            IsHealthy = ErrorRate < 0.05 && ActiveConnections < 500,
        };
    }

    private void TakeSnapshot()
    {
        var snapshot = new MonitorSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalRequests = TotalRequests,
            TotalErrors = TotalErrors,
            ActiveConnections = ActiveConnections,
            MemoryBytes = GetCurrentMemoryBytes(),
            ThreadCount = ThreadPool.ThreadCount,
        };

        lock (_lock)
        {
            _snapshots.Add(snapshot);
            if (_snapshots.Count > 1440)
            {
                _snapshots.RemoveRange(0, _snapshots.Count - 1440);
            }
        }
    }

    private static long GetCurrentMemoryBytes()
    {
        try
        {
            return GC.GetTotalMemory(false);
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }

        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }

    public void Dispose()
    {
        _snapshotTimer.Dispose();
        _memoryCounter?.Dispose();
    }
}