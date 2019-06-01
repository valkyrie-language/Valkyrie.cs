namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 模块依赖图
/// 跟踪模块间的 import 依赖关系，支持增量编译时的依赖失效传播
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _moduleToDependencies;
    private readonly Dictionary<string, HashSet<string>> _moduleToDependents;

    public DependencyGraph()
    {
        _moduleToDependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        _moduleToDependents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 更新模块的依赖关系
    /// </summary>
    public void UpdateModule(string filePath, List<string> dependencies)
    {
        RemoveModule(filePath);

        var depSet = new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase);
        _moduleToDependencies[filePath] = depSet;

        foreach (var dep in dependencies)
        {
            if (!_moduleToDependents.TryGetValue(dep, out var dependents))
            {
                dependents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _moduleToDependents[dep] = dependents;
            }

            dependents.Add(filePath);
        }
    }

    /// <summary>
    /// 移除模块及其依赖关系
    /// </summary>
    public void RemoveModule(string filePath)
    {
        if (_moduleToDependencies.TryGetValue(filePath, out var oldDeps))
        {
            foreach (var dep in oldDeps)
            {
                if (_moduleToDependents.TryGetValue(dep, out var dependents))
                {
                    dependents.Remove(filePath);
                    if (dependents.Count == 0)
                    {
                        _moduleToDependents.Remove(dep);
                    }
                }
            }

            _moduleToDependencies.Remove(filePath);
        }
    }

    /// <summary>
    /// 获取直接依赖指定模块的所有模块
    /// </summary>
    public List<string> GetDependents(string filePath)
    {
        if (!_moduleToDependents.TryGetValue(filePath, out var dependents))
        {
            return [];
        }

        return dependents.ToList();
    }

    /// <summary>
    /// 获取指定模块的所有传递依赖者（递归）
    /// </summary>
    public List<string> GetTransitiveDependents(string filePath)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectTransitiveDependents(filePath, result);
        return result.ToList();
    }

    /// <summary>
    /// 获取指定模块的直接依赖
    /// </summary>
    public List<string> GetDependencies(string filePath)
    {
        if (!_moduleToDependencies.TryGetValue(filePath, out var dependencies))
        {
            return [];
        }

        return dependencies.ToList();
    }

    /// <summary>
    /// 获取拓扑排序的编译顺序
    /// </summary>
    public List<string> GetTopologicalOrder()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in _moduleToDependencies.Keys)
        {
            VisitTopological(module, visited, visiting, result);
        }

        return result;
    }

    /// <summary>
    /// 检测是否存在循环依赖
    /// </summary>
    public bool HasCircularDependency()
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in _moduleToDependencies.Keys)
        {
            if (DetectCycle(module, visiting, visited))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 清除所有依赖关系
    /// </summary>
    public void Clear()
    {
        _moduleToDependencies.Clear();
        _moduleToDependents.Clear();
    }

    /// <summary>
    /// 获取模块数量
    /// </summary>
    public int ModuleCount => _moduleToDependencies.Count;

    #region 私有方法

    private void CollectTransitiveDependents(string filePath, HashSet<string> result)
    {
        if (!_moduleToDependents.TryGetValue(filePath, out var dependents))
        {
            return;
        }

        foreach (var dep in dependents)
        {
            if (result.Add(dep))
            {
                CollectTransitiveDependents(dep, result);
            }
        }
    }

    private void VisitTopological(
        string module,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<string> result)
    {
        if (visited.Contains(module))
        {
            return;
        }

        if (visiting.Contains(module))
        {
            return;
        }

        visiting.Add(module);

        if (_moduleToDependencies.TryGetValue(module, out var deps))
        {
            foreach (var dep in deps)
            {
                if (_moduleToDependencies.ContainsKey(dep))
                {
                    VisitTopological(dep, visited, visiting, result);
                }
            }
        }

        visiting.Remove(module);
        visited.Add(module);
        result.Add(module);
    }

    private bool DetectCycle(
        string module,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(module))
        {
            return false;
        }

        if (visiting.Contains(module))
        {
            return true;
        }

        visiting.Add(module);

        if (_moduleToDependencies.TryGetValue(module, out var deps))
        {
            foreach (var dep in deps)
            {
                if (_moduleToDependencies.ContainsKey(dep) && DetectCycle(dep, visiting, visited))
                {
                    return true;
                }
            }
        }

        visiting.Remove(module);
        visited.Add(module);
        return false;
    }

    #endregion
}