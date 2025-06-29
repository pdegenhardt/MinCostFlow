using System;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon.Graphs;

/// <summary>
/// Interface for a directed graph structure, based on LEMON's graph concepts.
/// Provides efficient access to nodes and arcs.
/// </summary>
public interface IGraph
{
    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    int NodeCount { get; }

    /// <summary>
    /// Gets the total number of arcs in the graph.
    /// </summary>
    int ArcCount { get; }

    /// <summary>
    /// Gets the source node of the specified arc.
    /// </summary>
    Node Source(Arc arc);

    /// <summary>
    /// Gets the target node of the specified arc.
    /// </summary>
    Node Target(Arc arc);

    /// <summary>
    /// Gets all outgoing arcs from the specified node.
    /// Returns a span for zero-allocation enumeration.
    /// </summary>
    ReadOnlySpan<Arc> GetOutArcs(Node node);

    /// <summary>
    /// Gets all incoming arcs to the specified node.
    /// Returns a span for zero-allocation enumeration.
    /// </summary>
    ReadOnlySpan<Arc> GetInArcs(Node node);

    /// <summary>
    /// Checks if a node is valid in this graph.
    /// </summary>
    bool IsValidNode(Node node);

    /// <summary>
    /// Checks if an arc is valid in this graph.
    /// </summary>
    bool IsValidArc(Arc arc);

    /// <summary>
    /// Gets the first node for iteration.
    /// </summary>
    Node FirstNode();

    /// <summary>
    /// Gets the next node in iteration order.
    /// </summary>
    Node NextNode(Node node);

    /// <summary>
    /// Gets the first arc for iteration.
    /// </summary>
    Arc FirstArc();

    /// <summary>
    /// Gets the next arc in iteration order.
    /// </summary>
    Arc NextArc(Arc arc);
}

/// <summary>
/// Extension methods for IGraph to provide additional functionality.
/// </summary>
public static class GraphExtensions
{
    /// <summary>
    /// Gets the head (target) node of an arc by index.
    /// Handles negative arc indices for reverse arcs.
    /// </summary>
    public static int Head(this IGraph graph, int arc)
    {
        if (arc >= 0)
        {
            return graph.Target(new Arc(arc)).Id;
        }
        else
        {
            // Negative arc means we want the tail of the opposite arc
            return graph.Source(new Arc(-arc - 1)).Id;
        }
    }

    /// <summary>
    /// Gets the tail (source) node of an arc by index.
    /// Handles negative arc indices for reverse arcs.
    /// </summary>
    public static int Tail(this IGraph graph, int arc)
    {
        if (arc >= 0)
        {
            return graph.Source(new Arc(arc)).Id;
        }
        else
        {
            // Negative arc means we want the head of the opposite arc
            return graph.Target(new Arc(-arc - 1)).Id;
        }
    }

    /// <summary>
    /// Checks if an arc index is valid.
    /// Handles negative arc indices for reverse arcs.
    /// </summary>
    public static bool IsArcValid(this IGraph graph, int arc)
    {
        if (arc >= 0)
        {
            return graph.IsValidArc(new Arc(arc));
        }
        else
        {
            int oppositeArc = -arc - 1;
            return oppositeArc >= 0 && graph.IsValidArc(new Arc(oppositeArc));
        }
    }

}