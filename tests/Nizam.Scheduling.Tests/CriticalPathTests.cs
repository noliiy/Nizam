using FluentAssertions;
using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Models;
using Nizam.Scheduling.Services;

namespace Nizam.Scheduling.Tests;

public class CriticalPathTests
{
    [Fact]
    public void SimpleChain_AllCritical()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();
        var engine = new SchedulingEngine();

        var input = new ScheduleInput
        {
            ProjectStart = new DateTime(2024, 1, 1, 8, 0, 0),
            Calendar = WorkingCalendar.CreateStandard5x8(),
            Activities =
            [
                new ScheduleActivityInput { Id = aId, DurationMinutes = 480 },
                new ScheduleActivityInput { Id = bId, DurationMinutes = 480 },
                new ScheduleActivityInput { Id = cId, DurationMinutes = 480 }
            ],
            Relationships =
            [
                new ScheduleRelationshipInput { PredecessorId = aId, SuccessorId = bId, Type = 0 },
                new ScheduleRelationshipInput { PredecessorId = bId, SuccessorId = cId, Type = 0 }
            ]
        };

        var result = engine.Calculate(input);
        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Activities.Should().OnlyContain(a => a.IsCritical);
        result.Activities.Should().OnlyContain(a => a.TotalFloatMinutes == 0);
        result.CriticalActivityIds.Should().HaveCount(3);
    }
}
