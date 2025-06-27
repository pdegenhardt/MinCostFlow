using System;
using System.Collections.Generic;
using System.Linq;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;
using MinCostFlow.Problems.Sets;

namespace MinCostFlow.Benchmarks.Analysis;

/// <summary>
/// Manages the discovery and organization of benchmark problems from embedded resources.
/// </summary>
public class BenchmarkProblemSet
{
    private readonly DynamicProblems _dynamicProblems;
    private readonly EmbeddedResourceLoader _loader;
    private Dictionary<string, List<ProblemWithSolution>>? _problemsByCategory;
    private readonly object _lock = new();

    /// <summary>
    /// Represents a problem with its optional solution.
    /// </summary>
    public class ProblemWithSolution
    {
        public MinCostFlowProblem Problem { get; set; } = null!;
        public SolutionLoader.Solution? Solution { get; set; }
        public string ResourceName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Category { get; set; } = "";
        public bool HasSolution => Solution != null;
    }

    public BenchmarkProblemSet()
    {
        _loader = new EmbeddedResourceLoader();
        _dynamicProblems = new DynamicProblems(_loader);
    }

    /// <summary>
    /// Gets all available problem categories.
    /// </summary>
    public IEnumerable<string> Categories
    {
        get
        {
            var problems = GetProblemsByCategory();
            // Sort categories alphabetically
            return problems.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets the total number of problems.
    /// </summary>
    public int ProblemCount => _dynamicProblems.Count;

    /// <summary>
    /// Gets the number of problems with solutions.
    /// </summary>
    public int ProblemsWithSolutionCount
    {
        get
        {
            var problems = GetProblemsByCategory();
            return problems.Values.SelectMany(list => list).Count(p => p.HasSolution);
        }
    }

    /// <summary>
    /// Discovers all problems from embedded resources.
    /// </summary>
    public Dictionary<string, List<ProblemWithSolution>> DiscoverProblems()
    {
        lock (_lock)
        {
            if (_problemsByCategory != null)
            {
                return _problemsByCategory;
            }

            _problemsByCategory = [];
            var allProblems = _loader.GetAvailableProblems();
            var allSolutions = _loader.GetAvailableSolutions();

            // Create a set of solution resources for quick lookup
            var solutionSet = new HashSet<string>(allSolutions, StringComparer.OrdinalIgnoreCase);

            foreach (var problemResource in allProblems)
            {
                try
                {
                    var problem = _loader.LoadFromResource(problemResource);
                    var category = DetermineCategory(problemResource);
                    var displayName = GenerateDisplayName(problemResource, category);

                    // Ensure problem has metadata and set the category
                    if (problem.Metadata == null)
                    {
                        problem.Metadata = new MinCostFlow.Problems.Models.ProblemMetadata();
                    }
                    problem.Metadata.Category = category;
                    
                    var problemWithSolution = new ProblemWithSolution
                    {
                        Problem = problem,
                        ResourceName = problemResource,
                        DisplayName = displayName,
                        Category = category
                    };

                    // Try to load the solution
                    var solutionResource = problemResource.Replace(".min", ".sol");
                    if (solutionSet.Contains(solutionResource))
                    {
                        try
                        {
                            problemWithSolution.Solution = _loader.LoadSolutionFromResource(solutionResource);
                            
                            // Update problem metadata with optimal cost
                            if (problem.Metadata != null && problemWithSolution.Solution != null)
                            {
                                problem.Metadata.OptimalCost = problemWithSolution.Solution.OptimalCost;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to load solution for {problemResource}: {ex.Message}");
                        }
                    }

                    if (!_problemsByCategory.ContainsKey(category))
                    {
                        _problemsByCategory[category] = [];
                    }
                    _problemsByCategory[category].Add(problemWithSolution);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load problem {problemResource}: {ex.Message}");
                }
            }

            // Sort problems within each category
            foreach (var list in _problemsByCategory.Values)
            {
                list.Sort((a, b) => 
                {
                    // Sort by node count first, then arc count, then name
                    var nodeCompare = a.Problem.NodeCount.CompareTo(b.Problem.NodeCount);
                    if (nodeCompare != 0)
                    {
                        return nodeCompare;
                    }

                    var arcCompare = a.Problem.ArcCount.CompareTo(b.Problem.ArcCount);
                    if (arcCompare != 0)
                    {
                        return arcCompare;
                    }

                    return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                });
            }

            return _problemsByCategory;
        }
    }

    /// <summary>
    /// Gets all problems in a specific category.
    /// </summary>
    public IEnumerable<ProblemWithSolution> GetByCategory(string category)
    {
        var problems = GetProblemsByCategory();
        return problems.TryGetValue(category, out var list) ? list : Enumerable.Empty<ProblemWithSolution>();
    }

    /// <summary>
    /// Gets all problems.
    /// </summary>
    public IEnumerable<ProblemWithSolution> GetAll()
    {
        var problems = GetProblemsByCategory();
        
        // Return in category order
        foreach (var category in Categories)
        {
            foreach (var problem in problems[category])
            {
                yield return problem;
            }
        }
    }

    /// <summary>
    /// Gets problems filtered by a predicate.
    /// </summary>
    public IEnumerable<ProblemWithSolution> GetFiltered(Func<ProblemWithSolution, bool> predicate)
    {
        return GetAll().Where(predicate);
    }

    /// <summary>
    /// Validates a solution against the expected solution.
    /// </summary>
    public static bool ValidateSolution(long computedCost, SolutionLoader.Solution expectedSolution)
    {
        return computedCost == expectedSolution.OptimalCost;
    }

    /// <summary>
    /// Gets problem count by category.
    /// </summary>
    public Dictionary<string, int> GetCountByCategory()
    {
        var problems = GetProblemsByCategory();
        return problems.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }

    /// <summary>
    /// Gets solution coverage statistics.
    /// </summary>
    public Dictionary<string, (int total, int withSolution)> GetSolutionCoverage()
    {
        var problems = GetProblemsByCategory();
        return problems.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.Count, kvp.Value.Count(p => p.HasSolution))
        );
    }

    private Dictionary<string, List<ProblemWithSolution>> GetProblemsByCategory()
    {
        return DiscoverProblems();
    }

    private static string DetermineCategory(string resourceName)
    {
        // Extract the folder name from the resource path
        // Expected format: MinCostFlow.Problems.Resources.{folder}.{filename}.min
        var parts = resourceName.Split('.');
        
        // Find the index of "Resources" in the path
        var resourcesIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Equals("Resources", StringComparison.OrdinalIgnoreCase))
            {
                resourcesIndex = i;
                break;
            }
        }
        
        // If we found "Resources" and there's a folder after it
        if (resourcesIndex >= 0 && resourcesIndex + 1 < parts.Length - 2)
        {
            var folder = parts[resourcesIndex + 1];
            
            // Capitalize the folder name for display
            if (!string.IsNullOrEmpty(folder))
            {
                return char.ToUpper(folder[0]) + folder[1..].ToLower();
            }
        }
        
        // Fallback to "Other" if we can't determine the category
        return "Other";
    }

    private static string GenerateDisplayName(string resourceName, string category)
    {
        // Extract the filename without extension
        var parts = resourceName.Split('.');
        if (parts.Length >= 2)
        {
            var filename = parts[^2];
            
            // Clean up the name
            filename = filename.Replace('_', ' ');
            
            // Capitalize first letter of each word
            var words = filename.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
                }
            }
            
            return $"{category}_{string.Join("", words).Replace(" ", "")}";
        }
        
        return resourceName;
    }

}