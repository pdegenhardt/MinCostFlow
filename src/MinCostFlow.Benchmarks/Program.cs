using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Types;
using MinCostFlow.Core.Validation;
using MinCostFlow.Problems;

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
            RunComparisonMode();
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
        Console.WriteLine("  --stats, -s       Run single-pass statistics (default)");
        Console.WriteLine("  --benchmark, -b   Run full BenchmarkDotNet benchmarks");
        Console.WriteLine("  --compare, -c     Run NetworkSimplex vs OR-Tools comparison");
        Console.WriteLine("  --generate, -g    Generate new test problems");
        Console.WriteLine("  --help, -h        Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                            # Run statistics mode");
        Console.WriteLine("  dotnet run -- --stats                 # Run statistics mode");
        Console.WriteLine("  dotnet run -- --benchmark             # Run benchmarks");
        Console.WriteLine("  dotnet run -- --compare               # Run solver comparison");
        Console.WriteLine("  dotnet run -- --generate assignment 50  # Generate 50x50 assignment problem");
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
    
    private static void RunComparisonMode()
    {
        Console.WriteLine("Running NetworkSimplex vs OR-Tools comparison...");
        Console.WriteLine();
        
        RunComparison.RunPerformanceComparison();
    }
}