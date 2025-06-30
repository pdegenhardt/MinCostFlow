using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Linq;
using MinCostFlow.Benchmarks.Analysis;
using MinCostFlow.Benchmarks.Solvers;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Models;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return;
        }
        
        // Check which mode to run
        if (args.Contains("--generate") || args.Contains("-g"))
        {
            ProblemGeneratorCommand.Execute(args);
        }
        else if (args.Contains("--stats") || args.Contains("-s"))
        {
            RunStatisticsMode();
        }
        else if (args.Contains("--benchmark") || args.Contains("-b"))
        {
            RunBenchmarkMode();
        }
        else if (args.Contains("--compare") || args.Contains("-c"))
        {
            RunComparisonMode(args);
        }
        else if (args.Contains("--quick-opt") || args.Contains("-q"))
        {
            RunQuickOptimizationBenchmark();
        }
        else if (args.Contains("--list-problems") || args.Contains("-l"))
        {
            ListProblems(args);
        }
        else if (args.Contains("--validate-only") || args.Contains("-v"))
        {
            RunValidationMode(args);
        }
        else if (args.Contains("--knapzack"))
        {
            RunKnapzackTest();
        }
        else if (args.Contains("--gort") || args.Contains("-G"))
        {
            BenchmarkRunner.Run<GortGraphBenchmarks>();
        }
        else if (args.Contains("--maxflow") || args.Contains("-M"))
        {
            BenchmarkRunner.Run<MaxFlowBenchmarks>();
        }
        else
        {
            // Default to statistics mode
            RunStatisticsMode();
        }
    }
    
    private static void PrintHelp()
    {
        Console.WriteLine("MinCostFlow Benchmarks");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --stats, -s          Run single-pass statistics (default)");
        Console.WriteLine("  --benchmark, -b      Run full BenchmarkDotNet benchmarks");
        Console.WriteLine("  --compare, -c        Run four-algorithm comparison (NetworkSimplex, CostScaling, OR-Tools, TarjanEnhanced)");
        Console.WriteLine("  --quick-opt, -q      Run quick optimization benchmark on Circulation_1000");
        Console.WriteLine("  --generate, -g       Generate new test problems");
        Console.WriteLine("  --list-problems, -l  List all available benchmark problems");
        Console.WriteLine("  --validate-only, -v  Run validation only on problems with solutions");
        Console.WriteLine("  --knapzack           Run only the Knapzack problem for debugging");
        Console.WriteLine("  --gort, -G           Run Gort data structure benchmarks");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Comparison options:");
        Console.WriteLine("  --category <name>    Run only specific category (Small, Medium, Large, DIMACS, LEMON)");
        Console.WriteLine("  --no-generated       Skip generated problems");
        Console.WriteLine("  --include-large      Include large problems (disabled by default)");
        Console.WriteLine("  --max-nodes <n>      Maximum node count to test");
        Console.WriteLine("  --max-arcs <n>       Maximum arc count to test");
        Console.WriteLine("  --detailed           Show detailed output");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                                    # Run statistics mode");
        Console.WriteLine("  dotnet run -- --compare                       # Run full four-algorithm comparison");
        Console.WriteLine("  dotnet run -- --compare --category Small      # Run only small problems");
        Console.WriteLine("  dotnet run -- --compare --no-generated        # Skip generated problems");
        Console.WriteLine("  dotnet run -- --list-problems                 # List all problems");
        Console.WriteLine("  dotnet run -- --list-problems --detailed      # List with details");
        Console.WriteLine("  dotnet run -- --validate-only                 # Validate all solutions");
        Console.WriteLine("  dotnet run -- --generate assignment 50        # Generate 50x50 assignment");
        Console.WriteLine("  dotnet run -- --debug-costscaling assignment_3x3  # Debug specific problem");
        Console.WriteLine("  dotnet run -- --gort                          # Run Gort benchmarks");
        Console.WriteLine("  dotnet run -- --gort --detailed               # Run detailed Gort benchmarks");
        Console.WriteLine();
        Console.WriteLine("For problem generation help:");
        Console.WriteLine("  dotnet run -- --generate");
    }
    
    private static void RunBenchmarkMode()
    {
        Console.WriteLine("Running BenchmarkDotNet benchmarks...");
        Console.WriteLine("This may take a while for large problems.");
        Console.WriteLine();
        
        BenchmarkRunner.Run<NetworkSimplexBenchmarks>();
    }
    
    private static void RunStatisticsMode()
    {
        Console.WriteLine("Running single-pass statistics collection...");
        Console.WriteLine();
        
        var solver = new SinglePassSolver();
        solver.RunAllProblems();
    }
    
    private static void RunComparisonMode(string[] args)
    {
        var runner = new DynamicBenchmarkRunner();
        var options = ParseComparisonOptions(args);
        
        runner.RunPerformanceComparison(options);
    }
    
    
    private static void RunQuickOptimizationBenchmark()
    {
        Console.WriteLine("Running quick optimization benchmark on Circulation_1000...");
        Console.WriteLine();
        
        var repository = new ProblemRepository();
        var problem = repository.GenerateCirculationProblem(1000, 0.05);
        
        Console.WriteLine($"Problem: {problem.NodeCount} nodes, {problem.ArcCount} arcs");
        Console.WriteLine($"Density: {(double)problem.ArcCount / (problem.NodeCount * (problem.NodeCount - 1)):F3}");
        Console.WriteLine();
        
        // Test configurations
        var configs = new[]
        {
            (name: "Baseline", flags: OptimizationFlags.None),
            (name: "SmallBlocks", flags: OptimizationFlags.SmallBlocksForDense),
            (name: "AdaptiveBlocks", flags: OptimizationFlags.AdaptiveBlockSize),
            (name: "Both", flags: OptimizationFlags.SmallBlocksForDense | OptimizationFlags.AdaptiveBlockSize),
            // Reduced cost caching disabled - not effective for dense networks
            // (name: "Both+Cache", flags: OptimizationFlags.SmallBlocksForDense | OptimizationFlags.AdaptiveBlockSize | OptimizationFlags.ReducedCostCaching),
        };
        
        // Warm up
        Console.WriteLine("Warming up...");
        foreach (var (_, flags) in configs)
        {
            SolveProblemWithConfig(problem, flags);
        }
        
        // Run benchmarks
        Console.WriteLine("\nRunning benchmarks (5 iterations each)...");
        Console.WriteLine();
        Console.WriteLine("Configuration       Time(ms)  Iter  Ratio  BlockSize    PivotSearch%  TreeUpdate%  PotentialUpdate%  AvgArcs/Pivot  Time/Iter(μs)");
        Console.WriteLine("".PadRight(140, '-'));
        
        foreach (var (name, flags) in configs)
        {
            var stopwatch = new Stopwatch();
            var metrics = new SolverMetrics();
            long totalTime = 0;
            
            // Run 5 times and take average
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Restart();
                var result = SolveProblemWithConfig(problem, flags);
                stopwatch.Stop();
                
                totalTime += stopwatch.ElapsedMilliseconds;
                if (i == 2) // Take middle run metrics
                {
                    metrics = result.metrics;
                }
            }
            
            double avgTime = totalTime / 5.0;
            
            double timePerIteration = metrics.TotalSolveTimeMicros > 0 ? 
                metrics.TotalSolveTimeMicros / metrics.Iterations : 
                avgTime * 1000.0 / metrics.Iterations;
            
            // Use microsecond timing for percentages if available
            double pivotPercent = metrics.TotalSolveTimeMicros > 0 ? 
                metrics.PivotSearchTimeMicros * 100.0 / metrics.TotalSolveTimeMicros :
                metrics.PivotSearchTimeMs * 100.0 / metrics.TotalSolveTimeMs;
            double treePercent = metrics.TotalSolveTimeMicros > 0 ?
                metrics.TreeUpdateTimeMicros * 100.0 / metrics.TotalSolveTimeMicros :
                metrics.TreeUpdateTimeMs * 100.0 / metrics.TotalSolveTimeMs;
            double potentialPercent = metrics.TotalSolveTimeMicros > 0 ?
                metrics.PotentialUpdateTimeMicros * 100.0 / metrics.TotalSolveTimeMicros :
                metrics.PotentialUpdateTimeMs * 100.0 / metrics.TotalSolveTimeMs;
            
            Console.WriteLine($"{name,-18} {avgTime,8:F1} {metrics.Iterations,5} {metrics.IterationRatio,5:F1}x {metrics.InitialBlockSize,3}->{metrics.FinalBlockSize,-6} " +
                            $"{pivotPercent,10:F1}% " +
                            $"{treePercent,10:F1}% " +
                            $"{potentialPercent,15:F1}% " +
                            $"{metrics.AverageArcsCheckedPerPivot,13:F0} " +
                            $"{timePerIteration,13:F1}");
        }
        
        Console.WriteLine();
    }
    
    private static (SolverStatus status, long totalCost, SolverMetrics metrics) SolveProblemWithConfig(
        MinCostFlowProblem problem, OptimizationFlags flags)
    {
        var solver = new NetworkSimplex(problem.Graph);
        solver.EnableOptimizations(flags);
        
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
        
        var status = solver.Solve();
        var totalCost = status == SolverStatus.Optimal ? solver.GetTotalCost() : 0;
        var metrics = solver.GetMetrics();
        
        return (status, totalCost, metrics);
    }
    
    private static void ListProblems(string[] args)
    {
        var runner = new DynamicBenchmarkRunner();
        var detailed = args.Contains("--detailed");
        runner.ListProblems(detailed);
    }
    
    private static void RunValidationMode(string[] args)
    {
        var runner = new DynamicBenchmarkRunner();
        var options = ParseComparisonOptions(args);
        options.ValidateOnly = true;
        
        Console.WriteLine("Running validation on all problems with known solutions...");
        Console.WriteLine();
        
        runner.RunPerformanceComparison(options);
    }
    
    private static DynamicBenchmarkRunner.RunOptions ParseComparisonOptions(string[] args)
    {
        var options = new DynamicBenchmarkRunner.RunOptions();
        
        // Parse category
        var categoryIndex = Array.IndexOf(args, "--category");
        if (categoryIndex >= 0 && categoryIndex < args.Length - 1)
        {
            options.Categories = [args[categoryIndex + 1]];
        }
        
        // Parse flags
        options.IncludeGenerated = !args.Contains("--no-generated");
        options.IncludeLarge = args.Contains("--include-large");
        options.ValidateOnly = args.Contains("--validate-only") || args.Contains("-v");
        
        // Parse numeric limits
        var maxNodesIndex = Array.IndexOf(args, "--max-nodes");
        if (maxNodesIndex >= 0 && maxNodesIndex < args.Length - 1)
        {
            if (int.TryParse(args[maxNodesIndex + 1], out var maxNodes))
            {
                options.MaxNodes = maxNodes;
            }
        }
        
        var maxArcsIndex = Array.IndexOf(args, "--max-arcs");
        if (maxArcsIndex >= 0 && maxArcsIndex < args.Length - 1)
        {
            if (int.TryParse(args[maxArcsIndex + 1], out var maxArcs))
            {
                options.MaxArcs = maxArcs;
            }
        }
        
        return options;
    }
    
    
    private static void RunKnapzackTest()
    {
        Console.WriteLine("Running Knapzack problem test...");
        Console.WriteLine();
        
        // First, let's see what the problem looks like
        var problemSet = new BenchmarkProblemSet();
        var knapzackProblems = problemSet.GetByCategory("Knapzack").ToList();
        
        if (knapzackProblems.Count == 0)
        {
            Console.WriteLine("No Knapzack problems found!");
            return;
        }
        
        var problem = knapzackProblems[0];
        Console.WriteLine($"Problem: {problem.DisplayName}");
        Console.WriteLine($"Nodes: {problem.Problem.NodeCount}");
        Console.WriteLine($"Arcs: {problem.Problem.ArcCount}");
        Console.WriteLine($"Has solution: {problem.HasSolution}");
        if (problem.HasSolution && problem.Solution != null)
        {
            if (problem.Solution.OptimalCost == long.MinValue)
            {
                Console.WriteLine($"Expected optimal cost: Not specified in solution file");
                Console.WriteLine($"Solution has {problem.Solution.ArcFlowsByEndpoints.Count} flows");
                Console.WriteLine("Note: Solution file contains flows but no optimal cost value");
            }
            else
            {
                Console.WriteLine($"Expected optimal cost: {problem.Solution.OptimalCost}");
                Console.WriteLine($"Solution has {problem.Solution.ArcFlowsByEndpoints.Count} flows");
            }
        }
        Console.WriteLine();
        
        // Run each solver individually
        // Run in order: OR-Tools → CostScaling → NetworkSimplex → TarjanEnhanced
        var solvers = new[] { "OrTools", "CostScaling", "NetworkSimplex", "TarjanEnhanced" };
        
        foreach (var solverName in solvers)
        {
            Console.WriteLine($"\nTesting {solverName}...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var (status, cost) = RunSingleSolver(problem.Problem, solverName);
                sw.Stop();
                
                Console.WriteLine($"  Status: {status}");
                Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
                if (status == SolverStatus.Optimal)
                {
                    Console.WriteLine($"  Cost: {cost}");
                    if (problem.HasSolution && problem.Solution != null)
                    {
                        if (problem.Solution.OptimalCost == long.MinValue)
                        {
                            Console.WriteLine($"  Validation: N/A (no expected cost in solution file)");
                        }
                        else
                        {
                            var validated = cost == problem.Solution.OptimalCost;
                            Console.WriteLine($"  Validation: {(validated ? "PASSED" : $"FAILED - expected {problem.Solution.OptimalCost}")}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"  ERROR: {ex.Message}");
                Console.WriteLine($"  Time before error: {sw.ElapsedMilliseconds}ms");
            }
        }
    }
    
    private static (SolverStatus status, long cost) RunSingleSolver(MinCostFlowProblem problem, string solverName)
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
            var status = solver.Solve();
            return (status, status == SolverStatus.Optimal ? solver.GetTotalCost() : 0);
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
            
            var status = solver.Solve(CostScaling.Method.PartialAugment);
            return (status, status == SolverStatus.Optimal ? solver.GetTotalCost() : 0);
        }
        else if (solverName == "TarjanEnhanced")
        {
            throw new NotImplementedException();
        }
        else
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
            var status = solver.Solve();
            return (status, status == SolverStatus.Optimal ? solver.GetTotalCost() : 0);
        }
    }
    
}