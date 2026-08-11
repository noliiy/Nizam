using FluentAssertions;
using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Models;
using Nizam.Scheduling.Services;

namespace Nizam.Scheduling.Tests;

public class RelationshipTypesTests
{
    [Fact]
    public void Ss_Basic()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var engine = new SchedulingEngine();

        var input = new ScheduleInput
        {
            ProjectStart = new DateTime(2024, 1, 1, 8, 0, 0),
            Calendar = WorkingCalendar.CreateStandard5x8(),
            Activities =
            [
                new ScheduleActivityInput { Id = aId, DurationMinutes = 960 },
                new ScheduleActivityInput { Id = bId, DurationMinutes = 480 }
            ],
            Relationships =
            [
                new ScheduleRelationshipInput
                {
                    PredecessorId = aId,
                    SuccessorId = bId,
                    Type = 1, // SS
                    LagMinutes = 0
                }
            ]
        };

        var result = engine.Calculate(input);
        result.Success.Should().BeTrue(result.ErrorMessage);

        var a = result.Activities.Single(x => x.Id == aId);
        var b = result.Activities.Single(x => x.Id == bId);

        // SS: B starts when A starts
        b.EarlyStart.Should().Be(a.EarlyStart);
        b.EarlyStart.Should().Be(new DateTime(2024, 1, 1, 8, 0, 0));
        b.EarlyFinish.Should().Be(new DateTime(2024, 1, 1, 17, 0, 0));
    }
}
