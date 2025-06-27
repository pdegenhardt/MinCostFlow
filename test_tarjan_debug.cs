using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

class TestDebug
{
    static void Main()
    {
        Console.WriteLine("Creating graph...");
        var graph = new ReverseArcGraph();
        var n0 = graph.AddNode();
        var n1 = graph.AddNode();
        var arc = graph.AddArc(n0, n1);
        Console.WriteLine($"Graph created: {graph.NodeCount} nodes, {graph.ArcCount} arcs");
        
        Console.WriteLine("Creating solver...");
        try
        {
            using var solver = new TarjanEnhanced(graph);
            Console.WriteLine("Solver created successfully");
            
            Console.WriteLine("Setting node supplies...");
            solver.SetNodeSupply(n0, 5);
            solver.SetNodeSupply(n1, -5);
            
            Console.WriteLine("Setting arc bounds and costs...");
            solver.SetArcBounds(arc, 0, 10);
            solver.SetArcCost(arc, 1);
            
            Console.WriteLine("Calling Solve()...");
            var status = solver.Solve();
            Console.WriteLine($"Solve completed with status: {status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}