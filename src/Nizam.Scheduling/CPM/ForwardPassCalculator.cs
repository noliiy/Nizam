using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Constraints;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.CPM;

public static class ForwardPassCalculator
{
    public const int Fs = 0;
    public const int Ss = 1;
    public const int Ff = 2;
    public const int Sf = 3;

    public static void Calculate(
        IReadOnlyList<ActivityNode> topologicalOrder,
        IReadOnlyDictionary<Guid, ActivityNode> nodeById,
        IReadOnlyList<DependencyEdge> edges,
        WorkingCalendar calendar,
        DateTime projectStart,
        DateTime? dataDate)
    {
        var incoming = edges
            .GroupBy(e => e.SuccessorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DependencyEdge>)g.ToList());

        var normalizedStart = calendar.IsWorkingTime(projectStart)
            ? Truncate(projectStart)
            : calendar.GetNextWorkingTime(projectStart);

        DateTime? dataDateWorking = null;
        if (dataDate is not null)
        {
            var dd = Truncate(dataDate.Value);
            dataDateWorking = calendar.IsWorkingTime(dd) ? dd : calendar.GetNextWorkingTime(dd);
        }

        foreach (var node in topologicalOrder)
        {
            if (node.ActualFinish is not null)
            {
                var af = Truncate(node.ActualFinish.Value);
                node.EarlyStart = node.ActualStart is not null ? Truncate(node.ActualStart.Value) : af;
                node.EarlyFinish = af;
                node.DurationMinutes = 0;
                node.RemainingStart = null;
                node.RemainingFinish = null;
                continue;
            }

            var duration = Math.Max(0, node.DurationMinutes);
            incoming.TryGetValue(node.Id, out var preds);
            preds ??= Array.Empty<DependencyEdge>();

            DateTime? esCandidate = null;
            DateTime? efCandidate = null;

            if (preds.Count == 0)
            {
                esCandidate = normalizedStart;
            }
            else
            {
                foreach (var edge in preds)
                {
                    var pred = nodeById[edge.PredecessorId];
                    switch (edge.Type)
                    {
                        case Ss:
                        {
                            var ssEs = calendar.AddWorkingMinutes(pred.EarlyStart, edge.LagMinutes);
                            esCandidate = Max(esCandidate, ssEs);
                            break;
                        }
                        case Ff:
                        {
                            var ffEf = calendar.AddWorkingMinutes(pred.EarlyFinish, edge.LagMinutes);
                            efCandidate = Max(efCandidate, ffEf);
                            break;
                        }
                        case Sf:
                        {
                            var sfEf = calendar.AddWorkingMinutes(pred.EarlyStart, edge.LagMinutes);
                            efCandidate = Max(efCandidate, sfEf);
                            break;
                        }
                        default: // FS
                        {
                            var fsEs = calendar.AddWorkingMinutes(pred.EarlyFinish, edge.LagMinutes);
                            esCandidate = Max(esCandidate, fsEs);
                            break;
                        }
                    }
                }
            }

            DateTime es;
            DateTime ef;

            if (duration == 0)
            {
                es = esCandidate ?? normalizedStart;
                if (efCandidate is not null)
                    es = Max(es, efCandidate.Value);
                es = ConstraintProcessor.ApplyStartOnOrAfter(node, es, calendar);
                if (!calendar.IsWorkingTime(es))
                    es = calendar.GetNextWorkingTime(es);

                if (dataDateWorking is not null && es < dataDateWorking.Value)
                    es = dataDateWorking.Value;

                node.EarlyStart = es;
                node.EarlyFinish = es;
                node.RemainingStart = es;
                node.RemainingFinish = es;
                continue;
            }

            if (efCandidate is not null)
            {
                var esFromEf = calendar.SubtractWorkingMinutes(efCandidate.Value, duration);
                esCandidate = Max(esCandidate, esFromEf);
            }

            es = esCandidate ?? normalizedStart;

            if (node.ActualStart is not null)
                es = Max(es, Truncate(node.ActualStart.Value));

            es = ConstraintProcessor.ApplyStartOnOrAfter(node, es, calendar);
            if (!calendar.IsWorkingTime(es))
                es = calendar.GetNextWorkingTime(es);

            var remainingStart = es;
            if (dataDateWorking is not null && remainingStart < dataDateWorking.Value)
                remainingStart = dataDateWorking.Value;
            if (!calendar.IsWorkingTime(remainingStart))
                remainingStart = calendar.GetNextWorkingTime(remainingStart);

            ef = calendar.AddWorkingMinutes(remainingStart, duration);
            if (efCandidate is not null && efCandidate.Value > ef)
            {
                ef = efCandidate.Value;
                es = calendar.SubtractWorkingMinutes(ef, duration);
                remainingStart = es;
                if (dataDateWorking is not null && remainingStart < dataDateWorking.Value)
                {
                    remainingStart = dataDateWorking.Value;
                    if (!calendar.IsWorkingTime(remainingStart))
                        remainingStart = calendar.GetNextWorkingTime(remainingStart);
                    ef = calendar.AddWorkingMinutes(remainingStart, duration);
                    if (efCandidate.Value > ef)
                        ef = efCandidate.Value;
                }
            }

            node.EarlyStart = es;
            node.EarlyFinish = ef;
            node.RemainingStart = remainingStart;
            node.RemainingFinish = calendar.AddWorkingMinutes(remainingStart, duration);
        }
    }

    private static DateTime Max(DateTime? current, DateTime value) =>
        current is null || value > current.Value ? value : current.Value;

    private static DateTime Truncate(DateTime dt) =>
        DateTime.SpecifyKind(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0), DateTimeKind.Unspecified);
}
