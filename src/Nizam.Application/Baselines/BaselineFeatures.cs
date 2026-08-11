using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Automation.Contracts.Events;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;
using Nizam.Scheduling.Calendars;

namespace Nizam.Application.Baselines;

public sealed record BaselineDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    BaselineType BaselineType,
    BaselineStatus Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt);

public sealed record ActivityVarianceDto(
    Guid ActivityId,
    string ActivityCode,
    string Name,
    DateTime? BaselineEarlyStart,
    DateTime? CurrentEarlyStart,
    DateTime? BaselineEarlyFinish,
    DateTime? CurrentEarlyFinish,
    int? StartVarianceMinutes,
    int? FinishVarianceMinutes,
    int BaselineDurationMinutes,
    int CurrentDurationMinutes);

public sealed record BaselineCompareDto(
    Guid BaselineId,
    Guid ProjectId,
    string BaselineName,
    IReadOnlyList<ActivityVarianceDto> Variances);

public sealed record CreateBaselineCommand(
    Guid ProjectId,
    string Name,
    BaselineType BaselineType = BaselineType.Approved) : IRequest<BaselineDto>;

public sealed class CreateBaselineValidator : AbstractValidator<CreateBaselineCommand>
{
    public CreateBaselineValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Baseline adı zorunludur.");
    }
}

public sealed class CreateBaselineHandler : IRequestHandler<CreateBaselineCommand, BaselineDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IOutboxWriter _outbox;
    private readonly IAuditService _audit;

    public CreateBaselineHandler(
        IApplicationDbContext db,
        IDateTime clock,
        ICurrentUser currentUser,
        IOutboxWriter outbox,
        IAuditService audit)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _outbox = outbox;
        _audit = audit;
    }

    public async Task<BaselineDto> Handle(CreateBaselineCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var now = _clock.UtcNow;
        var baseline = new Baseline
        {
            Id = Guid.NewGuid(),
            OrganizationId = project.OrganizationId,
            ProjectId = project.Id,
            Name = request.Name.Trim(),
            BaselineType = request.BaselineType,
            Status = BaselineStatus.Approved,
            CreatedAt = now,
            ApprovedAt = now,
            CreatedByUserId = _currentUser.UserId
        };
        _db.Baselines.Add(baseline);

        var wbsNodes = await _db.WbsNodes.Where(w => w.ProjectId == project.Id).ToListAsync(cancellationToken);
        foreach (var w in wbsNodes)
        {
            _db.BaselineWbsNodes.Add(new BaselineWbsNode
            {
                Id = Guid.NewGuid(),
                BaselineId = baseline.Id,
                OrganizationId = project.OrganizationId,
                ProjectId = project.Id,
                WbsNodeId = w.Id,
                ParentWbsId = w.ParentWbsId,
                WbsCode = w.WbsCode,
                Name = w.Name,
                Description = w.Description,
                SortOrder = w.SortOrder,
                PlannedStart = w.PlannedStart,
                PlannedFinish = w.PlannedFinish,
                CurrentStart = w.CurrentStart,
                CurrentFinish = w.CurrentFinish,
                Progress = w.Progress
            });
        }

        var activities = await _db.Activities.Where(a => a.ProjectId == project.Id).ToListAsync(cancellationToken);
        foreach (var a in activities)
        {
            _db.BaselineActivities.Add(new BaselineActivity
            {
                Id = Guid.NewGuid(),
                BaselineId = baseline.Id,
                OrganizationId = project.OrganizationId,
                ProjectId = project.Id,
                ActivityId = a.Id,
                WbsId = a.WbsId,
                ActivityCode = a.ActivityCode,
                Name = a.Name,
                ActivityType = a.ActivityType,
                OriginalDurationMinutes = a.OriginalDurationMinutes,
                RemainingDurationMinutes = a.RemainingDurationMinutes,
                PlannedStart = a.PlannedStart,
                PlannedFinish = a.PlannedFinish,
                EarlyStart = a.EarlyStart,
                EarlyFinish = a.EarlyFinish,
                LateStart = a.LateStart,
                LateFinish = a.LateFinish,
                ActualStart = a.ActualStart,
                ActualFinish = a.ActualFinish,
                TotalFloatMinutes = a.TotalFloatMinutes,
                FreeFloatMinutes = a.FreeFloatMinutes,
                PercentComplete = a.PercentComplete,
                IsCritical = a.IsCritical,
                ConstraintType = a.ConstraintType,
                ConstraintDate = a.ConstraintDate
            });
        }

        await _audit.WriteAsync(
            project.OrganizationId,
            nameof(Baseline),
            baseline.Id.ToString(),
            "Created",
            projectId: project.Id,
            newValue: baseline.Name,
            cancellationToken: cancellationToken);

        await _outbox.EnqueueAsync("BASELINE_CREATED", new BaselineCreatedEvent
        {
            OrganizationId = project.OrganizationId,
            ProjectId = project.Id,
            BaselineId = baseline.Id,
            Name = baseline.Name,
            CreatedAt = now
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new BaselineDto(
            baseline.Id, baseline.ProjectId, baseline.Name,
            baseline.BaselineType, baseline.Status, baseline.CreatedAt, baseline.ApprovedAt);
    }
}

public sealed record CompareBaselineQuery(Guid ProjectId, Guid BaselineId) : IRequest<BaselineCompareDto>;

public sealed class CompareBaselineHandler : IRequestHandler<CompareBaselineQuery, BaselineCompareDto>
{
    private readonly IApplicationDbContext _db;

    public CompareBaselineHandler(IApplicationDbContext db) => _db = db;

    public async Task<BaselineCompareDto> Handle(CompareBaselineQuery request, CancellationToken cancellationToken)
    {
        var baseline = await _db.Baselines.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BaselineId && b.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Baseline bulunamadı.");

        var snaps = await _db.BaselineActivities.AsNoTracking()
            .Where(b => b.BaselineId == baseline.Id)
            .ToListAsync(cancellationToken);

        var current = await _db.Activities.AsNoTracking()
            .Where(a => a.ProjectId == request.ProjectId)
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var variances = new List<ActivityVarianceDto>();
        var calendar = WorkingCalendar.CreateStandard5x8();
        foreach (var snap in snaps)
        {
            current.TryGetValue(snap.ActivityId, out var act);
            int? startVar = null;
            int? finishVar = null;
            if (snap.EarlyStart is not null && act?.EarlyStart is not null)
                startVar = WorkingVarianceMinutes(calendar, snap.EarlyStart.Value, act.EarlyStart.Value);
            if (snap.EarlyFinish is not null && act?.EarlyFinish is not null)
                finishVar = WorkingVarianceMinutes(calendar, snap.EarlyFinish.Value, act.EarlyFinish.Value);

            variances.Add(new ActivityVarianceDto(
                snap.ActivityId,
                snap.ActivityCode,
                act?.Name ?? snap.Name,
                snap.EarlyStart,
                act?.EarlyStart,
                snap.EarlyFinish,
                act?.EarlyFinish,
                startVar,
                finishVar,
                snap.OriginalDurationMinutes,
                act?.OriginalDurationMinutes ?? snap.OriginalDurationMinutes));
        }

        return new BaselineCompareDto(baseline.Id, baseline.ProjectId, baseline.Name, variances);
    }

    /// <summary>
    /// Signed working-time variance: positive means current is later than baseline.
    /// </summary>
    public static int WorkingVarianceMinutes(WorkingCalendar calendar, DateTime baseline, DateTime current)
    {
        if (current >= baseline)
            return calendar.CalculateWorkingDuration(baseline, current);
        return -calendar.CalculateWorkingDuration(current, baseline);
    }
}
