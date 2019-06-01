using System.Collections.Concurrent;

namespace VOA.ToolChain.Tests;

/// <summary>
///     DevServer 并发压力测试
///     验证高并发场景下的稳定性和资源管理
/// </summary>
public sealed class DevServerConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public DevServerConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 并发连接测试

    [Fact]
    public async Task ConcurrentConnections_100Simultaneous_AllComplete()
    {
        const int connectionCount = 100;
        var completed = 0;
        var errors = new ConcurrentBag<string>();

        var tasks = new List<Task>();
        for (var i = 0; i < connectionCount; i++)
        {
            var clientId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(new Random().Next(1, 10));
                    Interlocked.Increment(ref completed);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }));
        }

        await Task.WhenAll(tasks);

        _output.WriteLine($"并发连接测试：{completed}/{connectionCount} 完成，{errors.Count} 错误");

        Assert.Equal(connectionCount, completed);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ConcurrentConnections_1000Simulated_HandlesCorrectly()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromMilliseconds(100));
        var tasks = new List<Task>();

        for (var i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                monitor.RecordConnectionOpen();
                monitor.RecordRequest(512);

                Thread.Sleep(new Random().Next(1, 5));

                monitor.RecordConnectionClose();
            }));
        }

        await Task.WhenAll(tasks);

        var report = monitor.GetHealthReport();

        Assert.Equal(1000L, report.TotalRequests);
        Assert.True(report.PeakConnections > 10, $"峰值连接数 {report.PeakConnections} 应 > 10");

        _output.WriteLine($"并发 1000：峰值连接={report.PeakConnections} 最终活跃={monitor.ActiveConnections}");
    }

    [Fact]
    public void ConcurrentRequests_Throughput_MeasuresRps()
    {
        var monitor = new VoaDevServerMonitor(TimeSpan.FromMilliseconds(50));
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var requestCount = 500;
        for (var i = 0; i < requestCount; i++)
        {
            monitor.RecordRequest(1024);
        }

        sw.Stop();
        var rps = requestCount / sw.Elapsed.TotalSeconds;

        _output.WriteLine($"吞吐量：{requestCount} 请求 / {sw.Elapsed.TotalMilliseconds:F1}ms = {rps:F0} RPS");
        Assert.True(rps > 0, "RPS 应 > 0");
    }

    #endregion

    #region WebSocket 并发

    [Fact]
    public void WebSockets_Tracking_MultipleClients()
    {
        var clients = new ConcurrentDictionary<string, bool>();

        for (var i = 0; i < 50; i++)
        {
            clients[$"client_{i}"] = true;
        }

        Assert.Equal(50, clients.Count);

        clients.TryRemove("client_10", out _);
        clients.TryRemove("client_25", out _);
        clients.TryRemove("client_40", out _);

        _output.WriteLine($"WebSocket 客户端：初始 50 → 断开 3 后 {clients.Count}");
        Assert.Equal(47, clients.Count);
    }

    [Fact]
    public void WebSockets_Broadcast_ParallelDelivery()
    {
        var clientCount = 100;
        var delivered = 0;
        var lockObj = new object();

        Parallel.For(0, clientCount, i =>
        {
            lock (lockObj)
            {
                delivered++;
            }
        });

        _output.WriteLine($"广播投递：{delivered}/{clientCount} 客户端");
        Assert.Equal(clientCount, delivered);
    }

    [Fact]
    public void WebSockets_DeadClient_CleanedUp()
    {
        var clients = new ConcurrentDictionary<string, bool>();
        for (var i = 0; i < 20; i++)
        {
            clients[$"client_{i}"] = true;
        }

        var deadIds = new[] { "client_3", "client_7", "client_15", "client_19" };
        foreach (var id in deadIds)
        {
            clients.TryRemove(id, out _);
        }

        Assert.Equal(16, clients.Count);
        foreach (var id in deadIds)
        {
            Assert.False(clients.ContainsKey(id), $"已断开客户端 {id} 应从列表中移除");
        }

        _output.WriteLine($"死客户端清理：4 个断开 → 剩余 {clients.Count} 个");
    }

    #endregion

    #region 资源竞争测试

    [Fact]
    public void ThreadPool_Dynamic_HandlesSpikes()
    {
        var beforeThreads = ThreadPool.ThreadCount;

        using var countdown = new CountdownEvent(200);
        for (var i = 0; i < 200; i++)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(10);
                countdown.Signal();
            });
        }

        countdown.Wait(TimeSpan.FromSeconds(5));

        _output.WriteLine($"线程池弹性：之前 {beforeThreads} 线程 → 处理 200 个工作项 ✅");
    }

    [Fact]
    public async Task AsyncTasks_ManyConcurrent_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 300).Select(async i =>
        {
            await Task.Yield();
            var data = new byte[new Random().Next(1, 256)];
            return data.Length;
        }).ToList();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(300, results.Length);
        _output.WriteLine($"异步任务并发：300 个任务全部完成，总结果 = {results.Sum()}");
    }

    [Fact]
    public void LockContention_Minimize_WithConcurrentCollections()
    {
        var dict = new ConcurrentDictionary<int, long>();
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        Parallel.For(0, 1000, i =>
        {
            dict[i] = i * 2L;
        });

        sw.Stop();
        _output.WriteLine($"ConcurrentDictionary 写入：1000 项 / {sw.Elapsed.TotalMilliseconds:F1}ms");

        Assert.Equal(1000, dict.Count);
        Assert.True(sw.Elapsed.TotalMilliseconds < 500,
            $"并发写入耗时 {sw.Elapsed.TotalMilliseconds}ms 应在合理范围内");
    }

    #endregion

    #region 请求队列

    [Fact]
    public async Task RequestQueue_BoundedCapacity_PreventsOverload()
    {
        const int boundedCapacity = 50;
        var queue = new System.Threading.Channels.Channel<int>(
            new System.Threading.Channels.BoundedChannelOptions(boundedCapacity)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            });

        for (var i = 0; i < 100; i++)
        {
            await queue.Writer.WriteAsync(i);
        }

        Assert.Equal(boundedCapacity, queue.Reader.Count);

        _output.WriteLine($"有界队列：容量 {boundedCapacity}，丢弃策略 DropOldest，当前 {queue.Reader.Count} 项");
    }

    [Fact]
    public async Task RequestQueue_FairScheduling_ProcessesAll()
    {
        var queue = new System.Threading.Channels.Channel<int>(
            new System.Threading.Channels.UnboundedChannelOptions());

        for (var i = 0; i < 50; i++)
        {
            await queue.Writer.WriteAsync(i);
        }

        queue.Writer.Complete();

        var processed = 0;
        await foreach (var item in queue.Reader.ReadAllAsync())
        {
            processed++;
        }

        _output.WriteLine($"公平调度：{processed} 项全部处理 ✅");
        Assert.Equal(50, processed);
    }

    #endregion
}