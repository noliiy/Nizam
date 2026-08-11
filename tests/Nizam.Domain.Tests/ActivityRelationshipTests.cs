using Nizam.Domain.Entities;
using Nizam.Domain.Enums;
using Nizam.Domain.Exceptions;
using FluentAssertions;

namespace Nizam.Domain.Tests;

public class ActivityRelationshipTests
{
    [Fact]
    public void Create_RejectsSelfDependency()
    {
        var activityId = Guid.NewGuid();

        var act = () => ActivityRelationship.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            activityId,
            activityId,
            RelationshipType.FinishToStart);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("self_dependency");
    }

    [Fact]
    public void Create_AllowsDistinctActivities()
    {
        var pred = Guid.NewGuid();
        var succ = Guid.NewGuid();

        var rel = ActivityRelationship.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            pred,
            succ,
            RelationshipType.FinishToStart,
            lagMinutes: 60);

        rel.PredecessorActivityId.Should().Be(pred);
        rel.SuccessorActivityId.Should().Be(succ);
        rel.LagMinutes.Should().Be(60);
    }
}
