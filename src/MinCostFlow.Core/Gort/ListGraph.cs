using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Simple linked-list based graph implementation with fast construction.
/// Memory usage: 1 × NodeCapacity + 2 × ArcCapacity (or 3 × with tail array).
/// </summary>
public class ListGraph : IGraphBase
{
    private int[] _firstOut;  // First outgoing arc for each node
    private int[] _next;      // Next arc in linked list
    private int[] _head;      // Head node of each arc
    private int[]? _tail;     // Optional: Tail node of each arc
    
    private int _numNodes;
    private int _numArcs;
    private int _nodeCapacity;
    private int _arcCapacity;
    private bool _capacitiesFrozen;
    private readonly bool _storeTails;

    /// <summary>
    /// Creates a new ListGraph.
    /// </summary>
    /// <param name="storeTails">If true, stores tail nodes for O(1) Tail() access.</param>
    public ListGraph(bool storeTails = false)
    {
        _storeTails = storeTails;
        _firstOut = Array.Empty<int>();
        _next = Array.Empty<int>();
        _head = Array.Empty<int>();
        if (_storeTails)
        {
            _tail = Array.Empty<int>();
        }
        _numNodes = 0;
        _numArcs = 0;
        _nodeCapacity = 0;
        _arcCapacity = 0;
        _capacitiesFrozen = false;
    }

    public int NumNodes => _numNodes;
    public int NumArcs => _numArcs;
    public int NodeCapacity => _nodeCapacity;
    public int ArcCapacity => _arcCapacity;

    public bool IsNodeValid(int node) => node >= 0 && node < _numNodes;

    public bool IsArcValid(int arc) => arc >= 0 && arc < _numArcs;

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < _numNodes; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllForwardArcs()
    {
        for (int i = 0; i < _numArcs; i++)
        {
            yield return i;
        }
    }

    public void ReserveNodes(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        if (bound <= _nodeCapacity) return;

        int newCapacity = CalculateNewCapacity(_nodeCapacity, bound);
        Array.Resize(ref _firstOut, newCapacity);
        
        // Initialize new nodes with NilArc
        for (int i = _nodeCapacity; i < newCapacity; i++)
        {
            _firstOut[i] = IGraphBase.NilArc;
        }
        
        _nodeCapacity = newCapacity;
    }

    public void ReserveArcs(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        if (bound <= _arcCapacity) return;

        int newCapacity = CalculateNewCapacity(_arcCapacity, bound);
        Array.Resize(ref _next, newCapacity);
        Array.Resize(ref _head, newCapacity);
        if (_storeTails)
        {
            Array.Resize(ref _tail!, newCapacity);
        }
        
        _arcCapacity = newCapacity;
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
        Debug.Assert(node >= 0, "Node index must be non-negative");
        
        if (node >= _numNodes)
        {
            if (node >= _nodeCapacity)
            {
                ReserveNodes(node + 1);
            }
            
            // Initialize new nodes
            for (int i = _numNodes; i <= node; i++)
            {
                _firstOut[i] = IGraphBase.NilArc;
            }
            
            _numNodes = node + 1;
        }
    }

    public int AddArc(int tail, int head)
    {
        Debug.Assert(IsNodeValid(tail), $"Invalid tail node: {tail}");
        Debug.Assert(IsNodeValid(head), $"Invalid head node: {head}");
        
        if (_numArcs >= _arcCapacity)
        {
            ReserveArcs(_numArcs + 1);
        }
        
        int arc = _numArcs;
        _head[arc] = head;
        if (_storeTails)
        {
            _tail![arc] = tail;
        }
        
        // Insert at beginning of adjacency list
        _next[arc] = _firstOut[tail];
        _firstOut[tail] = arc;
        
        _numArcs++;
        return arc;
    }

    public int[]? Build()
    {
        // ListGraph doesn't require build
        return null;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        return _head[arc];
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        
        if (_storeTails)
        {
            return _tail![arc];
        }
        
        // O(n+m) search if tails not stored
        for (int node = 0; node < _numNodes; node++)
        {
            for (int a = _firstOut[node]; a != IGraphBase.NilArc; a = _next[a])
            {
                if (a == arc)
                {
                    return node;
                }
            }
        }
        
        throw new InvalidOperationException($"Arc {arc} not found in graph");
    }

    public int OutDegree(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        int count = 0;
        for (int arc = _firstOut[node]; arc != IGraphBase.NilArc; arc = _next[arc])
        {
            count++;
        }
        return count;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        for (int arc = _firstOut[node]; arc != IGraphBase.NilArc; arc = _next[arc])
        {
            yield return arc;
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int startArc)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        Debug.Assert(IsArcValid(startArc), $"Invalid arc: {startArc}");
        
        // Find the start arc in the list
        bool found = false;
        for (int arc = _firstOut[node]; arc != IGraphBase.NilArc; arc = _next[arc])
        {
            if (arc == startArc)
            {
                found = true;
            }
            if (found)
            {
                yield return arc;
            }
        }
    }

    private static int CalculateNewCapacity(int currentCapacity, int required)
    {
        if (currentCapacity == 0)
        {
            return Math.Max(4, required);
        }
        
        long newCapacity = Math.Max((long)(currentCapacity * 1.5), required);
        if (newCapacity > int.MaxValue)
        {
            throw new OutOfMemoryException("Graph capacity would exceed maximum size");
        }
        
        return (int)newCapacity;
    }
}