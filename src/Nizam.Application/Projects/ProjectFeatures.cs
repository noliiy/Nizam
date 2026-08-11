using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Automation.Contracts.Events;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;

namespace Nizam.Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid OrganizationId,
    string ProjectCode,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime? DataDate,
    DateTime? CurrentFinishDate,
    Guid? DefaultCalendarId,
    decimal PercentComplete);

public sealed record CreateProjectCommand(
    Guid OrganizationId,
    string ProjectCode,
    string Name,
    string? Description) : IRequest<ProjectDto>;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organizasyon zorunludur.");
        RuleFor(x => x.ProjectCode).NotEmpty().WithMessage("Proje kodu zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Proje adı zorunludur.");
    }
}

public sealed class CreateProjectHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly IAuditService _audit;
    private readonly IOutboxWriter _outbox;
    private readonly ICurrentUser _currentUser;

    public CreateProjectHandler(
        IApplicationDbContext db,
        IDateTime clock,
        IAuditService audit,
        IOutboxWriter outbox,
        ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
        _outbox = outbox;
        _currentUser = currentUser;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        await TenantAuthorization.EnsureOrganizationAccessAsync(
            _db, _currentUser, request.OrganizationId, cancellationToken);

        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);
        if (!orgExists)
            throw new NotFoundException("Organizasyon bulunamadı.");

        var codeTaken = await _db.Projects.AnyAsync(
            p => p.OrganizationId == request.OrganizationId && p.ProjectCode == request.ProjectCode,
            cancellationToken);
        if (codeTaken)
            throw new ConflictException("Bu proje kodu zaten kullanılıyor.");

        var now = _clock.UtcNow;
        var projectId = Guid.NewGuid();
        var calendarId = Guid.NewGuid();

        var calendar = new Calendar
        {
            Id = calendarId,
            OrganizationId = request.OrganizationId,
            ProjectId = projectId,
            Name = "Standard 5x8",
            Description = "Pazartesi–Cuma 08:00–12:00 / 13:00–17:00",
            IsDefault = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Calendars.Add(calendar);

        for (var d = DayOfWeek.Monday; d <= DayOfWeek.Friday; d++)
        {
            var patternId = Guid.NewGuid();
            _db.CalendarWorkPatterns.Add(new CalendarWorkPattern
            {
                Id = patternId,
                CalendarId = calendarId,
                DayOfWeek = d,
                IsWorking = true
            });
            _db.CalendarWorkIntervals.Add(new CalendarWorkInterval
            {
                Id = Guid.NewGuid(),
                CalendarWorkPatternId = patternId,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            });
            _db.CalendarWorkIntervals.Add(new CalendarWorkInterval
            {
                Id = Guid.NewGuid(),
                CalendarWorkPatternId = patternId,
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });
        }

        foreach (var weekend in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            _db.CalendarWorkPatterns.Add(new CalendarWorkPattern
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId,
                DayOfWeek = weekend,
                IsWorking = false
            });
        }

        var project = new Project
        {
            Id = projectId,
            OrganizationId = request.OrganizationId,
            ProjectCode = request.ProjectCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = ProjectStatus.Planning,
            Priority = 0,
            DefaultCalendarId = calendarId,
            Currency = "TRY",
            Timezone = "Europe/Istanbul",
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        _db.Projects.Add(project);

        _db.WbsNodes.Add(new WbsNode
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            ProjectId = projectId,
            ParentWbsId = null,
            WbsCode = "1",
            Name = project.Name,
            SortOrder = 0,
            Progress = 0,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _audit.WriteAsync(
            request.OrganizationId,
            nameof(Project),
            projectId.ToString(),
            "Created",
            newValue: project.Name,
            projectId: projectId,
            cancellationToken: cancellationToken);

        await _outbox.EnqueueAsync("PROJECT_CREATED", new ProjectCreatedEvent
        {
            OrganizationId = request.OrganizationId,
            ProjectId = projectId,
            ProjectCode = project.ProjectCode,
            Name = project.Name,
            CreatedAt = now
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    internal static ProjectDto Map(Project p) => new(
        p.Id, p.OrganizationId, p.ProjectCode, p.Name, p.Description, p.Status,
        p.DataDate, p.CurrentFinishDate, p.DefaultCalendarId, p.PercentComplete);
}

public sealed record GetProjectQuery(Guid ProjectId) : IRequest<ProjectDto>;

public sealed class GetProjectHandler : IRequestHandler<GetProjectQuery, ProjectDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetProjectHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProjectDto> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var p = await TenantAuthorization.EnsureProjectAccessAsync(
            _db, _currentUser, request.ProjectId, cancellationToken, tracking: false);
        return CreateProjectHandler.Map(p);
    }
}

public sealed record ListProjectsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<ProjectDto>>;

public sealed class ListProjectsHandler : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListProjectsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        await TenantAuthorization.EnsureOrganizationAccessAsync(
            _db, _currentUser, request.OrganizationId, cancellationToken);

        var list = await _db.Projects.AsNoTracking()
            .Where(p => p.OrganizationId == request.OrganizationId)
            .OrderBy(p => p.ProjectCode)
            .ToListAsync(cancellationToken);
        return list.Select(CreateProjectHandler.Map).ToList();
    }
}

public sealed record SetDataDateCommand(Guid ProjectId, DateTime DataDate) : IRequest<ProjectDto>;

public sealed class SetDataDateValidator : AbstractValidator<SetDataDateCommand>
{
    public SetDataDateValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
    }
}

public sealed class SetDataDateHandler : IRequestHandler<SetDataDateCommand, ProjectDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _currentUser;

    public SetDataDateHandler(IApplicationDbContext db, IDateTime clock, IAuditService audit, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<ProjectDto> Handle(SetDataDateCommand request, CancellationToken cancellationToken)
    {
        var project = await TenantAuthorization.EnsureProjectAccessAsync(
            _db, _currentUser, request.ProjectId, cancellationToken);

        var old = project.DataDate?.ToString("O");
        project.DataDate = DateTime.SpecifyKind(request.DataDate, DateTimeKind.Unspecified);
        project.UpdatedAt = _clock.UtcNow;

        await _audit.WriteAsync(
            project.OrganizationId,
            nameof(Project),
            project.Id.ToString(),
            "DataDateUpdated",
            old,
            project.DataDate?.ToString("O"),
            project.Id,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return CreateProjectHandler.Map(project);
    }
}
