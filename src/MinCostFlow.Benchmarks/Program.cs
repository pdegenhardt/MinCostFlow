using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Linq;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--quick"))
        {
            RunQuickPerformanceCheck();
        }
        else
        {
            BenchmarkRunner.Run<NetworkSimplexBenchmarks>();
        }
    }

    private static void RunQuickPerformanceCheck()
    {
        Console.WriteLine("Running quick performance validation...\n");
        
        var benchmarks = new NetworkSimplexBenchmarks();
        benchmarks.Setup();
        
        // Find the 10,000 node problem - try simple first as it's most reliable
        var problem = benchmarks.GetProblems().FirstOrDefault(p => p.Name == "Simple_10000")
                      ?? benchmarks.GetProblems().FirstOrDefault(p => p.Name == "Circulation_10000") 
                      ?? benchmarks.GetProblems().FirstOrDefault(p => p.Name == "Transport_10000_sparse")
                      ?? benchmarks.GetProblems().FirstOrDefault(p => p.Name == "Transport_10000");
        if (problem == null)
        {
            Console.WriteLine("ERROR: Could not find 10,000 node problem");
            return;
        }
        
        Console.WriteLine($"Problem: {problem.Name}");
        Console.WriteLine($"Nodes: {problem.NodeCount:N0}");
        Console.WriteLine($"Arcs: {problem.ArcCount:N0}");
        
        // Check supply balance
        long totalSupply = 0;
        for (int i = 0; i < problem.NodeCount; i++)
        {
            totalSupply += problem.Supplies[i];
        }
        Console.WriteLine($"Total Supply: {totalSupply} (should be 0 for balanced problem)");
        
        // Warm up
        var solver = new NetworkSimplex(problem.Graph);
        SetupSolver(solver, problem);
        solver.Solve();
        
        // Measure memory before
        var memoryBefore = GC.GetTotalMemory(true);
        
        // Time the solve
        solver = new NetworkSimplex(problem.Graph);
        SetupSolver(solver, problem);
        
        var sw = Stopwatch.StartNew();
        var status = solver.Solve();
        sw.Stop();
        
        // Measure memory after
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = (memoryAfter - memoryBefore) / (1024 * 1024);
        
        Console.WriteLine($"\nResult: {status}");
        Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Memory used: ~{memoryUsed}MB");
        
        // Check targets
        Console.WriteLine("\nPerformance Targets:");
        Console.WriteLine($"✓ Time < 1000ms: {(sw.ElapsedMilliseconds < 1000 ? "PASS" : "FAIL")} ({sw.ElapsedMilliseconds}ms)");
        Console.WriteLine($"✓ Memory < 200MB: {(memoryUsed < 200 ? "PASS" : "FAIL")} (~{memoryUsed}MB)");
        
        // Get solution stats
        if (status == SolverStatus.Optimal)
        {
            var totalCost = solver.GetTotalCost();
            Console.WriteLine($"\nTotal Cost: {totalCost:N0}");
        }
    }
    
    private static void SetupSolver(NetworkSimplex solver, NetworkSimplexBenchmarks.BenchmarkProblem problem)
    {
        // Set supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            solver.SetNodeSupply(new Node(i), problem.Supplies[i]);
        }
        
        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcCost(arc, problem.Costs[i]);
            solver.SetArcBounds(arc, problem.LowerBounds[i], problem.UpperBounds[i]);
        }
    }
}