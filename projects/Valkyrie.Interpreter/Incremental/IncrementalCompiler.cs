namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 增量编译器
/// 管理模块级编译缓存和依赖图，支持增量编译
/// </summary>
public class IncrementalCompiler
{
    private readonly Dictionary<string, CachedModule> _cache;
    private readonly DependencyGraph _dependencyGraph;
    private readonly CompilationStats _stats;

    public IncrementalCompiler()
    {
        _cache = new Dictionary<string, CachedModule>(StringComparer.OrdinalIgnoreCase);
        _dependencyGraph = new DependencyGraph();
        _stats = new CompilationStats();
    }

    /// <summary>
    ///     获取编译统计信息
    /// </summary>
    public CompilationStats Stats => _stats;

    /// <summary>
    /// 获取缓存的模块
    /// </summary>
    public CachedModule? GetCachedModule(string filePath)
    {
        var cached = _cache.TryGetValue(filePath, out var result) ? result : null;

        if (cached is not null)
        {
            _stats.IncrementHits();
        }
        else
        {
            _stats.IncrementMisses();
        }

        return cached;
    }

    /// <summary>
    /// 更新模块缓存
    /// </summary>
    public void UpdateCache(string filePath, CachedModule module)
    {
        _cache[filePath] = module;
        _dependencyGraph.UpdateModule(filePath, module.Dependencies);
    }

    /// <summary>
    /// 使指定文件的缓存失效，并返回所有受影响的文件
    /// </summary>
    public List<string> Invalidate(string filePath)
    {
        var affected = _dependencyGraph.GetDependents(filePath);
        affected.Add(filePath);

        foreach (var path in affected)
        {
            _cache.Remove(path);
        }

        _dependencyGraph.RemoveModule(filePath);

        return affected;
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        _dependencyGraph.Clear();
    }

    /// <summary>
    /// 获取所有已缓存的文件路径
    /// </summary>
    public IReadOnlyList<string> GetCachedFiles()
    {
        return _cache.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取依赖图
    /// </summary>
    public DependencyGraph GetDependencyGraph()
    {
        return _dependencyGraph;
    }
}