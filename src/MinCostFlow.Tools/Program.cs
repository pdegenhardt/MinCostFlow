using MinCostFlow.Core.Algorithms;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Core.Types;
using MinCostFlow.Core.Validation;
using System;
using System.Diagnostics;
using System.IO;

namespace MinCostFlow.Tools;

/// <summary>
/// Main program for validating MinCostFlow solver against benchmark problems.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "debug-tarjan")
        {
            TarjanDebugRunner.Run();
            return;
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        string problemPath = args[0];
        
        
        if (File.Exists(problemPath))
        {
            ValidateSingleProblem(problemPath);
        }
        else if (Directory.Exists(problemPath))
        {
            ValidateDirectory(problemPath);
        }
        else
        {
            Console.WriteLine($"Error: Path '{problemPath}' not found");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("MinCostFlow Validation Tool");
        Console.WriteLine("Usage: MinCostFlow.Validation <problem-file-or-directory>");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  MinCostFlow.Validation benchmarks/data/small/test.min");
        Console.WriteLine("  MinCostFlow.Validation benchmarks/data/");
    }

    private static void ValidateDirectory(string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath, "*.min", SearchOption.AllDirectories);
        
        if (files.Length == 0)
        {
            Console.WriteLine($"No .min files found in {directoryPath}");
            return;
        }
        
        Console.WriteLine($"Found {files.Length} problems to validate");
        Console.WriteLine();
        
        int passed = 0;
        int failed = 0;
        int errors = 0;
        
        foreach (var file in files)
        {
            Console.WriteLine($"Testing: {Path.GetRelativePath(directoryPath, file)}");
            
            try
            {
                var problem = DimacsReader.ReadFromFile(file);
                var solver = new NetworkSimplex(problem.Graph);
                
                // Set up problem
                for (int i = 0; i < problem.NodeCount; i++)
                {
                    solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                }
                
                for (int i = 0; i < problem.ArcCount; i++)
                {
                    var arc = new Arc(i);
                    solver.SetArcCost(arc, problem.ArcCosts[i]);
                    solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                }
                
                // Solve
                var sw = Stopwatch.StartNew();
                var status = solver.Solve();
                sw.Stop();
                
                // Quick validation
                var validator = new SolutionValidator(problem.Graph, solver);
                
                var result = validator.Validate();
                
                if (result.IsValid)
                {
                    Console.WriteLine($"  ✓ PASSED - {problem.NodeCount}n/{problem.ArcCount}a - " +
                                    $"{sw.ElapsedMilliseconds}ms - optimal: {result.ObjectiveValue}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"  ✗ FAILED - {result.Errors.Count} errors");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ! ERROR - {ex.Message}");
                errors++;
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Passed: {passed}");
        Console.WriteLine($"  Failed: {failed}");
        Console.WriteLine($"  Errors: {errors}");
        Console.WriteLine($"  Total:  {files.Length}");
    }


}