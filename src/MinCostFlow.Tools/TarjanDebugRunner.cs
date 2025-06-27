using System;
using System.Linq;
using MinCostFlow.Benchmarks.Analysis;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Tools;

public class TarjanDebugRunner
{
    public static void Run()
    {
        // Enable debug mode
        Environment.SetEnvironmentVariable("TARJAN_DEBUG", "1");
        
        var problemSet = new BenchmarkProblemSet();
        var knapzackProblems = problemSet.GetByCategory("Knapzack").ToList();
        
        var problemWithSolution = knapzackProblems.FirstOrDefault(p => 
            p.DisplayName.Contains("Canonicalproblemillustrationwithhighretirementdelaypenalty", StringComparison.OrdinalIgnoreCase));
        
        if (problemWithSolution == null)
        {
            Console.WriteLine("Problem not found!");
            return;
        }
        
        var problem = problemWithSolution.Problem;
        Console.WriteLine($"Problem: {problem.Metadata?.Name ?? "Unknown"}");
        Console.WriteLine($"Nodes: {problem.NodeCount}, Arcs: {problem.ArcCount}");
        
        var solver = new TarjanEnhanced(problem.Graph);
        
        // Set up the problem
        for (int i = 0; i < problem.NodeCount; i++)
        {
            var node = new Node(i);
            solver.SetNodeSupply(node, problem.NodeSupplies[i]);
        }
        
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            solver.SetArcCost(arc, problem.ArcCosts[i]);
        }
        
        try
        {
            Console.WriteLine("\n=== Starting TarjanEnhanced solve ===\n");
            var status = solver.Solve();
            Console.WriteLine($"\nStatus: {status}");
            
            if (status == SolverStatus.Optimal)
            {
                Console.WriteLine($"Total Cost: {solver.GetTotalCost()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}