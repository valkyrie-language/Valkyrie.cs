namespace Legion.Workspace;

public class WorkspaceDependencyGraph
{
    public Dictionary<string, List<string>> InternalDependencies { get; set; } = new();
    public Dictionary<string, List<string>> ExternalDependencies { get; set; } = new();

    public List<string> GetTopologicalOrder()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var node in InternalDependencies.Keys)
        {
            TopologicalVisit(node, visited, visiting, result);
        }

        return result;
    }

    private void TopologicalVisit(string node, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visited.Contains(node))
        {
            return;
        }

        if (visiting.Contains(node))
        {
            return;
        }

        visiting.Add(node);

        if (InternalDependencies.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                TopologicalVisit(dep, visited, visiting, result);
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        result.Add(node);
    }
}