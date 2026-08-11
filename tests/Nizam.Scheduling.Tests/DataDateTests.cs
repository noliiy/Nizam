using FluentAssertions;
using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Models;
using Nizam.Scheduling.Services;

namespace Nizam.Scheduling.Tests;

public class DataDateTests
{
    [Fact]
    public void DataDate_DelaysUnfinishedWork()
    {
        var aId = Guid.NewGuid();
        var engine = new SchedulingEngine();

        // Project start Monday; DataDate Wednesday morning — unfinished activity cannot start before DataDate
        var input = new ScheduleInput
        {
            ProjectStart = new DateTime(2024, 1, 1, 8, 0, 0),
            DataDate = new DateTime(2024, 1, 3, 8, 0, 0),
            Calendar = WorkingCalendar.CreateStandard5x8(),
            Activities =
            [
                new ScheduleActivityInput { Id = aId, DurationMinutes = 480 }
            ]
        };

        var result = engine.Calculate(input);
        result.Success.Should().BeTrue(result.ErrorMessage);

        var a = result.Activities.Single();
        a.RemainingStart.Should().Be(new DateTime(2024, 1, 3, 8, 0, 0));
        a.EarlyFinish.Should().Be(new DateTime(2024, 1, 3, 17, 0, 0));
    }
}
