using System;
using System.Linq;
using MinCostFlow.Core.Gort;

class TestIncomingArcs
{
    static void Main()
    {
        var graph = new ReverseArcListGraph();
        
        // Add nodes 0-3
        for (int i = 0; i <= 3; i++)
        {
            graph.AddNode(i);
        }
        
        // Add arcs
        Console.WriteLine("Adding arcs:");
        var arc1 = graph.AddArc(0, 1);  // Arc 1: 0 -> 1
        Console.WriteLine($"Arc 1: {0} -> {1}");
        
        var arc2 = graph.AddArc(1, 2);  // Arc 2: 1 -> 2
        Console.WriteLine($"Arc 2: {1} -> {2}");
        
        var arc3 = graph.AddArc(1, 3);  // Arc 3: 1 -> 3
        Console.WriteLine($"Arc 3: {1} -> {3}");
        
        var arc4 = graph.AddArc(0, 2);  // Arc 4: 0 -> 2
        Console.WriteLine($"Arc 4: {0} -> {2}");
        
        var arc5 = graph.AddArc(2, 3);  // Arc 5: 2 -> 3
        Console.WriteLine($"Arc 5: {2} -> {3}");
        
        Console.WriteLine($"\nArc IDs: arc1={arc1}, arc2={arc2}, arc3={arc3}, arc4={arc4}, arc5={arc5}");
        
        // Check incoming arcs for node 3
        Console.WriteLine("\nIncoming arcs for node 3:");
        var incomingArcs = graph.IncomingArcs(3).ToList();
        foreach (var arc in incomingArcs)
        {
            Console.WriteLine($"  Arc {arc}: {graph.Tail(arc)} -> {graph.Head(arc)}");
        }
        
        Console.WriteLine($"\nExpected: arcs {arc3} and {arc5} (arcs 1->3 and 2->3)");
        Console.WriteLine($"Actual: {string.Join(", ", incomingArcs)}");
        
        // Let's debug the internal state
        Console.WriteLine("\n--- Debugging Internal State ---");
        
        // Check what's in the adjacency list for node 3
        Console.WriteLine("\nAll arcs in node 3's adjacency list:");
        var allArcs = graph.OutgoingOrOppositeIncomingArcs(3).ToList();
        foreach (var arc in allArcs)
        {
            if (arc > 0)
            {
                Console.WriteLine($"  Forward arc {arc}: {graph.Tail(arc)} -> {graph.Head(arc)}");
            }
            else
            {
                Console.WriteLine($"  Reverse arc {arc} (= ~{~arc}): represents incoming arc {~arc}: {graph.Tail(~arc)} -> {graph.Head(~arc)}");
            }
        }
        
        // Check OppositeIncomingArcs
        Console.WriteLine("\nOpposite incoming arcs for node 3:");
        var oppositeIncoming = graph.OppositeIncomingArcs(3).ToList();
        foreach (var arc in oppositeIncoming)
        {
            Console.WriteLine($"  Arc {arc} (= ~{~arc})");
        }
    }
}