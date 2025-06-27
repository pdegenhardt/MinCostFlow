using MinCostFlow.Experiment.Graphs;
using System.Diagnostics;

namespace MinCostFlow.Experiment;

// Basic graph implementation without reverse arcs
public class ListGraph : BaseGraph
{
    private List<int> _start = [];
    private List<int> _next = [];
    private readonly List<int> _head = [];
    private List<int> tail = [];

    public ListGraph() : base(false) { }

    public ListGraph(int numNodes, int arcCapacity) : base(false)
    {
        Reserve(numNodes, arcCapacity);
        FreezeCapacities();
        if (numNodes > 0)
        {
            AddNode(numNodes - 1);
        }
    }

    public void AddNode(int node)
    {
        if (node < NumNodesInternal) return;
        Debug.Assert(!ConstCapacitiesInternal || node < NodeCapacityInternal);
        NumNodesInternal = node + 1;

        while (_start.Count < NumNodesInternal)
        {
            _start.Add(NilArc);
        }
    }

    public int AddArc(int tailNode, int headNode)
    {
        Debug.Assert(tailNode >= 0);
        Debug.Assert(headNode >= 0);
        AddNode(tailNode > headNode ? tailNode : headNode);

        _head.Add(headNode);
        tail.Add(tailNode);
        _next.Add(_start[tailNode]);
        _start[tailNode] = NumArcsInternal;

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

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return tail[arc];
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        return _head[arc];
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

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node));
        var arc = _start[node];
        while (arc != NilArc)
        {
            yield return arc;
            arc = _next[arc];
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int from)
    {
        Debug.Assert(IsNodeValid(node));
        if (from == NilArc) yield break;
        Debug.Assert(Tail(from) == node);

        var arc = from;
        while (arc != NilArc)
        {
            yield return arc;
            arc = _next[arc];
        }
    }

    // Indexer for convenient access to heads of outgoing arcs
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

        if (_start.Capacity < bound) _start.Capacity = bound;
    }

    public override void ReserveArcs(int bound)
    {
        base.ReserveArcs(bound);
        if (bound <= NumArcsInternal) return;

        if (_head.Capacity < bound) _head.Capacity = bound;
        if (tail.Capacity < bound) tail.Capacity = bound;
        if (_next.Capacity < bound) _next.Capacity = bound;
    }
}
