namespace Nizam.Scheduling.Calendars;

/// <summary>
/// Facade over <see cref="WorkingCalendar"/> for calendar arithmetic helpers.
/// </summary>
public sealed class CalendarCalculator
{
    private readonly WorkingCalendar _calendar;

    public CalendarCalculator(WorkingCalendar calendar)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }

    public WorkingCalendar Calendar => _calendar;

    public bool IsWorkingTime(DateTime dateTime) => _calendar.IsWorkingTime(dateTime);

    public DateTime GetNextWorkingTime(DateTime dateTime) => _calendar.GetNextWorkingTime(dateTime);

    public DateTime GetPreviousWorkingTime(DateTime dateTime) => _calendar.GetPreviousWorkingTime(dateTime);

    public DateTime AddWorkingMinutes(DateTime start, int minutes) =>
        _calendar.AddWorkingMinutes(start, minutes);

    public DateTime SubtractWorkingMinutes(DateTime finish, int minutes) =>
        _calendar.SubtractWorkingMinutes(finish, minutes);

    public int CalculateWorkingDuration(DateTime start, DateTime finish) =>
        _calendar.CalculateWorkingDuration(start, finish);
}
