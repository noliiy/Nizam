using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.CPM;
using Nizam.Scheduling.Graph;
using Nizam.Scheduling.Models;

namespace Nizam.Scheduling.Services;

public interface ISchedulingEngine
{
    ScheduleResult Calculate(ScheduleInput input);
}

public sealed class SchedulingEngine : ISchedulingEngine
{
    public ScheduleResult Calculate(ScheduleInput input)
    {
        if (input is null)
            return ScheduleResult.Fail("Schedule input is required.");

        if (input.Activities is null || input.Activities.Count == 0)
            return ScheduleResult.Fail("Schedule input must contain at least one activity.");

        var calendar = input.Calendar ?? WorkingCalendar.CreateStandard5x8();
        var relationships = input.Relationships ?? Array.Empty<ScheduleRelationshipInput>();

        var nodes = input.Activities.Select(a =>
        {
            var duration = ResolveDuration(a);
            return new ActivityNode
            {
                Id = a.Id,
                DurationMinutes = duration,
                ActualStart = a.ActualStart,
                ActualFinish = a.ActualFinish,
                ConstraintType = a.ConstraintType,
                ConstraintDate = a.ConstraintDate
            };
        }).ToList();

        var nodeById = nodes.ToDictionary(n => n.Id);
        var edges = new List<DependencyEdge>();

        foreach (var rel in relationships)
        {
            if (!nodeById.ContainsKey(rel.PredecessorId) || !nodeById.ContainsKey(rel.SuccessorId))
                return ScheduleResult.Fail(
                    $"Relationship references unknown activity ('{rel.PredecessorId}' -> '{rel.SuccessorId}').");

            if (rel.PredecessorId == rel.SuccessorId)
                return ScheduleResult.Fail("Self-dependency relationships are not allowed.");

            edges.Add(new DependencyEdge
            {
                PredecessorId = rel.PredecessorId,
                SuccessorId = rel.SuccessorId,
                Type = rel.Type,
                LagMinutes = rel.LagMinutes
            });
        }

        if (CycleDetector.HasCycle(nodes, edges))
            return ScheduleResult.Fail("Activity relationship graph contains a cycle.");

        IReadOnlyList<ActivityNode> order;
        try
        {
            order = TopologicalSorter.Sort(nodes, edges);
        }
        catch (InvalidOperationException ex)
        {
            return ScheduleResult.Fail(ex.Message);
        }

        var projectStart = DateTime.SpecifyKind(
            new DateTime(
                input.ProjectStart.Year,
                input.ProjectStart.Month,
                input.ProjectStart.Day,
                input.ProjectStart.Hour,
                input.ProjectStart.Minute,
                0),
            DateTimeKind.Unspecified);

        ForwardPassCalculator.Calculate(order, nodeById, edges, calendar, projectStart, input.DataDate);

        var projectFinish = nodes.Max(n => n.EarlyFinish);
        BackwardPassCalculator.Calculate(order, nodeById, edges, calendar, projectFinish);
        FloatCalculator.Calculate(nodes, nodeById, edges, calendar);
        CriticalPathCalculator.Calculate(nodes, input.CriticalFloatThresholdMinutes);
        LongestPathCalculator.Calculate(nodes, nodeById, edges, calendar, projectFinish);

        var results = nodes.Select(n => new ActivityScheduleResult
        {
            Id = n.Id,
            EarlyStart = n.EarlyStart,
            EarlyFinish = n.EarlyFinish,
            LateStart = n.LateStart,
            LateFinish = n.LateFinish,
            TotalFloatMinutes = n.TotalFloatMinutes,
            FreeFloatMinutes = n.FreeFloatMinutes,
            IsCritical = n.IsCritical,
            IsLongestPath = n.IsLongestPath,
            RemainingStart = n.RemainingStart,
            RemainingFinish = n.RemainingFinish
        }).ToList();

        return new ScheduleResult
        {
            Success = true,
            Activities = results,
            ProjectFinish = projectFinish,
            CriticalActivityIds = results.Where(r => r.IsCritical).Select(r => r.Id).ToList()
        };
    }

    private static int ResolveDuration(ScheduleActivityInput activity)
    {
        if (activity.ActualFinish is not null)
            return 0;

        if (activity.ActualStart is not null)
            return activity.RemainingDurationMinutes ?? activity.DurationMinutes;

        return Math.Max(0, activity.DurationMinutes);
    }
}
