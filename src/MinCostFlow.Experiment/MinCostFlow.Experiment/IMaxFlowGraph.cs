namespace MinCostFlow.Experiment;

/// <summary>
/// Interface for graphs that support the max flow algorithm.
/// </summary>
public interface IMaxFlowGraph
{
    int NumNodes { get; }
    int NumArcs { get; }
    int NodeCapacity { get; }
    int ArcCapacity { get; }

    int Head(int arc);
    int Tail(int arc);
    int OppositeArc(int arc);
    bool IsNodeValid(int node);
    bool IsArcValid(int arc);

    IEnumerable<int> OutgoingArcs(int node);
    IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node);
    IEnumerable<int> OutgoingOrOppositeIncomingArcsStartingFrom(int node, int startArc);

    // Indicates if the graph uses negative indices for reverse arcs
    bool HasNegativeReverseArcs { get; }
    int NilArc { get; }
}
