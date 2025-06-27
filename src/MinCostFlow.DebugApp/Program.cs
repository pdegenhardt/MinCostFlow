using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

namespace MinCostFlow.DebugApp;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing ReverseArcGraph...");
        
        // Test the graph structure first
        var graph = new ReverseArcGraph();
        var nodes = new[] { graph.AddNode(), graph.AddNode() };
        
        Console.WriteLine($"Created {graph.NodeCount} nodes");
        
        var arc = graph.AddArc(nodes[0], nodes[1]);
        Console.WriteLine($"Created arc {arc.Id} from node {nodes[0].Id} to node {nodes[1].Id}");
        
        // Test arc indexing
        Console.WriteLine($"Forward arc {arc.Id}: {graph.SourceByIndex(arc.Id)} -> {graph.TargetByIndex(arc.Id)}");
        int reverseArc = ReverseArcGraph.OppositeArc(arc.Id);
        Console.WriteLine($"Reverse arc {reverseArc}: {graph.SourceByIndex(reverseArc)} -> {graph.TargetByIndex(reverseArc)}");
        
        // Test outgoing arcs
        Console.WriteLine("\nOutgoing arcs from node 0:");
        var outArcs0 = graph.GetOutArcs(nodes[0]);
        foreach (var a in outArcs0)
        {
            Console.WriteLine($"  Arc {a.Id}");
        }
        
        Console.WriteLine("\nOutgoing arcs from node 1:");
        var outArcs1 = graph.GetOutArcs(nodes[1]);
        foreach (var a in outArcs1)
        {
            Console.WriteLine($"  Arc {a.Id}");
        }
        
        // Now test the solver
        Console.WriteLine("\n\nTesting TarjanEnhancedOrTools solver...");
        var solver = new TarjanEnhancedOrTools(graph);
        
        // Set supplies
        solver.SetNodeSupply(nodes[0], 1);
        solver.SetNodeSupply(nodes[1], -1);
        
        // Set arc capacity and cost
        solver.SetArcCapacity(arc, 2);
        solver.SetArcCost(arc, 3);
        
        Console.WriteLine("\nStarting solve...");
        var status = solver.Solve();
        
        Console.WriteLine($"\nStatus: {status}");
        if (status == SolverStatus.Optimal)
        {
            Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
            Console.WriteLine($"Flow on arc: {solver.GetFlow(arc)}");
        }
    }
}