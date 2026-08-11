using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Automation.Contracts.Events;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;
using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Models;
using Nizam.Scheduling.Services;

namespace Nizam.Application.Scheduling;

public sealed record ScheduleRunDto(
    Guid RunId,
    Guid ProjectId,
    DateTime? ProjectFinish,
    ScheduleRunStatus Status,
    int ActivityCount,
    string? Error);

public sealed record RunScheduleCommand(Guid ProjectId) : IRequest<ScheduleRunDto>;

public sealed class RunScheduleValidator : AbstractValidator<RunScheduleCommand>
{
    public RunScheduleValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
    }
}

public sealed class RunScheduleHandler : IRequestHandler<RunScheduleCommand, ScheduleRunDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly ISchedulingEngine _engine;
    private readonly IAuditService _audit;
    private readonly IOutboxWriter _outbox;
    private readonly ICurrentUser _currentUser;

    public RunScheduleHandler(
        IApplicationDbContext db,
        IDateTime clock,
        ISchedulingEngine engine,
        IAuditService audit,
        IOutboxWriter outbox,
        ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _engine = engine;
        _audit = audit;
        _outbox = outbox;
        _currentUser = currentUser;
    }

    public async Task<ScheduleRunDto> Handle(RunScheduleCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var activities = await _db.Activities
            .Where(a => a.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        if (activities.Count == 0)
            throw new Common.ValidationException("Zamanlama için en az bir aktivite gerekir.");

        var relationships = await _db.ActivityRelationships
            .Where(r => r.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var calendar = WorkingCalendar.CreateStandard5x8();
        if (project.DefaultCalendarId is not null)
        {
            var loaded = await LoadWorkingCalendarAsync(project.DefaultCalendarId.Value, cancellationToken);
            if (loaded is not null)
                calendar = loaded;
        }

        var projectStart = project.PlannedStartDate
            ?? project.CurrentStartDate
            ?? activities.Min(a => a.PlannedStart) 
            ?? DateTime.SpecifyKind(new DateTime(2024, 1, 1, 8, 0, 0), DateTimeKind.Unspecified);

        if (projectStart.TimeOfDay == TimeSpan.Zero)
            projectStart = projectStart.Date.AddHours(8);

        projectStart = DateTime.SpecifyKind(
            new DateTime(projectStart.Year, projectStart.Month, projectStart.Day,
                projectStart.Hour, projectStart.Minute, 0),
            DateTimeKind.Unspecified);

        var input = new ScheduleInput
        {
            ProjectStart = projectStart,
            DataDate = project.DataDate,
            Calendar = calendar,
            Activities = activities.Select(a => new ScheduleActivityInput
            {
                Id = a.Id,
                DurationMinutes = a.RemainingDurationMinutes > 0 || a.ActualFinish is not null
                    ? a.RemainingDurationMinutes
                    : a.OriginalDurationMinutes,
                ActualStart = a.ActualStart,
                ActualFinish = a.ActualFinish,
                RemainingDurationMinutes = a.RemainingDurationMinutes,
                ConstraintType = (int)a.ConstraintType,
                ConstraintDate = a.ConstraintDate
            }).ToList(),
            Relationships = relationships.Select(r => new ScheduleRelationshipInput
            {
                PredecessorId = r.PredecessorActivityId,
                SuccessorId = r.SuccessorActivityId,
                Type = (int)r.RelationshipType,
                LagMinutes = r.LagMinutes
            }).ToList()
        };

        var started = _clock.UtcNow;
        var run = new ScheduleRun
        {
            RunId = Guid.NewGuid(),
            ProjectId = project.Id,
            OrganizationId = project.OrganizationId,
            DataDate = project.DataDate ?? projectStart,
            StartedAt = started,
            Status = ScheduleRunStatus.Running,
            TriggeredBy = _currentUser.UserId,
            ActivityCount = activities.Count,
            RelationshipCount = relationships.Count
        };
        _db.ScheduleRuns.Add(run);

        var result = _engine.Calculate(input);
        var completed = _clock.UtcNow;
        run.CompletedAt = completed;
        run.DurationMs = (long)(completed - started).TotalMilliseconds;

        if (!result.Success)
        {
            run.Status = ScheduleRunStatus.Failed;
            run.Error = result.ErrorMessage;
            await _db.SaveChangesAsync(cancellationToken);
            return new ScheduleRunDto(run.RunId, project.Id, null, run.Status, run.ActivityCount, run.Error);
        }

        var byId = activities.ToDictionary(a => a.Id);
        foreach (var item in result.Activities)
        {
            if (!byId.TryGetValue(item.Id, out var activity))
                continue;

            activity.EarlyStart = item.EarlyStart;
            activity.EarlyFinish = item.EarlyFinish;
            activity.LateStart = item.LateStart;
            activity.LateFinish = item.LateFinish;
            activity.TotalFloatMinutes = item.TotalFloatMinutes;
            activity.FreeFloatMinutes = item.FreeFloatMinutes;
            activity.IsCritical = item.IsCritical;
            activity.IsLongestPath = item.IsLongestPath;
            activity.RemainingStart = item.RemainingStart;
            activity.RemainingFinish = item.RemainingFinish;
            activity.UpdatedAt = completed;
            activity.RowVersion = Guid.NewGuid().ToByteArray();
        }

        // Persist current dates using Early as current for MVP
        foreach (var activity in activities)
        {
            // Planned kept as-is; Early/Late already set
        }

        project.CurrentStartDate = activities.Min(a => a.EarlyStart);
        project.CurrentFinishDate = result.ProjectFinish;
        project.UpdatedAt = completed;
        project.RowVersion = Guid.NewGuid().ToByteArray();

        run.Status = ScheduleRunStatus.Completed;
        run.ProjectFinish = result.ProjectFinish;
        run.Error = null;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            project.OrganizationId,
            nameof(ScheduleRun),
            run.RunId.ToString(),
            "Completed",
            projectId: project.Id,
            newValue: result.ProjectFinish?.ToString("O"),
            cancellationToken: cancellationToken);

        await _outbox.EnqueueAsync("SCHEDULE_CALCULATED", new ScheduleCalculatedEvent
        {
            OrganizationId = project.OrganizationId,
            ProjectId = project.Id,
            RunId = run.RunId,
            ProjectFinish = result.ProjectFinish,
            CriticalCount = result.CriticalActivityIds.Count,
            CalculatedAt = completed
        }, cancellationToken);

        return new ScheduleRunDto(run.RunId, project.Id, run.ProjectFinish, run.Status, run.ActivityCount, null);
    }

    private async Task<WorkingCalendar?> LoadWorkingCalendarAsync(Guid calendarId, CancellationToken ct)
    {
        var patterns = await _db.CalendarWorkPatterns
            .Where(p => p.CalendarId == calendarId)
            .ToListAsync(ct);
        if (patterns.Count == 0)
            return null;

        var patternIds = patterns.Select(p => p.Id).ToList();
        var intervals = await _db.CalendarWorkIntervals
            .Where(i => patternIds.Contains(i.CalendarWorkPatternId))
            .ToListAsync(ct);

        var exceptions = await _db.CalendarExceptions
            .Where(e => e.CalendarId == calendarId)
            .ToListAsync(ct);

        var patternData = patterns.Select(p =>
        {
            var ints = intervals
                .Where(i => i.CalendarWorkPatternId == p.Id)
                .Select(i => (i.StartTime, i.EndTime))
                .ToList() as IReadOnlyList<(TimeSpan, TimeSpan)>;
            if (!p.IsWorking)
                ints = Array.Empty<(TimeSpan, TimeSpan)>();
            return (p.DayOfWeek, ints);
        }).ToList();

        var exceptionMap = exceptions.ToDictionary(
            e => e.Date,
            e => e.IsWorking
                ? (IReadOnlyList<(TimeSpan, TimeSpan)>)new List<(TimeSpan, TimeSpan)>
                {
                    (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
                    (new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0))
                }
                : Array.Empty<(TimeSpan, TimeSpan)>());

        return new WorkingCalendar(patternData, exceptionMap);
    }
}
