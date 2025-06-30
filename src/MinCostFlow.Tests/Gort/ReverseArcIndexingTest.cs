using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class ReverseArcIndexingTest
{
    private readonly ITestOutputHelper _output;

    public ReverseArcIndexingTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_ArcIndexingIssue()
    {
        // This test reproduces the exact scenario where arc -4 goes missing
        var graph = new ReverseArcListGraph();
        
        // Add nodes 0-5 exactly like in MultiplePaths test
        for (int i = 0; i < 6; i++)
        {
            graph.AddNode(i);
        }
        
        // Add arcs in the exact same order as MultiplePaths test
        _output.WriteLine("Adding arcs in order:");
        
        var arc01 = graph.AddArc(0, 1);
        _output.WriteLine($"1. Added arc {arc01}: 0->1");
        _output.WriteLine($"   Graph capacity after: {graph.ArcCapacity}");
        
        var arc02 = graph.AddArc(0, 2);
        _output.WriteLine($"2. Added arc {arc02}: 0->2");
        _output.WriteLine($"   Graph capacity after: {graph.ArcCapacity}");
        
        // This is where the problem occurs
        var arc13 = graph.AddArc(1, 3);
        _output.WriteLine($"3. Added arc {arc13}: 1->3, opposite arc = {~arc13}");
        _output.WriteLine($"   Graph capacity after: {graph.ArcCapacity}");
        
        // Check node 3's adjacency list after adding arc13
        var node3ArcsAfterArc13 = graph.OutgoingOrOppositeIncomingArcs(3).ToList();
        _output.WriteLine($"   Node 3 arcs after arc13: {string.Join(", ", node3ArcsAfterArc13)}");
        _output.WriteLine($"   Should contain: -4 (which is ~3)");
        
        if (!node3ArcsAfterArc13.Contains(-4))
        {
            _output.WriteLine("   ERROR: Arc -4 is already missing!");
        }
        
        var arc14 = graph.AddArc(1, 4);
        _output.WriteLine($"4. Added arc {arc14}: 1->4");
        _output.WriteLine($"   Graph capacity after: {graph.ArcCapacity}");
        
        var arc23 = graph.AddArc(2, 3);
        _output.WriteLine($"5. Added arc {arc23}: 2->3, opposite arc = {~arc23}");
        _output.WriteLine($"   Graph capacity after: {graph.ArcCapacity}");
        
        // Final check
        var node3ArcsFinal = graph.OutgoingOrOppositeIncomingArcs(3).ToList();
        _output.WriteLine($"\nFinal Node 3 arcs: {string.Join(", ", node3ArcsFinal)}");
        _output.WriteLine($"Expected: 7, -6, -4");
        
        // Assert the specific missing arc
        node3ArcsFinal.Should().Contain(-4, "Arc -4 (~3) should be in node 3's adjacency list");
    }
    
    [Fact]
    public void TestGetNextIndexCalculation()
    {
        // Test the GetNextIndex calculation for various scenarios
        _output.WriteLine("Testing GetNextIndex calculation:");
        
        // When _arcCapacity grows to accommodate new arcs, the array must be sized correctly
        // Testing various arc capacities and their corresponding array sizes
        for (int arcCapacity = 1; arcCapacity <= 10; arcCapacity++)
        {
            _output.WriteLine($"\nTesting arcCapacity = {arcCapacity}:");
            
            // Array size must accommodate indices for both forward and reverse arcs
            int arraySize = 2 * arcCapacity + 2;
            _output.WriteLine($"  Array size = 2 * {arcCapacity} + 2 = {arraySize}");
            
            // Test forward arcs (1 to arcCapacity)
            for (int arc = 1; arc <= arcCapacity; arc++)
            {
                int index = arc;  // Forward arcs map directly
                _output.WriteLine($"  Forward arc {arc}: index = {index}");
                index.Should().BeLessThan(arraySize, $"Forward arc {arc} index should be valid");
            }
            
            // Test reverse arcs (~1 to ~arcCapacity)
            for (int arc = 1; arc <= arcCapacity; arc++)
            {
                int reverseArc = ~arc;
                int index = arcCapacity - reverseArc;  // GetNextIndex calculation
                _output.WriteLine($"  Reverse arc ~{arc}={reverseArc}: index = {arcCapacity} - ({reverseArc}) = {index}");
                index.Should().BeLessThan(arraySize, $"Reverse arc {reverseArc} index should be valid");
            }
        }
    }
}