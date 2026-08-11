using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Automation.Contracts.Events;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;

namespace Nizam.Application.Progress;

public sealed record ProgressUpdateDto(
    Guid Id,
    Guid ActivityId,
    DateTime? ActualStart,
    DateTime? ActualFinish,
    decimal PercentComplete,
    int? RemainingDurationMinutes,
    string? Notes,
    DateTime CreatedAt);

public sealed record CreateProgressUpdateCommand(
    Guid ProjectId,
    Guid ActivityId,
    DateTime? ActualStart,
    DateTime? ActualFinish,
    decimal PercentComplete,
    int? RemainingDurationMinutes,
    string? Notes) : IRequest<ProgressUpdateDto>;

public sealed class CreateProgressUpdateValidator : AbstractValidator<CreateProgressUpdateCommand>
{
    public CreateProgressUpdateValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
        RuleFor(x => x.ActivityId).NotEmpty().WithMessage("Aktivite zorunludur.");
        RuleFor(x => x.PercentComplete).InclusiveBetween(0, 100)
            .WithMessage("İlerleme yüzdesi 0 ile 100 arasında olmalıdır.");
    }
}

public sealed class CreateProgressUpdateHandler : IRequestHandler<CreateProgressUpdateCommand, ProgressUpdateDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IOutboxWriter _outbox;
    private readonly IAuditService _audit;

    public CreateProgressUpdateHandler(
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

    public async Task<ProgressUpdateDto> Handle(CreateProgressUpdateCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var activity = await _db.Activities.FirstOrDefaultAsync(
            a => a.Id == request.ActivityId && a.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Aktivite bulunamadı.");

        var now = _clock.UtcNow;
        var update = new ProgressUpdate
        {
            Id = Guid.NewGuid(),
            OrganizationId = project.OrganizationId,
            ProjectId = request.ProjectId,
            ActivityId = request.ActivityId,
            ActualStart = request.ActualStart,
            ActualFinish = request.ActualFinish,
            PercentComplete = request.PercentComplete,
            RemainingDurationMinutes = request.RemainingDurationMinutes,
            Notes = request.Notes,
            CreatedAt = now,
            CreatedByUserId = _currentUser.UserId
        };
        _db.ProgressUpdates.Add(update);

        if (request.ActualStart is not null)
            activity.ActualStart = request.ActualStart;
        if (request.ActualFinish is not null)
        {
            activity.ActualFinish = request.ActualFinish;
            activity.RemainingDurationMinutes = 0;
            activity.PercentComplete = 100;
            activity.Status = ActivityStatus.Completed;
        }
        else
        {
            activity.PercentComplete = request.PercentComplete;
            if (request.RemainingDurationMinutes is not null)
                activity.RemainingDurationMinutes = request.RemainingDurationMinutes.Value;
            activity.Status = request.PercentComplete > 0
                ? ActivityStatus.InProgress
                : ActivityStatus.NotStarted;
        }

        activity.UpdatedAt = now;
        activity.RowVersion = Guid.NewGuid().ToByteArray();

        // Roll up simple project % as average of activities
        var all = await _db.Activities.Where(a => a.ProjectId == request.ProjectId).ToListAsync(cancellationToken);
        // include current activity in-memory state
        if (all.Count > 0)
            project.PercentComplete = all.Average(a => a.PercentComplete);
        project.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            project.OrganizationId,
            nameof(ProgressUpdate),
            update.Id.ToString(),
            "Created",
            projectId: project.Id,
            newValue: request.PercentComplete.ToString("0.##"),
            cancellationToken: cancellationToken);

        await _outbox.EnqueueAsync("PROGRESS_UPDATED", new ProgressUpdatedEvent
        {
            OrganizationId = project.OrganizationId,
            ProjectId = project.Id,
            ActivityId = activity.Id,
            ProgressUpdateId = update.Id,
            PercentComplete = activity.PercentComplete,
            UpdatedAt = now
        }, cancellationToken);

        return new ProgressUpdateDto(
            update.Id, update.ActivityId, update.ActualStart, update.ActualFinish,
            update.PercentComplete, update.RemainingDurationMinutes, update.Notes, update.CreatedAt);
    }
}
