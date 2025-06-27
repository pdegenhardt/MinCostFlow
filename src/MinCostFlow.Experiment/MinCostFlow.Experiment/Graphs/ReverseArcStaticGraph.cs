using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// StaticGraph with reverse arcs
public class ReverseArcStaticGraph : BaseGraph
{
    private bool isBuilt;
    private List<int> start = [];
    private List<int> reverseStart = [];
    private SVector<int, int> head;
    private SVector<int, int> opposite;

    public ReverseArcStaticGraph() : base(true)
    {
        isBuilt = false;
        head = new SVector<int, int>();
        opposite = new SVector<int, int>();
    }

    public ReverseArcStaticGraph(int numNodes, int arcCapacity) : this()
    {
        Reserve(numNodes, arcCapacity);
        FreezeCapacities();
        if (numNodes > 0)
        {
            AddNode(numNodes - 1);
        }
    }

    public int OppositeArc(int arc)
    {
        Debug.Assert(isBuilt);
        Debug.Assert(IsArcValid(arc));
        return opposite[arc];
    }

    public int Head(int arc)
    {
        Debug.Assert(isBuilt);
        Debug.Assert(IsArcValid(arc));
        return head[arc];
    }

    public int Tail(int arc)
    {
        Debug.Assert(isBuilt);
        return head[OppositeArc(arc)];
    }

    public int OutDegree(int node)
    {
        return DirectArcLimit(node) - start[node];
    }

    public int InDegree(int node)
    {
        return ReverseArcLimit(node) - reverseStart[node];
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        int startArc = start[node];
        int limit = DirectArcLimit(node);
        for (int arc = startArc; arc < limit; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(from >= start[node]);
        int limit = DirectArcLimit(node);
        int startArc = from == BaseGraph.NilArc ? limit : from;
        for (int arc = startArc; arc < limit; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OppositeIncomingArcs(int node)
    {
        int startArc = reverseStart[node];
        int limit = ReverseArcLimit(node);
        for (int arc = startArc; arc < limit; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OppositeIncomingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(from >= reverseStart[node]);
        int limit = ReverseArcLimit(node);
        int startArc = from == BaseGraph.NilArc ? limit : from;
        for (int arc = startArc; arc < limit; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> IncomingArcs(int node)
    {
        foreach (var arc in OppositeIncomingArcs(node))
        {
            yield return OppositeArc(arc);
        }
    }

    public IEnumerable<int> IncomingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        int oppositeFrom = from == BaseGraph.NilArc ? BaseGraph.NilArc : OppositeArc(from);
        foreach (var arc in OppositeIncomingArcsStartingFrom(node, oppositeFrom))
        {
            yield return OppositeArc(arc);
        }
    }

    public IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node)
    {
        foreach (var arc in OppositeIncomingArcs(node))
        {
            yield return arc;
        }
        foreach (var arc in OutgoingArcs(node))
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OutgoingOrOppositeIncomingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        if (from == BaseGraph.NilArc) yield break;

        // If starting from a negative arc
        if (from < 0)
        {
            foreach (var arc in OppositeIncomingArcsStartingFrom(node, from))
            {
                yield return arc;
            }
            foreach (var arc in OutgoingArcs(node))
            {
                yield return arc;
            }
        }
        else
        {
            foreach (var arc in OutgoingArcsStartingFrom(node, from))
            {
                yield return arc;
            }
        }
    }

    public IEnumerable<int> this[int node]
    {
        get
        {
            foreach (var arc in OutgoingArcs(node))
            {
                yield return Head(arc);
            }
        }
    }

    public override void ReserveArcs(int bound)
    {
        base.ReserveArcs(bound);
        if (bound <= NumArcsInternal) return;
        head.Reserve(bound);
    }

    public void AddNode(int node)
    {
        if (node < NumNodesInternal) return;
        Debug.Assert(!ConstCapacitiesInternal || node < NodeCapacityInternal);
        NumNodesInternal = node + 1;
    }

    public int AddArc(int tailNode, int headNode)
    {
        Debug.Assert(tailNode >= 0);
        Debug.Assert(headNode >= 0);
        AddNode(tailNode > headNode ? tailNode : headNode);

        // We inverse head and tail here for build efficiency
        head.Grow(headNode, tailNode);
        Debug.Assert(!ConstCapacitiesInternal || NumArcsInternal < ArcCapacityInternal);
        var result = NumArcsInternal;
        NumArcsInternal++;
        return result;
    }

    public void Build() => Build(null);

    public void Build(List<int> permutation)
    {
        if (isBuilt) return;
        isBuilt = true;
        NodeCapacityInternal = NumNodesInternal;
        ArcCapacityInternal = NumArcsInternal;
        FreezeCapacities();

        if (NumNodesInternal == 0) return;

        BuildStartAndForwardHead(permutation);

        // Compute incoming degree of each node
        for (int i = 0; i <= NumNodesInternal; i++)
        {
            reverseStart.Add(0);
        }

        for (int i = 0; i < NumArcsInternal; i++)
        {
            int headIndex = head[i];
            reverseStart[headIndex] = reverseStart[headIndex] + 1;
        }

        ComputeCumulativeSum(reverseStart);

        // Compute the reverse arcs
        opposite.Reserve(NumArcsInternal);
        for (int i = 0; i < NumArcsInternal; i++)
        {
            int arc = i;
            int headIndex = head[arc];
            int reverseArc = reverseStart[headIndex] - NumArcsInternal;
            opposite.Grow(0, reverseArc);
            reverseStart[headIndex] = reverseStart[headIndex] + 1;
        }

        // Restore reverse_start
        reverseStart[NumNodesInternal] = 0; // Sentinel
        for (int i = NumNodesInternal - 1; i > 0; i--)
        {
            reverseStart[i] = reverseStart[i - 1] - NumArcsInternal;
        }
        if (NumNodesInternal != 0)
        {
            reverseStart[0] = -NumArcsInternal;
        }

        // Fill reverse arc information
        for (int i = 0; i < NumArcsInternal; i++)
        {
            int arc = i;
            opposite[opposite[arc]] = arc;
        }

        foreach (var node in AllNodes())
        {
            foreach (var arc in OutgoingArcs(node))
            {
                head[opposite[arc]] = node;
            }
        }
    }

    private void BuildStartAndForwardHead(List<int> permutation)
    {
        // Initialize start array
        for (int i = 0; i <= NumNodesInternal; i++)
        {
            start.Add(0);
        }

        // Count outgoing arcs for each node
        for (int i = 0; i < NumArcsInternal; i++)
        {
            int arc = i;
            int tailNode = head[arc]; // Remember we swapped head/tail
            start[tailNode] = start[tailNode] + 1;
        }

        ComputeCumulativeSum(start);

        // Build permutation and rearrange head array
        if (permutation != null || true) // We need to rearrange
        {
            var perm = new List<int>(NumArcsInternal);
            for (int i = 0; i < NumArcsInternal; i++)
            {
                perm.Add(0);
            }

            var tempStart = new List<int>(start);
            for (int i = 0; i < NumArcsInternal; i++)
            {
                int arc = i;
                int tailNode = head[arc];
                perm[i] = tempStart[tailNode];
                tempStart[tailNode] = tempStart[tailNode] + 1;
            }

            // Create new head array with proper ordering
            head.Resize(NumArcsInternal);

            for (int i = 0; i < NumArcsInternal; i++)
            {
                int arc = i;
                int negArc = -(arc + 1);
                head[perm[i]] = head[negArc];
            }

            if (permutation != null)
            {
                permutation.Clear();
                permutation.AddRange(perm);
            }
        }

        // Restore start array
        for (int i = NumNodesInternal - 1; i > 0; i--)
        {
            start[i] = start[i - 1];
        }
        start[0] = 0;
    }

    private int DirectArcLimit(int node)
    {
        Debug.Assert(isBuilt);
        Debug.Assert(IsNodeValid(node));
        return start[node + 1];
    }

    private int ReverseArcLimit(int node)
    {
        Debug.Assert(isBuilt);
        Debug.Assert(IsNodeValid(node));
        return reverseStart[node + 1];
    }
}
