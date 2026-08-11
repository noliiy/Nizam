using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Constraints;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.CPM;

public static class BackwardPassCalculator
{
    public static void Calculate(
        IReadOnlyList<ActivityNode> topologicalOrder,
        IReadOnlyDictionary<Guid, ActivityNode> nodeById,
        IReadOnlyList<DependencyEdge> edges,
        WorkingCalendar calendar,
        DateTime projectFinish)
    {
        var outgoing = edges
            .GroupBy(e => e.PredecessorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DependencyEdge>)g.ToList());

        for (var i = topologicalOrder.Count - 1; i >= 0; i--)
        {
            var node = topologicalOrder[i];
            var duration = Math.Max(0, node.DurationMinutes);

            outgoing.TryGetValue(node.Id, out var succs);
            succs ??= Array.Empty<DependencyEdge>();

            DateTime lf;

            if (succs.Count == 0)
            {
                lf = projectFinish;
            }
            else
            {
                DateTime? lfCandidate = null;
                DateTime? lsCandidate = null;

                foreach (var edge in succs)
                {
                    var succ = nodeById[edge.SuccessorId];
                    switch (edge.Type)
                    {
                        case ForwardPassCalculator.Ss:
                        {
                            // Pred.LS + lag <= Succ.LS  => Pred.LS <= Subtract(Succ.LS, lag)
                            var ssLs = calendar.SubtractWorkingMinutes(succ.LateStart, edge.LagMinutes);
                            lsCandidate = Min(lsCandidate, ssLs);
                            break;
                        }
                        case ForwardPassCalculator.Ff:
                        {
                            // Pred.LF + lag <= Succ.LF => Pred.LF <= Subtract(Succ.LF, lag)
                            var ffLf = calendar.SubtractWorkingMinutes(succ.LateFinish, edge.LagMinutes);
                            lfCandidate = Min(lfCandidate, ffLf);
                            break;
                        }
                        case ForwardPassCalculator.Sf:
                        {
                            // Pred.ES-driven: Pred.LS + lag relates to Succ.LF
                            // Succ.LF >= Pred.LS + lag => Pred.LS <= Subtract(Succ.LF, lag)
                            var sfLs = calendar.SubtractWorkingMinutes(succ.LateFinish, edge.LagMinutes);
                            lsCandidate = Min(lsCandidate, sfLs);
                            break;
                        }
                        default: // FS
                        {
                            // Pred.LF + lag <= Succ.LS => Pred.LF <= Subtract(Succ.LS, lag)
                            var fsLf = calendar.SubtractWorkingMinutes(succ.LateStart, edge.LagMinutes);
                            lfCandidate = Min(lfCandidate, fsLf);
                            break;
                        }
                    }
                }

                if (lsCandidate is not null)
                {
                    var lfFromLs = duration == 0
                        ? lsCandidate.Value
                        : calendar.AddWorkingMinutes(lsCandidate.Value, duration);
                    lfCandidate = Min(lfCandidate, lfFromLs);
                }

                lf = lfCandidate ?? projectFinish;
            }

            lf = ConstraintProcessor.ApplyFinishOnOrBefore(node, lf, calendar);

            DateTime ls;
            if (duration == 0)
            {
                ls = lf;
            }
            else
            {
                ls = calendar.SubtractWorkingMinutes(lf, duration);
            }

            // Completed activities: lock late dates to early
            if (node.ActualFinish is not null)
            {
                node.LateStart = node.EarlyStart;
                node.LateFinish = node.EarlyFinish;
                continue;
            }

            node.LateFinish = lf;
            node.LateStart = ls;
        }
    }

    private static DateTime Min(DateTime? current, DateTime value) =>
        current is null || value < current.Value ? value : current.Value;
}
