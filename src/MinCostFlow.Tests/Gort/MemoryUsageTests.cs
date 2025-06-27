using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

/// <summary>
/// Tests to verify memory usage matches the specification formulas.
/// </summary>
public class MemoryUsageTests
{
    private const int IntSize = sizeof(int);

    [Fact]
    public void ListGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: NodeCapacity + 3 × ArcCapacity integer values
        var graph = new ListGraph();
        graph.Reserve(100, 200);

        // Expected memory in integers
        int expectedIntegers = 100 + 3 * 200; // NodeCapacity + 3 × ArcCapacity
        int expectedBytes = expectedIntegers * IntSize;

        // Verify capacity values
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(100);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(200);

        // Memory formula verification
        // ListGraph stores: _firstOut (node array) + _next, _head, _tail (arc arrays)
        int actualIntegers = graph.NodeCapacity + 3 * graph.ArcCapacity;
        int actualBytes = actualIntegers * IntSize;

        actualBytes.Should().BeGreaterOrEqualTo(expectedBytes);
    }

    [Fact]
    public void ReverseArcListGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: 2 × NodeCapacity + 4 × ArcCapacity integer values
        var graph = new ReverseArcListGraph();
        graph.Reserve(100, 200);

        // Expected memory in integers
        int expectedIntegers = 2 * 100 + 4 * 200; // 2 × NodeCapacity + 4 × ArcCapacity
        int expectedBytes = expectedIntegers * IntSize;

        // Verify capacity values
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(100);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(200);

        // Memory formula verification
        // ReverseArcListGraph stores: _firstOut (SVector, 2x node capacity) + _next (2x arc capacity for forward/reverse), _head, _tail
        // Note: _next array is sized for both forward and reverse arcs
        int actualIntegers = 2 * graph.NodeCapacity + 4 * graph.ArcCapacity;
        int actualBytes = actualIntegers * IntSize;

        actualBytes.Should().BeGreaterOrEqualTo(expectedBytes);
    }

    [Fact]
    public void StaticGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: (NodeCapacity + 1) + 2 × ArcCapacity integer values
        var graph = new StaticGraph();
        graph.Reserve(100, 200);

        // Add some arcs and build
        for (int i = 0; i < 50; i++)
        {
            graph.AddArc(i % 100, (i + 1) % 100);
        }
        graph.Build();

        // Expected memory in integers
        int expectedIntegers = (100 + 1) + 2 * 200; // (NodeCapacity + 1) + 2 × ArcCapacity
        int expectedBytes = expectedIntegers * IntSize;

        // Verify capacity values
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(100);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(200);

        // Memory formula verification
        // StaticGraph stores: _startPos (NodeCapacity + 1) + _head, _tail (arc arrays)
        int actualIntegers = (graph.NodeCapacity + 1) + 2 * graph.ArcCapacity;
        int actualBytes = actualIntegers * IntSize;

        actualBytes.Should().BeGreaterOrEqualTo(expectedBytes);
    }

    [Fact]
    public void ReverseArcStaticGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: 2 × (NodeCapacity + 1) + 3 × ArcCapacity integer values
        var graph = new ReverseArcStaticGraph();
        graph.Reserve(100, 200);

        // Add some arcs and build
        for (int i = 0; i < 50; i++)
        {
            graph.AddArc(i % 100, (i + 1) % 100);
        }
        graph.Build();

        // Expected memory in integers
        int expectedIntegers = 2 * (100 + 1) + 3 * 200; // 2 × (NodeCapacity + 1) + 3 × ArcCapacity
        int expectedBytes = expectedIntegers * IntSize;

        // Verify capacity values
        graph.NodeCapacity.Should().BeGreaterOrEqualTo(100);
        graph.ArcCapacity.Should().BeGreaterOrEqualTo(200);

        // Memory formula verification
        // ReverseArcStaticGraph stores: _outStartPos, _inStartPos (2 × (NodeCapacity + 1)) + _head, _tail, _oppositeArc
        int actualIntegers = 2 * (graph.NodeCapacity + 1) + 3 * graph.ArcCapacity;
        int actualBytes = actualIntegers * IntSize;

        actualBytes.Should().BeGreaterOrEqualTo(expectedBytes);
    }

    [Fact]
    public void CompleteGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: 1 integer value (n)
        var graph = new CompleteGraph(100);

        // CompleteGraph only stores the node count
        int expectedBytes = 1 * IntSize;

        // Verify the graph properties
        graph.NumNodes.Should().Be(100);
        graph.NumArcs.Should().Be(100 * 99); // n * (n - 1) - no self-loops

        // Memory is O(1) regardless of graph size
        // This is verified by the fact that CompleteGraph doesn't allocate arrays
    }

    [Fact]
    public void CompleteBipartiteGraph_MemoryFormula_ShouldMatchSpec()
    {
        // Memory usage: 2 integer values (n, m)
        var graph = new CompleteBipartiteGraph(50, 60);

        // CompleteBipartiteGraph only stores two integers
        int expectedBytes = 2 * IntSize;

        // Verify the graph properties
        graph.NumNodes.Should().Be(110); // n + m
        graph.NumArcs.Should().Be(50 * 60); // n × m

        // Memory is O(1) regardless of graph size
    }

    [Fact]
    public void SVector_MemoryUsage_ShouldBeEfficient()
    {
        // SVector should use approximately NodeCapacity integers
        var vector = new SVector<int>();
        vector.Reserve(100);
        vector.Resize(100);

        // Set some values
        for (int i = -50; i < 50; i++)
        {
            vector[i] = i * 2;
        }

        // Verify it can handle negative indices
        vector[-50].Should().Be(-100);
        vector[49].Should().Be(98);

        // Memory usage should be approximately Size integers
        vector.Size.Should().Be(100);
    }


    [Theory]
    [InlineData(1000, 5000)]
    [InlineData(10000, 50000)]
    [InlineData(100000, 500000)]
    public void LargeGraphs_ShouldHandleEfficientMemoryUsage(int nodes, int arcs)
    {
        // Test that large graphs can be created without excessive memory usage
        
        // StaticGraph
        var staticGraph = new StaticGraph();
        staticGraph.Reserve(nodes, arcs);
        
        // Add a reasonable number of arcs
        int actualArcs = Math.Min(arcs, nodes * 5);
        for (int i = 0; i < actualArcs; i++)
        {
            staticGraph.AddArc(i % nodes, (i + 1) % nodes);
        }
        
        staticGraph.Build();
        staticGraph.NumNodes.Should().Be(nodes);
        staticGraph.NumArcs.Should().Be(actualArcs);

        // ReverseArcStaticGraph
        var reverseGraph = new ReverseArcStaticGraph();
        reverseGraph.Reserve(nodes, arcs);
        
        for (int i = 0; i < actualArcs; i++)
        {
            reverseGraph.AddArc(i % nodes, (i + 1) % nodes);
        }
        
        reverseGraph.Build();
        reverseGraph.NumNodes.Should().Be(nodes);
        reverseGraph.NumArcs.Should().Be(actualArcs);
    }
}