using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;

namespace Nizam.Application.Activities;

public sealed record ActivityDto(
    Guid Id,
    Guid ProjectId,
    Guid WbsId,
    string ActivityCode,
    string Name,
    string? Description,
    ActivityType ActivityType,
    ActivityStatus Status,
    int OriginalDurationMinutes,
    int RemainingDurationMinutes,
    DateTime? EarlyStart,
    DateTime? EarlyFinish,
    DateTime? LateStart,
    DateTime? LateFinish,
    int TotalFloatMinutes,
    int FreeFloatMinutes,
    bool IsCritical,
    bool IsLongestPath,
    decimal PercentComplete,
    DateTime? ActualStart,
    DateTime? ActualFinish);

public sealed record CreateActivityCommand(
    Guid ProjectId,
    Guid WbsId,
    string ActivityCode,
    string Name,
    string? Description,
    int OriginalDurationMinutes,
    ActivityType ActivityType = ActivityType.TaskDependent) : IRequest<ActivityDto>;

public sealed class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
        RuleFor(x => x.WbsId).NotEmpty().WithMessage("WBS zorunludur.");
        RuleFor(x => x.ActivityCode).NotEmpty().WithMessage("Aktivite kodu zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Aktivite adı zorunludur.");
        RuleFor(x => x.OriginalDurationMinutes).GreaterThanOrEqualTo(0)
            .WithMessage("Süre negatif olamaz.");
    }
}

public sealed class CreateActivityHandler : IRequestHandler<CreateActivityCommand, ActivityDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public CreateActivityHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ActivityDto> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var wbsOk = await _db.WbsNodes.AnyAsync(
            w => w.Id == request.WbsId && w.ProjectId == request.ProjectId, cancellationToken);
        if (!wbsOk)
            throw new NotFoundException("WBS bulunamadı.");

        var codeTaken = await _db.Activities.AnyAsync(
            a => a.ProjectId == request.ProjectId && a.ActivityCode == request.ActivityCode,
            cancellationToken);
        if (codeTaken)
            throw new ConflictException("Bu aktivite kodu zaten kullanılıyor.");

        var now = _clock.UtcNow;
        var duration = request.OriginalDurationMinutes;
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            OrganizationId = project.OrganizationId,
            ProjectId = request.ProjectId,
            WbsId = request.WbsId,
            ActivityCode = request.ActivityCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            ActivityType = request.ActivityType,
            Status = ActivityStatus.NotStarted,
            OriginalDurationMinutes = duration,
            RemainingDurationMinutes = duration,
            PercentComplete = 0,
            ConstraintType = ConstraintType.None,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(activity);
    }

    internal static ActivityDto Map(Activity a) => new(
        a.Id, a.ProjectId, a.WbsId, a.ActivityCode, a.Name, a.Description,
        a.ActivityType, a.Status, a.OriginalDurationMinutes, a.RemainingDurationMinutes,
        a.EarlyStart, a.EarlyFinish, a.LateStart, a.LateFinish,
        a.TotalFloatMinutes, a.FreeFloatMinutes, a.IsCritical, a.IsLongestPath,
        a.PercentComplete, a.ActualStart, a.ActualFinish);
}

public sealed record UpdateActivityCommand(
    Guid ProjectId,
    Guid ActivityId,
    string Name,
    string? Description,
    int OriginalDurationMinutes,
    int RemainingDurationMinutes,
    Guid? WbsId = null) : IRequest<ActivityDto>;

public sealed class UpdateActivityValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Aktivite adı zorunludur.");
        RuleFor(x => x.OriginalDurationMinutes).GreaterThanOrEqualTo(0)
            .WithMessage("Süre negatif olamaz.");
        RuleFor(x => x.RemainingDurationMinutes).GreaterThanOrEqualTo(0)
            .WithMessage("Kalan süre negatif olamaz.");
    }
}

public sealed class UpdateActivityHandler : IRequestHandler<UpdateActivityCommand, ActivityDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public UpdateActivityHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ActivityDto> Handle(UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(
            a => a.Id == request.ActivityId && a.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Aktivite bulunamadı.");

        if (request.WbsId is not null)
        {
            var wbsOk = await _db.WbsNodes.AnyAsync(
                w => w.Id == request.WbsId && w.ProjectId == request.ProjectId, cancellationToken);
            if (!wbsOk)
                throw new NotFoundException("WBS bulunamadı.");
            activity.WbsId = request.WbsId.Value;
        }

        activity.Name = request.Name.Trim();
        activity.Description = request.Description;
        activity.OriginalDurationMinutes = request.OriginalDurationMinutes;
        activity.RemainingDurationMinutes = request.RemainingDurationMinutes;
        activity.UpdatedAt = _clock.UtcNow;
        activity.RowVersion = Guid.NewGuid().ToByteArray();
        await _db.SaveChangesAsync(cancellationToken);
        return CreateActivityHandler.Map(activity);
    }
}

public sealed record ListActivitiesQuery(Guid ProjectId) : IRequest<IReadOnlyList<ActivityDto>>;

public sealed class ListActivitiesHandler : IRequestHandler<ListActivitiesQuery, IReadOnlyList<ActivityDto>>
{
    private readonly IApplicationDbContext _db;

    public ListActivitiesHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ActivityDto>> Handle(ListActivitiesQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Activities.AsNoTracking()
            .Where(a => a.ProjectId == request.ProjectId)
            .OrderBy(a => a.ActivityCode)
            .ToListAsync(cancellationToken);
        return list.Select(CreateActivityHandler.Map).ToList();
    }
}
