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

    #region Critical OppositeArc Behavior Tests

    [Fact]
    public void OppositeArc_MustUseBitwiseNot_NotNegation()
    {
        // This test documents a CRITICAL requirement: OppositeArc uses bitwise NOT (~), not negation (-)
        // This is required for compatibility with OR-Tools and correct BFS traversal in max flow algorithms
        
        // For positive arcs
        ReverseArcListGraph.OppositeArc(1).Should().Be(~1).And.Be(-2);  // ~1 = -2, NOT -1
        ReverseArcListGraph.OppositeArc(2).Should().Be(~2).And.Be(-3);  // ~2 = -3, NOT -2
        ReverseArcListGraph.OppositeArc(3).Should().Be(~3).And.Be(-4);  // ~3 = -4, NOT -3
        
        // For negative arcs (reverse of reverse should give original)
        ReverseArcListGraph.OppositeArc(~1).Should().Be(1);  // ~(-2) = 1
        ReverseArcListGraph.OppositeArc(~2).Should().Be(2);  // ~(-3) = 2
        ReverseArcListGraph.OppositeArc(~3).Should().Be(3);  // ~(-4) = 3
        
        // Verify the mathematical property: OppositeArc(OppositeArc(x)) = x
        for (int arc = 1; arc <= 10; arc++)
        {
            ReverseArcListGraph.OppositeArc(ReverseArcListGraph.OppositeArc(arc)).Should().Be(arc,
                "OppositeArc must be its own inverse");
        }
    }

    #endregion

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
        graph.IsArcValid(~arc).Should().BeTrue();
        graph.IsArcValid(0).Should().BeFalse();

        graph.Head(arc).Should().Be(1);
        graph.Tail(arc).Should().Be(0);
        graph.Head(~arc).Should().Be(0);
        graph.Tail(~arc).Should().Be(1);

        ReverseArcListGraph.OppositeArc(arc).Should().Be(~arc);
        ReverseArcListGraph.OppositeArc(~arc).Should().Be(arc);
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

    [Fact]
    public void IMaxFlowGraph_HasNegativeReverseArcs_ShouldBeTrue()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        var maxFlowGraph = (IMaxFlowGraph)graph;

        // Assert
        maxFlowGraph.HasNegativeReverseArcs.Should().BeTrue();
    }

    [Fact]
    public void IMaxFlowGraph_OppositeArc_ShouldReturnBitwiseNot()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        var maxFlowGraph = (IMaxFlowGraph)graph;
        graph.AddNode(0);
        graph.AddNode(1);
        var arc = graph.AddArc(0, 1);

        // Act & Assert
        maxFlowGraph.OppositeArc(arc).Should().Be(~arc);
        maxFlowGraph.OppositeArc(~arc).Should().Be(arc);
        maxFlowGraph.Opposite(arc).Should().Be(~arc); // Test extension method
    }

    [Fact]
    public void OutgoingOrOppositeIncomingArcsStartingFrom_ShouldResumeIteration()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        var arc01 = graph.AddArc(0, 1);
        var arc02 = graph.AddArc(0, 2);
        var arc10 = graph.AddArc(1, 0);
        var arc20 = graph.AddArc(2, 0);

        // Get all arcs to understand the order
        var allArcs = graph.OutgoingOrOppositeIncomingArcs(0).ToList();
        
        // Find arc01 in the list
        var arc01Index = allArcs.IndexOf(arc01);
        arc01Index.Should().BeGreaterOrEqualTo(0);

        // Act - Start from arc01
        var arcsFromArc01 = graph.OutgoingOrOppositeIncomingArcsStartingFrom(0, arc01).ToList();
        
        // Assert - Should get all arcs from arc01 onwards
        arcsFromArc01.Should().HaveCount(allArcs.Count - arc01Index);
        arcsFromArc01[0].Should().Be(arc01);
        
        // The remaining arcs should match the tail of allArcs
        for (int i = 0; i < arcsFromArc01.Count; i++)
        {
            arcsFromArc01[i].Should().Be(allArcs[arc01Index + i]);
        }

        // Act - Start from an opposite incoming arc
        var oppositeArcIndex = allArcs.IndexOf(~arc10);
        if (oppositeArcIndex >= 0)
        {
            var arcsFromOpposite = graph.OutgoingOrOppositeIncomingArcsStartingFrom(0, ~arc10).ToList();
            arcsFromOpposite[0].Should().Be(~arc10);
        }
    }

    [Fact]
    public void OutgoingOrOppositeIncomingArcs_ShouldReturnAllIncidentArcs()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        var arc01 = graph.AddArc(0, 1);
        var arc02 = graph.AddArc(0, 2);
        var arc10 = graph.AddArc(1, 0);
        var arc20 = graph.AddArc(2, 0);

        // Act
        var allArcs = graph.OutgoingOrOppositeIncomingArcs(0).ToList();

        // Assert - Should have 2 outgoing and 2 opposite incoming
        allArcs.Should().HaveCount(4);
        allArcs.Should().Contain(arc01);
        allArcs.Should().Contain(arc02);
        allArcs.Should().Contain(~arc10);
        allArcs.Should().Contain(~arc20);
    }
}