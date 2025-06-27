using System;
using System.Linq;
using FluentAssertions;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;
using Xunit;

namespace MinCostFlow.Tests.Lemon;

public class GraphTests
{
    [Fact]
    public void CompactDigraph_AddNode_ShouldIncreaseNodeCount()
    {
        // Arrange
        var graph = new CompactDigraph();
        
        // Act
        var node1 = graph.AddNode();
        var node2 = graph.AddNode();
        
        // Assert
        graph.NodeCount.Should().Be(2);
        node1.Should().NotBe(node2);
        node1.IsValid.Should().BeTrue();
        node2.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void CompactDigraph_AddArc_ShouldIncreaseArcCount()
    {
        // Arrange
        var graph = new CompactDigraph();
        var node1 = graph.AddNode();
        var node2 = graph.AddNode();
        
        // Act
        var arc = graph.AddArc(node1, node2);
        
        // Assert
        graph.ArcCount.Should().Be(1);
        arc.IsValid.Should().BeTrue();
        graph.Source(arc).Should().Be(node1);
        graph.Target(arc).Should().Be(node2);
    }
    
    [Fact]
    public void CompactDigraph_GetOutArcs_ShouldReturnCorrectArcs()
    {
        // Arrange
        var graph = new CompactDigraph();
        var node1 = graph.AddNode();
        var node2 = graph.AddNode();
        var node3 = graph.AddNode();
        
        var arc1 = graph.AddArc(node1, node2);
        var arc2 = graph.AddArc(node1, node3);
        var arc3 = graph.AddArc(node2, node3);
        
        // Act
        var outArcs = graph.GetOutArcs(node1).ToArray();
        
        // Assert
        outArcs.Should().HaveCount(2);
        outArcs.Should().Contain(arc1);
        outArcs.Should().Contain(arc2);
        outArcs.Should().NotContain(arc3);
    }
    
    [Fact]
    public void CompactDigraph_GetInArcs_ShouldReturnCorrectArcs()
    {
        // Arrange
        var graph = new CompactDigraph();
        var node1 = graph.AddNode();
        var node2 = graph.AddNode();
        var node3 = graph.AddNode();
        
        var arc1 = graph.AddArc(node1, node3);
        var arc2 = graph.AddArc(node2, node3);
        var arc3 = graph.AddArc(node1, node2);
        
        // Act
        var inArcs = graph.GetInArcs(node3).ToArray();
        
        // Assert
        inArcs.Should().HaveCount(2);
        inArcs.Should().Contain(arc1);
        inArcs.Should().Contain(arc2);
        inArcs.Should().NotContain(arc3);
    }
    
    [Fact]
    public void CompactDigraph_NodeIteration_ShouldVisitAllNodes()
    {
        // Arrange
        var graph = new CompactDigraph();
        var nodes = new Node[5];
        for (int i = 0; i < 5; i++)
        {
            nodes[i] = graph.AddNode();
        }
        
        // Act
        var visitedNodes = new System.Collections.Generic.List<Node>();
        for (var node = graph.FirstNode(); node.IsValid; node = graph.NextNode(node))
        {
            visitedNodes.Add(node);
        }
        
        // Assert
        visitedNodes.Should().HaveCount(5);
        visitedNodes.Should().BeEquivalentTo(nodes);
    }
    
    [Fact]
    public void CompactDigraph_ArcIteration_ShouldVisitAllArcs()
    {
        // Arrange
        var graph = new CompactDigraph();
        var node1 = graph.AddNode();
        var node2 = graph.AddNode();
        var node3 = graph.AddNode();
        
        var arcs = new[]
        {
            graph.AddArc(node1, node2),
            graph.AddArc(node2, node3),
            graph.AddArc(node3, node1)
        };
        
        // Act
        var visitedArcs = new System.Collections.Generic.List<Arc>();
        for (var arc = graph.FirstArc(); arc.IsValid; arc = graph.NextArc(arc))
        {
            visitedArcs.Add(arc);
        }
        
        // Assert
        visitedArcs.Should().HaveCount(3);
        visitedArcs.Should().BeEquivalentTo(arcs);
    }
    
    [Fact]
    public void CompactDigraph_LargeGraph_ShouldHandleEfficiently()
    {
        // Arrange
        var graph = new CompactDigraph();
        const int nodeCount = 1000;
        const int arcCount = 5000;
        
        var nodes = new Node[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            nodes[i] = graph.AddNode();
        }
        
        // Act - Add random arcs
        var random = new Random(42);
        for (int i = 0; i < arcCount; i++)
        {
            int source = random.Next(nodeCount);
            int target = random.Next(nodeCount);
            graph.AddArc(nodes[source], nodes[target]);
        }
        
        // Assert
        graph.NodeCount.Should().Be(nodeCount);
        graph.ArcCount.Should().Be(arcCount);
        
        // Verify all nodes are valid
        for (int i = 0; i < nodeCount; i++)
        {
            graph.IsValidNode(nodes[i]).Should().BeTrue();
        }
    }
    
    [Fact]
    public void GraphBuilder_ShouldCreateGraphCorrectly()
    {
        // Arrange & Act
        var builder = new GraphBuilder();
        builder.AddNodes(4)
               .AddArc(0, 1)
               .AddArc(1, 2)
               .AddArc(2, 3)
               .AddArc(3, 0);
        
        var graph = builder.Build();
        
        // Assert
        graph.NodeCount.Should().Be(4);
        graph.ArcCount.Should().Be(4);
        
        // Verify cycle exists
        var node0 = builder.GetNode(0);
        var outArcs = graph.GetOutArcs(node0).ToArray();
        outArcs.Should().HaveCount(1);
        
        var target = graph.Target(outArcs[0]);
        target.Should().Be(builder.GetNode(1));
    }
}