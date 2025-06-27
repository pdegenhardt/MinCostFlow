using System.Collections.Generic;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Base interface for all graph implementations following the specification.
/// Nodes and arcs are represented by integer indices.
/// </summary>
public interface IGraphBase
{
    /// <summary>
    /// Special constant representing an invalid/non-existent node.
    /// </summary>
    const int NilNode = int.MaxValue;

    /// <summary>
    /// Special constant representing an invalid/non-existent arc.
    /// </summary>
    const int NilArc = int.MaxValue;

    /// <summary>
    /// Number of valid nodes in the graph.
    /// </summary>
    int NumNodes { get; }

    /// <summary>
    /// Alias for NumNodes (for collection compatibility).
    /// </summary>
    int Size => NumNodes;

    /// <summary>
    /// Number of valid forward arcs in the graph.
    /// </summary>
    int NumArcs { get; }

    /// <summary>
    /// Reserved capacity for nodes (always >= NumNodes).
    /// </summary>
    int NodeCapacity { get; }

    /// <summary>
    /// Reserved capacity for arcs (always >= NumArcs).
    /// </summary>
    int ArcCapacity { get; }

    /// <summary>
    /// Returns true if node ∈ [0, NumNodes).
    /// </summary>
    bool IsNodeValid(int node);

    /// <summary>
    /// Returns true if arc is valid.
    /// For forward-only graphs: arc ∈ [0, NumArcs)
    /// For graphs with reverse arcs: arc ∈ [-NumArcs, NumArcs) where arc ≠ 0
    /// </summary>
    bool IsArcValid(int arc);

    /// <summary>
    /// Returns iterable range [0, NumNodes).
    /// </summary>
    IEnumerable<int> AllNodes();

    /// <summary>
    /// Returns iterable range [0, NumArcs).
    /// </summary>
    IEnumerable<int> AllForwardArcs();

    /// <summary>
    /// Pre-allocate space for at least 'bound' nodes.
    /// </summary>
    void ReserveNodes(int bound);

    /// <summary>
    /// Pre-allocate space for at least 'bound' arcs.
    /// </summary>
    void ReserveArcs(int bound);

    /// <summary>
    /// Combined reservation.
    /// </summary>
    void Reserve(int nodeCapacity, int arcCapacity);

    /// <summary>
    /// Prevent future capacity changes (enforced in debug mode).
    /// </summary>
    void FreezeCapacities();

    /// <summary>
    /// Ensures node is valid (extends graph if needed).
    /// </summary>
    void AddNode(int node);

    /// <summary>
    /// Adds arc and returns its index.
    /// </summary>
    int AddArc(int tail, int head);

    /// <summary>
    /// Finalizes graph construction (required for some implementations).
    /// Returns permutation array if arcs were reordered, null otherwise.
    /// </summary>
    int[]? Build();

    /// <summary>
    /// Returns destination node of arc.
    /// </summary>
    int Head(int arc);

    /// <summary>
    /// Returns source node of arc.
    /// </summary>
    int Tail(int arc);

    /// <summary>
    /// Number of outgoing arcs from node.
    /// </summary>
    int OutDegree(int node);

    /// <summary>
    /// Iterate over arcs leaving the node.
    /// </summary>
    IEnumerable<int> OutgoingArcs(int node);

    /// <summary>
    /// Resume iteration from specific arc.
    /// </summary>
    IEnumerable<int> OutgoingArcsStartingFrom(int node, int arc);
}

/// <summary>
/// Extension methods for convenience.
/// </summary>
public static class GraphBaseExtensions
{
    /// <summary>
    /// Iterate over head nodes of outgoing arcs.
    /// </summary>
    public static IEnumerable<int> OutgoingNodes(this IGraphBase graph, int node)
    {
        foreach (var arc in graph.OutgoingArcs(node))
        {
            yield return graph.Head(arc);
        }
    }
}