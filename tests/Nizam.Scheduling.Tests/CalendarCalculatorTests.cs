using FluentAssertions;
using Nizam.Scheduling.Calendars;

namespace Nizam.Scheduling.Tests;

public class CalendarCalculatorTests
{
    private readonly CalendarCalculator _calc =
        new(WorkingCalendar.CreateStandard5x8());

    [Fact]
    public void AddWorkingMinutes_CrossesLunch()
    {
        // Monday 2024-01-01 11:00 + 120 min = skip lunch after 60 min morning → 14:00
        var start = new DateTime(2024, 1, 1, 11, 0, 0);
        var result = _calc.AddWorkingMinutes(start, 120);
        result.Should().Be(new DateTime(2024, 1, 1, 14, 0, 0));
    }

    [Fact]
    public void AddWorkingMinutes_CrossesWeekend()
    {
        // Friday 2024-01-05 16:00 + 120 min = 60 Fri afternoon + 60 Mon morning → Mon 09:00
        var start = new DateTime(2024, 1, 5, 16, 0, 0);
        var result = _calc.AddWorkingMinutes(start, 120);
        result.Should().Be(new DateTime(2024, 1, 8, 9, 0, 0));
    }

    [Fact]
    public void AddWorkingMinutes_FullWorkDay()
    {
        var start = new DateTime(2024, 1, 1, 8, 0, 0);
        var result = _calc.AddWorkingMinutes(start, 480);
        result.Should().Be(new DateTime(2024, 1, 1, 17, 0, 0));
    }

    [Fact]
    public void SubtractWorkingMinutes_CrossesLunch()
    {
        var finish = new DateTime(2024, 1, 1, 14, 0, 0);
        var result = _calc.SubtractWorkingMinutes(finish, 120);
        result.Should().Be(new DateTime(2024, 1, 1, 11, 0, 0));
    }

    [Fact]
    public void CalculateWorkingDuration_FullDay()
    {
        var start = new DateTime(2024, 1, 1, 8, 0, 0);
        var finish = new DateTime(2024, 1, 1, 17, 0, 0);
        _calc.CalculateWorkingDuration(start, finish).Should().Be(480);
    }

    [Fact]
    public void GetNextWorkingTime_SkipsWeekend()
    {
        var fridayEvening = new DateTime(2024, 1, 5, 17, 0, 0);
        _calc.GetNextWorkingTime(fridayEvening).Should().Be(new DateTime(2024, 1, 8, 8, 0, 0));
    }
}
