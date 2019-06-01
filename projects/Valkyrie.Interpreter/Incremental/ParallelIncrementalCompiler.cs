using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 并行增量编译器
/// 在 IncrementalCompiler 基础上增加文件级并行编译能力，提升多核利用率
/// </summary>
public sealed class ParallelIncrementalCompiler : IncrementalCompiler
{
    private readonly int _maxDegreeOfParallelism;
    private readonly bool _preserveDependencyOrder;

    /// <summary>
    /// 并行编译选项
    /// </summary>
    public ParallelCompilationOptions Options { get; }

    /// <summary>
    ///     创建并行增量编译器
    /// </summary>
    /// <param name="options">并行编译选项</param>
    public ParallelIncrementalCompiler(ParallelCompilationOptions? options = null)
        : base()
    {
        Options = options ?? new ParallelCompilationOptions();
        _maxDegreeOfParallelism = Options.MaxDegreeOfParallelism > 0
            ? Options.MaxDegreeOfParallelism
            : Environment.ProcessorCount;
        _preserveDependencyOrder = Options.PreserveDependencyOrder;
    }

    /// <summary>
    ///     并行编译多个模块（仅编译不受依赖顺序限制的文件）
    /// </summary>
    /// <param name="filePaths">待编译的文件路径列表</param>
    /// <param name="compileFunc">编译函数，接收文件路径返回编译结果</param>
    /// <returns>编译结果映射</returns>
    public async Task<Dictionary<string, ParallelCompilationResult>> CompileInParallelAsync(
        IEnumerable<string> filePaths,
        Func<string, Task<ParallelCompilationResult>> compileFunc)
    {
        var files = filePaths.ToList();
        var results = new ConcurrentDictionary<string, ParallelCompilationResult>();
        var allFiles = files.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_preserveDependencyOrder)
        {
            return await CompileInParallelWithDependencyOrderAsync(files, compileFunc);
        }

        var limitedParallelism = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            files,
            limitedParallelism,
            async (file, ct) =>
            {
                try
                {
                    var result = await compileFunc(file);
                    results[file] = result;
                }
                catch (Exception ex)
                {
                    results[file] = ParallelCompilationResult.FromError(file, ex.Message);
                }
            });

        return new Dictionary<string, ParallelCompilationResult>(results, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     保留依赖顺序的并行编译
    ///     通过拓扑排序分层，同一层内的文件可以并行编译
    /// </summary>
    private async Task<Dictionary<string, ParallelCompilationResult>> CompileInParallelWithDependencyOrderAsync(
        List<string> filePaths,
        Func<string, Task<ParallelCompilationResult>> compileFunc)
    {
        var results = new ConcurrentDictionary<string, ParallelCompilationResult>();
        var dependencyGraph = GetDependencyGraph();

        var layers = ComputeCompilationLayers(filePaths, dependencyGraph);
        var totalLayers = layers.Count;

        for (var layerIndex = 0; layerIndex < totalLayers; layerIndex++)
        {
            var layer = layers[layerIndex];

            var tasks = layer.Select(async file =>
            {
                try
                {
                    var result = await compileFunc(file);
                    results[file] = result;
                }
                catch (Exception ex)
                {
                    results[file] = ParallelCompilationResult.FromError(file, ex.Message);
                }
            });

            await Task.WhenAll(tasks);
        }

        return new Dictionary<string, ParallelCompilationResult>(results, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     计算编译分层（同一层内文件无依赖关系，可以并行）
    /// </summary>
    private List<List<string>> ComputeCompilationLayers(
        List<string> files,
        DependencyGraph dependencyGraph)
    {
        var layers = new List<List<string>>();
        var remaining = files.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (remaining.Count > 0)
        {
            var currentLayer = remaining
                .Where(f => !HasUnprocessedDependencies(f, dependencyGraph, processed))
                .ToList();

            if (currentLayer.Count == 0)
            {
                currentLayer = remaining.ToList();
            }

            foreach (var file in currentLayer)
            {
                remaining.Remove(file);
                processed.Add(file);
            }

            layers.Add(currentLayer);
        }

        return layers;
    }

    /// <summary>
    ///     检查文件是否有未处理的依赖
    /// </summary>
    private bool HasUnprocessedDependencies(
        string file,
        DependencyGraph dependencyGraph,
        HashSet<string> processed)
    {
        var dependencies = dependencyGraph.GetDependencies(file);

        foreach (var dep in dependencies)
        {
            if (processed.Contains(dep))
            {
                continue;
            }

            if (dependencyGraph.GetDependencies(dep).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     使用数据流块进行并行编译
    ///     支持更细粒度的任务调度和背压控制
    /// </summary>
    public async Task<Dictionary<string, ParallelCompilationResult>> CompileWithDataflowAsync(
        IEnumerable<string> filePaths,
        Func<string, Task<ParallelCompilationResult>> compileFunc,
        CancellationToken cancellationToken = default)
    {
        var files = filePaths.ToList();
        var results = new ConcurrentDictionary<string, ParallelCompilationResult>();

        var inputBlock = new BufferBlock<string>(new DataflowBlockOptions
        {
            BoundedCapacity = _maxDegreeOfParallelism * 2,
            CancellationToken = cancellationToken
        });

        var computeBlock = new TransformBlock<string, (string File, ParallelCompilationResult Result)>(
            async file =>
            {
                try
                {
                    var result = await compileFunc(file);
                    return (file, result);
                }
                catch (Exception ex)
                {
                    return (file, ParallelCompilationResult.FromError(file, ex.Message));
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            });

        var outputBlock = new ActionBlock<(string File, ParallelCompilationResult Result)>(
            tuple =>
            {
                results[tuple.File] = tuple.Result;
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                CancellationToken = cancellationToken
            });

        inputBlock.LinkTo(computeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        computeBlock.LinkTo(outputBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var file in files)
        {
            await inputBlock.SendAsync(file, cancellationToken);
        }

        inputBlock.Complete();

        await Task.WhenAll(
            inputBlock.Completion,
            computeBlock.Completion,
            outputBlock.Completion);

        return new Dictionary<string, ParallelCompilationResult>(results, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     估算并行编译加速比
    /// </summary>
    /// <param name="totalFiles">总文件数</param>
    /// <param name="dependencyDepth">依赖深度</param>
    /// <returns>预估加速比</returns>
    public double EstimateSpeedup(int totalFiles, int dependencyDepth)
    {
        var parallelism = Math.Min(_maxDegreeOfParallelism, totalFiles);
        var layers = Math.Max(1, dependencyDepth);

        var sequentialTime = totalFiles;
        var parallelTime = layers > 0
            ? Math.Ceiling((double)totalFiles / parallelism) * layers
            : Math.Ceiling((double)totalFiles / parallelism);

        return sequentialTime / parallelTime;
    }
}
