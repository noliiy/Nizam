using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;
using Nizam.Scheduling.Graph;

namespace Nizam.Application.Relationships;

public sealed record RelationshipDto(
    Guid Id,
    Guid ProjectId,
    Guid PredecessorActivityId,
    Guid SuccessorActivityId,
    RelationshipType RelationshipType,
    int LagMinutes);

public sealed record CreateRelationshipCommand(
    Guid ProjectId,
    Guid PredecessorActivityId,
    Guid SuccessorActivityId,
    RelationshipType RelationshipType,
    int LagMinutes = 0) : IRequest<RelationshipDto>;

public sealed class CreateRelationshipValidator : AbstractValidator<CreateRelationshipCommand>
{
    public CreateRelationshipValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
        RuleFor(x => x.PredecessorActivityId).NotEmpty().WithMessage("Öncül aktivite zorunludur.");
        RuleFor(x => x.SuccessorActivityId).NotEmpty().WithMessage("Ardıl aktivite zorunludur.");
    }
}

public sealed class CreateRelationshipHandler : IRequestHandler<CreateRelationshipCommand, RelationshipDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public CreateRelationshipHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<RelationshipDto> Handle(CreateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var activities = await _db.Activities
            .Where(a => a.ProjectId == request.ProjectId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (!activities.Contains(request.PredecessorActivityId) || !activities.Contains(request.SuccessorActivityId))
            throw new NotFoundException("Aktivite bulunamadı.");

        // Domain self-dependency check
        var rel = ActivityRelationship.Create(
            project.OrganizationId,
            request.ProjectId,
            request.PredecessorActivityId,
            request.SuccessorActivityId,
            request.RelationshipType,
            request.LagMinutes,
            createdAt: _clock.UtcNow);

        var existingRels = await _db.ActivityRelationships
            .Where(r => r.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var nodes = activities.Select(id => new ActivityNode { Id = id }).ToList();
        var edges = existingRels
            .Select(r => new DependencyEdge
            {
                PredecessorId = r.PredecessorActivityId,
                SuccessorId = r.SuccessorActivityId,
                Type = (int)r.RelationshipType,
                LagMinutes = r.LagMinutes
            })
            .Append(new DependencyEdge
            {
                PredecessorId = rel.PredecessorActivityId,
                SuccessorId = rel.SuccessorActivityId,
                Type = (int)rel.RelationshipType,
                LagMinutes = rel.LagMinutes
            })
            .ToList();

        if (CycleDetector.HasCycle(nodes, edges))
            throw new ConflictException("Bu ilişki döngüsel bağımlılık oluşturur.");

        _db.ActivityRelationships.Add(rel);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(rel);
    }

    internal static RelationshipDto Map(ActivityRelationship r) => new(
        r.Id, r.ProjectId, r.PredecessorActivityId, r.SuccessorActivityId, r.RelationshipType, r.LagMinutes);
}

public sealed record DeleteRelationshipCommand(Guid ProjectId, Guid RelationshipId) : IRequest<Unit>;

public sealed class DeleteRelationshipHandler : IRequestHandler<DeleteRelationshipCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public DeleteRelationshipHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteRelationshipCommand request, CancellationToken cancellationToken)
    {
        var rel = await _db.ActivityRelationships.FirstOrDefaultAsync(
            r => r.Id == request.RelationshipId && r.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("İlişki bulunamadı.");

        _db.ActivityRelationships.Remove(rel);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
