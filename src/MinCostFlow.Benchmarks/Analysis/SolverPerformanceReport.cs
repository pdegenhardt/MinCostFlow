using System;
using System.Collections.Generic;
using System.Linq;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Benchmarks.Analysis;

/// <summary>
/// Comprehensive performance metrics for a single solver on a single problem.
/// </summary>
public class SolverPerformanceReport
{
    public string SolverName { get; set; } = "";
    public string ProblemName { get; set; } = "";
    public string ProblemCategory { get; set; } = "";
    
    // Problem characteristics
    public int NodeCount { get; set; }
    public int ArcCount { get; set; }
    public double NetworkDensity { get; set; }
    public bool IsTransportation { get; set; }
    public bool IsCirculation { get; set; }
    public bool IsAssignment { get; set; }
    
    // Time metrics
    public double SolveTimeMs { get; set; }
    public double MemoryUsageMB { get; set; }
    public double MemoryAllocatedMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    
    // Algorithm-specific metrics
    public int Iterations { get; set; }
    public int PivotOperations { get; set; }      // NetworkSimplex
    public int RelabelOperations { get; set; }    // TarjanEnhanced, CostScaling
    public int PushOperations { get; set; }       // TarjanEnhanced, CostScaling
    public int EpsilonPhases { get; set; }        // Cost scaling algorithms
    public int PriceUpdates { get; set; }         // TarjanEnhanced
    
    // Solution quality
    public long OptimalCost { get; set; }
    public long ExpectedCost { get; set; }
    public bool IsOptimal { get; set; }
    public bool CostMatches { get; set; }
    public SolverStatus Status { get; set; }
    public string ValidationStatus { get; set; } = "";
    
    // Performance comparison
    public double RelativePerformance { get; set; } // Ratio to baseline solver
    public string PerformanceCategory { get; set; } = ""; // "Fastest", "Competitive", "Slower"
    
    // Timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Error information
    public string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}

/// <summary>
/// Collection of performance reports with analysis capabilities.
/// </summary>
public class PerformanceReportCollection
{
    private readonly List<SolverPerformanceReport> _reports = new();
    
    public IReadOnlyList<SolverPerformanceReport> Reports => _reports.AsReadOnly();
    
    public void AddReport(SolverPerformanceReport report)
    {
        _reports.Add(report);
    }
    
    public void AddReports(IEnumerable<SolverPerformanceReport> reports)
    {
        _reports.AddRange(reports);
    }
    
    /// <summary>
    /// Gets reports for a specific problem.
    /// </summary>
    public IEnumerable<SolverPerformanceReport> GetReportsForProblem(string problemName)
    {
        return _reports.Where(r => r.ProblemName == problemName);
    }
    
    /// <summary>
    /// Gets reports for a specific solver.
    /// </summary>
    public IEnumerable<SolverPerformanceReport> GetReportsForSolver(string solverName)
    {
        return _reports.Where(r => r.SolverName == solverName);
    }
    
    /// <summary>
    /// Gets reports for a specific problem category.
    /// </summary>
    public IEnumerable<SolverPerformanceReport> GetReportsForCategory(string category)
    {
        return _reports.Where(r => r.ProblemCategory == category);
    }
    
    /// <summary>
    /// Calculates relative performance metrics for all reports.
    /// </summary>
    public void CalculateRelativePerformance(string baselineSolver = "NetworkSimplex")
    {
        var problemGroups = _reports.GroupBy(r => r.ProblemName);
        
        foreach (var problemGroup in problemGroups)
        {
            var baseline = problemGroup.FirstOrDefault(r => r.SolverName == baselineSolver);
            if (baseline == null || baseline.SolveTimeMs <= 0)
            {
                // Use fastest solver as baseline if specified baseline not available
                baseline = problemGroup.OrderBy(r => r.SolveTimeMs).FirstOrDefault();
            }
            
            if (baseline == null || baseline.SolveTimeMs <= 0) continue;
            
            foreach (var report in problemGroup)
            {
                report.RelativePerformance = report.SolveTimeMs / baseline.SolveTimeMs;
                
                report.PerformanceCategory = report.RelativePerformance switch
                {
                    <= 1.1 => "Fastest",
                    <= 2.0 => "Competitive", 
                    <= 5.0 => "Slower",
                    _ => "Much Slower"
                };
            }
        }
    }
    
    /// <summary>
    /// Generates performance summary by solver.
    /// </summary>
    public Dictionary<string, SolverSummary> GetSolverSummaries()
    {
        var summaries = new Dictionary<string, SolverSummary>();
        
        var solverGroups = _reports.Where(r => !r.HasError).GroupBy(r => r.SolverName);
        
        foreach (var solverGroup in solverGroups)
        {
            var reports = solverGroup.ToList();
            
            summaries[solverGroup.Key] = new SolverSummary
            {
                SolverName = solverGroup.Key,
                TotalProblems = reports.Count,
                SuccessfulSolves = reports.Count(r => r.IsOptimal),
                AverageTimeMs = reports.Average(r => r.SolveTimeMs),
                MedianTimeMs = reports.OrderBy(r => r.SolveTimeMs).Skip(reports.Count / 2).First().SolveTimeMs,
                FastestTimeMs = reports.Min(r => r.SolveTimeMs),
                SlowestTimeMs = reports.Max(r => r.SolveTimeMs),
                AverageMemoryMB = reports.Average(r => r.MemoryUsageMB),
                FastestProblems = reports.Count(r => r.PerformanceCategory == "Fastest"),
                CompetitiveProblems = reports.Count(r => r.PerformanceCategory == "Competitive"),
                SlowerProblems = reports.Count(r => r.PerformanceCategory == "Slower"),
                ProblemsWithErrors = _reports.Count(r => r.SolverName == solverGroup.Key && r.HasError)
            };
        }
        
        return summaries;
    }
    
    /// <summary>
    /// Generates performance matrix showing solver performance across problem categories.
    /// </summary>
    public Dictionary<string, Dictionary<string, CategoryPerformance>> GetPerformanceMatrix()
    {
        var matrix = new Dictionary<string, Dictionary<string, CategoryPerformance>>();
        
        var categoryGroups = _reports.Where(r => !r.HasError).GroupBy(r => r.ProblemCategory);
        
        foreach (var categoryGroup in categoryGroups)
        {
            var category = categoryGroup.Key;
            var solverGroups = categoryGroup.GroupBy(r => r.SolverName);
            
            foreach (var solverGroup in solverGroups)
            {
                var solver = solverGroup.Key;
                var reports = solverGroup.ToList();
                
                if (!matrix.ContainsKey(solver))
                {
                    matrix[solver] = new Dictionary<string, CategoryPerformance>();
                }
                
                matrix[solver][category] = new CategoryPerformance
                {
                    ProblemCount = reports.Count,
                    AverageTimeMs = reports.Average(r => r.SolveTimeMs),
                    AverageRelativePerformance = reports.Average(r => r.RelativePerformance),
                    BestPerformanceCount = reports.Count(r => r.PerformanceCategory == "Fastest"),
                    CompetitivePerformanceCount = reports.Count(r => r.PerformanceCategory == "Competitive")
                };
            }
        }
        
        return matrix;
    }
}

/// <summary>
/// Summary statistics for a solver across all problems.
/// </summary>
public class SolverSummary
{
    public string SolverName { get; set; } = "";
    public int TotalProblems { get; set; }
    public int SuccessfulSolves { get; set; }
    public double AverageTimeMs { get; set; }
    public double MedianTimeMs { get; set; }
    public double FastestTimeMs { get; set; }
    public double SlowestTimeMs { get; set; }
    public double AverageMemoryMB { get; set; }
    public int FastestProblems { get; set; }
    public int CompetitiveProblems { get; set; }
    public int SlowerProblems { get; set; }
    public int ProblemsWithErrors { get; set; }
    
    public double SuccessRate => TotalProblems > 0 ? (double)SuccessfulSolves / TotalProblems : 0.0;
    public double FastestRate => TotalProblems > 0 ? (double)FastestProblems / TotalProblems : 0.0;
}

/// <summary>
/// Performance statistics for a solver in a specific problem category.
/// </summary>
public class CategoryPerformance
{
    public int ProblemCount { get; set; }
    public double AverageTimeMs { get; set; }
    public double AverageRelativePerformance { get; set; }
    public int BestPerformanceCount { get; set; }
    public int CompetitivePerformanceCount { get; set; }
    
    public double DominanceRate => ProblemCount > 0 ? (double)BestPerformanceCount / ProblemCount : 0.0;
    public double CompetitivenessRate => ProblemCount > 0 ? (double)(BestPerformanceCount + CompetitivePerformanceCount) / ProblemCount : 0.0;
}