using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

class Program
{
    static void Main()
    {
        Console.WriteLine("Creating simple test problem...");
        
        var builder = new GraphBuilder();
        builder.AddNodes(2);
        builder.AddArc(0, 1);
        
        var graph = builder.Build();
        
        Console.WriteLine($"Graph: {graph.NodeCount} nodes, {graph.ArcCount} arcs");
        
        using var solver = new TarjanEnhanced(graph);
        
        solver.SetNodeSupply(builder.GetNode(0), 5);
        solver.SetNodeSupply(builder.GetNode(1), -5);
        solver.SetArcBounds(new Arc(0), 0, 10);
        solver.SetArcCost(new Arc(0), 1);
        
        Console.WriteLine("Starting solve...");
        
        try
        {
            var status = solver.Solve();
            Console.WriteLine($"Status: {status}");
            
            if (status == SolverStatus.Optimal)
            {
                Console.WriteLine($"Flow on arc 0: {solver.GetFlow(new Arc(0))}");
                Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}