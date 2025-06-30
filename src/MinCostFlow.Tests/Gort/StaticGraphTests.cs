using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class StaticGraphTests : GraphBaseTests
{
    protected override IGraphBase CreateGraph() => new StaticGraph();
    protected override bool RequiresBuild => true;
    protected override bool SupportsReverseArcs => false;

    [Fact]
    public void Build_ShouldBeRequiredBeforeQueries()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddArc(0, 1);

        // Act & Assert - operations should throw before Build()
        Assert.Throws<InvalidOperationException>(() => graph.Head(0));
        Assert.Throws<InvalidOperationException>(() => graph.Tail(0));
        Assert.Throws<InvalidOperationException>(() => graph.OutDegree(0));
        Assert.Throws<InvalidOperationException>(() => graph.OutgoingArcs(0).ToList());
        Assert.Throws<InvalidOperationException>(() => graph.OutgoingArcsStartingFrom(0, 0).ToList());
    }

    [Fact]
    public void Build_ShouldReturnPermutationWhenArcsReordered()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        // Add arcs in non-tail order
        int arc1 = graph.AddArc(1, 2); // Should become index 2
        int arc2 = graph.AddArc(0, 1); // Should become index 0
        int arc3 = graph.AddArc(2, 0); // Should become index 3
        int arc4 = graph.AddArc(0, 2); // Should become index 1

        // Act
        var permutation = graph.Build();

        // Assert
        permutation.Should().NotBeNull();
        permutation.Should().HaveCount(4);
        
        // Verify the permutation maps old indices to new indices correctly
        permutation![arc1].Should().Be(2);
        permutation[arc2].Should().Be(0);
        permutation[arc3].Should().Be(3);
        permutation[arc4].Should().Be(1);

        // Verify arcs are now ordered by tail
        graph.Tail(0).Should().Be(0);
        graph.Tail(1).Should().Be(0);
        graph.Tail(2).Should().Be(1);
        graph.Tail(3).Should().Be(2);
    }

    [Fact]
    public void Build_WithAlreadySortedArcs_ShouldReturnIdentityPermutation()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);

        // Add arcs in tail order
        graph.AddArc(0, 1);
        graph.AddArc(0, 2);
        graph.AddArc(1, 2);
        graph.AddArc(2, 0);

        // Act
        var permutation = graph.Build();

        // Assert
        permutation.Should().NotBeNull();
        GraphUtilities.IsIdentityPermutation(permutation!).Should().BeTrue();
    }

    [Fact]
    public void Build_CalledTwice_ShouldReturnNull()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddArc(0, 0);
        
        // Act
        var perm1 = graph.Build();
        var perm2 = graph.Build();

        // Assert
        perm1.Should().NotBeNull();
        perm2.Should().BeNull();
    }

    [Fact]
    public void AddNodeOrArcAfterBuild_ShouldThrow()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => graph.AddNode(2));
        Assert.Throws<InvalidOperationException>(() => graph.AddArc(0, 1));
    }

    [Fact]
    public void OutDegree_ShouldBeO1()
    {
        // Arrange
        var graph = new StaticGraph();
        for (int i = 0; i < 100; i++)
        {
            graph.AddNode(i);
        }

        // Node 0 has many outgoing arcs
        for (int i = 1; i < 100; i++)
        {
            graph.AddArc(0, i);
        }

        graph.Build();

        // Act & Assert - Should be O(1), not require iteration
        graph.OutDegree(0).Should().Be(99);
        graph.OutDegree(50).Should().Be(0);
    }

    [Fact]
    public void MemoryEfficiency_ShouldMatchSpecification()
    {
        // Arrange
        var graph = new StaticGraph();
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
        // Memory usage should be: 1 × (NodeCapacity + 1) + 2 × ArcCapacity integer values
        // We can't directly measure memory, but we can verify the structure
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(1000);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(5000);
        graph.NumNodes.Should().Be(500);
        graph.NumArcs.Should().Be(1000);
    }

    [Fact]
    public void LargeGraph_ShouldHandleEfficiently()
    {
        // Arrange
        var graph = new StaticGraph();
        const int numNodes = 10000;
        const int numArcs = 50000;

        // Pre-allocate for efficiency
        graph.Reserve(numNodes, numArcs);

        // Create a random graph
        var random = new Random(42);
        for (int i = 0; i < numArcs; i++)
        {
            int tail = random.Next(numNodes);
            int head = random.Next(numNodes);
            graph.AddArc(tail, head);
        }

        // Act
        var permutation = graph.Build();

        // Assert
        permutation.Should().NotBeNull();
        permutation.Should().HaveCount(numArcs);
        graph.NumNodes.Should().Be(numNodes);
        graph.NumArcs.Should().Be(numArcs);

        // Verify all arcs are accessible and correctly sorted by tail
        int lastTail = -1;
        foreach (int arc in graph.AllForwardArcs())
        {
            int tail = graph.Tail(arc);
            tail.Should().BeGreaterOrEqualTo(lastTail);
            lastTail = tail;
        }
    }

    [Fact]
    public void HighDegreeNode_ShouldIterateEfficiently()
    {
        // Arrange
        var graph = new StaticGraph();
        const int hubNode = 0;
        const int numConnections = 1000;

        // Create a hub node with many connections
        for (int i = 0; i <= numConnections; i++)
        {
            graph.AddNode(i);
        }
        for (int i = 1; i <= numConnections; i++)
        {
            graph.AddArc(hubNode, i);
        }

        graph.Build();

        // Act
        var outgoingArcs = graph.OutgoingArcs(hubNode).ToList();

        // Assert
        outgoingArcs.Should().HaveCount(numConnections);
        graph.OutDegree(hubNode).Should().Be(numConnections);

        // Verify contiguous storage
        for (int i = 1; i < outgoingArcs.Count; i++)
        {
            outgoingArcs[i].Should().Be(outgoingArcs[i - 1] + 1);
        }
    }

    [Fact]
    public void EmptyGraph_BuildShouldWork()
    {
        // Arrange
        var graph = new StaticGraph();

        // Act
        var permutation = graph.Build();

        // Assert
        permutation.Should().BeNull();
        graph.NumNodes.Should().Be(0);
        graph.NumArcs.Should().Be(0);
    }

    [Fact]
    public void NodesWithoutArcs_ShouldBeHandledCorrectly()
    {
        // Arrange
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(3);
        
        // Only add arcs from nodes 0 and 2
        graph.AddArc(0, 1);
        graph.AddArc(2, 3);

        // Act
        graph.Build();

        // Assert
        graph.OutDegree(0).Should().Be(1);
        graph.OutDegree(1).Should().Be(0);
        graph.OutDegree(2).Should().Be(1);
        graph.OutDegree(3).Should().Be(0);
        
        graph.OutgoingArcs(1).Should().BeEmpty();
        graph.OutgoingArcs(3).Should().BeEmpty();
    }
}