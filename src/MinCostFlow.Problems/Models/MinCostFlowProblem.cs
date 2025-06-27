using MinCostFlow.Core.Lemon.Graphs;

namespace MinCostFlow.Problems.Models;

/// <summary>
/// Base class for all minimum cost flow problems.
/// </summary>
public abstract class MinCostFlowProblem
{
    /// <summary>
    /// Gets or sets the graph structure.
    /// </summary>
    public IGraph Graph { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of nodes.
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of arcs.
    /// </summary>
    public int ArcCount { get; set; }

    /// <summary>
    /// Gets or sets the supply/demand values for each node.
    /// Positive values indicate supply, negative values indicate demand.
    /// </summary>
    public long[] NodeSupplies { get; set; } = null!;

    /// <summary>
    /// Gets or sets the lower capacity bounds for each arc.
    /// </summary>
    public long[] ArcLowerBounds { get; set; } = null!;

    /// <summary>
    /// Gets or sets the upper capacity bounds for each arc.
    /// </summary>
    public long[] ArcUpperBounds { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cost per unit flow for each arc.
    /// </summary>
    public long[] ArcCosts { get; set; } = null!;

    /// <summary>
    /// Gets or sets optional metadata about the problem.
    /// </summary>
    public ProblemMetadata? Metadata { get; set; }

    /// <summary>
    /// Validates that the problem is well-formed.
    /// </summary>
    /// <returns>True if the problem is valid, false otherwise.</returns>
    public virtual bool Validate()
    {
        if (Graph == null || NodeSupplies == null || ArcLowerBounds == null || 
            ArcUpperBounds == null || ArcCosts == null)
        {
            return false;
        }

        if (NodeCount != NodeSupplies.Length)
        {
            return false;
        }

        if (ArcCount != ArcLowerBounds.Length || ArcCount != ArcUpperBounds.Length || 
            ArcCount != ArcCosts.Length)
        {
            return false;
        }

        // Check that supplies sum to zero
        long totalSupply = 0;
        for (int i = 0; i < NodeCount; i++)
        {
            totalSupply += NodeSupplies[i];
        }

        return totalSupply == 0;
    }
}