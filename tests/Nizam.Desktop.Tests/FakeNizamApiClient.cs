using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.Tests;

internal sealed class FakeNizamApiClient : INizamApiClient
{
    public int LoginCalls { get; private set; }
    public int ScheduleRuns { get; private set; }
    public List<CreateActivityRequest> CreatedActivities { get; } = new();
    public List<CreateRelationshipRequest> Relationships { get; } = new();
    public List<ActivityDto> Activities { get; } = new();
    public List<ProjectDto> Projects { get; } = new();
    public List<WbsNodeDto> WbsNodes { get; } = new();

    public Task<LoginResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        LoginCalls++;
        return Task.FromResult(new LoginResponseDto
        {
            Token = "fake-token",
            UserId = Guid.NewGuid(),
            Email = email,
            DisplayName = "Test",
            OrganizationId = Guid.Parse("99999999-9999-9999-9999-999999999999")
        });
    }

    public Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProjectDto>>(Projects);

    public Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var p = new ProjectDto
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            ProjectCode = request.ProjectCode,
            Name = request.Name
        };
        Projects.Add(p);
        return Task.FromResult(p);
    }

    public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ActivityDto>>(Activities.Where(a => a.ProjectId == projectId || Activities.Count > 0).ToList());

    public Task<ActivityDto> CreateActivityAsync(Guid projectId, CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        CreatedActivities.Add(request);
        var dto = new ActivityDto
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            WbsId = request.WbsId,
            ActivityCode = request.ActivityCode,
            Name = request.Name,
            OriginalDurationMinutes = request.OriginalDurationMinutes,
            RemainingDurationMinutes = request.OriginalDurationMinutes
        };
        Activities.Add(dto);
        return Task.FromResult(dto);
    }

    public Task<ActivityDto> UpdateActivityAsync(Guid projectId, Guid activityId, UpdateActivityRequest request, CancellationToken cancellationToken = default)
    {
        var existing = Activities.First(a => a.Id == activityId);
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.OriginalDurationMinutes = request.OriginalDurationMinutes;
        existing.RemainingDurationMinutes = request.RemainingDurationMinutes;
        return Task.FromResult(existing);
    }

    public Task<RelationshipDto> CreateRelationshipAsync(Guid projectId, CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        Relationships.Add(request);
        return Task.FromResult(new RelationshipDto
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            PredecessorActivityId = request.PredecessorActivityId,
            SuccessorActivityId = request.SuccessorActivityId,
            RelationshipType = request.RelationshipType,
            LagMinutes = request.LagMinutes
        });
    }

    public Task<ScheduleRunDto> RunScheduleAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ScheduleRuns++;
        return Task.FromResult(new ScheduleRunDto
        {
            RunId = Guid.NewGuid(),
            ProjectId = projectId,
            Status = 1,
            ActivityCount = Activities.Count
        });
    }

    public Task<ProjectDto> SetDataDateAsync(Guid projectId, DateTime dataDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectDto { Id = projectId, DataDate = dataDate });

    public Task<ProgressUpdateDto> CreateProgressAsync(Guid projectId, CreateProgressRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProgressUpdateDto
        {
            Id = Guid.NewGuid(),
            ActivityId = request.ActivityId,
            PercentComplete = request.PercentComplete,
            CreatedAt = DateTime.UtcNow
        });

    public Task<BaselineDto> CreateBaselineAsync(Guid projectId, CreateBaselineRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new BaselineDto
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        });

    public Task<BaselineCompareDto> CompareBaselineAsync(Guid projectId, Guid baselineId, CancellationToken cancellationToken = default)
        => Task.FromResult(new BaselineCompareDto
        {
            BaselineId = baselineId,
            ProjectId = projectId,
            BaselineName = "BL",
            Variances = Array.Empty<ActivityVarianceDto>()
        });

    public Task<ProjectDashboardDto> GetDashboardAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectDashboardDto { ProjectId = projectId, Name = "Demo" });

    public Task<IReadOnlyList<WbsNodeDto>> GetWbsAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WbsNodeDto>>(WbsNodes);

    public Task<WbsNodeDto> CreateWbsAsync(Guid projectId, CreateWbsRequest request, CancellationToken cancellationToken = default)
    {
        var n = new WbsNodeDto
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            WbsCode = request.WbsCode,
            Name = request.Name,
            ParentWbsId = request.ParentWbsId
        };
        WbsNodes.Add(n);
        return Task.FromResult(n);
    }
}
