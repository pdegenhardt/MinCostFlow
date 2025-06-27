using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class CompleteGraphTests
{

    [Fact]
    public void CompleteGraph_ShouldHaveAllConnections()
    {
        // Arrange
        var graph = new CompleteGraph(3);

        // Act & Assert
        graph.NumNodes.Should().Be(3);
        graph.NumArcs.Should().Be(6); // 3 * (3 - 1) = 6 (no self-loops)

        // Verify all connections exist (except self-loops)
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i != j) // No self-loops
                {
                    var arcIndex = graph.GetArcIndex(i, j);
                    graph.IsArcValid(arcIndex).Should().BeTrue();
                    graph.Tail(arcIndex).Should().Be(i);
                    graph.Head(arcIndex).Should().Be(j);
                }
            }
        }
    }

    [Fact]
    public void CompleteGraph_ArcIndices_ShouldBeDeterministic()
    {
        // Arrange
        var graph = new CompleteGraph(4);

        // Act & Assert
        // No self-loops, so arc indices skip the diagonal
        graph.GetArcIndex(0, 1).Should().Be(0);
        graph.GetArcIndex(0, 2).Should().Be(1);
        graph.GetArcIndex(0, 3).Should().Be(2);
        graph.GetArcIndex(1, 0).Should().Be(3);
        graph.GetArcIndex(1, 2).Should().Be(4);
        graph.GetArcIndex(1, 3).Should().Be(5);
        // etc.
    }
}