namespace Nizam.Domain.Entities;

public class CalendarWorkPattern
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorking { get; set; }
}
