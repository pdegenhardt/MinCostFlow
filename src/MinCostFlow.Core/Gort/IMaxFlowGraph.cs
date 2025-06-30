using System.Collections.Generic;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Extended graph interface for max flow algorithms.
/// Adds support for reverse arc queries required by push-relabel algorithms.
/// </summary>
public interface IMaxFlowGraph : IGraphBase
{
    /// <summary>
    /// Indicates whether this graph uses negative indices for reverse arcs.
    /// If true: reverse arcs have indices in range [-NumArcs, -1]
    /// If false: all arcs have indices in range [0, NumArcs)
    /// </summary>
    bool HasNegativeReverseArcs { get; }

    /// <summary>
    /// Returns the opposite/reverse arc for the given arc.
    /// For graphs with negative reverse arcs: Opposite(arc) = -arc
    /// For other graphs: implementation-specific mapping
    /// </summary>
    int OppositeArc(int arc);

    /// <summary>
    /// Iterate over all arcs incident to the node (both outgoing and opposite incoming).
    /// For outgoing arcs: returns the arc index directly
    /// For incoming arcs: returns the opposite (reverse) arc index
    /// </summary>
    IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node);

    /// <summary>
    /// Resume iteration over incident arcs starting from a specific arc.
    /// Useful for continuing iteration after modifications.
    /// </summary>
    /// <param name="node">The node to iterate from</param>
    /// <param name="arc">The arc to start from (must be incident to node)</param>
    IEnumerable<int> OutgoingOrOppositeIncomingArcsStartingFrom(int node, int arc);
}

/// <summary>
/// Extension methods for max flow graphs.
/// </summary>
public static class MaxFlowGraphExtensions
{
    /// <summary>
    /// Alias for OppositeArc to match alternative naming conventions.
    /// </summary>
    public static int Opposite(this IMaxFlowGraph graph, int arc) => graph.OppositeArc(arc);
}