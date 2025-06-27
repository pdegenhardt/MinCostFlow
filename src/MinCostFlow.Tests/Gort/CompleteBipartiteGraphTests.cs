using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class CompleteBipartiteGraphTests
{

    [Fact]
    public void CompleteBipartiteGraph_ShouldHaveCorrectStructure()
    {
        // Arrange
        var graph = new CompleteBipartiteGraph(3, 2);

        // Act & Assert
        graph.NumNodes.Should().Be(5); // 3 + 2
        graph.NumArcs.Should().Be(6); // 3 * 2
        graph.LeftNodes.Should().Be(3);
        graph.RightNodes.Should().Be(2);

        // Left nodes should have outgoing arcs
        graph.OutDegree(0).Should().Be(2);
        graph.OutDegree(1).Should().Be(2);
        graph.OutDegree(2).Should().Be(2);

        // Right nodes should have no outgoing arcs
        graph.OutDegree(3).Should().Be(0);
        graph.OutDegree(4).Should().Be(0);
    }

    [Fact]
    public void CompleteBipartiteGraph_ShouldOnlyAllowLeftToRightArcs()
    {
        // Arrange
        var graph = new CompleteBipartiteGraph(2, 2);

        // Act & Assert
        // Valid arcs (left to right)
        graph.HasArc(0, 2).Should().BeTrue();
        graph.HasArc(0, 3).Should().BeTrue();
        graph.HasArc(1, 2).Should().BeTrue();
        graph.HasArc(1, 3).Should().BeTrue();

        // Invalid arcs
        graph.HasArc(2, 0).Should().BeFalse(); // Right to left
        graph.HasArc(0, 1).Should().BeFalse(); // Left to left
        graph.HasArc(2, 3).Should().BeFalse(); // Right to right
    }
}