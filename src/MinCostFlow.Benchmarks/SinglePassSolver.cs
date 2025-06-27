using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Models;
using MinCostFlow.Problems.Sets;

namespace MinCostFlow.Benchmarks;

public class SinglePassSolver
{
    private readonly ProblemRepository _repository = new();
    private readonly List<ProblemResult> _results = [];

    public class ProblemResult
    {
        public MinCostFlowProblem Problem { get; set; } = null!;
        public double RuntimeMs { get; set; }
        public long ObjectiveCost { get; set; }
        public SolverStatus Status { get; set; }
    }

    public void RunAllProblems()
    {
        var problems = GatherAllProblems();
        
        Console.WriteLine($"Found {problems.Count} problems to solve.\n");
        
        // Solve each problem
        foreach (var problem in problems)
        {
            var name = problem.Metadata?.Name ?? "Unknown";
            Console.Write($"Solving {name,-25} ({problem.NodeCount,6:N0} nodes, {problem.ArcCount,8:N0} arcs)... ");
            
            var result = SolveProblem(problem);
            _results.Add(result);
            
            if (result.Status == SolverStatus.Optimal)
            {
                Console.WriteLine($"Done in {result.RuntimeMs,8:F2} ms, Objective: {result.ObjectiveCost,15:N0}");
            }
            else
            {
                Console.WriteLine($"Failed: {result.Status}");
            }
        }
        
        Console.WriteLine();
        PrintStatistics();
    }

    private List<MinCostFlowProblem> GatherAllProblems()
    {
        var problems = new List<MinCostFlowProblem>
        {
            // Add generated problems
            _repository.GenerateTransportationProblem(10, 10, 1000), // 100 nodes
            _repository.GenerateTransportationProblem(22, 22, 1000), // ~500 nodes
            _repository.GenerateTransportationProblem(31, 31, 1000), // ~1000 nodes
            _repository.GenerateTransportationProblem(70, 70, 1000), // ~5000 nodes
            _repository.GenerateTransportationProblem(100, 100, 1000) // 10000 nodes
        };
        
        // Update names
        problems[^5].Metadata!.Name = "Transport_100";
        problems[^4].Metadata!.Name = "Transport_500";
        problems[^3].Metadata!.Name = "Transport_1000";
        problems[^2].Metadata!.Name = "Transport_5000";
        problems[^1].Metadata!.Name = "Transport_10000";
        
        // Add circulation problems
        problems.Add(_repository.GenerateCirculationProblem(1000, 0.05));
        problems.Add(_repository.GenerateCirculationProblem(5000, 0.05));
        problems.Add(_repository.GenerateCirculationProblem(6000, 0.05));
        
        problems[^3].Metadata!.Name = "Circulation_1000";
        problems[^2].Metadata!.Name = "Circulation_5000";
        problems[^1].Metadata!.Name = "Circulation_6000";
        
        // Add path problem
        var pathProblem = _repository.GeneratePathProblem(10000, 1000);
        pathProblem.Metadata!.Name = "Simple_10000";
        problems.Add(pathProblem);
        
        // Add grid problem
        var gridProblem = _repository.GenerateGridProblem(100, 100, 0, 0, 99, 99, 1000);
        gridProblem.Metadata!.Name = "Grid_100x100";
        problems.Add(gridProblem);
        
        // Add embedded problems dynamically by category
        foreach (var dimacs in StandardProblems.GetByCategory("DIMACS"))
            problems.Add(dimacs);
        foreach (var small in StandardProblems.GetByCategory("Small"))
            problems.Add(small);
        
        // Sort by node count, then arc count
        return problems.OrderBy(p => p.NodeCount).ThenBy(p => p.ArcCount).ToList();
    }

    private static ProblemResult SolveProblem(MinCostFlowProblem problem)
    {
        var solver = new NetworkSimplex(problem.Graph);
        
        // Set supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
        }
        
        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcCost(arc, problem.ArcCosts[i]);
            solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
        }
        
        var sw = Stopwatch.StartNew();
        var status = solver.Solve();
        sw.Stop();
        
        return new ProblemResult
        {
            Problem = problem,
            RuntimeMs = sw.Elapsed.TotalMilliseconds,
            ObjectiveCost = status == SolverStatus.Optimal ? solver.GetTotalCost() : 0,
            Status = status
        };
    }

    private void PrintStatistics()
    {
        Console.WriteLine("=== MinCostFlow Problem Statistics ===");
        Console.WriteLine();
        Console.WriteLine($"{"Name",-25} {"Nodes",10} {"Arcs",10} {"Density",10} {"Runtime (ms)",15} {"Objective",15} {"Status",-10} {"Category",-15}");
        Console.WriteLine(new string('-', 130));

        // Group by category
        var categories = _results.GroupBy(r => r.Problem.Metadata?.Category ?? "Unknown")
                                .OrderBy(g => g.Key);

        foreach (var category in categories)
        {
            foreach (var result in category.OrderBy(r => r.Problem.NodeCount).ThenBy(r => r.Problem.ArcCount))
            {
                var problem = result.Problem;
                var name = problem.Metadata?.Name ?? "Unknown";
                var nodes = problem.NodeCount;
                var arcs = problem.ArcCount;
                var density = nodes > 1 ? (double)arcs / (nodes * (nodes - 1)) : 0;
                var cat = problem.Metadata?.Category ?? "Unknown";

                Console.WriteLine($"{name,-25} {nodes,10:N0} {arcs,10:N0} {density,10:P2} {result.RuntimeMs,15:F2} {result.ObjectiveCost,15:N0} {result.Status,-10} {cat,-15}");
            }
            Console.WriteLine();
        }

        // Print summary statistics
        Console.WriteLine(new string('-', 130));
        var successful = _results.Where(r => r.Status == SolverStatus.Optimal).ToList();
        
        Console.WriteLine($"Total problems: {_results.Count}");
        Console.WriteLine($"Successful solves: {successful.Count}");
        Console.WriteLine($"Failed solves: {_results.Count - successful.Count}");
        Console.WriteLine($"Total nodes: {_results.Sum(r => (long)r.Problem.NodeCount):N0}");
        Console.WriteLine($"Total arcs: {_results.Sum(r => (long)r.Problem.ArcCount):N0}");
        
        if (successful.Count > 0)
        {
            Console.WriteLine($"Total runtime: {successful.Sum(r => r.RuntimeMs):F2} ms");
            Console.WriteLine($"Average runtime: {successful.Average(r => r.RuntimeMs):F2} ms");
            Console.WriteLine($"Min runtime: {successful.Min(r => r.RuntimeMs):F2} ms ({successful.MinBy(r => r.RuntimeMs)?.Problem.Metadata?.Name})");
            Console.WriteLine($"Max runtime: {successful.Max(r => r.RuntimeMs):F2} ms ({successful.MaxBy(r => r.RuntimeMs)?.Problem.Metadata?.Name})");
        }
        
        Console.WriteLine();
        
        // Category breakdown
        Console.WriteLine("Category Breakdown:");
        foreach (var cat in _results.GroupBy(r => r.Problem.Metadata?.Category ?? "Unknown").OrderBy(g => g.Key))
        {
            var catSuccessful = cat.Count(r => r.Status == SolverStatus.Optimal);
            Console.WriteLine($"  {cat.Key}: {cat.Count()} problems, {catSuccessful} solved successfully");
        }
    }
}