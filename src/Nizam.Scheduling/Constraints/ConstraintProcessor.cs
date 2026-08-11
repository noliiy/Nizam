using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.Constraints;

public static class ConstraintProcessor
{
    public const int None = 0;
    public const int StartOnOrAfter = 2;
    public const int FinishOnOrBefore = 6;

    /// <summary>Raises EarlyStart when StartOnOrAfter applies.</summary>
    public static DateTime ApplyStartOnOrAfter(
        ActivityNode node,
        DateTime earlyStart,
        WorkingCalendar calendar)
    {
        if (node.ConstraintType != StartOnOrAfter || node.ConstraintDate is null)
            return earlyStart;

        var constraint = DateTime.SpecifyKind(
            new DateTime(
                node.ConstraintDate.Value.Year,
                node.ConstraintDate.Value.Month,
                node.ConstraintDate.Value.Day,
                node.ConstraintDate.Value.Hour,
                node.ConstraintDate.Value.Minute,
                0),
            DateTimeKind.Unspecified);

        if (earlyStart >= constraint)
            return earlyStart;

        return calendar.IsWorkingTime(constraint)
            ? constraint
            : calendar.GetNextWorkingTime(constraint);
    }

    /// <summary>Lowers LateFinish when FinishOnOrBefore applies.</summary>
    public static DateTime ApplyFinishOnOrBefore(
        ActivityNode node,
        DateTime lateFinish,
        WorkingCalendar calendar)
    {
        if (node.ConstraintType != FinishOnOrBefore || node.ConstraintDate is null)
            return lateFinish;

        var constraint = DateTime.SpecifyKind(
            new DateTime(
                node.ConstraintDate.Value.Year,
                node.ConstraintDate.Value.Month,
                node.ConstraintDate.Value.Day,
                node.ConstraintDate.Value.Hour,
                node.ConstraintDate.Value.Minute,
                0),
            DateTimeKind.Unspecified);

        if (lateFinish <= constraint)
            return lateFinish;

        // Prefer finishing at constraint if it is a valid finish boundary / working time
        if (calendar.IsWorkingTime(constraint) || IsIntervalEnd(calendar, constraint))
            return constraint;

        return calendar.GetPreviousWorkingTime(constraint).AddMinutes(1) is var adjusted
            && adjusted <= constraint
            ? DateTime.SpecifyKind(adjusted, DateTimeKind.Unspecified)
            : constraint;
    }

    private static bool IsIntervalEnd(WorkingCalendar calendar, DateTime dt)
    {
        // Interval ends are not IsWorkingTime; detect by checking previous minute is working
        // and current is not, or AddWorkingMinutes from previous lands here.
        var prev = dt.AddMinutes(-1);
        return !calendar.IsWorkingTime(dt) && calendar.IsWorkingTime(prev);
    }
}
