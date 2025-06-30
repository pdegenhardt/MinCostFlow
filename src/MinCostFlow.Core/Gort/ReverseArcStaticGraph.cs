using System;
using System.Collections.Generic;
using System.Linq;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Memory-efficient static graph implementation with reverse arc support.
/// Requires Build() to be called before use.
/// </summary>
public class ReverseArcStaticGraph : IMaxFlowGraph
{
    // Node data
    private int _numNodes;
    private int _nodeCapacity;
    
    // Arc data during construction
    private readonly List<(int tail, int head)> _constructionArcs = new();
    private bool _built;
    private bool _capacitiesFrozen;
    
    // Static arc data (after Build)
    private int[]? _outStartPos;     // Outgoing arc start positions (size: nodeCapacity + 1)
    private int[]? _inStartPos;      // Incoming arc start positions (size: nodeCapacity + 1)
    private int[]? _head;            // Head node for each arc (size: arcCapacity)
    private int[]? _tail;            // Tail node for each arc (size: arcCapacity)
    private int[]? _oppositeArc;     // Opposite arc index for each arc (size: arcCapacity)
    private int _arcCapacity;
    private int _numArcs;            // Actual number of forward arcs (after Build)

    public int NumNodes => _numNodes;
    public int NumArcs => _built ? _numArcs : _constructionArcs.Count;
    public int NodeCapacity => _nodeCapacity;
    public int ArcCapacity => _arcCapacity;

    public bool IsNodeValid(int node) => node >= 0 && node < _numNodes;
    
    public bool IsArcValid(int arc)
    {
        if (!_built)
            return arc > 0 && arc <= _constructionArcs.Count; // 1-based during construction
        
        // For reverse arc support with 1-based indexing: arc ∈ [-NumArcs, -1] ∪ [1, NumArcs]
        return arc != 0 && arc >= -_numArcs && arc <= _numArcs;
    }

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < _numNodes; i++)
            yield return i;
    }

    public IEnumerable<int> AllForwardArcs()
    {
        // Return 1-based arc indices
        for (int i = 1; i <= NumArcs; i++)
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
                // Resize node arrays
                Array.Resize(ref _outStartPos, _nodeCapacity + 1);
                Array.Resize(ref _inStartPos, _nodeCapacity + 1);
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
                Array.Resize(ref _oppositeArc, _arcCapacity);
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
        
        // Return 1-based index (arc indices start at 1, not 0)
        return arcIndex + 1;
    }

    public int[]? Build()
    {
        if (_built)
            return null;
        
        _built = true;
        _numArcs = _constructionArcs.Count;
        
        // Allocate arrays
        _outStartPos = new int[_nodeCapacity + 1];
        _inStartPos = new int[_nodeCapacity + 1];
        _head = new int[_arcCapacity];
        _tail = new int[_arcCapacity];
        _oppositeArc = new int[_arcCapacity];
        
        if (_numArcs == 0)
            return null;
        
        // Count outgoing and incoming arcs per node
        foreach (var (tail, head) in _constructionArcs)
        {
            _outStartPos[tail + 1]++;
            _inStartPos[head + 1]++;
        }
        
        // Convert to cumulative counts
        for (int i = 1; i <= _nodeCapacity; i++)
        {
            _outStartPos[i] += _outStartPos[i - 1];
            _inStartPos[i] += _inStartPos[i - 1];
        }
        
        // Create permutation array to track arc reordering
        int[] permutation = new int[_numArcs];
        int[] currentOutPos = new int[_nodeCapacity];
        int[] currentInPos = new int[_nodeCapacity];
        Array.Copy(_outStartPos, currentOutPos, _nodeCapacity);
        Array.Copy(_inStartPos, currentInPos, _nodeCapacity);
        
        // First pass: place forward arcs in their sorted positions
        for (int oldArc = 0; oldArc < _numArcs; oldArc++)
        {
            var (tail, head) = _constructionArcs[oldArc];
            int newArc = currentOutPos[tail]++;
            _head[newArc] = head;
            _tail[newArc] = tail;
            // Return 1-based indices in permutation
            permutation[oldArc] = newArc + 1;
        }
        
        // Second pass: set up opposite arc indices
        // We need to find where each reverse arc would be placed
        var reverseArcMap = new Dictionary<(int, int), int>();
        
        for (int arc = 0; arc < _numArcs; arc++)
        {
            reverseArcMap[(_tail[arc], _head[arc])] = arc;
        }
        
        // Reset current positions for incoming arcs
        Array.Copy(_inStartPos, currentInPos, _nodeCapacity);
        
        // For each arc, find its opposite in the incoming arc list
        for (int arc = 0; arc < _numArcs; arc++)
        {
            int tail = _tail[arc];
            int head = _head[arc];
            
            // The opposite arc goes from head to tail
            if (reverseArcMap.TryGetValue((head, tail), out int oppositeForward))
            {
                // If the opposite direction exists as a forward arc, use its negative
                _oppositeArc[arc] = -oppositeForward;
            }
            else
            {
                // Otherwise, create a virtual reverse arc index
                // This would be placed in the incoming arc list of 'tail'
                int reversePos = currentInPos[tail]++;
                _oppositeArc[arc] = -(_numArcs + reversePos);
            }
        }
        
        // Clear construction data
        _constructionArcs.Clear();
        
        return permutation;
    }

    public int Head(int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        
        if (arc > 0)
        {
            if (arc > _numArcs)
                throw new ArgumentOutOfRangeException(nameof(arc));
            return _head![arc - 1]; // Convert 1-based to 0-based for array access
        }
        else if (arc < 0)
        {
            arc = -arc;
            if (arc > _numArcs)
                throw new ArgumentOutOfRangeException(nameof(arc));
            return _tail![arc - 1]; // Reverse arc: head becomes tail
        }
        else
        {
            throw new ArgumentException("Arc index cannot be zero");
        }
    }

    public int Tail(int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        
        if (arc > 0)
        {
            if (arc > _numArcs)
                throw new ArgumentOutOfRangeException(nameof(arc));
            return _tail![arc - 1]; // Convert 1-based to 0-based for array access
        }
        else if (arc < 0)
        {
            arc = -arc;
            if (arc > _numArcs)
                throw new ArgumentOutOfRangeException(nameof(arc));
            return _head![arc - 1]; // Reverse arc: tail becomes head
        }
        else
        {
            throw new ArgumentException("Arc index cannot be zero");
        }
    }

    public int OutDegree(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        return _outStartPos![node + 1] - _outStartPos[node];
    }

    public int InDegree(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        return _inStartPos![node + 1] - _inStartPos[node];
    }

    public static int OppositeArc(int arc)
    {
        return -arc;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        int start = _outStartPos![node];
        int end = _outStartPos[node + 1];
        
        for (int arc = start; arc < end; arc++)
        {
            yield return arc + 1; // Return 1-based indices
        }
    }

    public IEnumerable<int> IncomingArcs(int node)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        
        // We need to find all arcs where head == node
        // These are sorted by tail in the main arc list
        for (int arc = 0; arc < _numArcs; arc++)
        {
            if (_head![arc] == node)
            {
                yield return arc + 1; // Return 1-based indices
            }
        }
    }

    public IEnumerable<int> OppositeIncomingArcs(int node)
    {
        foreach (var arc in IncomingArcs(node))
        {
            yield return -arc;
        }
    }

    public IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node)
    {
        foreach (var arc in OutgoingArcs(node))
        {
            yield return arc;
        }
        foreach (var arc in OppositeIncomingArcs(node))
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
        
        // Only works for forward arcs
        if (arc <= 0)
            throw new ArgumentException("Starting arc must be a forward arc");
        
        // Convert to 0-based for internal use
        int internalArc = arc - 1;
        
        // Verify the arc belongs to this node
        if (_tail![internalArc] != node)
            throw new ArgumentException("Arc does not belong to the specified node");
        
        int end = _outStartPos![node + 1];
        
        for (int i = internalArc; i < end; i++)
        {
            yield return i + 1; // Return 1-based indices
        }
    }

    #region IMaxFlowGraph Implementation

    public bool HasNegativeReverseArcs => true;

    int IMaxFlowGraph.OppositeArc(int arc) => OppositeArc(arc);

    public IEnumerable<int> OutgoingOrOppositeIncomingArcsStartingFrom(int node, int arc)
    {
        if (!_built)
            throw new InvalidOperationException("Must call Build() first");
        if (!IsNodeValid(node))
            throw new ArgumentOutOfRangeException(nameof(node));
        if (!IsArcValid(arc))
            throw new ArgumentOutOfRangeException(nameof(arc));
        
        bool found = false;
        
        // Check outgoing arcs first
        if (arc > 0)
        {
            // Convert to 0-based for internal use
            int internalArc = arc - 1;
            
            // Verify the arc belongs to this node
            if (_tail![internalArc] == node)
            {
                int end = _outStartPos![node + 1];
                for (int i = internalArc; i < end; i++)
                {
                    yield return i + 1; // Return 1-based indices
                }
                found = true;
            }
        }
        
        // Then check opposite incoming arcs
        foreach (var currentArc in OppositeIncomingArcs(node))
        {
            if (!found && currentArc == arc)
            {
                found = true;
            }
            if (found)
            {
                yield return currentArc;
            }
        }
        
        if (!found)
            throw new ArgumentException("Arc does not belong to the specified node");
    }

    #endregion
}