using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public abstract class GraphBaseTests
{
    protected abstract IGraphBase CreateGraph();
    protected abstract bool RequiresBuild { get; }
    protected abstract bool SupportsReverseArcs { get; }

    #region Basic Operations Tests

    [Fact]
    public void EmptyGraph_ShouldHaveCorrectInitialState()
    {
        // Arrange & Act
        var graph = CreateGraph();

        // Assert
        graph.NumNodes.Should().Be(0);
        graph.Size.Should().Be(0);
        graph.NumArcs.Should().Be(0);
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(0);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(0);
        graph.AllNodes().Should().BeEmpty();
        graph.AllForwardArcs().Should().BeEmpty();
    }

    [Fact]
    public void AddNode_ShouldIncreaseNodeCount()
    {
        // Arrange
        var graph = CreateGraph();

        // Act
        graph.AddNode(0);
        graph.AddNode(2);
        graph.AddNode(1);

        // Assert
        graph.NumNodes.Should().Be(3);
        graph.IsNodeValid(0).Should().BeTrue();
        graph.IsNodeValid(1).Should().BeTrue();
        graph.IsNodeValid(2).Should().BeTrue();
        graph.IsNodeValid(3).Should().BeFalse();
    }

    [Fact]
    public void AddArc_ShouldIncreaseArcCount()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        // Act
        int arc1 = graph.AddArc(0, 1);
        int arc2 = graph.AddArc(1, 2);
        int arc3 = graph.AddArc(2, 0);

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Assert
        graph.NumArcs.Should().Be(3);
        graph.Head(arc1).Should().Be(1);
        graph.Tail(arc1).Should().Be(0);
        graph.Head(arc2).Should().Be(2);
        graph.Tail(arc2).Should().Be(1);
        graph.Head(arc3).Should().Be(0);
        graph.Tail(arc3).Should().Be(2);
    }

    #endregion

    #region Capacity Management Tests

    [Fact]
    public void ReserveNodes_ShouldPreallocateSpace()
    {
        // Arrange
        var graph = CreateGraph();

        // Act
        graph.ReserveNodes(100);

        // Assert
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(100);
        graph.NumNodes.Should().Be(0); // Reserve shouldn't add nodes
    }

    [Fact]
    public void ReserveArcs_ShouldPreallocateSpace()
    {
        // Arrange
        var graph = CreateGraph();

        // Act
        graph.ReserveArcs(200);

        // Assert
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(200);
        graph.NumArcs.Should().Be(0); // Reserve shouldn't add arcs
    }

    [Fact]
    public void Reserve_ShouldPreallocateBothCapacities()
    {
        // Arrange
        var graph = CreateGraph();

        // Act
        graph.Reserve(50, 100);

        // Assert
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(50);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(100);
    }

    #endregion

    #region Iteration Tests

    [Fact]
    public void AllNodes_ShouldIterateOverAllValidNodes()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(4); // Skip 3

        // Act
        var nodes = graph.AllNodes().ToList();

        // Assert
        nodes.Should().HaveCount(5); // 0, 1, 2, 3, 4
        nodes.Should().BeInAscendingOrder();
        nodes.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public void AllForwardArcs_ShouldIterateOverAllArcs()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        var arcIds = new List<int>();
        arcIds.Add(graph.AddArc(0, 1));
        arcIds.Add(graph.AddArc(1, 2));
        arcIds.Add(graph.AddArc(2, 0));

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Act
        var arcs = graph.AllForwardArcs().ToList();

        // Assert
        arcs.Should().HaveCount(3);
        if (!RequiresBuild) // ListGraph preserves order
        {
            arcs.Should().Equal(arcIds);
        }
    }

    #endregion

    #region Arc Iteration Tests

    [Fact]
    public void OutgoingArcs_ShouldReturnCorrectArcs()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(3);

        var arc01 = graph.AddArc(0, 1);
        var arc02 = graph.AddArc(0, 2);
        var arc03 = graph.AddArc(0, 3);
        var arc12 = graph.AddArc(1, 2);

        if (RequiresBuild)
        {
            var permutation = graph.Build();
            if (permutation != null)
            {
                // Handle different indexing schemes
                if (graph is ReverseArcStaticGraph)
                {
                    // ReverseArcStaticGraph uses 1-based indexing
                    arc01 = permutation[arc01 - 1];
                    arc02 = permutation[arc02 - 1];
                    arc03 = permutation[arc03 - 1];
                    arc12 = permutation[arc12 - 1];
                }
                else
                {
                    // Other graphs use 0-based indexing
                    arc01 = permutation[arc01];
                    arc02 = permutation[arc02];
                    arc03 = permutation[arc03];
                    arc12 = permutation[arc12];
                }
            }
        }

        // Act
        var node0Arcs = graph.OutgoingArcs(0).ToList();
        var node1Arcs = graph.OutgoingArcs(1).ToList();
        var node2Arcs = graph.OutgoingArcs(2).ToList();

        // Assert
        node0Arcs.Should().HaveCount(3);
        node0Arcs.Should().Contain(new[] { arc01, arc02, arc03 });
        
        node1Arcs.Should().HaveCount(1);
        node1Arcs.Should().Contain(arc12);
        
        node2Arcs.Should().BeEmpty();

        // Verify arc properties
        foreach (var arc in node0Arcs)
        {
            graph.Tail(arc).Should().Be(0);
            graph.Head(arc).Should().BeOneOf(1, 2, 3);
        }
    }

    [Fact]
    public void OutDegree_ShouldReturnCorrectCount()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        graph.AddArc(0, 1);
        graph.AddArc(0, 2);
        graph.AddArc(1, 2);

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Act & Assert
        graph.OutDegree(0).Should().Be(2);
        graph.OutDegree(1).Should().Be(1);
        graph.OutDegree(2).Should().Be(0);
    }

    [Fact]
    public void OutgoingArcsStartingFrom_ShouldResumeIteration()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(3);

        var arcs = new List<int>();
        arcs.Add(graph.AddArc(0, 1));
        arcs.Add(graph.AddArc(0, 2));
        arcs.Add(graph.AddArc(0, 3));

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Act
        var allArcs = graph.OutgoingArcs(0).ToList();
        var resumedArcs = graph.OutgoingArcsStartingFrom(0, allArcs[1]).ToList();

        // Assert
        resumedArcs.Should().HaveCount(2);
        resumedArcs[0].Should().Be(allArcs[1]);
        resumedArcs[1].Should().Be(allArcs[2]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SelfLoop_ShouldBeHandledCorrectly()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);

        // Act
        var arc = graph.AddArc(0, 0);

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Assert
        graph.Head(arc).Should().Be(0);
        graph.Tail(arc).Should().Be(0);
        graph.OutgoingArcs(0).Should().Contain(arc);
        graph.OutDegree(0).Should().Be(1);
    }

    [Fact]
    public void MultipleArcsBetweenNodes_ShouldBeAllowed()
    {
        // Arrange
        var graph = CreateGraph();
        graph.AddNode(0);
        graph.AddNode(1);

        // Act
        var arc1 = graph.AddArc(0, 1);
        var arc2 = graph.AddArc(0, 1);
        var arc3 = graph.AddArc(0, 1);

        if (RequiresBuild)
        {
            graph.Build();
        }

        // Assert
        graph.NumArcs.Should().Be(3);
        graph.OutDegree(0).Should().Be(3);
        var outArcs = graph.OutgoingArcs(0).ToList();
        outArcs.Should().HaveCount(3);
        outArcs.All(a => graph.Head(a) == 1).Should().BeTrue();
    }

    #endregion
}