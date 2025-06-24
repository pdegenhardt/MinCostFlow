using System;
using System.IO;
using MinCostFlow.Benchmarks.Analysis;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Sets;

namespace MinCostFlow.Benchmarks
{
    /// <summary>
    /// Standalone program to run performance comparison between NetworkSimplex and OR-Tools.
    /// </summary>
    public class RunComparison
    {
        public static void RunPerformanceComparison()
        {
            Console.WriteLine("Starting NetworkSimplex vs OR-Tools Performance Comparison");
            Console.WriteLine("=========================================================\n");

            var report = new PerformanceComparisonReport();
            var repository = new ProblemRepository();

            // Small problems
            Console.WriteLine("Testing Small Problems:");
            report.RunComparison(StandardProblems.Small.Path5Node, "Small_Path5");
            report.RunComparison(StandardProblems.Small.Path10Node, "Small_Path10");
            report.RunComparison(StandardProblems.Small.Grid2x2, "Small_Grid2x2");
            report.RunComparison(StandardProblems.Small.DiamondGraph, "Small_Diamond");
            report.RunComparison(StandardProblems.Small.Simple4Node, "Small_Simple4");
            report.RunComparison(StandardProblems.Small.StarGraph, "Small_Star");
            report.RunComparison(StandardProblems.Small.CycleShortcut, "Small_Cycle");
            report.RunComparison(StandardProblems.Small.Assignment3x3, "Small_Assignment3x3");
            report.RunComparison(StandardProblems.Small.Transport2x3, "Small_Transport2x3");

            // Medium generated problems
            Console.WriteLine("\nTesting Medium Generated Problems:");
            report.RunComparison(
                repository.GenerateTransportationProblem(10, 10, 1000), 
                "Transport_100");
            report.RunComparison(
                repository.GenerateTransportationProblem(22, 22, 1000), 
                "Transport_500");
            report.RunComparison(
                repository.GenerateCirculationProblem(1000, 0.05), 
                "Circulation_1000");

            // LEMON test problem
            Console.WriteLine("\nTesting LEMON Test Problem:");
            report.RunComparison(StandardProblems.Lemon.Test12Node, "LEMON_Test12");
            
            // DIMACS problems
            Console.WriteLine("\nTesting DIMACS Benchmark Problems:");
            report.RunComparison(StandardProblems.Dimacs.Netgen8_08a, "DIMACS_Netgen8_08a");
            report.RunComparison(StandardProblems.Dimacs.Netgen8_10a, "DIMACS_Netgen8_10a");
            
            // Large problems (fewer iterations)
            Console.WriteLine("\nTesting Large Problems:");
            report.RunComparison(
                repository.GenerateTransportationProblem(71, 71, 1000), 
                "Transport_5000");
            report.RunComparison(
                repository.GenerateCirculationProblem(5000, 0.05), 
                "Circulation_5000", 3); // Fewer iterations for large problems
            report.RunComparison(
                repository.GenerateCirculationProblem(6000, 0.05), 
                "Circulation_6000", 3); // Fewer iterations for large problems
            report.RunComparison(
                repository.GeneratePathProblem(10000, 1000), 
                "Path_10000");
            report.RunComparison(
                repository.GenerateGridProblem(100, 100, 0, 0, 99, 99, 1000), 
                "Grid_100x100");

            // Only run very large DIMACS if requested
            if (Environment.GetEnvironmentVariable("RUN_LARGE_BENCHMARKS") == "true")
            {
                Console.WriteLine("\nTesting Very Large DIMACS Problems:");
                report.RunComparison(StandardProblems.Dimacs.Netgen8_13a, "DIMACS_Netgen8_13a");
                report.RunComparison(StandardProblems.Dimacs.Netgen8_14a, "DIMACS_Netgen8_14a");
                report.RunComparison(StandardProblems.Dimacs.Netgen8_15a, "DIMACS_Netgen8_15a");
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
    }
}