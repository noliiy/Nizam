namespace Nizam.Automation.Contracts.Events;

public sealed class ProjectCreatedEvent
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ScheduleCalculatedEvent
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public DateTime? ProjectFinish { get; set; }
    public int CriticalCount { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public sealed class ProgressUpdatedEvent
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ActivityId { get; set; }
    public Guid ProgressUpdateId { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class BaselineCreatedEvent
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid BaselineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
