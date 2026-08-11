namespace Nizam.Scheduling.Calendars;

/// <summary>
/// Working calendar with day-of-week patterns and date exceptions.
/// Intervals are half-open [start, end). All DateTimes are Unspecified, whole minutes.
/// </summary>
public sealed class WorkingCalendar
{
    private readonly Dictionary<DayOfWeek, IReadOnlyList<(TimeSpan Start, TimeSpan End)>> _patterns;
    private readonly Dictionary<DateOnly, IReadOnlyList<(TimeSpan Start, TimeSpan End)>> _exceptions;

    public WorkingCalendar(
        IEnumerable<(DayOfWeek Day, IReadOnlyList<(TimeSpan Start, TimeSpan End)> Intervals)> patterns,
        IReadOnlyDictionary<DateOnly, IReadOnlyList<(TimeSpan Start, TimeSpan End)>>? exceptions = null)
    {
        _patterns = new Dictionary<DayOfWeek, IReadOnlyList<(TimeSpan Start, TimeSpan End)>>();
        foreach (var (day, intervals) in patterns)
            _patterns[day] = NormalizeIntervals(intervals);

        _exceptions = new Dictionary<DateOnly, IReadOnlyList<(TimeSpan Start, TimeSpan End)>>();
        if (exceptions is null)
            return;

        foreach (var (date, intervals) in exceptions)
            _exceptions[date] = NormalizeIntervals(intervals);
    }

    public static WorkingCalendar CreateStandard5x8()
    {
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> dayIntervals =
        [
            (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
            (new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0))
        ];

        var patterns = new List<(DayOfWeek, IReadOnlyList<(TimeSpan, TimeSpan)>)>
        {
            (DayOfWeek.Monday, dayIntervals),
            (DayOfWeek.Tuesday, dayIntervals),
            (DayOfWeek.Wednesday, dayIntervals),
            (DayOfWeek.Thursday, dayIntervals),
            (DayOfWeek.Friday, dayIntervals),
            (DayOfWeek.Saturday, Array.Empty<(TimeSpan, TimeSpan)>()),
            (DayOfWeek.Sunday, Array.Empty<(TimeSpan, TimeSpan)>())
        };

        return new WorkingCalendar(patterns);
    }

    public bool IsWorkingTime(DateTime dateTime)
    {
        var dt = Truncate(dateTime);
        var tod = dt.TimeOfDay;
        foreach (var (start, end) in GetIntervals(DateOnly.FromDateTime(dt)))
        {
            if (tod >= start && tod < end)
                return true;
        }

        return false;
    }

    public DateTime GetNextWorkingTime(DateTime dateTime)
    {
        var original = Truncate(dateTime);

        for (var i = 0; i < 3660; i++)
        {
            var day = original.Date.AddDays(i);
            var tod = i == 0 ? original.TimeOfDay : TimeSpan.Zero;

            foreach (var (start, end) in GetIntervals(DateOnly.FromDateTime(day)))
            {
                if (tod < start)
                    return Specify(day + start);
                if (tod < end)
                    return Specify(day + tod);
            }
        }

        throw new InvalidOperationException("No working time found within search horizon.");
    }

    public DateTime GetPreviousWorkingTime(DateTime dateTime)
    {
        var original = Truncate(dateTime);
        if (IsWorkingTime(original))
            return original;

        var (segStart, segEnd) = FindPreviousWorkingSegment(original);
        // Return last working minute of that segment
        var last = segEnd.AddMinutes(-1);
        return last >= segStart ? last : segStart;
    }

    public DateTime AddWorkingMinutes(DateTime start, int minutes)
    {
        if (minutes < 0)
            return SubtractWorkingMinutes(start, -minutes);

        var current = Truncate(start);
        if (minutes == 0)
            return IsWorkingTime(current) ? current : GetNextWorkingTime(current);

        if (!IsWorkingTime(current))
            current = GetNextWorkingTime(current);

        var remaining = minutes;
        for (var guard = 0; remaining > 0; guard++)
        {
            if (guard > 1_000_000)
                throw new InvalidOperationException("AddWorkingMinutes exceeded iteration limit.");

            var intervals = GetIntervals(DateOnly.FromDateTime(current));
            var tod = current.TimeOfDay;
            var found = false;

            foreach (var (intervalStart, intervalEnd) in intervals)
            {
                if (tod < intervalStart || tod >= intervalEnd)
                    continue;

                found = true;
                var available = (int)(intervalEnd - tod).TotalMinutes;
                if (remaining <= available)
                    return Specify(current.AddMinutes(remaining));

                remaining -= available;
                current = GetNextWorkingTime(Specify(current.Date + intervalEnd));
                break;
            }

            if (!found)
                current = GetNextWorkingTime(current);
        }

        return current;
    }

    public DateTime SubtractWorkingMinutes(DateTime finish, int minutes)
    {
        if (minutes < 0)
            return AddWorkingMinutes(finish, -minutes);

        var cursor = Truncate(finish);
        if (minutes == 0)
            return cursor;

        var remaining = minutes;
        for (var guard = 0; remaining > 0; guard++)
        {
            if (guard > 1_000_000)
                throw new InvalidOperationException("SubtractWorkingMinutes exceeded iteration limit.");

            var (segStart, segEnd) = FindPreviousWorkingSegment(cursor);
            var available = (int)(segEnd - segStart).TotalMinutes;
            if (available <= 0)
            {
                cursor = segStart;
                continue;
            }

            if (remaining <= available)
                return Specify(segEnd.AddMinutes(-remaining));

            remaining -= available;
            cursor = segStart;
        }

        return cursor;
    }

    public int CalculateWorkingDuration(DateTime start, DateTime finish)
    {
        var s = Truncate(start);
        var f = Truncate(finish);
        if (f <= s)
            return 0;

        var total = 0;
        var current = IsWorkingTime(s) ? s : GetNextWorkingTime(s);

        for (var guard = 0; current < f; guard++)
        {
            if (guard > 1_000_000)
                throw new InvalidOperationException("CalculateWorkingDuration exceeded iteration limit.");

            var intervals = GetIntervals(DateOnly.FromDateTime(current));
            var tod = current.TimeOfDay;
            var progressed = false;

            foreach (var (intervalStart, intervalEnd) in intervals)
            {
                if (tod >= intervalEnd)
                    continue;

                if (tod < intervalStart)
                {
                    current = Specify(current.Date + intervalStart);
                    if (current >= f)
                        return total;
                    tod = current.TimeOfDay;
                }

                var segmentEnd = current.Date + intervalEnd;
                if (segmentEnd > f)
                    segmentEnd = f;

                total += (int)(segmentEnd - current).TotalMinutes;
                current = Specify(segmentEnd);
                progressed = true;
                break;
            }

            if (!progressed)
                current = GetNextWorkingTime(Specify(current.Date.AddDays(1)));
        }

        return total;
    }

    /// <summary>
    /// Finds the latest working segment that ends at or before <paramref name="exclusiveEnd"/>
    /// (half-open), clipped so the segment end equals min(interval end, exclusiveEnd).
    /// </summary>
    private (DateTime Start, DateTime End) FindPreviousWorkingSegment(DateTime exclusiveEnd)
    {
        var t = Truncate(exclusiveEnd);

        for (var dayOffset = 0; dayOffset < 3660; dayOffset++)
        {
            var day = t.Date.AddDays(-dayOffset);
            var intervals = GetIntervals(DateOnly.FromDateTime(day));
            var limit = dayOffset == 0 ? t.TimeOfDay : TimeSpan.FromDays(1);

            for (var i = intervals.Count - 1; i >= 0; i--)
            {
                var (start, end) = intervals[i];
                if (start >= limit)
                    continue;

                var segEnd = end < limit ? end : limit;
                if (segEnd <= start)
                    continue;

                return (Specify(day + start), Specify(day + segEnd));
            }
        }

        throw new InvalidOperationException("No previous working segment found within search horizon.");
    }

    private IReadOnlyList<(TimeSpan Start, TimeSpan End)> GetIntervals(DateOnly date)
    {
        if (_exceptions.TryGetValue(date, out var ex))
            return ex;
        if (_patterns.TryGetValue(date.DayOfWeek, out var pattern))
            return pattern;
        return Array.Empty<(TimeSpan, TimeSpan)>();
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> NormalizeIntervals(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> intervals)
    {
        if (intervals.Count == 0)
            return intervals;

        return intervals
            .Select(i => (
                TimeSpan.FromMinutes((int)i.Start.TotalMinutes),
                TimeSpan.FromMinutes((int)i.End.TotalMinutes)))
            .Where(i => i.Item2 > i.Item1)
            .OrderBy(i => i.Item1)
            .ToList();
    }

    private static DateTime Truncate(DateTime dt) =>
        Specify(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0));

    private static DateTime Specify(DateTime dt) =>
        DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
}
