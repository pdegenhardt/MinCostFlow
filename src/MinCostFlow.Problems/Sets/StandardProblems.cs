using System.Collections.Generic;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Sets;

/// <summary>
/// Provides dynamic access to all embedded test problems and their metadata, grouped by category.
/// </summary>
public static class StandardProblems
{
    private static readonly EmbeddedResourceLoader _loader = new();
    private static readonly DynamicProblems _dynamicProblems = new(_loader);

    /// <summary>
    /// Gets all available embedded problems grouped by category.
    /// </summary>
    public static Dictionary<string, List<MinCostFlowProblem>> GetAllByCategory()
    {
        return _dynamicProblems.GetAllByCategory();
    }

    /// <summary>
    /// Gets all available embedded problems with their metadata, grouped by category.
    /// </summary>
    public static Dictionary<string, List<(MinCostFlowProblem Problem, ProblemMetadata? Metadata)>> GetAllWithMetadataByCategory()
    {
        var result = new Dictionary<string, List<(MinCostFlowProblem, ProblemMetadata?)>>();
        foreach (var kvp in _dynamicProblems.GetAllByCategory())
        {
            var list = new List<(MinCostFlowProblem, ProblemMetadata?)>();
            foreach (var problem in kvp.Value)
            {
                list.Add((problem, problem.Metadata));
            }
            result[kvp.Key] = list;
        }
        return result;
    }

    /// <summary>
    /// Gets all available categories.
    /// </summary>
    public static IEnumerable<string> GetCategories() => _dynamicProblems.GetAllByCategory().Keys;

    /// <summary>
    /// Gets all problems in a specific category.
    /// </summary>
    public static IEnumerable<MinCostFlowProblem> GetByCategory(string category) => _dynamicProblems.GetByCategory(category);

    /// <summary>
    /// Gets a specific problem by resource name.
    /// </summary>
    public static MinCostFlowProblem? GetProblem(string resourceName) => _dynamicProblems.GetProblem(resourceName);

    /// <summary>
    /// Clears the internal cache.
    /// </summary>
    public static void ClearCache() => _dynamicProblems.ClearCache();
}