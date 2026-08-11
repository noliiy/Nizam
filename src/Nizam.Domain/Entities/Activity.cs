using Nizam.Domain.Enums;

namespace Nizam.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WbsId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityType ActivityType { get; set; }
    public ActivityStatus Status { get; set; }

    public int OriginalDurationMinutes { get; set; }
    public int RemainingDurationMinutes { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedFinish { get; set; }
    public DateTime? EarlyStart { get; set; }
    public DateTime? EarlyFinish { get; set; }
    public DateTime? LateStart { get; set; }
    public DateTime? LateFinish { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public DateTime? RemainingStart { get; set; }
    public DateTime? RemainingFinish { get; set; }

    public int TotalFloatMinutes { get; set; }
    public int FreeFloatMinutes { get; set; }
    public decimal PercentComplete { get; set; }
    public bool IsCritical { get; set; }
    public bool IsLongestPath { get; set; }

    public ConstraintType ConstraintType { get; set; }
    public DateTime? ConstraintDate { get; set; }
    public Guid? CalendarId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
