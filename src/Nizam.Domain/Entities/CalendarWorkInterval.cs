namespace Nizam.Domain.Entities;

public class CalendarWorkInterval
{
    public Guid Id { get; set; }
    public Guid CalendarWorkPatternId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
