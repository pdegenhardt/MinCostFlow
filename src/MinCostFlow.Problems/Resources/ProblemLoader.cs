using System;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Resources;

/// <summary>
/// Provides convenient methods to load embedded problems with proper metadata.
/// </summary>
public static class ProblemLoader
{
    private static readonly EmbeddedResourceLoader _loader = new();
    
    /// <summary>
    /// Loads a problem from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The full resource name (use constants from EmbeddedProblems class).</param>
    /// <returns>The loaded problem with metadata properly set.</returns>
    public static MinCostFlowProblem Load(string resourceName)
    {
        var problem = _loader.LoadFromResource(resourceName);
        
        // Ensure metadata is properly set
        if (problem.Metadata != null)
        {
            // Extract meaningful name from resource path
            var parts = resourceName.Split('.');
            if (parts.Length >= 3)
            {
                var fileName = parts[^2]; // Get filename without extension
                problem.Metadata.Name = fileName;
                
                // Set category based on folder
                var folderIndex = parts.Length - 3;
                if (folderIndex >= 0)
                {
                    var folder = parts[folderIndex];
                    problem.Metadata.Category = folder switch
                    {
                        "path" => "Path",
                        "grid" => "Grid",
                        "circulation" => "Circulation",
                        "transport" => "Transport",
                        "assignment" => "Assignment",
                        "netgen" => "DIMACS",
                        "knapzack" => "Knapsack",
                        _ => "Other"
                    };
                }
            }
            
            // Try to load solution to get optimal cost
            var solutionResource = resourceName.Replace(".min", ".sol");
            try
            {
                var solution = _loader.LoadSolutionFromResource(solutionResource);
                if (solution != null)
                {
                    problem.Metadata.OptimalCost = solution.OptimalCost;
                }
            }
            catch
            {
                // Solution not available, that's okay
            }
        }
        
        return problem;
    }
    
    /// <summary>
    /// Loads a problem and its solution from embedded resources.
    /// </summary>
    /// <param name="problemResource">The problem resource name.</param>
    /// <returns>A tuple containing the problem and its solution (if available).</returns>
    public static (MinCostFlowProblem problem, SolutionLoader.Solution? solution) LoadWithSolution(string problemResource)
    {
        var problem = Load(problemResource);
        SolutionLoader.Solution? solution = null;
        
        var solutionResource = problemResource.Replace(".min", ".sol");
        try
        {
            solution = _loader.LoadSolutionFromResource(solutionResource);
        }
        catch
        {
            // Solution not available
        }
        
        return (problem, solution);
    }
}