namespace Nizam.Desktop.Models;

public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
}

public sealed class ProjectDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTime? DataDate { get; set; }
    public DateTime? CurrentFinishDate { get; set; }
    public Guid? DefaultCalendarId { get; set; }
    public decimal PercentComplete { get; set; }
}

public sealed class CreateProjectRequest
{
    public Guid OrganizationId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class WbsNodeDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentWbsId { get; set; }
    public string WbsCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public decimal Progress { get; set; }
}

public sealed class CreateWbsRequest
{
    public Guid? ParentWbsId { get; set; }
    public string WbsCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ActivityDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WbsId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ActivityType { get; set; }
    public int Status { get; set; }
    public int OriginalDurationMinutes { get; set; }
    public int RemainingDurationMinutes { get; set; }
    public DateTime? EarlyStart { get; set; }
    public DateTime? EarlyFinish { get; set; }
    public DateTime? LateStart { get; set; }
    public DateTime? LateFinish { get; set; }
    public int TotalFloatMinutes { get; set; }
    public int FreeFloatMinutes { get; set; }
    public bool IsCritical { get; set; }
    public bool IsLongestPath { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
}

public sealed class CreateActivityRequest
{
    public Guid WbsId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OriginalDurationMinutes { get; set; }
    public int ActivityType { get; set; }
}

public sealed class UpdateActivityRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OriginalDurationMinutes { get; set; }
    public int RemainingDurationMinutes { get; set; }
    public Guid? WbsId { get; set; }
}

public sealed class RelationshipDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PredecessorActivityId { get; set; }
    public Guid SuccessorActivityId { get; set; }
    public int RelationshipType { get; set; }
    public int LagMinutes { get; set; }
}

public sealed class CreateRelationshipRequest
{
    public Guid PredecessorActivityId { get; set; }
    public Guid SuccessorActivityId { get; set; }
    public int RelationshipType { get; set; }
    public int LagMinutes { get; set; }
}

public sealed class ScheduleRunDto
{
    public Guid RunId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime? ProjectFinish { get; set; }
    public int Status { get; set; }
    public int ActivityCount { get; set; }
    public string? Error { get; set; }
}

public sealed class SetDataDateRequest
{
    public DateTime DataDate { get; set; }
}

public sealed class ProgressUpdateDto
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public decimal PercentComplete { get; set; }
    public int? RemainingDurationMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateProgressRequest
{
    public Guid ActivityId { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public decimal PercentComplete { get; set; }
    public int? RemainingDurationMinutes { get; set; }
    public string? Notes { get; set; }
}

public sealed class BaselineDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BaselineType { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public sealed class CreateBaselineRequest
{
    public string Name { get; set; } = string.Empty;
    public int BaselineType { get; set; }
}

public sealed class ActivityVarianceDto
{
    public Guid ActivityId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? BaselineEarlyStart { get; set; }
    public DateTime? CurrentEarlyStart { get; set; }
    public DateTime? BaselineEarlyFinish { get; set; }
    public DateTime? CurrentEarlyFinish { get; set; }
    public int? StartVarianceMinutes { get; set; }
    public int? FinishVarianceMinutes { get; set; }
    public int BaselineDurationMinutes { get; set; }
    public int CurrentDurationMinutes { get; set; }
}

public sealed class BaselineCompareDto
{
    public Guid BaselineId { get; set; }
    public Guid ProjectId { get; set; }
    public string BaselineName { get; set; } = string.Empty;
    public IReadOnlyList<ActivityVarianceDto> Variances { get; set; } = Array.Empty<ActivityVarianceDto>();
}

public sealed class ProjectDashboardDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PercentComplete { get; set; }
    public DateTime? ForecastFinish { get; set; }
    public DateTime? DataDate { get; set; }
    public int ActivityCount { get; set; }
    public int CriticalCount { get; set; }
    public int CompletedCount { get; set; }
    public Guid? LatestBaselineId { get; set; }
    public string? LatestBaselineName { get; set; }
    public double? AverageFinishVarianceMinutes { get; set; }
}

/// <summary>Bar model for Gantt binding (UI-agnostic).</summary>
public sealed class GanttBarModel
{
    public Guid ActivityId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime Finish { get; set; }
    public bool IsCritical { get; set; }
    public bool IsMilestone { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineFinish { get; set; }
    public decimal Progress { get; set; }
    public int RowIndex { get; set; }
}

public enum GanttZoomLevel
{
    Day = 0,
    Week = 1,
    Month = 2
}
