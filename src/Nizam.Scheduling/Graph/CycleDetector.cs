namespace Nizam.Scheduling.Graph;

public static class CycleDetector
{
    public static bool HasCycle(
        IReadOnlyList<ActivityNode> nodes,
        IReadOnlyList<DependencyEdge> edges)
    {
        var adjacency = BuildAdjacency(nodes, edges);
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();

        foreach (var node in nodes)
        {
            if (visited.Contains(node.Id))
                continue;

            if (Dfs(node.Id, adjacency, visiting, visited))
                return true;
        }

        return false;
    }

    private static bool Dfs(
        Guid id,
        Dictionary<Guid, List<Guid>> adjacency,
        HashSet<Guid> visiting,
        HashSet<Guid> visited)
    {
        if (visiting.Contains(id))
            return true;
        if (visited.Contains(id))
            return false;

        visiting.Add(id);
        if (adjacency.TryGetValue(id, out var successors))
        {
            foreach (var next in successors)
            {
                if (Dfs(next, adjacency, visiting, visited))
                    return true;
            }
        }

        visiting.Remove(id);
        visited.Add(id);
        return false;
    }

    private static Dictionary<Guid, List<Guid>> BuildAdjacency(
        IReadOnlyList<ActivityNode> nodes,
        IReadOnlyList<DependencyEdge> edges)
    {
        var adjacency = nodes.ToDictionary(n => n.Id, _ => new List<Guid>());
        foreach (var edge in edges)
        {
            if (!adjacency.ContainsKey(edge.PredecessorId) || !adjacency.ContainsKey(edge.SuccessorId))
                continue;
            adjacency[edge.PredecessorId].Add(edge.SuccessorId);
        }

        return adjacency;
    }
}
