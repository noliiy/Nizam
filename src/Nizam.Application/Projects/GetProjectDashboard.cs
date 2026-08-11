using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;

namespace Nizam.Application.Projects;

public sealed record ProjectDashboardDto(
    Guid ProjectId,
    string Name,
    decimal PercentComplete,
    DateTime? ForecastFinish,
    DateTime? DataDate,
    int ActivityCount,
    int CriticalCount,
    int CompletedCount,
    Guid? LatestBaselineId,
    string? LatestBaselineName,
    double? AverageFinishVarianceMinutes);

public sealed record GetProjectDashboardQuery(Guid ProjectId) : IRequest<ProjectDashboardDto>;

public sealed class GetProjectDashboardHandler : IRequestHandler<GetProjectDashboardQuery, ProjectDashboardDto>
{
    private readonly IApplicationDbContext _db;

    public GetProjectDashboardHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProjectDashboardDto> Handle(GetProjectDashboardQuery request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var activities = await _db.Activities.AsNoTracking()
            .Where(a => a.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var baseline = await _db.Baselines.AsNoTracking()
            .Where(b => b.ProjectId == request.ProjectId)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        double? avgVariance = null;
        if (baseline is not null)
        {
            var snaps = await _db.BaselineActivities.AsNoTracking()
                .Where(b => b.BaselineId == baseline.Id && b.EarlyFinish != null)
                .ToListAsync(cancellationToken);

            var vars = new List<double>();
            foreach (var snap in snaps)
            {
                var cur = activities.FirstOrDefault(a => a.Id == snap.ActivityId);
                if (cur?.EarlyFinish is not null && snap.EarlyFinish is not null)
                    vars.Add((cur.EarlyFinish.Value - snap.EarlyFinish.Value).TotalMinutes);
            }

            if (vars.Count > 0)
                avgVariance = vars.Average();
        }

        return new ProjectDashboardDto(
            project.Id,
            project.Name,
            project.PercentComplete,
            project.CurrentFinishDate,
            project.DataDate,
            activities.Count,
            activities.Count(a => a.IsCritical),
            activities.Count(a => a.Status == Domain.Enums.ActivityStatus.Completed),
            baseline?.Id,
            baseline?.Name,
            avgVariance);
    }
}
