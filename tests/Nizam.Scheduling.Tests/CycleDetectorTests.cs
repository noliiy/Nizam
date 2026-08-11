using FluentAssertions;
using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.Tests;

public class CycleDetectorTests
{
    [Fact]
    public void HasCycle_ReturnsFalse_ForDag()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var nodes = new[]
        {
            new ActivityNode { Id = a },
            new ActivityNode { Id = b },
            new ActivityNode { Id = c }
        };

        var edges = new[]
        {
            new DependencyEdge { PredecessorId = a, SuccessorId = b, Type = 0 },
            new DependencyEdge { PredecessorId = b, SuccessorId = c, Type = 0 }
        };

        CycleDetector.HasCycle(nodes, edges).Should().BeFalse();
    }

    [Fact]
    public void HasCycle_ReturnsTrue_ForCycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var nodes = new[]
        {
            new ActivityNode { Id = a },
            new ActivityNode { Id = b },
            new ActivityNode { Id = c }
        };

        var edges = new[]
        {
            new DependencyEdge { PredecessorId = a, SuccessorId = b, Type = 0 },
            new DependencyEdge { PredecessorId = b, SuccessorId = c, Type = 0 },
            new DependencyEdge { PredecessorId = c, SuccessorId = a, Type = 0 }
        };

        CycleDetector.HasCycle(nodes, edges).Should().BeTrue();
    }

    [Fact]
    public void TopologicalSorter_Throws_OnCycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var nodes = new[]
        {
            new ActivityNode { Id = a },
            new ActivityNode { Id = b }
        };

        var edges = new[]
        {
            new DependencyEdge { PredecessorId = a, SuccessorId = b, Type = 0 },
            new DependencyEdge { PredecessorId = b, SuccessorId = a, Type = 0 }
        };

        var act = () => TopologicalSorter.Sort(nodes, edges);
        act.Should().Throw<InvalidOperationException>();
    }
}
