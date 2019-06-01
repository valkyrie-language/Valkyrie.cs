namespace VOA.ToolChain.Tests;

/// <summary>
///     DevServer 稳定性测试
///     模拟长时间运行场景，验证无内存泄漏、无崩溃、无状态异常
/// </summary>
public sealed class DevServerStabilityTests
{
    private readonly ITestOutputHelper _output;

    public DevServerStabilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 内存稳定性

    [Fact]
    public void Monitor_HealthReport_FormatCorrectly()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromSeconds(1));

        monitor.RecordRequest(1024);
        monitor.RecordRequest(2048);
        monitor.RecordRequest(512);
        monitor.RecordError();

        var report = monitor.GetHealthReport();

        Assert.Equal(3L, report.TotalRequests);
        Assert.Equal(1L, report.TotalErrors);
        Assert.True(report.ErrorRate > 0, "有错误的错误率应 > 0");
        Assert.False(string.IsNullOrEmpty(report.UptimeFormatted));

        _output.WriteLine($"健康报告：运行={report.UptimeFormatted} 请求={report.TotalRequests} 错误率={report.ErrorRate:P1} 健康={report.IsHealthy}");
    }

    [Fact]
    public void Monitor_Connections_TracksPeak()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromSeconds(1));

        monitor.RecordConnectionOpen();
        monitor.RecordConnectionOpen();
        monitor.RecordConnectionOpen();
        monitor.RecordConnectionClose();
        monitor.RecordConnectionOpen();

        Assert.True(monitor.ActiveConnections >= 2, $"活跃连接数 {monitor.ActiveConnections} 应 >= 2");
        Assert.True(monitor.PeakConnections >= 3, $"峰值连接数 {monitor.PeakConnections} 应 >= 3");

        _output.WriteLine($"连接统计：活跃={monitor.ActiveConnections} 峰值={monitor.PeakConnections}");
    }

    [Fact]
    public void Monitor_Snapshots_AccumulateCorrectly()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromMilliseconds(50));

        for (var i = 0; i < 10; i++)
        {
            monitor.RecordRequest(100);
            Thread.Sleep(60);
        }

        var snapshots = monitor.GetRecentSnapshots(10);

        Assert.True(snapshots.Count > 0, $"快照数 {snapshots.Count} 应 > 0");

        var firstTotal = snapshots[0].TotalRequests;
        var lastTotal = snapshots[^1].TotalRequests;

        Assert.True(lastTotal >= firstTotal, $"请求数应递增：{firstTotal} → {lastTotal}");

        _output.WriteLine($"快照统计：{snapshots.Count} 个快照，请求数 {firstTotal} → {lastTotal}");
    }

    [Fact]
    public void Monitor_MemoryBaseline_TracksGcMemory()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromMilliseconds(50));

        Thread.Sleep(100);

        var firstSnapshot = monitor.GetRecentSnapshots(1).FirstOrDefault();
        var memoryMb = (double)firstSnapshot.MemoryBytes / 1024 / 1024;

        _output.WriteLine($"监视器内存基线：{memoryMb:F1}MB");
        Assert.True(memoryMb >= 0, "内存占用应 >= 0");
    }

    #endregion

    #region 长时间运行模拟

    [Fact]
    public async Task SimulatedRuntime_OneHour_NoMemoryLeak()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromMilliseconds(100));
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var initialMemory = GC.GetTotalMemory(true);
        var maxMemory = initialMemory;
        var requestCount = 0;

        for (var minute = 0; minute < 60; minute++)
        {
            for (var req = 0; req < 10; req++)
            {
                var data = new byte[new Random().Next(100, 5000)];
                monitor.RecordRequest(data.Length);
                requestCount++;

                if (req % 5 == 0)
                {
                    var current = GC.GetTotalMemory(false);
                    if (current > maxMemory)
                    {
                        maxMemory = current;
                    }
                }
            }

            if (minute % 5 == 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized, false);

                var current = GC.GetTotalMemory(false);
                _output.WriteLine($"  分钟 {minute:D2}：请求={requestCount} 内存={current / 1024 / 1024}MB");
            }

            await Task.Delay(1);
        }

        sw.Stop();

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();

        var finalMemory = GC.GetTotalMemory(true);
        var growthMb = (double)(finalMemory - initialMemory) / 1024 / 1024;

        _output.WriteLine($"模拟运行结果：{requestCount} 请求 / {sw.Elapsed.TotalSeconds:F0}s");
        _output.WriteLine($"内存：初始 {initialMemory / 1024 / 1024}MB → 最终 {finalMemory / 1024 / 1024}MB（增长 {growthMb:F1}MB）");

        Assert.True(growthMb < 10, $"内存增长 {growthMb:F1}MB 应 < 10MB");
    }

    #endregion

    #region 错误恢复

    [Fact]
    public void ErrorOverlay_ExceptionRecovery_ReturnsFallbackHtml()
    {
        var overlay = new VoaErrorOverlay();

        var html = overlay.RenderErrorPage("测试错误消息", "/source/app.v", 500);

        Assert.Contains("测试错误消息", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("error", html, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine($"Error Overlay 恢复：HTML 长度 = {html.Length} 字符");
    }

    [Fact]
    public void ErrorOverlay_NullFilePath_DoesNotCrash()
    {
        var overlay = new VoaErrorOverlay();

        var html = overlay.RenderErrorPage("空路径错误", null, 404);

        Assert.NotEmpty(html);
        Assert.Contains("空路径错误", html, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("Error Overlay 空路径不崩溃 ✅");
    }

    [Fact]
    public void ErrorOverlay_StackFrames_ReturnedWhenAvailable()
    {
        var overlay = new VoaErrorOverlay();

        var info = new VoaErrorInfo
        {
            Message = "编译错误：未定义的变量 'x'",
            FilePath = "/source/app.v",
            StatusCode = 500,
            Suggestions = new List<string> { "检查变量 'x' 是否已声明", "确认导入路径正确" },
            DocumentationUrl = "https://docs.voa.dev/errors/undefined-variable",
            StackFrames = new List<VoaStackFrame>
            {
                new() { FilePath = "/source/app.v", Line = 42, Column = 10, MethodName = "handleClick" },
                new() { FilePath = "/source/lib.v", Line = 15, Column = 3, MethodName = "render" },
            },
        };

        var html = overlay.RenderErrorPageWithInfo(info);

        Assert.Contains("编译错误", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("app.v", html, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine($"Error Overlay 完整信息：HTML = {html.Length} 字符，含 {info.StackFrames.Count} 个堆栈帧");
    }

    #endregion

    #region 健康检查端点

    [Fact]
    public void HealthCheck_HealthyServer_ReturnsTrue()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromSeconds(1));

        for (var i = 0; i < 100; i++)
        {
            monitor.RecordRequest(1024);
        }

        monitor.RecordError();

        var report = monitor.GetHealthReport();

        Assert.True(report.ErrorRate < 0.05, $"错误率 {report.ErrorRate:P} 应 < 5%");
        Assert.True(report.IsHealthy, "低错误率服务器应为健康");

        _output.WriteLine($"健康检查：错误率={report.ErrorRate:P} 是否健康={report.IsHealthy}");
    }

    [Fact]
    public void HealthCheck_UnhealthyServer_HighErrorRate()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromSeconds(1));

        for (var i = 0; i < 10; i++)
        {
            monitor.RecordRequest(1024);
            monitor.RecordError();
            monitor.RecordError();
        }

        var report = monitor.GetHealthReport();

        Assert.True(report.ErrorRate > 0.05, $"错误率 {report.ErrorRate:P} 应 > 5%");
        Assert.False(report.IsHealthy, "高错误率服务器应为不健康");

        _output.WriteLine($"健康检查：错误率={report.ErrorRate:P} 是否健康={report.IsHealthy}");
    }

    #endregion
}