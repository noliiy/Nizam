using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.CPM;

public static class FloatCalculator
{
    public static void Calculate(
        IReadOnlyList<ActivityNode> nodes,
        IReadOnlyDictionary<Guid, ActivityNode> nodeById,
        IReadOnlyList<DependencyEdge> edges,
        WorkingCalendar calendar)
    {
        var outgoing = edges
            .GroupBy(e => e.PredecessorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DependencyEdge>)g.ToList());

        foreach (var node in nodes)
        {
            node.TotalFloatMinutes = calendar.CalculateWorkingDuration(node.EarlyStart, node.LateStart);

            // Free float ≈ min over successors of (succ.ES - pred.EF - lag) in working minutes
            if (!outgoing.TryGetValue(node.Id, out var succs) || succs.Count == 0)
            {
                node.FreeFloatMinutes = node.TotalFloatMinutes;
                continue;
            }

            var minFree = int.MaxValue;
            foreach (var edge in succs)
            {
                var succ = nodeById[edge.SuccessorId];
                int free;
                switch (edge.Type)
                {
                    case ForwardPassCalculator.Ss:
                    {
                        var earliest = calendar.AddWorkingMinutes(node.EarlyStart, edge.LagMinutes);
                        free = calendar.CalculateWorkingDuration(earliest, succ.EarlyStart);
                        break;
                    }
                    case ForwardPassCalculator.Ff:
                    {
                        var earliest = calendar.AddWorkingMinutes(node.EarlyFinish, edge.LagMinutes);
                        free = calendar.CalculateWorkingDuration(earliest, succ.EarlyFinish);
                        break;
                    }
                    case ForwardPassCalculator.Sf:
                    {
                        var earliest = calendar.AddWorkingMinutes(node.EarlyStart, edge.LagMinutes);
                        free = calendar.CalculateWorkingDuration(earliest, succ.EarlyFinish);
                        break;
                    }
                    default:
                    {
                        var earliest = calendar.AddWorkingMinutes(node.EarlyFinish, edge.LagMinutes);
                        free = calendar.CalculateWorkingDuration(earliest, succ.EarlyStart);
                        break;
                    }
                }

                if (free < minFree)
                    minFree = free;
            }

            node.FreeFloatMinutes = minFree == int.MaxValue ? 0 : Math.Max(0, minFree);
        }
    }
}
