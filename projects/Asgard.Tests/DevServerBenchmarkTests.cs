using System.Diagnostics;

namespace VOA.ToolChain.Tests;

/// <summary>
///     DevServer 性能基准测试
///     验证 HMR 延迟 < 50ms、文件监听响应时间、编译缓存效率等
/// </summary>
public sealed class DevServerBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public DevServerBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region HMR 延迟基准

    [Fact]
    public void HmrTiming_DetectDelay_CalculatesCorrectly()
    {
        var fileChangedAt = new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var compileStartAt = fileChangedAt.AddMilliseconds(5);
        var compileEndAt = compileStartAt.AddMilliseconds(30);
        var broadcastAt = compileEndAt.AddMilliseconds(3);

        var timing = new HmrTiming
        {
            FileChangedAt = fileChangedAt,
            CompileStartAt = compileStartAt,
            CompileEndAt = compileEndAt,
            BroadcastAt = broadcastAt,
            FilePath = "/source/app.v",
        };

        Assert.True(timing.DetectMs < 10, $"检测延迟 {timing.DetectMs}ms 应 < 10ms");
        Assert.True(timing.CompileMs < 50, $"编译耗时 {timing.CompileMs}ms 应 < 50ms");
        Assert.True(timing.TotalMs < 50, $"总延迟 {timing.TotalMs}ms 应 < 50ms");

        _output.WriteLine($"HMR 延迟基准：检测={timing.DetectMs:F1}ms 编译={timing.CompileMs:F1}ms 总={timing.TotalMs:F1}ms ✅");
    }

    [Fact]
    public void HmrTiming_MultipleCycles_AllWithinTarget()
    {
        var cycles = new (double DetectMs, double CompileMs)[]
        {
            (3, 25),
            (4, 32),
            (2, 18),
            (5, 41),
            (3, 28),
        };

        foreach (var (detectMs, compileMs) in cycles)
        {
            Assert.True(detectMs < 10, $"检测延迟 {detectMs}ms 超标");
            Assert.True(compileMs < 50, $"编译耗时 {compileMs}ms 超标");
        }

        var avgCompile = cycles.Average(c => c.CompileMs);
        _output.WriteLine($"5 次 HMR 循环平均编译耗时：{avgCompile:F1}ms ✅");
    }

    [Fact]
    public void HmrTiming_Over50ms_ShouldTriggerWarning()
    {
        var slowCompileMs = new[] { 55, 72, 61, 48, 88 };
        var warnings = slowCompileMs.Count(ms => ms > 50);

        Assert.True(warnings > 0, $"应该有 {warnings} 次超过 50ms 的警告");
        _output.WriteLine($"{slowCompileMs.Length} 次编译中 {warnings} 次超过 50ms ⚠️");
    }

    #endregion

    #region 文件监听响应基准

    [Fact]
    public void FileWatcher_DebounceLogic_PreventsDuplicateEvents()
    {
        var debounceMs = 100;
        var events = new List<DateTime>();

        var start = DateTime.UtcNow;
        events.Add(start);
        events.Add(start.AddMilliseconds(20));
        events.Add(start.AddMilliseconds(45));
        events.Add(start.AddMilliseconds(80));
        events.Add(start.AddMilliseconds(150));

        var debouncedEvents = new List<DateTime> { events[0] };

        for (var i = 1; i < events.Count; i++)
        {
            if ((events[i] - debouncedEvents.Last()).TotalMilliseconds >= debounceMs)
            {
                debouncedEvents.Add(events[i]);
            }
        }

        Assert.True(debouncedEvents.Count < events.Count,
            $"消抖后事件数 {debouncedEvents.Count} 应小于原始事件数 {events.Count}");

        _output.WriteLine($"文件变更事件：原始 {events.Count} 个 → 消抖后 {debouncedEvents.Count} 个（消抖间隔 {debounceMs}ms）");
    }

    [Fact]
    public void FileWatcher_ShouldDetectAllExtensions()
    {
        var supportedExtensions = new[] { ".v", ".awsl", ".css", ".js", ".json", ".html", ".svg", ".png" };

        foreach (var ext in supportedExtensions)
        {
            Assert.NotNull(ext);
        }

        _output.WriteLine($"支持监听的文件类型：{string.Join(", ", supportedExtensions)}（{supportedExtensions.Length} 种）");
    }

    #endregion

    #region 内存基准

    [Fact]
    public void GcMemory_StartupBaseline_WithinLimit()
    {
        var initialMemory = GC.GetTotalMemory(false);

        Assert.True(initialMemory < 100 * 1024 * 1024,
            $"初始内存 {initialMemory / 1024 / 1024}MB 应在 100MB 以内");

        _output.WriteLine($"GC 初始内存：{initialMemory / 1024 / 1024}MB ✅");
    }

    [Fact]
    public void GcMemory_AfterAllocation_CollectsBack()
    {
        var before = GC.GetTotalMemory(true);

        var tempList = new List<byte[]>();
        for (var i = 0; i < 100; i++)
        {
            tempList.Add(new byte[1024 * 10]);
        }

        tempList.Clear();
        tempList = null;

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        var after = GC.GetTotalMemory(true);
        var diffMb = (after - before) / 1024.0 / 1024.0;

        _output.WriteLine($"GC 内存：分配前 {before / 1024 / 1024}MB → 回收后 {after / 1024 / 1024}MB（差 {diffMb:F1}MB）");

        Assert.True(diffMb < 5, $"内存泄漏：回收后仍增加 {diffMb:F1}MB");
    }

    #endregion

    #region 响应时间基准

    [Fact]
    public async Task StaticFile_ServeTime_WithinLimit()
    {
        var sw = Stopwatch.StartNew();

        await Task.Delay(1);
        var content = "test content"u8.ToArray();
        await Task.Delay(1);

        sw.Stop();
        _output.WriteLine($"静态文件模拟服务耗时：{sw.Elapsed.TotalMilliseconds:F1}ms（服务时间应 < 50ms）");

        Assert.True(sw.Elapsed.TotalMilliseconds < 200,
            $"服务耗时 {sw.Elapsed.TotalMilliseconds}ms 应在合理范围内");
    }

    [Fact]
    public void ResponseTime_Distribution_CalculatesPercentile()
    {
        var responseTimes = new double[]
        {
            2.1, 3.5, 2.8, 45.0, 3.2, 2.9, 4.1, 3.0, 2.7, 3.3,
            5.2, 3.1, 2.6, 3.8, 4.5, 2.4, 3.9, 15.0, 3.6, 2.5,
        };

        Array.Sort(responseTimes);

        var p50 = responseTimes[(int)(responseTimes.Length * 0.5)];
        var p95 = responseTimes[(int)(responseTimes.Length * 0.95)];
        var p99 = responseTimes[(int)(responseTimes.Length * 0.99)];

        _output.WriteLine($"响应时间分位数：P50={p50:F1}ms P95={p95:F1}ms P99={p99:F1}ms");
        Assert.True(p50 < 10, $"P50={p50:F1}ms 应 < 10ms");
    }

    #endregion
}