using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class ReverseArcListGraphDebugTests
{
    private readonly ITestOutputHelper _output;

    public ReverseArcListGraphDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IncomingArcs_WithSpecificScenario_ShouldReturnBothArcs()
    {
        // Arrange
        var graph = new ReverseArcListGraph();
        
        // Add nodes 0-3
        for (int i = 0; i <= 3; i++)
        {
            graph.AddNode(i);
        }
        
        // Add arcs
        _output.WriteLine("Adding arcs:");
        var arc1 = graph.AddArc(0, 1);  // Arc 1: 0 -> 1
        _output.WriteLine($"Arc 1: 0 -> 1 (ID={arc1})");
        
        var arc2 = graph.AddArc(1, 2);  // Arc 2: 1 -> 2
        _output.WriteLine($"Arc 2: 1 -> 2 (ID={arc2})");
        
        var arc3 = graph.AddArc(1, 3);  // Arc 3: 1 -> 3
        _output.WriteLine($"Arc 3: 1 -> 3 (ID={arc3})");
        
        var arc4 = graph.AddArc(0, 2);  // Arc 4: 0 -> 2
        _output.WriteLine($"Arc 4: 0 -> 2 (ID={arc4})");
        
        var arc5 = graph.AddArc(2, 3);  // Arc 5: 2 -> 3
        _output.WriteLine($"Arc 5: 2 -> 3 (ID={arc5})");
        
        // Act - Check incoming arcs for node 3
        _output.WriteLine("\nChecking incoming arcs for node 3:");
        var incomingArcs = graph.IncomingArcs(3).ToList();
        
        // Log what we found
        _output.WriteLine($"Found {incomingArcs.Count} incoming arcs:");
        foreach (var arc in incomingArcs)
        {
            _output.WriteLine($"  Arc {arc}: {graph.Tail(arc)} -> {graph.Head(arc)}");
        }
        
        // Debug: Check the adjacency list for node 3
        _output.WriteLine("\nAll arcs in node 3's adjacency list:");
        var allArcs = graph.OutgoingOrOppositeIncomingArcs(3).ToList();
        foreach (var arc in allArcs)
        {
            if (arc > 0)
            {
                _output.WriteLine($"  Forward arc {arc}: {graph.Tail(arc)} -> {graph.Head(arc)}");
            }
            else
            {
                _output.WriteLine($"  Reverse arc {arc} (= ~{~arc}): represents incoming arc {~arc}: {graph.Tail(~arc)} -> {graph.Head(~arc)}");
            }
        }
        
        // Assert
        incomingArcs.Should().HaveCount(2);
        incomingArcs.Should().Contain(arc3, "arc3 (1->3) should be an incoming arc to node 3");
        incomingArcs.Should().Contain(arc5, "arc5 (2->3) should be an incoming arc to node 3");
    }
    
    [Fact]
    public void IncomingArcs_AfterMultipleAdditions_ShouldMaintainCorrectList()
    {
        // This test verifies that the linked list structure is maintained correctly
        // when adding multiple arcs in different orders
        
        var graph = new ReverseArcListGraph();
        
        // Add nodes
        graph.AddNode(0);
        graph.AddNode(1);
        graph.AddNode(2);
        graph.AddNode(3);
        
        // First, add arc 2->3
        var arc1 = graph.AddArc(2, 3);
        _output.WriteLine($"Added arc {arc1}: 2 -> 3");
        
        // Check incoming arcs for node 3
        var incoming1 = graph.IncomingArcs(3).ToList();
        _output.WriteLine($"After first arc, node 3 has {incoming1.Count} incoming arcs: {string.Join(", ", incoming1)}");
        incoming1.Should().Equal(arc1);
        
        // Add arc 1->3
        var arc2 = graph.AddArc(1, 3);
        _output.WriteLine($"Added arc {arc2}: 1 -> 3");
        
        // Check incoming arcs for node 3 again
        var incoming2 = graph.IncomingArcs(3).ToList();
        _output.WriteLine($"After second arc, node 3 has {incoming2.Count} incoming arcs: {string.Join(", ", incoming2)}");
        
        // The order might be reversed due to linked list insertion at head
        incoming2.Should().HaveCount(2);
        incoming2.Should().Contain(arc1);
        incoming2.Should().Contain(arc2);
        
        // Add arc 0->3
        var arc3 = graph.AddArc(0, 3);
        _output.WriteLine($"Added arc {arc3}: 0 -> 3");
        
        // Final check
        var incoming3 = graph.IncomingArcs(3).ToList();
        _output.WriteLine($"After third arc, node 3 has {incoming3.Count} incoming arcs: {string.Join(", ", incoming3)}");
        
        incoming3.Should().HaveCount(3);
        incoming3.Should().Contain(new[] { arc1, arc2, arc3 });
    }
}