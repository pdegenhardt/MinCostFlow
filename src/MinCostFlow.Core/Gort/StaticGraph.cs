using System;
using System.Collections.Generic;
using System.Linq;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Memory-efficient static graph implementation with fast iteration.
/// Requires Build() to be called before use.
/// </summary>
public class StaticGraph : IGraphBase
{
    // Node data
    private int _numNodes;
    private int _nodeCapacity;
    
    // Arc data during construction
    private readonly List<(int tail, int head)> _constructionArcs = new();
    private bool _built;
    private bool _capacitiesFrozen;
    
    // Static arc data (after Build)
    private int[]? _startPos; // Cumulative arc counts by node (size: nodeCapacity + 1)
    private int[]? _head;     // Head node for each arc (size: arcCapacity)
    private int[]? _tail;     // Tail node for each arc (size: arcCapacity)
    private int _arcCapacity;
    private int _numArcs;     // Actual number of arcs after Build()

    public int NumNodes => _numNodes;
    public int NumArcs => _built ? _numArcs : _constructionArcs.Count;
    public int NodeCapacity => _nodeCapacity;
    public int ArcCapacity => _arcCapacity;

    public bool IsNodeValid(int node) => node >= 0 && node < _numNodes;
    public bool IsArcValid(int arc) => arc >= 0 && arc < NumArcs;

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < _numNodes; i++)
            yield return i;
    }

    public IEnumerable<int> AllForwardArcs()
    {
        for (int i = 0; i < NumArcs; i++)
            yield return i;
    }

    public void ReserveNodes(int bound)
    {
        if (_capacitiesFrozen)
            throw new InvalidOperationException("Capacities are frozen");
        
        if (bound > _nodeCapacity)
        {
            _nodeCapacity = Math.Max(bound, (int)(_nodeCapacity * 1.5));
            if (_built)
            {
                // Resize start positions array
                Array.Resize(ref _startPos, _nodeCapacity + 1);
            }
        }
    }

    public void ReserveArcs(int bound)
    {
        if (_capacitiesFrozen)
            throw new InvalidOperationException("Capacities are frozen");
        
        if (bound > _arcCapacity)
        {
            _arcCapacity = Math.Max(bound, (int)(_arcCapacity * 1.5));
            if (_built)
            {
                // Resize arc arrays
                Array.Resize(ref _head, _arcCapacity);
                Array.Resize(ref _tail, _arcCapacity);
            }
        }
    }

    public void Reserve(int nodeCapacity, int arcCapacity)
    {
        ReserveNodes(nodeCapacity);
        ReserveArcs(arcCapacity);
    }

    public void FreezeCapacities()
    {
        _capacitiesFrozen = true;
    }

    public void AddNode(int node)
    {
        if (_built)
            throw new InvalidOperationException("Cannot add nodes after Build()");
        
        if (node >= _numNodes)
        {
            int newNumNodes = node + 1;
            if (newNumNodes > _nodeCapacity)
            {
                ReserveNodes(newNumNodes);
            }
            _numNodes = newNumNodes;
        }
    }

    public int AddArc(int tail, int head)
    {
        if (_built)
            throw new InvalidOperationException("Cannot add arcs after Build()");
        
        // Ensure nodes exist
        AddNode(tail);
        AddNode(head);
        
        int arcIndex = _constructionArcs.Count;
        _constructionArcs.Add((tail, head));
        
        // Ensure capacity
        if (_constructionArcs.Count > _arcCapacity)
        {
            ReserveArcs(_constructionArcs.Count);
        }
        
        return arcIndex;
    }

    public int[]? Build()
    {
        if (_built)
            return null;
        
        _built = true;
        _numArcs = _constructionArcs.Count;
        
        // Allocate arrays
        _startPos = new int[_nodeCapacity + 1];
        _head = new int[_arcCapacity];
        _tail = new int[_arcCapacity];
        
        if (_numArcs == 0)
            return null;
        
        // Count outgoing arcs per node
        foreach (var (tail, _) in _constructionArcs)
        {
            _startPos[tail + 1]++;
        }
        
        // Convert to cumulative counts
        for (int i = 1; i <= _nodeCapacity; i++)
        {
            _startPos[i] += _startPos[i - 1];
        }
        
        // Create permutation array to track arc reordering
        int[] permutation = new int[_numArcs];
        int[] currentPos = new int[_nodeCapacity];
        Array.Copy(_startPos, currentPos, _nodeCapacity);
        
        // Place arcs in their sorted positions
        for (int oldArc = 0; oldArc < _numArcs; oldArc++)
        {
            var (tail, head) = _constructionArcs[oldArc];
            int newArc = currentPos[tail]++;
            _head[newArc] = head;
            _tail[newArc] = tail;
            permutation[oldArc] = newArc;
        }
        
        // Clear construction data
        _constructionArcs.Clear();
        
        return permutation;
    }

    public int Head(int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsArcValid(arc))
            throw new ArgumentOutOfRangeException(nameof(arc));
        
        return _head![arc];
    }

    public int Tail(int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsArcValid(arc))
            throw new ArgumentOutOfRangeException(nameof(arc));
        
        return _tail![arc];
    }

    public int OutDegree(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        return _startPos![node + 1] - _startPos[node];
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        int start = _startPos![node];
        int end = _startPos[node + 1];
        
        for (int arc = start; arc < end; arc++)
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        if (!IsArcValid(arc))
            throw new ArgumentOutOfRangeException(nameof(arc));
        
        // Verify the arc belongs to this node
        if (_tail![arc] != node)
            throw new ArgumentException("Arc does not belong to the specified node");
        
        int end = _startPos![node + 1];
        
        for (int i = arc; i < end; i++)
        {
            yield return i;
        }
    }
}