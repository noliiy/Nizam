using Nizam.Scheduling.Calendars;

namespace Nizam.Scheduling.Models;

public sealed class ScheduleActivityInput
{
    public Guid Id { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public int? RemainingDurationMinutes { get; set; }
    /// <summary>0=None, 2=StartOnOrAfter, 6=FinishOnOrBefore</summary>
    public int ConstraintType { get; set; }
    public DateTime? ConstraintDate { get; set; }
}

public sealed class ScheduleRelationshipInput
{
    public Guid PredecessorId { get; set; }
    public Guid SuccessorId { get; set; }
    /// <summary>0=FS, 1=SS, 2=FF, 3=SF</summary>
    public int Type { get; set; }
    public int LagMinutes { get; set; }
}

public sealed class ScheduleInput
{
    public DateTime ProjectStart { get; set; }
    public DateTime? DataDate { get; set; }
    public IReadOnlyList<ScheduleActivityInput> Activities { get; set; } = Array.Empty<ScheduleActivityInput>();
    public IReadOnlyList<ScheduleRelationshipInput> Relationships { get; set; } = Array.Empty<ScheduleRelationshipInput>();
    public WorkingCalendar Calendar { get; set; } = WorkingCalendar.CreateStandard5x8();
    public int CriticalFloatThresholdMinutes { get; set; }
}

public sealed class ActivityScheduleResult
{
    public Guid Id { get; set; }
    public DateTime EarlyStart { get; set; }
    public DateTime EarlyFinish { get; set; }
    public DateTime LateStart { get; set; }
    public DateTime LateFinish { get; set; }
    public int TotalFloatMinutes { get; set; }
    public int FreeFloatMinutes { get; set; }
    public bool IsCritical { get; set; }
    public bool IsLongestPath { get; set; }
    public DateTime? RemainingStart { get; set; }
    public DateTime? RemainingFinish { get; set; }
}

public sealed class ScheduleResult
{
    public IReadOnlyList<ActivityScheduleResult> Activities { get; set; } = Array.Empty<ActivityScheduleResult>();
    public DateTime? ProjectFinish { get; set; }
    public IReadOnlyList<Guid> CriticalActivityIds { get; set; } = Array.Empty<Guid>();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static ScheduleResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
