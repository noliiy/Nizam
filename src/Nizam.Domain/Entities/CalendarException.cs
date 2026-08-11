namespace Nizam.Domain.Entities;

public class CalendarException
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsWorking { get; set; }
    /// <summary>Optional description of exception intervals or reason.</summary>
    public string? Description { get; set; }
}
