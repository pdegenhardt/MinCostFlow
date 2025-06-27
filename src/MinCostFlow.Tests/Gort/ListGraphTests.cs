using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class ListGraphTests : GraphBaseTests
{
    protected override IGraphBase CreateGraph() => new ListGraph();
    protected override bool RequiresBuild => false;
    protected override bool SupportsReverseArcs => false;

    [Fact]
    public void ListGraph_WithTails_ShouldHaveEfficientTailAccess()
    {
        // Arrange
        var graph = new ListGraph(storeTails: true);
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        var arc = graph.AddArc(1, 2);

        // Act & Assert
        graph.Tail(arc).Should().Be(1);
        graph.Head(arc).Should().Be(2);
    }

    [Fact]
    public void ListGraph_WithoutTails_ShouldStillFindTail()
    {
        // Arrange
        var graph = new ListGraph(storeTails: false);
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        var arc = graph.AddArc(1, 2);

        // Act & Assert
        graph.Tail(arc).Should().Be(1);
        graph.Head(arc).Should().Be(2);
    }
}