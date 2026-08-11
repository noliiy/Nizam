using FluentAssertions;
using Nizam.Scheduling.Calendars;
using Nizam.Scheduling.Models;
using Nizam.Scheduling.Services;

namespace Nizam.Scheduling.Tests;

public class MilestoneTests
{
    [Fact]
    public void Milestone_HasZeroDuration_EsEqualsEf()
    {
        var taskId = Guid.NewGuid();
        var milestoneId = Guid.NewGuid();
        var engine = new SchedulingEngine();

        var input = new ScheduleInput
        {
            ProjectStart = new DateTime(2024, 1, 1, 8, 0, 0),
            Calendar = WorkingCalendar.CreateStandard5x8(),
            Activities =
            [
                new ScheduleActivityInput { Id = taskId, DurationMinutes = 480 },
                new ScheduleActivityInput { Id = milestoneId, DurationMinutes = 0 }
            ],
            Relationships =
            [
                new ScheduleRelationshipInput
                {
                    PredecessorId = taskId,
                    SuccessorId = milestoneId,
                    Type = 0
                }
            ]
        };

        var result = engine.Calculate(input);
        result.Success.Should().BeTrue(result.ErrorMessage);

        var milestone = result.Activities.Single(x => x.Id == milestoneId);
        milestone.EarlyStart.Should().Be(milestone.EarlyFinish);
        milestone.EarlyStart.Should().Be(new DateTime(2024, 1, 2, 8, 0, 0));
    }
}
