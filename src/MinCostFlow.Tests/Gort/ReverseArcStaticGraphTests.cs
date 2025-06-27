using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;
using System.Collections.Generic;

namespace MinCostFlow.Tests.Gort;

public class ReverseArcStaticGraphTests : GraphBaseTests
{
    protected override IGraphBase CreateGraph() => new ReverseArcStaticGraph();
    protected override bool RequiresBuild => true;
    protected override bool SupportsReverseArcs => true;

    [Fact]
    public void Build_ShouldBeRequiredBeforeQueries()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddArc(0, 1);

        // Act & Assert - operations should throw before Build()
        Assert.Throws<InvalidOperationException>(() => graph.Head(0));
        Assert.Throws<InvalidOperationException>(() => graph.Tail(0));
        Assert.Throws<InvalidOperationException>(() => graph.OutDegree(0));
        Assert.Throws<InvalidOperationException>(() => graph.InDegree(0));
        Assert.Throws<InvalidOperationException>(() => graph.OutgoingArcs(0).ToList());
        Assert.Throws<InvalidOperationException>(() => graph.IncomingArcs(0).ToList());
    }

    [Fact]
    public void ReverseArcs_ShouldWorkCorrectly()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);

        var arc = graph.AddArc(0, 1);
        graph.Build();

        // Act & Assert
        graph.IsArcValid(arc).Should().BeTrue();
        graph.IsArcValid(-arc).Should().BeTrue();
        graph.IsArcValid(0).Should().BeFalse();

        graph.Head(arc).Should().Be(1);
        graph.Tail(arc).Should().Be(0);
        graph.Head(-arc).Should().Be(0);
        graph.Tail(-arc).Should().Be(1);

        ReverseArcStaticGraph.OppositeArc(arc).Should().Be(-arc);
        ReverseArcStaticGraph.OppositeArc(-arc).Should().Be(arc);
    }

    [Fact]
    public void IncomingArcs_ShouldReturnCorrectArcs()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        var arc02 = graph.AddArc(0, 2);
        var arc12 = graph.AddArc(1, 2);
        
        var permutation = graph.Build();
        
        // Apply permutation if arcs were reordered
        if (permutation != null)
        {
            arc02 = permutation[arc02 - 1]; // Convert 1-based to 0-based for array access
            arc12 = permutation[arc12 - 1]; // Convert 1-based to 0-based for array access
        }

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
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        graph.AddArc(0, 2);
        graph.AddArc(1, 2);
        graph.Build();

        // Act & Assert
        graph.InDegree(0).Should().Be(0);
        graph.InDegree(1).Should().Be(0);
        graph.InDegree(2).Should().Be(2);
    }

    [Fact]
    public void OppositeIncomingArcs_ShouldReturnNegativeIndices()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        graph.AddArc(0, 2);
        graph.AddArc(1, 2);
        graph.Build();

        // Act
        var oppositeIncoming = graph.OppositeIncomingArcs(2).ToList();

        // Assert
        oppositeIncoming.Should().HaveCount(2);
        oppositeIncoming.Should().AllSatisfy(arc => arc.Should().BeNegative());
    }

    [Fact]
    public void OutgoingOrOppositeIncomingArcs_ShouldCombineBoth()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        graph.AddArc(1, 0); // Outgoing from 1
        graph.AddArc(1, 2); // Outgoing from 1
        graph.AddArc(0, 1); // Incoming to 1
        graph.AddArc(2, 1); // Incoming to 1
        graph.Build();

        // Act
        var combined = graph.OutgoingOrOppositeIncomingArcs(1).ToList();

        // Assert
        combined.Should().HaveCount(4);
        var outgoing = combined.Where(a => a >= 0).ToList();
        var oppositeIncoming = combined.Where(a => a < 0).ToList();
        
        outgoing.Should().HaveCount(2);
        oppositeIncoming.Should().HaveCount(2);
    }

    [Fact]
    public void Build_WithComplexGraph_ShouldMaintainCorrectStructure()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        const int numNodes = 5;
        
        // Create a more complex graph
        var expectedArcs = new List<(int tail, int head)>
        {
            (0, 1), (0, 2), (0, 3),
            (1, 2), (1, 4),
            (2, 3), (2, 4),
            (3, 4),
            (4, 0), (4, 1)
        };

        foreach (var (tail, head) in expectedArcs)
        {
            graph.AddArc(tail, head);
        }

        // Act
        var permutation = graph.Build();

        // Assert
        graph.NumNodes.Should().Be(numNodes);
        graph.NumArcs.Should().Be(expectedArcs.Count);

        // Verify out-degrees
        graph.OutDegree(0).Should().Be(3);
        graph.OutDegree(1).Should().Be(2);
        graph.OutDegree(2).Should().Be(2);
        graph.OutDegree(3).Should().Be(1);
        graph.OutDegree(4).Should().Be(2);

        // Verify in-degrees
        graph.InDegree(0).Should().Be(1);
        graph.InDegree(1).Should().Be(2);
        graph.InDegree(2).Should().Be(2);
        graph.InDegree(3).Should().Be(2);
        graph.InDegree(4).Should().Be(3);

        // Verify all arcs are accessible
        for (int node = 0; node < numNodes; node++)
        {
            var outgoing = graph.OutgoingArcs(node).ToList();
            outgoing.Should().HaveCount(graph.OutDegree(node));
            
            foreach (var arc in outgoing)
            {
                graph.Tail(arc).Should().Be(node);
                graph.IsArcValid(arc).Should().BeTrue();
                graph.IsArcValid(-arc).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void MemoryEfficiency_ShouldMatchSpecification()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.Reserve(1000, 5000);
        
        // Add some nodes and arcs
        for (int i = 0; i < 500; i++)
        {
            graph.AddNode(i);
        }
        for (int i = 0; i < 1000; i++)
        {
            graph.AddArc(i % 500, (i + 1) % 500);
        }

        // Act
        graph.Build();

        // Assert
        // Memory usage should be: 2 × (NodeCapacity + 1) + 3 × ArcCapacity integer values
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(1000);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(5000);
        graph.NumNodes.Should().Be(500);
        graph.NumArcs.Should().Be(1000);
    }

    [Fact]
    public void ZeroArcIndex_ShouldNotBeValid()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddArc(0, 1);
        graph.Build();

        // Act & Assert
        graph.IsArcValid(0).Should().BeFalse();
        Assert.Throws<ArgumentException>(() => graph.Head(0));
        Assert.Throws<ArgumentException>(() => graph.Tail(0));
    }

    [Fact]
    public void LargeArcIndices_ShouldThrow()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddArc(0, 1);
        graph.Build();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.Head(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.Tail(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.Head(-100));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.Tail(-100));
    }

    [Fact]
    public void OutgoingArcsStartingFrom_WithReverseArc_ShouldThrow()
    {
        // Arrange
        var graph = new ReverseArcStaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        var arc = graph.AddArc(0, 1);
        graph.Build();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            graph.OutgoingArcsStartingFrom(0, -arc).ToList());
    }
}