using Nizam.Desktop.Models;

namespace Nizam.Desktop.Services;

public interface INizamApiClient
{
    Task<LoginResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ActivityDto> CreateActivityAsync(Guid projectId, CreateActivityRequest request, CancellationToken cancellationToken = default);
    Task<ActivityDto> UpdateActivityAsync(Guid projectId, Guid activityId, UpdateActivityRequest request, CancellationToken cancellationToken = default);
    Task<RelationshipDto> CreateRelationshipAsync(Guid projectId, CreateRelationshipRequest request, CancellationToken cancellationToken = default);
    Task<ScheduleRunDto> RunScheduleAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDto> SetDataDateAsync(Guid projectId, DateTime dataDate, CancellationToken cancellationToken = default);
    Task<ProgressUpdateDto> CreateProgressAsync(Guid projectId, CreateProgressRequest request, CancellationToken cancellationToken = default);
    Task<BaselineDto> CreateBaselineAsync(Guid projectId, CreateBaselineRequest request, CancellationToken cancellationToken = default);
    Task<BaselineCompareDto> CompareBaselineAsync(Guid projectId, Guid baselineId, CancellationToken cancellationToken = default);
    Task<ProjectDashboardDto> GetDashboardAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WbsNodeDto>> GetWbsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<WbsNodeDto> CreateWbsAsync(Guid projectId, CreateWbsRequest request, CancellationToken cancellationToken = default);
}
