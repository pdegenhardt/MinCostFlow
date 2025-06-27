using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// Complete bipartite graph implementation
public class CompleteBipartiteGraph : BaseGraph
{
    private readonly int _leftNodes;
    private readonly int _rightNodes;
    private readonly int _divisor;

    public CompleteBipartiteGraph(int leftNodes, int rightNodes) : base(false)
    {
        this._leftNodes = leftNodes;
        this._rightNodes = rightNodes;
        _divisor = rightNodes > 1 ? rightNodes : 2;
        int totalNodes = leftNodes + rightNodes;
        int totalArcs = leftNodes * rightNodes;
        Reserve(totalNodes, totalArcs);
        FreezeCapacities();
        NumNodesInternal = totalNodes;
        NumArcsInternal = totalArcs;
    }

    public int GetArc(int leftNode, int rightNode)
    {
        Debug.Assert(leftNode < _leftNodes);
        Debug.Assert(rightNode >= _leftNodes);
        Debug.Assert(rightNode < NumNodesInternal);
        int rightOffset = rightNode - _leftNodes;
        return leftNode * _rightNodes + rightOffset;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        int offset = _rightNodes > 1 ? arc % _divisor : 0;
        return _leftNodes + offset;
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc));
        int tail = _rightNodes > 1 ? arc / _divisor : arc;
        return tail;
    }

    public int OutDegree(int node)
    {
        return node < _leftNodes ? _rightNodes : 0;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        if (node < _leftNodes)
        {
            int startArc = node * _rightNodes;
            int endArc = (node + 1) * _rightNodes;
            for (int arc = startArc; arc < endArc; arc++)
            {
                yield return arc;
            }
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int from)
    {
        if (node < _leftNodes)
        {
            int endArc = (node + 1) * _rightNodes;
            for (int arc = from; arc < endArc; arc++)
            {
                yield return arc;
            }
        }
    }

    public IEnumerable<int> this[int node]
    {
        get
        {
            if (node < _leftNodes)
            {
                int rightEnd = _leftNodes + _rightNodes;
                for (int i = _leftNodes; i < rightEnd; i++)
                {
                    yield return i;
                }
            }
        }
    }
}
