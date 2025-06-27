using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// Complete graph implementation - all nodes connected to all nodes
public class CompleteGraph : BaseGraph
{
    private readonly int divisor;

    public CompleteGraph(int numNodes) : base(false)
    {
        divisor = numNodes > 1 ? numNodes : 2;
        Reserve(numNodes, numNodes * numNodes);
        FreezeCapacities();
        this.NumNodesInternal = numNodes;
        NumArcsInternal = numNodes * numNodes;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return arc % divisor;
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return arc / divisor;
    }

    public int OutDegree(int node)
    {
        return NumNodesInternal;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(node < NumNodesInternal);
        int startArc = node * NumNodesInternal;
        int endArc = (node + 1) * NumNodesInternal;
        for (int arc = startArc; arc < endArc; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(node < NumNodesInternal);
        int endArc = (node + 1) * NumNodesInternal;
        for (int arc = from; arc < endArc; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> this[int node]
    {
        get
        {
            Debug.Assert(node < NumNodesInternal);
            for (int i = 0; i < NumNodesInternal; i++)
            {
                yield return i;
            }
        }
    }
}
