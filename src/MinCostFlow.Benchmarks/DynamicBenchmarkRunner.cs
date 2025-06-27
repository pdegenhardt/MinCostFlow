using System;
using System.IO;
using System.Linq;
using MinCostFlow.Benchmarks.Analysis;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Benchmarks;

/// <summary>
/// Runs benchmarks using dynamically discovered problems from embedded resources.
/// </summary>
public class DynamicBenchmarkRunner
{
    private readonly BenchmarkProblemSet _problemSet;
    private readonly ProblemRepository _repository;
    
    public DynamicBenchmarkRunner()
    {
        _problemSet = new BenchmarkProblemSet();
        _repository = new ProblemRepository();
    }
    
    /// <summary>
    /// Configuration options for the benchmark run.
    /// </summary>
    public class RunOptions
    {
        public string[]? Categories { get; set; }
        public int? MaxProblems { get; set; }
        public bool IncludeGenerated { get; set; } = true;
        public bool ValidateOnly { get; set; }
        public bool IncludeLarge { get; set; } = false;
        public int? MaxNodes { get; set; }
        public int? MaxArcs { get; set; }
    }
    
    /// <summary>
    /// Runs performance comparison with dynamic problem discovery.
    /// </summary>
    public void RunPerformanceComparison(RunOptions? options = null)
    {
        options ??= new RunOptions();
        
        Console.WriteLine("Starting NetworkSimplex vs CostScaling vs OR-Tools Performance Comparison");
        Console.WriteLine("========================================================================");
        Console.WriteLine();
        
        // Discover problems
        var allProblems = _problemSet.DiscoverProblems();
        
        Console.WriteLine($"Discovered {_problemSet.ProblemCount} problems in {allProblems.Count} categories");
        Console.WriteLine($"Problems with solutions: {_problemSet.ProblemsWithSolutionCount}");
        Console.WriteLine();
        
        var report = new PerformanceComparisonReport();
        
        // Filter categories if specified
        var categoriesToRun = options.Categories?.Length > 0 
            ? options.Categories 
            : _problemSet.Categories.ToArray();
        
        if (!options.IncludeLarge)
        {
            categoriesToRun = categoriesToRun.Where(c => c != "Large").ToArray();
        }
        
        int totalProblemsRun = 0;
        
        // Run problems by category
        foreach (var category in categoriesToRun)
        {
            var problems = _problemSet.GetByCategory(category);
            
            // Apply filters
            if (options.MaxNodes.HasValue)
            {
                problems = problems.Where(p => p.Problem.NodeCount <= options.MaxNodes.Value);
            }
            if (options.MaxArcs.HasValue)
            {
                problems = problems.Where(p => p.Problem.ArcCount <= options.MaxArcs.Value);
            }
            
            var problemList = problems.ToList();
            if (problemList.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"\nTesting {category} Problems ({problemList.Count} problems):");
            Console.WriteLine("-----------------------------------------------------------");
            
            foreach (var problemWithSolution in problemList)
            {
                if (options.MaxProblems.HasValue && totalProblemsRun >= options.MaxProblems.Value)
                {
                    break;
                }

                if (options.ValidateOnly && !problemWithSolution.HasSolution)
                {
                    continue;
                }

                report.RunComparison(
                    problemWithSolution.Problem, 
                    problemWithSolution.DisplayName, 
                    problemWithSolution.Solution);
                
                totalProblemsRun++;
            }
        }
        
        // Add generated problems if requested
        if (options.IncludeGenerated && !options.ValidateOnly)
        {
            Console.WriteLine("\nTesting Generated Problems:");
            Console.WriteLine("-----------------------------------------------------------");
            
            // Small generated problems
            if (IsInCategories("Small", categoriesToRun))
            {
                RunGeneratedProblem(report, 
                    _repository.GenerateTransportationProblem(10, 10, 1000), 
                    "Gen_Transport_100");
                RunGeneratedProblem(report, 
                    _repository.GeneratePathProblem(20, 100), 
                    "Gen_Path_20");
            }
            
            // Medium generated problems
            if (IsInCategories("Medium", categoriesToRun))
            {
                RunGeneratedProblem(report, 
                    _repository.GenerateTransportationProblem(22, 22, 1000), 
                    "Gen_Transport_500");
                RunGeneratedProblem(report, 
                    _repository.GenerateCirculationProblem(1000, 0.05), 
                    "Gen_Circulation_1000");
                RunGeneratedProblem(report, 
                    _repository.GenerateGridProblem(50, 50, 0, 0, 49, 49, 1000), 
                    "Gen_Grid_50x50");
            }
            
            // Large generated problems
            if (IsInCategories("Large", categoriesToRun) && options.IncludeLarge)
            {
                RunGeneratedProblem(report, 
                    _repository.GenerateTransportationProblem(71, 71, 1000), 
                    "Gen_Transport_5000");
                RunGeneratedProblem(report, 
                    _repository.GenerateCirculationProblem(5000, 0.05), 
                    "Gen_Circulation_5000");
                RunGeneratedProblem(report, 
                    _repository.GeneratePathProblem(10000, 1000), 
                    "Gen_Path_10000");
                RunGeneratedProblem(report, 
                    _repository.GenerateGridProblem(100, 100, 0, 0, 99, 99, 1000), 
                    "Gen_Grid_100x100");
                
                // Very large problems only if RUN_LARGE_BENCHMARKS is set
                if (Environment.GetEnvironmentVariable("RUN_LARGE_BENCHMARKS") == "true")
                {
                    RunGeneratedProblem(report, 
                        _repository.GenerateCirculationProblem(10000, 0.02), 
                        "Gen_Circulation_10000");
                }
            }
        }
        
        // Generate reports
        Console.WriteLine("\nGenerating reports...");
        
        var markdownReport = report.GenerateReport();
        var reportPath = Path.Combine("benchmarks", "performance_comparison.md");
        Directory.CreateDirectory("benchmarks");
        File.WriteAllText(reportPath, markdownReport);
        Console.WriteLine($"Markdown report saved to: {reportPath}");
        
        var csvPath = Path.Combine("benchmarks", "performance_comparison.csv");
        report.SaveToCsv(csvPath);
        Console.WriteLine($"CSV data saved to: {csvPath}");
        
        // Print summary to console
        Console.WriteLine("\n" + markdownReport);
    }
    
    /// <summary>
    /// Lists all available problems.
    /// </summary>
    public void ListProblems(bool detailed = false)
    {
        _ = _problemSet.DiscoverProblems();

        Console.WriteLine("Available Benchmark Problems");
        Console.WriteLine("============================");
        Console.WriteLine();
        
        var coverage = _problemSet.GetSolutionCoverage();
        
        foreach (var category in _problemSet.Categories)
        {
            var (total, withSolution) = coverage[category];
            Console.WriteLine($"{category} ({total} problems, {withSolution} with solutions):");
            
            var problems = _problemSet.GetByCategory(category);
            foreach (var p in problems)
            {
                var solStatus = p.HasSolution ? "✓" : " ";
                if (detailed)
                {
                    Console.WriteLine($"  [{solStatus}] {p.DisplayName} - {p.Problem.NodeCount} nodes, {p.Problem.ArcCount} arcs");
                    if (p.HasSolution && p.Solution != null)
                    {
                        Console.WriteLine($"       Optimal: {p.Solution.OptimalCost:N0}");
                    }
                }
                else
                {
                    Console.WriteLine($"  [{solStatus}] {p.DisplayName}");
                }
            }
            Console.WriteLine();
        }
        
        Console.WriteLine($"Total: {_problemSet.ProblemCount} problems across {_problemSet.Categories.Count()} categories");
        Console.WriteLine($"With solutions: {_problemSet.ProblemsWithSolutionCount} ({100.0 * _problemSet.ProblemsWithSolutionCount / _problemSet.ProblemCount:F1}%)");
        
        if (!detailed)
        {
            Console.WriteLine();
            Console.WriteLine("Use --list-problems --detailed for full listing");
        }
    }
    
    private static void RunGeneratedProblem(PerformanceComparisonReport report, MinCostFlowProblem problem, string name)
    {
        report.RunComparison(problem, name);
    }
    
    private static bool IsInCategories(string category, string[] categories)
    {
        return categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
    }
}