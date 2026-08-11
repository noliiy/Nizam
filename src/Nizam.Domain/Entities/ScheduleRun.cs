using Nizam.Domain.Enums;

namespace Nizam.Domain.Entities;

public class ScheduleRun
{
    public Guid RunId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime DataDate { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ScheduleRunStatus Status { get; set; }
    public long? DurationMs { get; set; }
    public Guid? TriggeredBy { get; set; }
    public int ActivityCount { get; set; }
    public int RelationshipCount { get; set; }
    public DateTime? ProjectFinish { get; set; }
    public string? Error { get; set; }
}
