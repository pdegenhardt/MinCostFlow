using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Problems.Loaders;

namespace MinCostFlow.Benchmarks.Analysis;

/// <summary>
/// Tracks and reports on solution validation results.
/// </summary>
public class ValidationReport
{
    /// <summary>
    /// Represents a validation result for a single problem/solver combination.
    /// </summary>
    public class ValidationResult
    {
        public string ProblemName { get; set; } = "";
        public string SolverName { get; set; } = "";
        public SolverStatus Status { get; set; }
        public long ComputedCost { get; set; }
        public long ExpectedCost { get; set; }
        public bool Passed { get; set; }
        public string? FailureReason { get; set; }
        public double SolveTimeMs { get; set; }
    }
    
    private readonly List<ValidationResult> _results = [];
    
    /// <summary>
    /// Adds a validation result.
    /// </summary>
    public void AddResult(ValidationResult result)
    {
        _results.Add(result);
    }
    
    /// <summary>
    /// Validates a solver result against an expected solution.
    /// </summary>
    public ValidationResult Validate(
        string problemName, 
        string solverName, 
        SolverStatus status,
        long computedCost,
        SolutionLoader.Solution expectedSolution,
        double solveTimeMs)
    {
        var result = new ValidationResult
        {
            ProblemName = problemName,
            SolverName = solverName,
            Status = status,
            ComputedCost = computedCost,
            ExpectedCost = expectedSolution.OptimalCost,
            SolveTimeMs = solveTimeMs
        };
        
        if (status != SolverStatus.Optimal)
        {
            result.Passed = false;
            result.FailureReason = $"Solver returned {status} instead of Optimal";
        }
        else if (computedCost != expectedSolution.OptimalCost)
        {
            result.Passed = false;
            result.FailureReason = $"Cost mismatch: expected {expectedSolution.OptimalCost:N0}, got {computedCost:N0}";
        }
        else
        {
            result.Passed = true;
        }
        
        _results.Add(result);
        return result;
    }
    
    /// <summary>
    /// Gets all validation results.
    /// </summary>
    public IEnumerable<ValidationResult> Results => _results;
    
    /// <summary>
    /// Gets validation results grouped by problem.
    /// </summary>
    public IEnumerable<IGrouping<string, ValidationResult>> GetResultsByProblem()
    {
        return _results.GroupBy(r => r.ProblemName);
    }
    
    /// <summary>
    /// Gets validation results grouped by solver.
    /// </summary>
    public IEnumerable<IGrouping<string, ValidationResult>> GetResultsBySolver()
    {
        return _results.GroupBy(r => r.SolverName);
    }
    
    /// <summary>
    /// Gets summary statistics.
    /// </summary>
    public (int total, int passed, int failed) GetSummary()
    {
        var total = _results.Count;
        var passed = _results.Count(r => r.Passed);
        var failed = total - passed;
        return (total, passed, failed);
    }
    
    /// <summary>
    /// Gets summary statistics by solver.
    /// </summary>
    public Dictionary<string, (int total, int passed, int failed)> GetSummaryBySolver()
    {
        return GetResultsBySolver()
            .ToDictionary(
                g => g.Key,
                g => (g.Count(), g.Count(r => r.Passed), g.Count(r => !r.Passed))
            );
    }
    
    /// <summary>
    /// Gets all failed validations.
    /// </summary>
    public IEnumerable<ValidationResult> GetFailures()
    {
        return _results.Where(r => !r.Passed);
    }
    
    /// <summary>
    /// Generates a markdown report.
    /// </summary>
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Solution Validation Report");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        var (total, passed, failed) = GetSummary();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Total validations: {total}");
        sb.AppendLine($"- Passed: {passed} ({100.0 * passed / total:F1}%)");
        sb.AppendLine($"- Failed: {failed} ({100.0 * failed / total:F1}%)");
        sb.AppendLine();
        
        // Summary by solver
        var solverSummary = GetSummaryBySolver();
        if (solverSummary.Count > 1)
        {
            sb.AppendLine("## Summary by Solver");
            sb.AppendLine("| Solver | Total | Passed | Failed | Pass Rate |");
            sb.AppendLine("|--------|------:|-------:|-------:|----------:|");
            
            foreach (var (solver, (solverTotal, solverPassed, solverFailed)) in solverSummary.OrderBy(s => s.Key))
            {
                var passRate = 100.0 * solverPassed / solverTotal;
                sb.AppendLine($"| {solver} | {solverTotal} | {solverPassed} | {solverFailed} | {passRate:F1}% |");
            }
            sb.AppendLine();
        }
        
        // Failed validations
        var failures = GetFailures().ToList();
        if (failures.Count != 0)
        {
            sb.AppendLine("## Failed Validations");
            sb.AppendLine();
            
            var failuresByProblem = failures.GroupBy(f => f.ProblemName).OrderBy(g => g.Key);
            foreach (var group in failuresByProblem)
            {
                sb.AppendLine($"### {group.Key}");
                sb.AppendLine("| Solver | Status | Computed Cost | Expected Cost | Reason |");
                sb.AppendLine("|--------|--------|---------------|---------------|---------|");
                
                foreach (var failure in group.OrderBy(f => f.SolverName))
                {
                    sb.AppendLine($"| {failure.SolverName} | {failure.Status} | " +
                                 $"{failure.ComputedCost:N0} | {failure.ExpectedCost:N0} | " +
                                 $"{failure.FailureReason} |");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("## All Validations Passed! ✓");
            sb.AppendLine();
        }
        
        // Detailed results
        sb.AppendLine("## Detailed Results");
        sb.AppendLine("| Problem | Solver | Status | Time (ms) | Computed | Expected | Result |");
        sb.AppendLine("|---------|--------|--------|-----------|----------|----------|--------|");
        
        foreach (var result in _results.OrderBy(r => r.ProblemName).ThenBy(r => r.SolverName))
        {
            var resultIcon = result.Passed ? "✓" : "✗";
            sb.AppendLine($"| {result.ProblemName} | {result.SolverName} | {result.Status} | " +
                         $"{result.SolveTimeMs:F2} | {result.ComputedCost:N0} | " +
                         $"{result.ExpectedCost:N0} | {resultIcon} |");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates a console summary.
    /// </summary>
    public void PrintSummary()
    {
        var (total, passed, failed) = GetSummary();
        
        if (failed == 0)
        {
            Console.WriteLine($"✓ All {total} validations passed!");
        }
        else
        {
            Console.WriteLine($"⚠ Validation Summary: {passed}/{total} passed ({100.0 * passed / total:F1}%)");
            
            var failures = GetFailures().Take(5).ToList();
            foreach (var failure in failures)
            {
                Console.WriteLine($"  ✗ {failure.ProblemName} - {failure.SolverName}: {failure.FailureReason}");
            }
            
            if (GetFailures().Count() > 5)
            {
                Console.WriteLine($"  ... and {GetFailures().Count() - 5} more failures");
            }
        }
    }
}