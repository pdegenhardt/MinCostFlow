using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MinCostFlow.Benchmarks.Solvers;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;
using ScottPlot;

namespace MinCostFlow.Benchmarks.Analysis;

public class PerformanceComparisonReport
{
    public class BenchmarkResult
    {
        public string ProblemName { get; set; } = "";
        public string Category { get; set; } = "";
        public int NodeCount { get; set; }
        public int ArcCount { get; set; }
        public string SolverName { get; set; } = "";
        public SolverStatus Status { get; set; }
        public long OptimalCost { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public long MemoryUsedBytes { get; set; }
        public int Iterations { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool? ValidationPassed { get; set; }
        public long? ExpectedCost { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private readonly List<BenchmarkResult> _results = [];

    public void AddResult(BenchmarkResult result)
    {
        _results.Add(result);
    }

    public void RunComparison(MinCostFlowProblem problem, string problemName)
    {
        RunComparison(problem, problemName, null);
    }

    public void RunComparison(MinCostFlowProblem problem, string problemName, SolutionLoader.Solution? expectedSolution)
    {
        // Output the problem name first, so it is always visible
        Console.WriteLine($"{problemName} | Nodes: {problem.NodeCount} | Arcs: {problem.ArcCount}");

        // Run all four benchmarks and output results immediately
        // Run in order: OR-Tools → CostScaling → NetworkSimplex → TarjanEnhanced
        var orResult = RunSingleBenchmarkForSummary(problem, problemName, "OrTools", expectedSolution);
        OutputSolverResult("OR-Tools", orResult, expectedSolution);
        
        var csResult = RunSingleBenchmarkForSummary(problem, problemName, "CostScaling", expectedSolution);
        OutputSolverResult("CostScaling", csResult, expectedSolution);
        
        var nsResult = RunSingleBenchmarkForSummary(problem, problemName, "NetworkSimplex", expectedSolution);
        OutputSolverResult("NetworkSimplex", nsResult, expectedSolution);
        
        var teResult = RunSingleBenchmarkForSummary(problem, problemName, "TarjanEnhanced", expectedSolution);
        OutputSolverResult("TarjanEnhanced", teResult, expectedSolution);

        // Check if all optimal solutions match
        var optimalResults = new[] { nsResult, csResult, orResult, teResult }.Where(r => r != null && r.Status == SolverStatus.Optimal).ToList();
        if (optimalResults.Count > 1)
        {
            var allMatch = optimalResults.All(r => r.OptimalCost == optimalResults[0].OptimalCost);
            if (!allMatch)
            {
                Console.WriteLine($"  WARNING: Different optimal costs found!");
            }
        }
        Console.WriteLine(); // Empty line between problems
    }
    
    private static void OutputSolverResult(string solverName, BenchmarkResult? result, SolutionLoader.Solution? expectedSolution)
    {
        if (result == null)
        {
            Console.WriteLine($"  {solverName,-15}: ERROR - No result");
            return;
        }
        
        var timeStr = $"{result.ElapsedMilliseconds:F2}ms";
        var statusStr = result.Status.ToString();
        
        // Handle error cases
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            statusStr = "ERROR";
        }
        
        var costStr = result.Status == SolverStatus.Optimal ? $"Cost: {result.OptimalCost}" : "Cost: -";
        
        // Validation status
        var validationStr = "";
        if (expectedSolution != null && result.Status == SolverStatus.Optimal)
        {
            if (result.ValidationPassed == null)
            {
                validationStr = " (no expected cost)";
            }
            else if (result.ValidationPassed == true)
            {
                validationStr = " ✓";
            }
            else
            {
                validationStr = $" FAILED (expected {result.ExpectedCost})";
            }
        }
        
        // Add error message if present
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            validationStr = $" | {result.ErrorMessage}";
        }
        
        Console.WriteLine($"  {solverName,-15}: {timeStr,10} | {statusStr,-10} | {costStr}{validationStr}");
    }

    // Helper for summary output (does not print to console)
    private BenchmarkResult? RunSingleBenchmarkForSummary(MinCostFlowProblem problem, string problemName, string solverName)
    {
        return RunSingleBenchmarkForSummary(problem, problemName, solverName, null);
    }

    private BenchmarkResult? RunSingleBenchmarkForSummary(MinCostFlowProblem problem, string problemName, string solverName, SolutionLoader.Solution? expectedSolution)
    {
        var sw = Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(true);

        SolverStatus status = SolverStatus.NotSolved;
        long optimalCost = 0;

        try
        {
            if (solverName == "NetworkSimplex")
            {
                var solver = new NetworkSimplex(problem.Graph);
                for (int i = 0; i < problem.NodeCount; i++)
                {
                    if (problem.NodeSupplies[i] != 0)
                    {
                        solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                    }
                }

                for (int i = 0; i < problem.ArcCount; i++)
                {
                    var arc = new Arc(i);
                    solver.SetArcCost(arc, problem.ArcCosts[i]);
                    solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                }
                status = solver.Solve();
                if (status == SolverStatus.Optimal)
                {
                    optimalCost = solver.GetTotalCost();
                }
            }
            else if (solverName == "CostScaling")
            {
                var solver = new CostScaling(problem.Graph);
                for (int i = 0; i < problem.NodeCount; i++)
                {
                    if (problem.NodeSupplies[i] != 0)
                    {
                        solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                    }
                }

                for (int i = 0; i < problem.ArcCount; i++)
                {
                    var arc = new Arc(i);
                    solver.SetArcCost(arc, problem.ArcCosts[i]);
                    solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                }
                status = solver.Solve(CostScaling.Method.PartialAugment); // Use PartialAugment as it's typically fastest
                if (status == SolverStatus.Optimal)
                {
                    optimalCost = solver.GetTotalCost();
                }
            }
            else if (solverName == "OrTools")
            {
                var solver = new OrToolsSolver(problem.Graph);
                for (int i = 0; i < problem.NodeCount; i++)
                {
                    if (problem.NodeSupplies[i] != 0)
                    {
                        solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                    }
                }

                for (int i = 0; i < problem.ArcCount; i++)
                {
                    var arc = new Arc(i);
                    solver.SetArcCost(arc, problem.ArcCosts[i]);
                    solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                }
                status = solver.Solve();
                if (status == SolverStatus.Optimal)
                {
                    optimalCost = solver.GetTotalCost();
                }
            }
            else if (solverName == "TarjanEnhanced")
            {

            }
        }
        catch (Exception ex)
        {
            // Handle solver errors gracefully
            status = SolverStatus.NotSolved;
            sw.Stop();
            
            return new BenchmarkResult
            {
                ProblemName = problemName,
                Category = problem.Metadata?.Category ?? "Other",
                SolverName = solverName,
                Status = status,
                ErrorMessage = ex.Message,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                MemoryUsedBytes = GC.GetTotalMemory(true) - startMemory,
                NodeCount = problem.NodeCount,
                ArcCount = problem.ArcCount,
                OptimalCost = 0,
                ExpectedCost = expectedSolution?.OptimalCost
            };
        }
        sw.Stop();
        var endMemory = GC.GetTotalMemory(false);
        var memoryUsed = endMemory - startMemory;
        var result = new BenchmarkResult
        {
            ProblemName = problemName,
            Category = problem.Metadata?.Category ?? "Other",
            NodeCount = problem.NodeCount,
            ArcCount = problem.ArcCount,
            SolverName = solverName,
            Status = status,
            OptimalCost = optimalCost,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
            MemoryUsedBytes = memoryUsed > 0 ? memoryUsed : 0
        };
        
        // Validate against expected solution if provided
        if (expectedSolution != null && status == SolverStatus.Optimal)
        {
            result.ExpectedCost = expectedSolution.OptimalCost;
            
            // Special handling for solution files that don't specify optimal cost
            if (expectedSolution.OptimalCost == long.MinValue)
            {
                // Solution file contains flows but no optimal cost - skip validation
                result.ValidationPassed = null; // null indicates "not applicable"
            }
            else
            {
                result.ValidationPassed = optimalCost == expectedSolution.OptimalCost;
            }
        }
        
        _results.Add(result);
        return result;
    }

    public string GenerateReport()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Performance Comparison Report");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        // Group results by problem
        var groupedResults = _results.GroupBy(r => r.ProblemName);
        
        sb.AppendLine("## Summary Table");
        sb.AppendLine();

        // Generate ASCII table with box-drawing characters
        GenerateAsciiTable(sb, groupedResults);
        
        // Add summary statistics after the table
        var nsWins = 0;
        var csWins = 0;
        var orWins = 0;
        var teWins = 0;
        var totalProblems = 0;
        var allFourOptimal = 0;
        var nsOptimal = 0;
        var csOptimal = 0;
        var orOptimal = 0;
        var teOptimal = 0;
        
        foreach (var group in groupedResults)
        {
            totalProblems++;
            
            var nsResults = group.Where(r => r.SolverName == "NetworkSimplex" && r.Status == SolverStatus.Optimal).ToList();
            var csResults = group.Where(r => r.SolverName == "CostScaling" && r.Status == SolverStatus.Optimal).ToList();
            var orResults = group.Where(r => r.SolverName == "OrTools" && r.Status == SolverStatus.Optimal).ToList();
            var teResults = group.Where(r => r.SolverName == "TarjanEnhanced" && r.Status == SolverStatus.Optimal).ToList();
            
            if (nsResults.Count > 0) nsOptimal++;
            if (csResults.Count > 0) csOptimal++;
            if (orResults.Count > 0) orOptimal++;
            if (teResults.Count > 0) teOptimal++;
            
            var nsAvg = nsResults.Count > 0 ? nsResults.Average(r => r.ElapsedMilliseconds) : double.MaxValue;
            var csAvg = csResults.Count > 0 ? csResults.Average(r => r.ElapsedMilliseconds) : double.MaxValue;
            var orAvg = orResults.Count > 0 ? orResults.Average(r => r.ElapsedMilliseconds) : double.MaxValue;
            var teAvg = teResults.Count > 0 ? teResults.Average(r => r.ElapsedMilliseconds) : double.MaxValue;
            
            // Count wins only when all four found optimal solutions
            if (nsResults.Count > 0 && csResults.Count > 0 && orResults.Count > 0 && teResults.Count > 0)
            {
                allFourOptimal++;
                if (nsAvg <= csAvg && nsAvg <= orAvg && nsAvg <= teAvg)
                {
                    nsWins++;
                }
                else if (csAvg <= nsAvg && csAvg <= orAvg && csAvg <= teAvg)
                {
                    csWins++;
                }
                else if (orAvg <= nsAvg && orAvg <= csAvg && orAvg <= teAvg)
                {
                    orWins++;
                }
                else
                {
                    teWins++;
                }
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("## Summary Statistics");
        sb.AppendLine($"- Total problems tested: {totalProblems}");
        sb.AppendLine($"- Problems solved by all four algorithms: {allFourOptimal}");
        sb.AppendLine();
        if (totalProblems > 0)
        {
            sb.AppendLine("### Success Rates:");
            sb.AppendLine($"- NetworkSimplex: {nsOptimal}/{totalProblems} ({100.0 * nsOptimal / totalProblems:F1}%)");
            sb.AppendLine($"- CostScaling: {csOptimal}/{totalProblems} ({100.0 * csOptimal / totalProblems:F1}%)");
            sb.AppendLine($"- OR-Tools: {orOptimal}/{totalProblems} ({100.0 * orOptimal / totalProblems:F1}%)");
            sb.AppendLine($"- TarjanEnhanced: {teOptimal}/{totalProblems} ({100.0 * teOptimal / totalProblems:F1}%)");
            sb.AppendLine();
        }
        if (allFourOptimal > 0)
        {
            sb.AppendLine("### Performance Winners (when all four solve optimally):");
            sb.AppendLine($"- NetworkSimplex wins: {nsWins}/{allFourOptimal} ({100.0 * nsWins / allFourOptimal:F1}%)");
            sb.AppendLine($"- CostScaling wins: {csWins}/{allFourOptimal} ({100.0 * csWins / allFourOptimal:F1}%)");
            sb.AppendLine($"- OR-Tools wins: {orWins}/{allFourOptimal} ({100.0 * orWins / allFourOptimal:F1}%)");
            sb.AppendLine($"- TarjanEnhanced wins: {teWins}/{allFourOptimal} ({100.0 * teWins / allFourOptimal:F1}%)");
        }
        
        // Add validation summary
        var allResultsWithExpected = _results.Where(r => r.ExpectedCost.HasValue).ToList();
        var validationResults = allResultsWithExpected.Where(r => r.ValidationPassed.HasValue).ToList();
        var noExpectedCostResults = allResultsWithExpected.Where(r => !r.ValidationPassed.HasValue && r.Status == SolverStatus.Optimal).ToList();
        
        if (allResultsWithExpected.Count != 0)
        {
            var totalProblemsWithSolutions = allResultsWithExpected.Select(r => r.ProblemName).Distinct().Count();
            var passedValidations = validationResults.Count(r => r.ValidationPassed == true);
            var failedValidations = validationResults.Count(r => r.ValidationPassed == false);
            var noExpectedCostCount = noExpectedCostResults.Count;
            
            sb.AppendLine();
            sb.AppendLine("## Validation Summary");
            sb.AppendLine($"- Total problems with solutions: {totalProblemsWithSolutions}");
            sb.AppendLine($"- Validations passed: {passedValidations}");
            sb.AppendLine($"- Validations failed: {failedValidations}");
            sb.AppendLine($"- Solution files without optimal cost: {noExpectedCostCount}");
            
            if (failedValidations > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Failed Validations:");
                var failures = validationResults.Where(r => r.ValidationPassed == false)
                    .GroupBy(r => r.ProblemName)
                    .OrderBy(g => g.Key);
                
                foreach (var group in failures)
                {
                    sb.AppendLine($"- **{group.Key}**:");
                    foreach (var result in group)
                    {
                        sb.AppendLine($"  - {result.SolverName}: Expected {result.ExpectedCost:N0}, Got {result.OptimalCost:N0}");
                    }
                }
            }
            
            if (noExpectedCostResults.Count != 0)
            {
                var problemsWithoutCost = noExpectedCostResults.Select(r => r.ProblemName).Distinct().OrderBy(p => p);
                sb.AppendLine();
                sb.AppendLine("### Solution Files Without Optimal Cost:");
                foreach (var problem in problemsWithoutCost)
                {
                    sb.AppendLine($"- {problem}");
                }
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("## Detailed Results by Category");
        sb.AppendLine();
        
        // Group by category first
        var categorizedGroups = groupedResults
            .GroupBy(g => g.First().Category)
            .OrderBy(c => c.Key);
        
        foreach (var categoryGroup in categorizedGroups)
        {
            sb.AppendLine($"### {categoryGroup.Key} Problems");
            sb.AppendLine();
            
            // Calculate worst-case runtime for each problem in this category
            var problemsWithWorstCase = categoryGroup
                .Select(group => new
                {
                    ProblemGroup = group,
                    WorstCaseTime = group
                        .Where(r => r.Status == SolverStatus.Optimal)
                        .Select(r => r.ElapsedMilliseconds)
                        .DefaultIfEmpty(0)
                        .Max(),
                    HasAnyResults = group.Any()
                })
                .Where(p => p.HasAnyResults)  // Include problems even if no solver found optimal solution
                .OrderByDescending(p => p.WorstCaseTime)
                .ToList();
            
            foreach (var item in problemsWithWorstCase)
            {
                var group = item.ProblemGroup;
                sb.AppendLine($"#### {group.Key}");
                sb.AppendLine($"- Nodes: {group.First().NodeCount:N0}");
                sb.AppendLine($"- Arcs: {group.First().ArcCount:N0}");
                if (item.WorstCaseTime > 0)
                {
                    sb.AppendLine($"- Worst-case runtime: {item.WorstCaseTime:F2} ms");
                }
                else
                {
                    sb.AppendLine($"- Worst-case runtime: No optimal solution found");
                }
                sb.AppendLine();
                
                sb.AppendLine("| Solver | Status | Time (ms) | Memory (KB) | Cost |");
                sb.AppendLine("|--------|--------|-----------|-------------|------|");
                
                foreach (var solverGroup in group.GroupBy(r => r.SolverName))
                {
                    var allResults = solverGroup.ToList();
                    if (allResults.Count != 0)
                    {
                        // Check if all results have the same status
                        var status = allResults.First().Status;
                        var allSameStatus = allResults.All(r => r.Status == status);
                        
                        if (status == SolverStatus.Optimal)
                        {
                            var avgTime = allResults.Average(r => r.ElapsedMilliseconds);
                            var minTime = allResults.Min(r => r.ElapsedMilliseconds);
                            var maxTime = allResults.Max(r => r.ElapsedMilliseconds);
                            var avgMemory = allResults.Average(r => r.MemoryUsedBytes) / 1024;
                            var cost = allResults.First().OptimalCost;
                            
                            sb.AppendLine($"| {solverGroup.Key} | Optimal | {avgTime:F2} ({minTime:F2}-{maxTime:F2}) | " +
                                         $"{avgMemory:F0} | {cost:N0} |");
                        }
                        else
                        {
                            // Show unsuccessful outcome
                            var avgTime = allResults.Average(r => r.ElapsedMilliseconds);
                            var avgMemory = allResults.Average(r => r.MemoryUsedBytes) / 1024;
                            var statusStr = allSameStatus ? status.ToString() : "Mixed";
                            
                            sb.AppendLine($"| {solverGroup.Key} | {statusStr} | {avgTime:F2} | " +
                                         $"{avgMemory:F0} | - |");
                        }
                    }
                }
                sb.AppendLine();
            }
        }
        
        // Add scatter plot visualization
        sb.AppendLine("## Performance Scatter Plot");
        sb.AppendLine();
        GenerateScatterPlot(sb, groupedResults);
        sb.AppendLine();
        
        return sb.ToString();
    }

    private static double CalculateSlope(double[] x, double[] y)
    {
        var n = x.Length;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (a, b) => a * b).Sum();
        var sumX2 = x.Select(a => a * a).Sum();
        
        return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    }

    private static void GenerateAsciiTable(StringBuilder sb, IEnumerable<IGrouping<string, BenchmarkResult>> groupedResults)
    {
        // Generate standard markdown table
        sb.AppendLine("| Category | Problem | Nodes | Arcs | NetworkSimplex (ms) | CostScaling (ms) | OR-Tools (ms) | TarjanEnhanced (ms) | Winner | Best vs Worst |");
        sb.AppendLine("|----------|---------|------:|-----:|--------------------:|-----------------:|--------------:|--------------------:|--------|---------------|");

        // Group by category first, then order by worst-case time within each category
        var categorizedData = groupedResults
            .Select(group => new
            {
                Category = group.First().Category,
                Group = group,
                WorstCaseTime = group
                    .Where(r => r.Status == SolverStatus.Optimal)
                    .Select(r => r.ElapsedMilliseconds)
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .GroupBy(x => x.Category)
            .OrderBy(c => c.Key);

        // Data rows
        foreach (var categoryGroup in categorizedData)
        {
            // Add category heading separator
            sb.AppendLine($"| **{categoryGroup.Key}** | | | | | | | | | |");
            
            foreach (var item in categoryGroup.OrderByDescending(x => x.WorstCaseTime))
            {
                var group = item.Group;
                var nsResults = group.Where(r => r.SolverName == "NetworkSimplex" && r.Status == SolverStatus.Optimal).ToList();
                var csResults = group.Where(r => r.SolverName == "CostScaling" && r.Status == SolverStatus.Optimal).ToList();
                var orResults = group.Where(r => r.SolverName == "OrTools" && r.Status == SolverStatus.Optimal).ToList();
                var teResults = group.Where(r => r.SolverName == "TarjanEnhanced" && r.Status == SolverStatus.Optimal).ToList();

            if (nsResults.Count != 0 && csResults.Count != 0 && orResults.Count != 0 && teResults.Count != 0)
            {
                var nsAvg = nsResults.Average(r => r.ElapsedMilliseconds);
                var csAvg = csResults.Average(r => r.ElapsedMilliseconds);
                var orAvg = orResults.Average(r => r.ElapsedMilliseconds);
                var teAvg = teResults.Average(r => r.ElapsedMilliseconds);
                
                // Find winner and speedup
                var times = new[] { ("NetworkSimplex", nsAvg), ("CostScaling", csAvg), ("OR-Tools", orAvg), ("TarjanEnhanced", teAvg) };
                var sorted = times.OrderBy(t => t.Item2).ToArray();
                var winner = sorted[0].Item1;
                var speedupStr = $"{sorted[3].Item2 / sorted[0].Item2:F2}×";

                sb.AppendLine($"| {item.Category} | {group.Key} | {nsResults.First().NodeCount:N0} | {nsResults.First().ArcCount:N0} | " +
                             $"{nsAvg:F2} | {csAvg:F2} | {orAvg:F2} | {teAvg:F2} | {winner} | {speedupStr} |");
            }
            else
            {
                // Not all solvers have results - show what we have
                var nsAvg = nsResults.Count != 0 ? nsResults.Average(r => r.ElapsedMilliseconds).ToString("F2") : "INFEASIBLE";
                var csAvg = csResults.Count != 0 ? csResults.Average(r => r.ElapsedMilliseconds).ToString("F2") : "INFEASIBLE";
                var orAvg = orResults.Count != 0 ? orResults.Average(r => r.ElapsedMilliseconds).ToString("F2") : "INFEASIBLE";
                var teAvg = teResults.Count != 0 ? teResults.Average(r => r.ElapsedMilliseconds).ToString("F2") : "INFEASIBLE";
                
                var winner = nsResults.Count != 0 ? "NetworkSimplex" : 
                           (csResults.Count != 0 ? "CostScaling" : 
                           (orResults.Count != 0 ? "OR-Tools" : 
                           (teResults.Count != 0 ? "TarjanEnhanced" : "None")));

                sb.AppendLine($"| {item.Category} | {group.Key} | {group.First().NodeCount:N0} | {group.First().ArcCount:N0} | " +
                             $"{nsAvg} | {csAvg} | {orAvg} | {teAvg} | {winner} | N/A |");
                }
            }
        }
    }

    private static void GenerateScatterPlot(StringBuilder sb, IEnumerable<IGrouping<string, BenchmarkResult>> groupedResults)
    {
        // Collect data points
        var nsPoints = new List<(double x, double y, string label)>();
        var csPoints = new List<(double x, double y, string label)>();
        var orPoints = new List<(double x, double y, string label)>();
        var tePoints = new List<(double x, double y, string label)>();
        
        foreach (var group in groupedResults)
        {
            var arcs = (double)group.First().ArcCount;
            var name = group.Key;
            
            var nsResults = group.Where(r => r.SolverName == "NetworkSimplex" && r.Status == SolverStatus.Optimal).ToList();
            var csResults = group.Where(r => r.SolverName == "CostScaling" && r.Status == SolverStatus.Optimal).ToList();
            var orResults = group.Where(r => r.SolverName == "OrTools" && r.Status == SolverStatus.Optimal).ToList();
            var teResults = group.Where(r => r.SolverName == "TarjanEnhanced" && r.Status == SolverStatus.Optimal).ToList();
            
            if (nsResults.Count != 0)
            {
                nsPoints.Add((arcs, nsResults.Average(r => r.ElapsedMilliseconds), name));
            }

            if (csResults.Count != 0)
            {
                csPoints.Add((arcs, csResults.Average(r => r.ElapsedMilliseconds), name));
            }

            if (orResults.Count != 0)
            {
                orPoints.Add((arcs, orResults.Average(r => r.ElapsedMilliseconds), name));
            }
            
            if (teResults.Count != 0)
            {
                tePoints.Add((arcs, teResults.Average(r => r.ElapsedMilliseconds), name));
            }
        }
        
        if (nsPoints.Count == 0 && csPoints.Count == 0 && orPoints.Count == 0 && tePoints.Count == 0)
        {
            return;
        }

        // Create the plot
        var plot = new Plot();
        plot.Title("Solver Performance Comparison");
        plot.XLabel("Number of Arcs");
        plot.YLabel("Runtime (ms)");
        
        // Transform data to log scale for plotting
        // Add scatter plots for each solver (no lines)
        if (nsPoints.Count != 0)
        {
            var nsScatter = plot.Add.ScatterPoints(
                nsPoints.Select(p => Math.Log10(p.x)).ToArray(),
                nsPoints.Select(p => Math.Log10(p.y)).ToArray());
            nsScatter.LegendText = "NetworkSimplex";
            nsScatter.MarkerShape = MarkerShape.FilledCircle;
            nsScatter.MarkerSize = 8;
            nsScatter.Color = ScottPlot.Colors.Blue;
        }
        
        if (csPoints.Count != 0)
        {
            var csScatter = plot.Add.ScatterPoints(
                csPoints.Select(p => Math.Log10(p.x)).ToArray(),
                csPoints.Select(p => Math.Log10(p.y)).ToArray());
            csScatter.LegendText = "CostScaling";
            csScatter.MarkerShape = MarkerShape.FilledSquare;
            csScatter.MarkerSize = 8;
            csScatter.Color = ScottPlot.Colors.Green;
        }
        
        if (orPoints.Count != 0)
        {
            var orScatter = plot.Add.ScatterPoints(
                orPoints.Select(p => Math.Log10(p.x)).ToArray(),
                orPoints.Select(p => Math.Log10(p.y)).ToArray());
            orScatter.LegendText = "OR-Tools";
            orScatter.MarkerShape = MarkerShape.FilledTriangleUp;
            orScatter.MarkerSize = 8;
            orScatter.Color = ScottPlot.Colors.Red;
        }
        
        if (tePoints.Count != 0)
        {
            var teScatter = plot.Add.ScatterPoints(
                tePoints.Select(p => Math.Log10(p.x)).ToArray(),
                tePoints.Select(p => Math.Log10(p.y)).ToArray());
            teScatter.LegendText = "TarjanEnhanced";
            teScatter.MarkerShape = MarkerShape.FilledDiamond;
            teScatter.MarkerSize = 8;
            teScatter.Color = ScottPlot.Colors.Orange;
        }
        
        // Configure the plot
        plot.ShowLegend();
        
        // Add labels to points
        // Group all points by problem name to determine which to label
        var problemGroups = new Dictionary<string, List<(string solver, double x, double y)>>();
        
        foreach (var (x, y, label) in nsPoints)
        {
            if (!problemGroups.ContainsKey(label))
            {
                problemGroups[label] = [];
            }

            problemGroups[label].Add(("NetworkSimplex", x, y));
        }
        
        foreach (var (x, y, label) in csPoints)
        {
            if (!problemGroups.ContainsKey(label))
            {
                problemGroups[label] = [];
            }

            problemGroups[label].Add(("CostScaling", x, y));
        }
        
        foreach (var (x, y, label) in orPoints)
        {
            if (!problemGroups.ContainsKey(label))
            {
                problemGroups[label] = [];
            }

            problemGroups[label].Add(("OR-Tools", x, y));
        }
        
        foreach (var (x, y, label) in tePoints)
        {
            if (!problemGroups.ContainsKey(label))
            {
                problemGroups[label] = [];
            }

            problemGroups[label].Add(("TarjanEnhanced", x, y));
        }
        
        // Determine which solver to label for each problem
        // Cycle through slowest, middle, fastest
        var labelOrder = new[] { "slowest", "middle", "fastest" };
        var labelIndex = 0;
        
        foreach (var problem in problemGroups.OrderBy(g => g.Value.Min(p => p.x)))
        {
            var solvers = problem.Value.OrderByDescending(s => s.y).ToList();
            
            if (solvers.Count == 0)
            {
                continue;
            }

            (string solver, double x, double y) selectedPoint;
            
            if (solvers.Count == 1)
            {
                // Only one solver, label it
                selectedPoint = solvers[0];
            }
            else if (solvers.Count == 2)
            {
                // Two solvers, alternate between slowest and fastest
                selectedPoint = labelIndex % 2 == 0 ? solvers[0] : solvers[1];
            }
            else if (solvers.Count == 3)
            {
                // Three solvers, cycle through slowest, middle, fastest
                var cyclePosition = labelOrder[labelIndex % 3];
                selectedPoint = cyclePosition switch
                {
                    "slowest" => solvers[0],
                    "middle" => solvers[1],
                    "fastest" => solvers[2],
                    _ => solvers[0]
                };
            }
            else
            {
                // Four or more solvers, cycle through positions
                var position = labelIndex % 4;
                selectedPoint = position switch
                {
                    0 => solvers[0], // slowest
                    1 => solvers[1], // second slowest
                    2 => solvers[solvers.Count - 2], // second fastest
                    3 => solvers[solvers.Count - 1], // fastest
                    _ => solvers[0]
                };
            }
            
            // Add the label
            var logX = Math.Log10(selectedPoint.x);
            var logY = Math.Log10(selectedPoint.y);
            
            var text = plot.Add.Text(problem.Key, logX, logY);
            text.LabelFontSize = 10;
            text.LabelAlignment = ScottPlot.Alignment.MiddleLeft;
            text.OffsetX = 10;
            text.OffsetY = 0;
            
            // Labels are always black - they represent problems, not algorithms
            text.LabelFontColor = ScottPlot.Colors.Black;
            
            // Add white background for better visibility
            text.LabelBackgroundColor = ScottPlot.Colors.White.WithAlpha(0.8);
            text.LabelBorderColor = ScottPlot.Colors.Transparent;
            
            labelIndex++;
        }
        
        // Set up logarithmic scale formatting
        var xTickGen = new ScottPlot.TickGenerators.NumericAutomatic();
        xTickGen.MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator();
        xTickGen.LabelFormatter = (double x) => Math.Pow(10, x).ToString("N0");
        
        var yTickGen = new ScottPlot.TickGenerators.NumericAutomatic();
        yTickGen.MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator();
        yTickGen.LabelFormatter = (double y) => Math.Pow(10, y).ToString("F1");
        
        plot.Axes.Bottom.TickGenerator = xTickGen;
        plot.Axes.Left.TickGenerator = yTickGen;
        plot.XLabel("Number of Arcs (log scale)");
        plot.YLabel("Runtime (ms) - log scale");
        
        // Set axis limits for log-transformed data
        var allArcs = nsPoints.Select(p => p.x)
            .Concat(csPoints.Select(p => p.x))
            .Concat(orPoints.Select(p => p.x))
            .Concat(tePoints.Select(p => p.x))
            .Where(a => a > 0)
            .ToList();
        var allTimes = nsPoints.Select(p => p.y)
            .Concat(csPoints.Select(p => p.y))
            .Concat(orPoints.Select(p => p.y))
            .Concat(tePoints.Select(p => p.y))
            .Where(t => t > 0)
            .ToList();
        
        if (allArcs.Count != 0 && allTimes.Count != 0)
        {
            var minArcs = allArcs.Min();
            var maxArcs = allArcs.Max();
            var minTime = allTimes.Min();
            var maxTime = allTimes.Max();
            
            // Transform to log scale for limits
            var logMinArcs = Math.Log10(Math.Max(1, minArcs * 0.8));
            var logMaxArcs = Math.Log10(maxArcs * 1.2);
            var logMinTime = Math.Log10(Math.Max(0.1, minTime * 0.8));
            var logMaxTime = Math.Log10(maxTime * 1.2);
            
            plot.Axes.SetLimitsX(logMinArcs, logMaxArcs);
            plot.Axes.SetLimitsY(logMinTime, logMaxTime);
        }
        
        // Save the plot
        var plotPath = Path.Combine("benchmarks", "performance_scatter_plot.png");
        Directory.CreateDirectory("benchmarks");
        plot.SavePng(plotPath, 800, 600);
        
        sb.AppendLine("![Performance Scatter Plot](performance_scatter_plot.png)");
        sb.AppendLine();
        sb.AppendLine("*Note: The scatter plot shows runtime (ms) vs number of arcs for each solver on logarithmic scales.*");
        sb.AppendLine("*NetworkSimplex (blue circles), CostScaling (green squares), OR-Tools (red triangles), TarjanEnhanced (orange diamonds)*");
        
        // Add a table of notable points
        sb.AppendLine();
        sb.AppendLine("### Notable Data Points");
        sb.AppendLine();
        sb.AppendLine("| Problem | Arcs | NS (ms) | CS (ms) | OR (ms) | TE (ms) | Fastest |");
        sb.AppendLine("|---------|-----:|--------:|--------:|--------:|--------:|---------|");
        
        // Combine all points and show the largest problems
        var allPoints = nsPoints.Select(p => (p.label, p.x, ns: p.y, cs: -1.0, or: -1.0, te: -1.0))
            .Concat(csPoints.Select(p => (p.label, p.x, ns: -1.0, cs: p.y, or: -1.0, te: -1.0)))
            .Concat(orPoints.Select(p => (p.label, p.x, ns: -1.0, cs: -1.0, or: p.y, te: -1.0)))
            .Concat(tePoints.Select(p => (p.label, p.x, ns: -1.0, cs: -1.0, or: -1.0, te: p.y)))
            .GroupBy(p => p.label)
            .Select(g => {
                var label = g.Key;
                var arcs = g.First().x;
                var ns = g.Where(p => p.ns > 0).Select(p => p.ns).DefaultIfEmpty(-1).First();
                var cs = g.Where(p => p.cs > 0).Select(p => p.cs).DefaultIfEmpty(-1).First();
                var or = g.Where(p => p.or > 0).Select(p => p.or).DefaultIfEmpty(-1).First();
                var te = g.Where(p => p.te > 0).Select(p => p.te).DefaultIfEmpty(-1).First();
                return (label, arcs, ns, cs, or, te);
            })
            .OrderByDescending(p => p.arcs)
            .Take(5);
        
        foreach (var (label, arcs, ns, cs, or, te) in allPoints)
        {
            var times = new[] { 
                (ns > 0 ? ns : double.MaxValue, "NS"),
                (cs > 0 ? cs : double.MaxValue, "CS"),
                (or > 0 ? or : double.MaxValue, "OR"),
                (te > 0 ? te : double.MaxValue, "TE")
            };
            var fastest = times.OrderBy(t => t.Item1).First().Item2;
            
            sb.AppendLine($"| {label} | {arcs:N0} | " +
                $"{(ns > 0 ? ns.ToString("F2") : "-")} | " +
                $"{(cs > 0 ? cs.ToString("F2") : "-")} | " +
                $"{(or > 0 ? or.ToString("F2") : "-")} | " +
                $"{(te > 0 ? te.ToString("F2") : "-")} | " +
                $"{fastest} |");
        }
    }

    public void SaveToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("ProblemName,NodeCount,ArcCount,SolverName,Status,OptimalCost,SolveTimeMs,MemoryBytes,Timestamp");
        
        foreach (var result in _results)
        {
            writer.WriteLine($"{result.ProblemName},{result.NodeCount},{result.ArcCount}," +
                           $"{result.SolverName},{result.Status},{result.OptimalCost}," +
                           $"{result.ElapsedMilliseconds},{result.MemoryUsedBytes},{result.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }
    }
}