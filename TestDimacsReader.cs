using System;
using MinCostFlow.Core.IO;
using MinCostFlow.Core.Types;

class TestDimacsReader
{
    static void Main()
    {
        var problemPath = "benchmarks/data/dimacs/netgen_8_10a.min";
        var problem = DimacsReader.ReadFromFile(problemPath);
        
        Console.WriteLine($"Nodes: {problem.NodeCount}");
        Console.WriteLine($"Arcs: {problem.ArcCount}");
        Console.WriteLine();
        
        // Check first few arcs
        Console.WriteLine("First 10 arcs from file:");
        for (int i = 0; i < 10; i++)
        {
            var arc = new Arc(i);
            var source = problem.Graph.Source(arc);
            var target = problem.Graph.Target(arc);
            Console.WriteLine($"Arc {i}: {source.Id} -> {target.Id}, " +
                            $"cost = {problem.ArcCosts[i]}, " +
                            $"bounds = [{problem.ArcLowerBounds[i]}, {problem.ArcUpperBounds[i]}]");
        }
        
        // Find arc from node 0 to node 152 (1->153 in file)
        Console.WriteLine("\nLooking for arc 0->152 (1->153 in file):");
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            var source = problem.Graph.Source(arc);
            var target = problem.Graph.Target(arc);
            if (source.Id == 0 && target.Id == 152)
            {
                Console.WriteLine($"Found at arc {i}: cost = {problem.ArcCosts[i]}");
                break;
            }
        }
    }
}