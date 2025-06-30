using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;
using System.Numerics;
using Xunit.Abstractions;
using MinCostFlow.Core.Utils;

namespace MinCostFlow.Tests.Gort;

public class XunitTestLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    
    public XunitTestLogger(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public void Log(string message)
    {
        _output.WriteLine(message);
    }
}

public class GenericMaxFlowTests
{
    private readonly ITestOutputHelper _output;
    
    public GenericMaxFlowTests(ITestOutputHelper output)
    {
        _output = output;
    }
    #region Basic Feasibility Tests

    [Fact]
    public void SimplePath_ShouldFindMaxFlow()
    {
        // Arrange - Linear chain: 0→1→2→3
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(3);

        var arc01 = graph.AddArc(0, 1);
        var arc12 = graph.AddArc(1, 2);
        var arc23 = graph.AddArc(2, 3);

        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 3);
        maxFlow.SetArcCapacity(arc01, 8);
        maxFlow.SetArcCapacity(arc12, 10);
        maxFlow.SetArcCapacity(arc23, 8);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(8);
        
        // Verify flow on each arc
        maxFlow.Flow(arc01).Should().Be(8);
        maxFlow.Flow(arc12).Should().Be(8);
        maxFlow.Flow(arc23).Should().Be(8);
        
        // Verify flow conservation
        ValidateFlowConservation(graph, maxFlow, 0, 3);
    }

    [Fact]
    public void MultiplePaths_ShouldDistributeFlow()
    {
        // Arrange - Diamond-like structure
        var graph = new ReverseArcListGraph();
        for (int i = 0; i < 6; i++)
        {
            graph.AddNode(i);
        }

        // Create paths: 0 -> {1,2} -> {3,4} -> 5
        var arc01 = graph.AddArc(0, 1);
        _output.WriteLine($"Added arc {arc01}: 0->1");
        var arc02 = graph.AddArc(0, 2);
        _output.WriteLine($"Added arc {arc02}: 0->2");
        var arc13 = graph.AddArc(1, 3);
        _output.WriteLine($"Added arc {arc13}: 1->3, opposite arc = {~arc13}");
        _output.WriteLine($"After adding arc13, Node 3 incoming arcs: {string.Join(", ", graph.IncomingArcs(3))}");
        var arc14 = graph.AddArc(1, 4);
        _output.WriteLine($"Added arc {arc14}: 1->4");
        var arc23 = graph.AddArc(2, 3);
        _output.WriteLine($"Added arc {arc23}: 2->3, opposite arc = {~arc23}");
        _output.WriteLine($"After adding arc23, Node 3 incoming arcs: {string.Join(", ", graph.IncomingArcs(3))}");
        var arc24 = graph.AddArc(2, 4);
        _output.WriteLine($"Added arc {arc24}: 2->4");
        var arc35 = graph.AddArc(3, 5);
        _output.WriteLine($"Added arc {arc35}: 3->5");
        var arc45 = graph.AddArc(4, 5);
        _output.WriteLine($"Added arc {arc45}: 4->5");

        // Create logger for debugging
        var logger = new XunitTestLogger(_output);
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 5, logger);
        
        // Set capacities
        maxFlow.SetArcCapacity(arc01, 10);
        maxFlow.SetArcCapacity(arc02, 10);
        maxFlow.SetArcCapacity(arc13, 5);
        maxFlow.SetArcCapacity(arc14, 5);
        maxFlow.SetArcCapacity(arc23, 5);
        maxFlow.SetArcCapacity(arc24, 5);
        maxFlow.SetArcCapacity(arc35, 10);
        maxFlow.SetArcCapacity(arc45, 10);

        // Debug: Check graph structure before solving
        _output.WriteLine("Graph structure:");
        _output.WriteLine($"Arc numbers: arc01={arc01}, arc02={arc02}, arc13={arc13}, arc14={arc14}, arc23={arc23}, arc24={arc24}, arc35={arc35}, arc45={arc45}");
        _output.WriteLine($"Opposite arcs: ~{arc13}={~arc13}, ~{arc14}={~arc14}, ~{arc23}={~arc23}, ~{arc24}={~arc24}");
        _output.WriteLine($"Node 3 outgoing arcs: {string.Join(", ", graph.OutgoingArcs(3))}");
        _output.WriteLine($"Node 3 incoming arcs: {string.Join(", ", graph.IncomingArcs(3))}");
        _output.WriteLine($"Node 3 opposite incoming arcs: {string.Join(", ", graph.OppositeIncomingArcs(3))}");
        _output.WriteLine($"Node 3 all arcs: {string.Join(", ", graph.OutgoingOrOppositeIncomingArcs(3))}");
        _output.WriteLine($"Node 4 all arcs: {string.Join(", ", graph.OutgoingOrOppositeIncomingArcs(4))}");
        _output.WriteLine($"Node 5 all arcs: {string.Join(", ", graph.OutgoingOrOppositeIncomingArcs(5))}");
        
        // Debug: Verify what we expect
        var node3Arcs = graph.OutgoingOrOppositeIncomingArcs(3).ToList();
        _output.WriteLine($"CRITICAL: Node 3 should have arcs 7, -6, -4 but has: {string.Join(", ", node3Arcs)}");
        if (!node3Arcs.Contains(-4))
        {
            _output.WriteLine("ERROR: Arc -4 is missing from node 3's adjacency list!");
        }
        _output.WriteLine("");
        
        // Act
        var solved = maxFlow.Solve();

        // Debug output
        _output.WriteLine($"Solved: {solved}");
        _output.WriteLine($"Status: {maxFlow.status}");
        _output.WriteLine($"Optimal Flow: {maxFlow.GetOptimalFlow()}");
        _output.WriteLine("");
        _output.WriteLine("Arc flows and residual capacities:");
        _output.WriteLine($"Arc {arc01} (0->1): flow={maxFlow.Flow(arc01)}, capacity={maxFlow.Capacity(arc01)}");
        _output.WriteLine($"Arc {arc02} (0->2): flow={maxFlow.Flow(arc02)}, capacity={maxFlow.Capacity(arc02)}");
        _output.WriteLine($"Arc {arc13} (1->3): flow={maxFlow.Flow(arc13)}, capacity={maxFlow.Capacity(arc13)}");
        _output.WriteLine($"Arc {arc14} (1->4): flow={maxFlow.Flow(arc14)}, capacity={maxFlow.Capacity(arc14)}");
        _output.WriteLine($"Arc {arc23} (2->3): flow={maxFlow.Flow(arc23)}, capacity={maxFlow.Capacity(arc23)}");
        _output.WriteLine($"Arc {arc24} (2->4): flow={maxFlow.Flow(arc24)}, capacity={maxFlow.Capacity(arc24)}");
        _output.WriteLine($"Arc {arc35} (3->5): flow={maxFlow.Flow(arc35)}, capacity={maxFlow.Capacity(arc35)}");
        _output.WriteLine($"Arc {arc45} (4->5): flow={maxFlow.Flow(arc45)}, capacity={maxFlow.Capacity(arc45)}");

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(20);
        
        // Verify total flow from source
        (maxFlow.Flow(arc01) + maxFlow.Flow(arc02)).Should().Be(20);
        
        // Verify total flow to sink
        (maxFlow.Flow(arc35) + maxFlow.Flow(arc45)).Should().Be(20);
        
        ValidateFlowConservation(graph, maxFlow, 0, 5);
    }

    [Fact]
    public void MultipleArcs_ShouldMaintainIndependentFlows()
    {
        // Arrange - Multiple arcs between same nodes
        var graph = new ReverseArcListGraph();
        for (int i = 0; i < 5; i++)
        {
            graph.AddNode(i);
        }

        // Create duplicate arcs
        var arc01_1 = graph.AddArc(0, 1);
        var arc01_2 = graph.AddArc(0, 1);
        var arc12_1 = graph.AddArc(1, 2);
        var arc12_2 = graph.AddArc(1, 2);
        var arc23 = graph.AddArc(2, 3);
        var arc34_1 = graph.AddArc(3, 4);
        var arc34_2 = graph.AddArc(3, 4);
        var arc34_3 = graph.AddArc(3, 4);

        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 4);
        
        // Set different capacities
        maxFlow.SetArcCapacity(arc01_1, 5);
        maxFlow.SetArcCapacity(arc01_2, 7);
        maxFlow.SetArcCapacity(arc12_1, 6);
        maxFlow.SetArcCapacity(arc12_2, 8);
        maxFlow.SetArcCapacity(arc23, 15);
        maxFlow.SetArcCapacity(arc34_1, 4);
        maxFlow.SetArcCapacity(arc34_2, 5);
        maxFlow.SetArcCapacity(arc34_3, 6);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        
        // Verify each arc maintains independent flow
        maxFlow.Flow(arc01_1).Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(5);
        maxFlow.Flow(arc01_2).Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(7);
        
        // Verify total flow
        var totalFlow = maxFlow.Flow(arc34_1) + maxFlow.Flow(arc34_2) + maxFlow.Flow(arc34_3);
        totalFlow.Should().Be(maxFlow.GetOptimalFlow());
        
        ValidateFlowConservation(graph, maxFlow, 0, 4);
    }

    [Fact]
    public void DirectSourceSinkArc_ShouldUseDirectPath()
    {
        // Arrange - Direct arc from source to sink plus other paths
        var graph = new ReverseArcListGraph();
        for (int i = 0; i < 4; i++)
        {
            graph.AddNode(i);
        }

        var arcDirect = graph.AddArc(0, 3); // Direct source to sink
        var arc01 = graph.AddArc(0, 1);
        var arc12 = graph.AddArc(1, 2);
        var arc23 = graph.AddArc(2, 3);

        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 3);
        
        maxFlow.SetArcCapacity(arcDirect, 10);
        maxFlow.SetArcCapacity(arc01, 5);
        maxFlow.SetArcCapacity(arc12, 5);
        maxFlow.SetArcCapacity(arc23, 5);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(15);
        
        // Both paths should be used
        maxFlow.Flow(arcDirect).Should().Be(10);
        maxFlow.Flow(arc01).Should().Be(5);
        
        ValidateFlowConservation(graph, maxFlow, 0, 3);
    }

    #endregion

    #region Capacity Limit Tests

    [Fact]
    public void HugeCapacity_ShouldHandleMaxValues()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var arc = graph.AddArc(0, 1);
        
        var logger = new XunitTestLogger(_output);
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1, logger);
        maxFlow.SetArcCapacity(arc, int.MaxValue);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(int.MaxValue);
        maxFlow.Flow(arc).Should().Be(int.MaxValue);
    }

    [Fact]
    public void FlowOverflowLimit_ShouldReturnOptimal()
    {
        // Arrange - Total capacity = long.MaxValue
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        var arc01 = graph.AddArc(0, 1);
        var arc12 = graph.AddArc(1, 2);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 2);
        
        // Set capacities that sum to less than long.MaxValue
        maxFlow.SetArcCapacity(arc01, int.MaxValue);
        maxFlow.SetArcCapacity(arc12, int.MaxValue);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(int.MaxValue);
    }

    [Fact]
    public void FlowOverflow_ShouldHandleIntMaxFlow()
    {
        // Arrange - Use int flow sum type with large capacities
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        // Create multiple parallel arcs that could theoretically exceed int.MaxValue
        var arcs = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            arcs.Add(graph.AddArc(0, 1));
        }
        var arc12 = graph.AddArc(1, 2);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, int>(graph, 0, 2);
        
        // Set capacities that would sum to more than int.MaxValue
        foreach (var arc in arcs)
        {
            maxFlow.SetArcCapacity(arc, int.MaxValue / 2);
        }
        maxFlow.SetArcCapacity(arc12, int.MaxValue);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        // The algorithm should find the optimal flow limited by the bottleneck arc12
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, int>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(int.MaxValue);
        
        // INT_OVERFLOW is only reported when we reach MaxFlowSum AND there's still
        // an augmenting path, meaning we can't represent the true max flow value
    }

    #endregion

    #region Graph Structure Tests

    [Fact]
    public void DisconnectedGraph_ShouldReturnZeroFlow()
    {
        // Arrange - Source and sink in different components
        var graph = new ReverseArcListGraph();
        for (int i = 0; i < 6; i++)
        {
            graph.AddNode(i);
        }
        
        // Component 1: 0-1-2
        var arc01 = graph.AddArc(0, 1);
        var arc12 = graph.AddArc(1, 2);
        
        // Component 2: 3-4-5 (disconnected)
        var arc34 = graph.AddArc(3, 4);
        var arc45 = graph.AddArc(4, 5);
        
        var logger = new XunitTestLogger(_output);
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 5, logger);
        
        // Set capacities for arcs in each component
        maxFlow.SetArcCapacity(arc01, 10);
        maxFlow.SetArcCapacity(arc12, 10);
        maxFlow.SetArcCapacity(arc34, 10);
        maxFlow.SetArcCapacity(arc45, 10);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(0);
        
        // Verify min-cut separates components
        var sourceSide = new List<int>();
        maxFlow.GetSourceSideMinCut(sourceSide);
        
        // Debug output
        _output.WriteLine($"Source side nodes: {string.Join(", ", sourceSide)}");
        
        // In a disconnected graph after max flow computation, the algorithm will have
        // attempted to push flow from source to reachable nodes, but since they can't
        // reach the sink, the flow is pushed back, leaving residual capacity 0 on 
        // forward arcs. Thus only the source is in the source side of the min-cut.
        sourceSide.Should().Equal(0);
        
        // Verify sink side contains the sink
        var sinkSide = new List<int>();
        maxFlow.GetSinkSideMinCut(sinkSide);
        sinkSide.Should().Contain(5);
    }

    [Fact]
    public void EmptyGraph_ShouldHandleGracefully()
    {
        // Arrange - No arcs
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(0);
    }

    [Fact]
    public void SingleNode_SourceEqualsSink_ShouldReturnZero()
    {
        // Arrange - Degenerate case
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 0);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(0);
    }

    [Fact]
    public void InvalidSourceOrSink_ShouldReturnOptimalWithZeroFlow()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddArc(0, 1);
        
        // Test invalid source
        var maxFlow1 = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 5, 1);
        var solved1 = maxFlow1.Solve();
        solved1.Should().BeTrue();
        maxFlow1.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow1.GetOptimalFlow().Should().Be(0);
        
        // Test invalid sink
        var maxFlow2 = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 5);
        var solved2 = maxFlow2.Solve();
        solved2.Should().BeTrue();
        maxFlow2.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, int, long>.Status.OPTIMAL);
        maxFlow2.GetOptimalFlow().Should().Be(0);
    }

    #endregion

    #region Type Flexibility Tests

    [Fact]
    public void UnsignedFlowType_ShouldWork()
    {
        // Arrange - Use uint for arc flow type
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var arc = graph.AddArc(0, 1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, uint, long>(graph, 0, 1);
        maxFlow.SetArcCapacity(arc, 100u);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<ReverseArcListGraph, uint, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(100);
        maxFlow.Flow(arc).Should().Be(100);
    }

    [Fact]   
    public void StandardGraph_ShouldWorkWithNonNegativeArcs()
    {
        // Arrange - Use StaticGraph which doesn't have negative reverse arcs
        var graph = new StaticGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        
        // Add forward arcs
        var arc01 = graph.AddArc(0, 1);
        var arc12 = graph.AddArc(1, 2);
        
        // Add reverse arcs (required for push-relabel algorithm)
        var arc10 = graph.AddArc(1, 0);
        var arc21 = graph.AddArc(2, 1);
        
        graph.Build();
        
        var maxFlow = new GenericMaxFlow<StaticGraph, int, long>(graph, 0, 2);
        maxFlow.SetArcCapacity(arc01, 50);
        maxFlow.SetArcCapacity(arc12, 30);
        maxFlow.SetArcCapacity(arc10, 0); // Reverse arcs start with 0 capacity
        maxFlow.SetArcCapacity(arc21, 0);

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.status.Should().Be(GenericMaxFlow<StaticGraph, int, long>.Status.OPTIMAL);
        maxFlow.GetOptimalFlow().Should().Be(30);
        
        // Note: We can't use ValidateFlowConservation for standard graphs
        // because it assumes automatic reverse arcs
    }

    #endregion

    #region Min-Cut Tests

    [Fact]
    public void MinCut_ShouldPartitionGraph()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        for (int i = 0; i < 6; i++)
        {
            graph.AddNode(i);
        }
        
        // Create a bottleneck at arcs 1->3 and 2->4
        var arc01 = graph.AddArc(0, 1);
        var arc02 = graph.AddArc(0, 2);
        var arc13 = graph.AddArc(1, 3);
        var arc24 = graph.AddArc(2, 4);
        var arc35 = graph.AddArc(3, 5);
        var arc45 = graph.AddArc(4, 5);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 5);
        
        // Set capacities with bottleneck
        maxFlow.SetArcCapacity(arc01, 100);
        maxFlow.SetArcCapacity(arc02, 100);
        maxFlow.SetArcCapacity(arc13, 5);  // Bottleneck
        maxFlow.SetArcCapacity(arc24, 3);  // Bottleneck
        maxFlow.SetArcCapacity(arc35, 100);
        maxFlow.SetArcCapacity(arc45, 100);

        // Act
        var solved = maxFlow.Solve();
        
        var sourceSide = new List<int>();
        var sinkSide = new List<int>();
        maxFlow.GetSourceSideMinCut(sourceSide);
        maxFlow.GetSinkSideMinCut(sinkSide);

        // Assert
        solved.Should().BeTrue();
        maxFlow.GetOptimalFlow().Should().Be(8);
        
        // Source side should include nodes before bottleneck
        sourceSide.Should().Contain(new[] { 0, 1, 2 });
        
        // Sink side should include nodes after bottleneck
        sinkSide.Should().Contain(new[] { 3, 4, 5 });
        
        // Verify cut value equals max flow
        long cutValue = 0;
        if (sourceSide.Contains(1) && !sourceSide.Contains(3))
            cutValue += maxFlow.Capacity(arc13);
        if (sourceSide.Contains(2) && !sourceSide.Contains(4))
            cutValue += maxFlow.Capacity(arc24);
        cutValue.Should().Be(8);
    }

    #endregion

    #region Validation Helpers

    private void ValidateFlowConservation<TGraph, TArcFlow, TFlowSum>(
        TGraph graph, 
        GenericMaxFlow<TGraph, TArcFlow, TFlowSum> maxFlow,
        int source,
        int sink)
        where TGraph : IMaxFlowGraph
        where TArcFlow : unmanaged, INumber<TArcFlow>
        where TFlowSum : unmanaged, INumber<TFlowSum>
    {
        // For reverse arc graphs with automatic flow tracking
        if (graph.HasNegativeReverseArcs)
        {
            // For each node except source and sink, verify flow in = flow out
            foreach (var node in graph.AllNodes())
            {
                if (node == source || node == sink)
                    continue;
                    
                long flowIn = 0;
                long flowOut = 0;
                
                // For graphs with negative reverse arcs, we need to handle flow differently
                if (graph is ReverseArcListGraph reverseGraph)
                {
                    // Calculate outgoing flow
                    foreach (var arc in reverseGraph.OutgoingArcs(node))
                    {
                        flowOut += Convert.ToInt64(maxFlow.Flow(arc));
                    }
                    
                    // Calculate incoming flow
                    foreach (var arc in reverseGraph.IncomingArcs(node))
                    {
                        flowIn += Convert.ToInt64(maxFlow.Flow(arc));
                    }
                }
                
                flowIn.Should().Be(flowOut, $"Flow conservation violated at node {node}");
            }
            
            // Verify antisymmetry for reverse arc graphs
            foreach (var arc in graph.AllForwardArcs())
            {
                var reverseArc = graph.OppositeArc(arc);
                var forwardFlow = Convert.ToInt64(maxFlow.Flow(arc));
                var reverseFlow = Convert.ToInt64(maxFlow.Flow(reverseArc));
                forwardFlow.Should().Be(-reverseFlow, $"Antisymmetry violated for arc {arc}");
            }
        }
        
        // Verify capacity constraints
        foreach (var arc in graph.AllForwardArcs())
        {
            var flow = Convert.ToInt64(maxFlow.Flow(arc));
            var capacity = Convert.ToInt64(maxFlow.Capacity(arc));
            flow.Should().BeGreaterThanOrEqualTo(0, $"Negative flow on arc {arc}");
            flow.Should().BeLessThanOrEqualTo(capacity, $"Flow exceeds capacity on arc {arc}");
        }
    }

    #endregion

    #region Self-Loop Tests

    [Fact]
    public void SelfLoop_ShouldBeIgnored()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var arc01 = graph.AddArc(0, 1);
        var selfLoop = graph.AddArc(0, 0); // Self-loop
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        maxFlow.SetArcCapacity(arc01, 10);
        maxFlow.SetArcCapacity(selfLoop, 100); // Should be ignored

        // Act
        var solved = maxFlow.Solve();

        // Assert
        solved.Should().BeTrue();
        maxFlow.GetOptimalFlow().Should().Be(10);
        maxFlow.Flow(selfLoop).Should().Be(0);
    }

    #endregion

    #region Augmenting Path Tests

    [Fact]
    public void AugmentingPathExists_ShouldDetectCorrectly()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var arc = graph.AddArc(0, 1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        maxFlow.SetArcCapacity(arc, 10);

        // Before solving, augmenting path should exist
        maxFlow.AugmentingPathExists().Should().BeTrue();

        // Act
        var solved = maxFlow.Solve();

        // Assert - After solving, no augmenting path should exist
        solved.Should().BeTrue();
        maxFlow.AugmentingPathExists().Should().BeFalse();
    }

    #endregion

    #region Debug and Diagnostic Tests

    [Fact]
    public void ZVector_ShouldSupportNegativeIndices()
    {
        // Test ZVector behavior for negative reverse arcs
        var zVector = ZVector<int>.ForArcs(10);
        
        // Set values
        zVector[1] = 100;
        zVector[-1] = 200;
        
        // Test that they are different
        zVector[1].Should().Be(100);
        zVector[-1].Should().Be(200);
    }

    [Fact]
    public void Flow_InitialState_ShouldBeZero()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        var arc = graph.AddArc(0, 1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        maxFlow.SetArcCapacity(arc, 10);
        
        // Before solving, flow should be 0
        maxFlow.Flow(arc).Should().Be(0, "flow should be 0 before solving");
    }

    [Fact]
    public void Capacity_NegativeReverseArcs_ShouldBeZero()
    {
        // Test to understand capacity initialization for negative reverse arcs
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        
        var arc = graph.AddArc(0, 1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        
        // Before SetArcCapacity
        maxFlow.Capacity(arc).Should().Be(0);
        maxFlow.Capacity(ReverseArcListGraph.OppositeArc(arc)).Should().Be(0);
        
        maxFlow.SetArcCapacity(arc, 10);
        
        // After SetArcCapacity - only forward arc has capacity
        maxFlow.Capacity(arc).Should().Be(10);
        maxFlow.Capacity(ReverseArcListGraph.OppositeArc(arc)).Should().Be(0);
    }


    #endregion
}