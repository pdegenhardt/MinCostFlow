using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// Extends the ListGraph by also storing the reverse arcs
public class ReverseArcListGraph : BaseGraph
{
    private List<int> start = [];
    private List<int> reverseStart = [];
    private SVector<int, int> next;
    private SVector<int, int> head;

    public ReverseArcListGraph() : base(true)
    {
        next = new SVector<int, int>();
        head = new SVector<int, int>();
    }

    public ReverseArcListGraph(int numNodes, int arcCapacity) : this()
    {
        Reserve(numNodes, arcCapacity);
        FreezeCapacities();
        if (numNodes > 0)
        {
            AddNode(numNodes - 1);
        }
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return head[arc];
    }

    public int Tail(int arc)
    {
        return head[OppositeArc(arc)];
    }

    public int OppositeArc(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return ~arc;
    }

    public int OutDegree(int node)
    {
        int degree = 0;
        foreach (var arc in OutgoingArcs(node))
        {
            degree++;
        }
        return degree;
    }

    public int InDegree(int node)
    {
        int degree = 0;
        foreach (var arc in OppositeIncomingArcs(node))
        {
            degree++;
        }
        return degree;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node));
        int arc = start[node];
        while (arc != BaseGraph.NilArc)
        {
            yield return arc;
            arc = next[arc];
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        if (from == BaseGraph.NilArc) yield break;
        Debug.Assert(from >= 0);
        Debug.Assert(Tail(from) == node);
        int arc = from;
        while (arc != BaseGraph.NilArc)
        {
            yield return arc;
            arc = next[arc];
        }
    }

    public IEnumerable<int> IncomingArcs(int node)
    {
        foreach (var oppositeArc in OppositeIncomingArcs(node))
        {
            yield return OppositeArc(oppositeArc);
        }
    }

    public IEnumerable<int> IncomingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        if (from == BaseGraph.NilArc) yield break;
        foreach (var oppositeArc in OppositeIncomingArcsStartingFrom(node, OppositeArc(from)))
        {
            yield return OppositeArc(oppositeArc);
        }
    }

    public IEnumerable<int> OppositeIncomingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node));
        int arc = reverseStart[node];
        while (arc != BaseGraph.NilArc)
        {
            yield return arc;
            arc = next[arc];
        }
    }

    public IEnumerable<int> OppositeIncomingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        if (from == BaseGraph.NilArc) yield break;
        Debug.Assert(from < 0);
        Debug.Assert(Tail(from) == node);
        int arc = from;
        while (arc != BaseGraph.NilArc)
        {
            yield return arc;
            arc = next[arc];
        }
    }

    public IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node));
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
        int arc = from;
        bool switchedToOutgoing = false;
        while (arc != BaseGraph.NilArc)
        {
            yield return arc;
            arc = next[arc];
            if (!switchedToOutgoing && arc == BaseGraph.NilArc && from < 0)
            {
                arc = start[node];
                switchedToOutgoing = true;
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

    public override void ReserveNodes(int bound)
    {
        base.ReserveNodes(bound);
        if (bound <= NumNodesInternal) return;
        if (start.Capacity < bound) start.Capacity = bound;
        if (reverseStart.Capacity < bound) reverseStart.Capacity = bound;
    }

    public override void ReserveArcs(int bound)
    {
        base.ReserveArcs(bound);
        if (bound <= NumArcsInternal) return;
        head.Reserve(bound);
        next.Reserve(bound);
    }

    public void AddNode(int node)
    {
        if (node < NumNodesInternal) return;
        Debug.Assert(!ConstCapacitiesInternal || node < NodeCapacityInternal);
        NumNodesInternal = node + 1;
        while (start.Count < NumNodesInternal)
        {
            start.Add(BaseGraph.NilArc);
            reverseStart.Add(BaseGraph.NilArc);
        }
    }

    public int AddArc(int tailNode, int headNode)
    {
        Debug.Assert(tailNode >= 0);
        Debug.Assert(headNode >= 0);
        AddNode(tailNode > headNode ? tailNode : headNode);
        head.Grow(tailNode, headNode);
        next.Grow(reverseStart[headNode], start[tailNode]);
        start[tailNode] = NumArcsInternal;
        reverseStart[headNode] = ~NumArcsInternal;
        Debug.Assert(!ConstCapacitiesInternal || NumArcsInternal < ArcCapacityInternal);
        var result = NumArcsInternal;
        NumArcsInternal++;
        return result;
    }

    public void Build() => Build(null);

    public void Build(List<int> permutation)
    {
        permutation?.Clear();
    }
}
