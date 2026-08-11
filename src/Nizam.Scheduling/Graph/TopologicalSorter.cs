namespace Nizam.Scheduling.Graph;

public static class TopologicalSorter
{
    public static IReadOnlyList<ActivityNode> Sort(
        IReadOnlyList<ActivityNode> nodes,
        IReadOnlyList<DependencyEdge> edges)
    {
        if (CycleDetector.HasCycle(nodes, edges))
            throw new InvalidOperationException("Cannot topologically sort a graph that contains a cycle.");

        var nodeById = nodes.ToDictionary(n => n.Id);
        var indegree = nodes.ToDictionary(n => n.Id, _ => 0);
        var adjacency = nodes.ToDictionary(n => n.Id, _ => new List<Guid>());

        foreach (var edge in edges)
        {
            if (!nodeById.ContainsKey(edge.PredecessorId) || !nodeById.ContainsKey(edge.SuccessorId))
                continue;
            adjacency[edge.PredecessorId].Add(edge.SuccessorId);
            indegree[edge.SuccessorId]++;
        }

        var queue = new Queue<Guid>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<ActivityNode>(nodes.Count);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(nodeById[id]);
            foreach (var next in adjacency[id])
            {
                indegree[next]--;
                if (indegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        if (result.Count != nodes.Count)
            throw new InvalidOperationException("Topological sort failed due to a cycle.");

        return result;
    }
}
