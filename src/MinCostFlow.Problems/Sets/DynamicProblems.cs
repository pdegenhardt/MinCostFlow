using System;
using System.Collections.Generic;
using System.Linq;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Sets;

/// <summary>
/// Provides dynamic enumeration of embedded problems without hard-coding.
/// </summary>
public class DynamicProblems(EmbeddedResourceLoader? loader = null)
{
    private readonly EmbeddedResourceLoader _loader = loader ?? new EmbeddedResourceLoader();
    private readonly Dictionary<string, MinCostFlowProblem> _cache = [];
    private readonly object _cacheLock = new();
    private Dictionary<string, List<string>>? _problemsByCategory;

    /// <summary>
    /// Gets all available problems grouped by category.
    /// </summary>
    public Dictionary<string, List<MinCostFlowProblem>> GetAllByCategory()
    {
        var problemsByCategory = GetProblemResourcesByCategory();
        var result = new Dictionary<string, List<MinCostFlowProblem>>();
        
        foreach (var (category, resources) in problemsByCategory)
        {
            var problems = new List<MinCostFlowProblem>();
            foreach (var resource in resources)
            {
                try
                {
                    var problem = LoadCached(resource);
                    problems.Add(problem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load {resource}: {ex.Message}");
                }
            }
            
            if (problems.Count > 0)
            {
                result[category] = problems;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets all problems in a specific category.
    /// </summary>
    public IEnumerable<MinCostFlowProblem> GetByCategory(string category)
    {
        var problemsByCategory = GetProblemResourcesByCategory();
        
        if (problemsByCategory.TryGetValue(category, out var resources))
        {
            foreach (var resource in resources)
            {
                yield return LoadCached(resource);
            }
        }
    }
    
    /// <summary>
    /// Gets all available problems.
    /// </summary>
    public IEnumerable<MinCostFlowProblem> GetAll()
    {
        var allResources = _loader.GetAvailableProblems();
        
        foreach (var resource in allResources)
        {
            yield return LoadCached(resource);
        }
    }
    
    /// <summary>
    /// Gets a specific problem by resource name.
    /// </summary>
    public MinCostFlowProblem? GetProblem(string resourceName)
    {
        try
        {
            return LoadCached(resourceName);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Gets the count of available problems.
    /// </summary>
    public int Count => _loader.GetAvailableProblems().Length;
    
    /// <summary>
    /// Gets the count of problems by category.
    /// </summary>
    public Dictionary<string, int> GetCountByCategory()
    {
        var problemsByCategory = GetProblemResourcesByCategory();
        return problemsByCategory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }
    
    /// <summary>
    /// Clears the internal cache.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
            _problemsByCategory = null;
        }
    }
    
    /// <summary>
    /// Gets problem resources grouped by category.
    /// </summary>
    private Dictionary<string, List<string>> GetProblemResourcesByCategory()
    {
        if (_problemsByCategory != null)
        {
            return _problemsByCategory;
        }

        var result = new Dictionary<string, List<string>>();
        var allResources = _loader.GetAvailableProblems();
        
        foreach (var resource in allResources)
        {
            var category = DetermineCategory(resource);
            if (!result.ContainsKey(category))
            {
                result[category] = [];
            }
            result[category].Add(resource);
        }
        
        // Sort resources within each category
        foreach (var list in result.Values)
        {
            list.Sort();
        }
        
        _problemsByCategory = result;
        return result;
    }
    
    /// <summary>
    /// Determines the category of a problem from its resource name.
    /// </summary>
    private static string DetermineCategory(string resourceName)
    {
        if (resourceName.Contains(".small."))
        {
            return "Small";
        }
        else if (resourceName.Contains(".medium."))
        {
            return "Medium";
        }
        else if (resourceName.Contains(".large."))
        {
            return "Large";
        }
        else if (resourceName.Contains(".dimacs."))
        {
            return "DIMACS";
        }
        else if (resourceName.Contains(".lemon."))
        {
            return "LEMON";
        }
        else
        {
            return "Other";
        }
    }
    
    /// <summary>
    /// Loads a problem from embedded resources with caching.
    /// </summary>
    private MinCostFlowProblem LoadCached(string resourceName)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var problem = _loader.LoadFromResource(resourceName);
            
            // Try to set optimal cost from solution if available
            var solutionResource = resourceName.Replace(".min", ".sol");
            try
            {
                var solution = _loader.LoadSolutionFromResource(solutionResource);
                if (problem.Metadata != null && solution != null)
                {
                    problem.Metadata.OptimalCost = solution.OptimalCost;
                }
            }
            catch
            {
                // Solution not available, that's okay
            }
            
            _cache[resourceName] = problem;
            return problem;
        }
    }
}