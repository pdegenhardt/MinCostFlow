using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting test...");
        
        try
        {
            var builder = new GraphBuilder();
            builder.AddNodes(2);
            builder.AddArc(0, 1);
            
            var graph = builder.Build();
            Console.WriteLine($"Graph built: {graph.NodeCount} nodes, {graph.ArcCount} arcs");
            
            using var solver = new TarjanEnhanced(graph);
            Console.WriteLine("Solver created");
            
            solver.SetNodeSupply(builder.GetNode(0), 5);
            solver.SetNodeSupply(builder.GetNode(1), -5);
            solver.SetArcBounds(new Arc(0), 0, 10);
            solver.SetArcCost(new Arc(0), 1);
            Console.WriteLine("Problem setup complete");
            
            var status = solver.Solve();
            Console.WriteLine($"Status: {status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
        }
    }
}