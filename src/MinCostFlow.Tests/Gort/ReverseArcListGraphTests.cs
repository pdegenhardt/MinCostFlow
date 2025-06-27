using System.Linq;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class ReverseArcListGraphTests : GraphBaseTests
{
    protected override IGraphBase CreateGraph() => new ReverseArcListGraph();
    protected override bool RequiresBuild => false;
    protected override bool SupportsReverseArcs => true;

    [Fact]
    public void ReverseArcs_ShouldWorkCorrectly()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);

        var arc = graph.AddArc(0, 1);

        // Act & Assert
        graph.IsArcValid(arc).Should().BeTrue();
        graph.IsArcValid(-arc).Should().BeTrue();
        graph.IsArcValid(0).Should().BeFalse();

        graph.Head(arc).Should().Be(1);
        graph.Tail(arc).Should().Be(0);
        graph.Head(-arc).Should().Be(0);
        graph.Tail(-arc).Should().Be(1);

        ReverseArcListGraph.OppositeArc(arc).Should().Be(-arc);
        ReverseArcListGraph.OppositeArc(-arc).Should().Be(arc);
    }

    [Fact]
    public void IncomingArcs_ShouldReturnCorrectArcs()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        var arc02 = graph.AddArc(0, 2);
        var arc12 = graph.AddArc(1, 2);

        // Act
        var incomingArcs = graph.IncomingArcs(2).ToList();

        // Assert
        incomingArcs.Should().HaveCount(2);
        incomingArcs.Should().Contain(new[] { arc02, arc12 });
    }

    [Fact]
    public void InDegree_ShouldReturnCorrectCount()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        graph.AddArc(0, 2);
        graph.AddArc(1, 2);

        // Act & Assert
        graph.InDegree(0).Should().Be(0);
        graph.InDegree(1).Should().Be(0);
        graph.InDegree(2).Should().Be(2);
    }

    [Fact]
    public void AllForwardArcs_ShouldExcludeZero()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        graph.AddArc(0, 1);
        graph.AddArc(1, 0);

        // Act
        var arcs = graph.AllForwardArcs().ToList();

        // Assert
        arcs.Should().HaveCount(2);
        arcs.Should().Equal(1, 2); // 1-based indexing, no 0
    }
}