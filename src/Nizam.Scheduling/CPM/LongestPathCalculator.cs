using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.CPM;

public static class LongestPathCalculator
{
    /// <summary>
    /// Marks the longest path from project start activities to project finish,
    /// preferring critical activities when present.
    /// </summary>
    public static void Calculate(
        IReadOnlyList<ActivityNode> nodes,
        IReadOnlyDictionary<Guid, ActivityNode> nodeById,
        IReadOnlyList<DependencyEdge> edges,
        WorkingCalendar calendar,
        DateTime projectFinish)
    {
        foreach (var node in nodes)
            node.IsLongestPath = false;

        if (nodes.Count == 0)
            return;

        var incoming = edges
            .GroupBy(e => e.SuccessorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DependencyEdge>)g.ToList());

        var outgoing = edges
            .GroupBy(e => e.PredecessorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DependencyEdge>)g.ToList());

        // Distance = working minutes from early start of roots to early finish of node
        var bestDist = nodes.ToDictionary(n => n.Id, _ => int.MinValue);
        var parent = new Dictionary<Guid, Guid?>();

        var order = TopologicalSorter.Sort(nodes, edges);
        foreach (var node in order)
        {
            if (!incoming.TryGetValue(node.Id, out var preds) || preds.Count == 0)
            {
                bestDist[node.Id] = node.DurationMinutes;
                parent[node.Id] = null;
                continue;
            }

            var best = int.MinValue;
            Guid? bestPred = null;
            foreach (var edge in preds)
            {
                var pred = nodeById[edge.PredecessorId];
                var via = bestDist[pred.Id];
                if (via == int.MinValue)
                    continue;

                // Path length accumulates successor remaining duration along the chain
                var candidate = via + node.DurationMinutes;
                // Prefer critical predecessors when distances tie
                if (candidate > best ||
                    (candidate == best && bestPred is not null && pred.IsCritical && !nodeById[bestPred.Value].IsCritical))
                {
                    best = candidate;
                    bestPred = pred.Id;
                }
            }

            if (best == int.MinValue)
            {
                bestDist[node.Id] = node.DurationMinutes;
                parent[node.Id] = null;
            }
            else
            {
                bestDist[node.Id] = best;
                parent[node.Id] = bestPred;
            }
        }

        // Choose finish activity: max EF among those matching projectFinish, prefer critical
        ActivityNode? end = null;
        foreach (var node in nodes)
        {
            if (node.EarlyFinish != projectFinish && node.EarlyFinish < projectFinish)
            {
                // still consider max EF
            }

            if (end is null ||
                node.EarlyFinish > end.EarlyFinish ||
                (node.EarlyFinish == end.EarlyFinish && node.IsCritical && !end.IsCritical) ||
                (node.EarlyFinish == end.EarlyFinish && bestDist[node.Id] > bestDist[end.Id]))
            {
                end = node;
            }
        }

        if (end is null)
            return;

        // Prefer among activities that finish at project finish
        var finishers = nodes.Where(n => n.EarlyFinish == projectFinish).ToList();
        if (finishers.Count > 0)
        {
            end = finishers
                .OrderByDescending(n => n.IsCritical)
                .ThenByDescending(n => bestDist[n.Id])
                .First();
        }

        var currentId = (Guid?)end.Id;
        while (currentId is not null)
        {
            nodeById[currentId.Value].IsLongestPath = true;
            parent.TryGetValue(currentId.Value, out var p);
            currentId = p;
        }

        // If we have critical activities, also ensure a critical path through finishers is marked
        // when longest path didn't cover them (MVP: longest among all is enough).
        _ = outgoing;
        _ = calendar;
    }
}
