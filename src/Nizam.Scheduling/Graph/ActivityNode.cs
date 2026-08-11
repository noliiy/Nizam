namespace Nizam.Scheduling.Graph;

public sealed class ActivityNode
{
    public Guid Id { get; init; }
    public int DurationMinutes { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public int ConstraintType { get; set; }
    public DateTime? ConstraintDate { get; set; }

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

public sealed class DependencyEdge
{
    public Guid PredecessorId { get; init; }
    public Guid SuccessorId { get; init; }
    /// <summary>0=FS, 1=SS, 2=FF, 3=SF</summary>
    public int Type { get; init; }
    public int LagMinutes { get; init; }
}
