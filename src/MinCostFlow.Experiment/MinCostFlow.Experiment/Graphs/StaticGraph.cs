using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// Most efficient implementation of a graph without reverse arcs
public class StaticGraph : BaseGraph
{
    private bool isBuilt;
    private bool arcInOrder;
    private int lastTailSeen;
    private List<int> start = [];
    private List<int> head = [];
    private List<int> tail = [];

    public StaticGraph() : base(false)
    {
        isBuilt = false;
        arcInOrder = true;
        lastTailSeen = 0;
    }

    public StaticGraph(int numNodes, int arcCapacity) : this()
    {
        Reserve(numNodes, arcCapacity);
        FreezeCapacities();
        if (numNodes > 0)
        {
            AddNode(numNodes - 1);
        }
    }

    public static StaticGraph FromArcs<TArcContainer>(int numNodes, TArcContainer arcs) where TArcContainer : IEnumerable<(int from, int to)>
    {
        var g = new StaticGraph(numNodes, arcs.Count());
        foreach (var (from, to) in arcs)
        {
            g.AddArc(from, to);
        }
        g.Build();
        return g;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return head[arc];
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return tail[arc];
    }

    public int OutDegree(int node)
    {
        return DirectArcLimit(node) - start[node];
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

    public IEnumerable<int> this[int node]
    {
        get
        {
            int startIndex = start[node];
            int endIndex = DirectArcLimit(node);
            for (int i = startIndex; i < endIndex; i++)
            {
                yield return head[i];
            }
        }
    }

    public override void ReserveNodes(int bound)
    {
        base.ReserveNodes(bound);
        if (bound <= NumNodesInternal) return;
        int capacity = bound + 1;
        if (start.Capacity < capacity) start.Capacity = capacity;
    }

    public override void ReserveArcs(int bound)
    {
        base.ReserveArcs(bound);
        if (bound <= NumArcsInternal) return;
        if (head.Capacity < bound) head.Capacity = bound;
        if (tail.Capacity < bound) tail.Capacity = bound;
    }

    public void AddNode(int node)
    {
        if (node < NumNodesInternal) return;
        Debug.Assert(!ConstCapacitiesInternal || node < NodeCapacityInternal);
        NumNodesInternal = node + 1;
        while (start.Count < NumNodesInternal + 1)
        {
            start.Add(0);
        }
    }

    public int AddArc(int tailNode, int headNode)
    {
        Debug.Assert(tailNode >= 0);
        Debug.Assert(headNode >= 0);
        Debug.Assert(!isBuilt);
        AddNode(tailNode > headNode ? tailNode : headNode);
        if (arcInOrder)
        {
            if (tailNode >= lastTailSeen)
            {
                start[tailNode] = start[tailNode] + 1;
                lastTailSeen = tailNode;
            }
            else
            {
                arcInOrder = false;
            }
        }
        tail.Add(tailNode);
        head.Add(headNode);
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
        if (arcInOrder)
        {
            permutation?.Clear();
            ComputeCumulativeSum(start);
            return;
        }
        for (int i = 0; i <= NumNodesInternal; i++)
        {
            start[i] = 0;
        }
        for (int i = 0; i < NumArcsInternal; i++)
        {
            start[tail[i]] = start[tail[i]] + 1;
        }
        ComputeCumulativeSum(start);
        var perm = new List<int>(NumArcsInternal);
        for (int i = 0; i < NumArcsInternal; i++)
        {
            perm.Add(0);
        }
        for (int i = 0; i < NumArcsInternal; i++)
        {
            var tailIndex = tail[i];
            perm[i] = start[tailIndex];
            start[tailIndex] = start[tailIndex] + 1;
        }
        var newHead = new List<int>(NumArcsInternal);
        for (int i = 0; i < NumArcsInternal; i++)
        {
            newHead.Add(0);
        }
        for (int i = 0; i < NumArcsInternal; i++)
        {
            newHead[perm[i]] = head[i];
        }
        head = newHead;
        if (permutation != null)
        {
            permutation.Clear();
            permutation.AddRange(perm);
        }
        for (int i = NumNodesInternal - 1; i > 0; i--)
        {
            start[i] = start[i - 1];
        }
        start[0] = 0;
        foreach (var node in AllNodes())
        {
            foreach (var arc in OutgoingArcs(node))
            {
                tail[arc] = node;
            }
        }
    }

    private int DirectArcLimit(int node)
    {
        Debug.Assert(isBuilt);
        Debug.Assert(IsNodeValid(node));
        return start[node + 1];
    }
}
