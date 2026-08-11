using FluentAssertions;
using Nizam.Application.Baselines;
using Nizam.Scheduling.Calendars;

namespace Nizam.Application.Tests;

public class BaselineVarianceTests
{
    [Fact]
    public void WorkingVariance_Ignores_Weekend_WallClock()
    {
        var calendar = WorkingCalendar.CreateStandard5x8();
        // Friday 17:00 → Monday 08:00 is 0 working minutes but ~63h wall clock
        var fridayEnd = new DateTime(2024, 1, 5, 17, 0, 0);
        var mondayStart = new DateTime(2024, 1, 8, 8, 0, 0);

        var variance = CompareBaselineHandler.WorkingVarianceMinutes(calendar, fridayEnd, mondayStart);

        variance.Should().Be(0);
        ((int)(mondayStart - fridayEnd).TotalMinutes).Should().BeGreaterThan(60 * 48);
    }

    [Fact]
    public void WorkingVariance_Is_Negative_When_Current_Is_Earlier()
    {
        var calendar = WorkingCalendar.CreateStandard5x8();
        var baseline = new DateTime(2024, 1, 2, 13, 0, 0); // Tuesday after lunch
        var current = new DateTime(2024, 1, 2, 8, 0, 0);  // same day morning

        var variance = CompareBaselineHandler.WorkingVarianceMinutes(calendar, baseline, current);

        variance.Should().Be(-240); // 08:00-12:00 = 240 working minutes earlier
    }
}
