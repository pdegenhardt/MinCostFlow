using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Linked-list implementation supporting reverse arc queries.
/// Uses signed arithmetic for reverse arc indices.
/// Memory usage: 2 × NodeCapacity + 4 × ArcCapacity.
/// </summary>
public class ReverseArcListGraph : IMaxFlowGraph
{
    private SVector<int> _firstOut;  // First outgoing arc for each node (using SVector for negative indices)
    private int[] _next;             // Next arc in linked list
    private int[] _head;             // Head node of each arc
    private int[] _tail;             // Tail node of each arc
    
    private int _numNodes;
    private int _numArcs;
    private int _nodeCapacity;
    private int _arcCapacity;
    private bool _capacitiesFrozen;

    public ReverseArcListGraph()
    {
        _firstOut = new SVector<int>();
        _next = Array.Empty<int>();
        _head = Array.Empty<int>();
        _tail = Array.Empty<int>();
        _numNodes = 0;
        _numArcs = 0;
        _nodeCapacity = 0;
        _arcCapacity = 0;
        _capacitiesFrozen = false;
    }
    
    public ReverseArcListGraph(int nodeCapacity, int arcCapacity) : this()
    {
        Reserve(nodeCapacity, arcCapacity);
    }

    public int NumNodes => _numNodes;
    public int NumArcs => _numArcs;
    public int NodeCapacity => _nodeCapacity;
    public int ArcCapacity => _arcCapacity;

    public bool IsNodeValid(int node) => node >= 0 && node < _numNodes;

    public bool IsArcValid(int arc)
    {
        if (arc == 0) return false;  // 0 is not a valid arc in reverse arc graphs
        if (arc > 0) return arc <= _numArcs;
        // For negative arcs: ~arc should be a valid positive arc
        return ~arc > 0 && ~arc <= _numArcs;
    }

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < _numNodes; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllForwardArcs()
    {
        for (int i = 1; i <= _numArcs; i++)
        {
            yield return i;
        }
    }

    public void ReserveNodes(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        if (bound <= _nodeCapacity) return;

        int newCapacity = CalculateNewCapacity(_nodeCapacity, bound);
        _firstOut.Reserve(newCapacity);
        _nodeCapacity = newCapacity;
    }

    public void ReserveArcs(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        if (bound <= _arcCapacity) return;

        int oldCapacity = _arcCapacity;
        int newCapacity = CalculateNewCapacity(_arcCapacity, bound);
        
        // Save the old _next array data for negative arcs
        int[] oldNext = _next;
        
        // Resize arrays - note we need space for both forward and reverse arcs
        // With bitwise NOT, ~n ranges from ~1 = -2 to ~newCapacity = -(newCapacity+1)
        // GetNextIndex maps negative arc to: _arcCapacity - arc
        // For arc = ~newCapacity = -(newCapacity+1), index = newCapacity - (-(newCapacity+1)) = 2*newCapacity + 1
        // So we need array size at least 2*newCapacity + 2 to ensure index 2*newCapacity + 1 is valid
        Array.Resize(ref _next, 2 * newCapacity + 2);  // +2 to handle bitwise NOT indices
        Array.Resize(ref _head, newCapacity + 1);
        Array.Resize(ref _tail, newCapacity + 1);
        
        // Remap the negative arc indices in _next array
        // We need to copy from the old positions to the new positions
        if (oldCapacity > 0)
        {
            // For each possible negative arc index
            for (int arc = 1; arc <= oldCapacity; arc++)
            {
                int negArc = ~arc;  // This gives us -2, -3, -4, etc.
                int oldIndex = oldCapacity - negArc;  // Old position in array
                int newIndex = newCapacity - negArc;  // New position in array
                
                // Copy the value if it was within bounds
                if (oldIndex < oldNext.Length)
                {
                    _next[newIndex] = oldNext[oldIndex];
                }
            }
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
            
            // Resize SVector to accommodate new nodes
            if (node >= _firstOut.Size)
            {
                _firstOut.Resize(node + 1);
            }
            
            // Initialize new nodes
            for (int i = _numNodes; i <= node; i++)
            {
                _firstOut[i] = 0;  // 0 means no arcs (since 0 is not a valid arc)
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
        
        _numArcs++;
        int arc = _numArcs;
        
        _head[arc] = head;
        _tail[arc] = tail;
        
        // Insert forward arc at beginning of tail's adjacency list
        _next[arc] = _firstOut[tail];
        _firstOut[tail] = arc;
        
        // Insert reverse arc at beginning of head's adjacency list
        int reverseArc = ~arc;
        int reverseIndex = GetNextIndex(reverseArc);
        
        _next[reverseIndex] = _firstOut[head];
        _firstOut[head] = reverseArc;
        
        return arc;
    }

    public int[]? Build()
    {
        // ReverseArcListGraph doesn't require build
        return null;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        
        if (arc > 0)
        {
            return _head[arc];
        }
        else
        {
            // Negative arc: head of reverse arc is tail of forward arc
            return _tail[~arc];
        }
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        
        if (arc > 0)
        {
            return _tail[arc];
        }
        else
        {
            // Negative arc: tail of reverse arc is head of forward arc
            return _head[~arc];
        }
    }

    public int OutDegree(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        int count = 0;
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc > 0) count++;  // Only count forward arcs
        }
        return count;
    }

    public int InDegree(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        int count = 0;
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc < 0) count++;  // Only count reverse arcs
        }
        return count;
    }

    public static int OppositeArc(int arc)
    {
        Debug.Assert(arc != 0, "Arc 0 is not valid");
        return ~arc;
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc > 0)  // Only return forward arcs
            {
                yield return arc;
            }
        }
    }

    public IEnumerable<int> IncomingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc < 0)  // Reverse arcs represent incoming arcs
            {
                yield return ~arc;  // Return the forward arc index
            }
        }
    }

    public IEnumerable<int> OppositeIncomingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc < 0)  // Return the reverse arcs themselves
            {
                yield return arc;
            }
        }
    }

    public IEnumerable<int> OutgoingOrOppositeIncomingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            yield return arc;  // Return all arcs (both forward and reverse)
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int startArc)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        Debug.Assert(IsArcValid(startArc) && startArc > 0, $"Invalid forward arc: {startArc}");
        
        bool found = false;
        for (int arc = _firstOut[node]; arc != 0; arc = _next[GetNextIndex(arc)])
        {
            if (arc > 0)  // Only forward arcs
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
    }

    #region IMaxFlowGraph Implementation

    public bool HasNegativeReverseArcs => true;

    int IMaxFlowGraph.OppositeArc(int arc) => OppositeArc(arc);

    public IEnumerable<int> OutgoingOrOppositeIncomingArcsStartingFrom(int node, int arc)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        
        bool found = false;
        for (int current = _firstOut[node]; current != 0; current = _next[GetNextIndex(current)])
        {
            if (current == arc)
            {
                found = true;
            }
            if (found)
            {
                yield return current;
            }
        }
    }

    #endregion

    // Helper method to convert arc index to array index for _next array
    private int GetNextIndex(int arc)
    {
        Debug.Assert(arc != 0, "Arc 0 is not valid");
        
        if (arc > 0)
        {
            return arc;
        }
        else
        {
            // With bitwise NOT, ~n = -(n+1), so ~1 = -2, ~2 = -3, etc.
            // For arc = ~n = -(n+1), we have n = -arc - 1
            // We want to map this to index _arcCapacity + 1 + n = _arcCapacity + 1 + (-arc - 1) = _arcCapacity - arc
            return _arcCapacity - arc;  // Map negative arcs to upper part of array
        }
    }

    private static int CalculateNewCapacity(int currentCapacity, int required)
    {
        if (currentCapacity == 0)
        {
            return Math.Max(4, required);
        }
        
        long newCapacity = Math.Max((long)(currentCapacity * 1.5), required);
        if (newCapacity > int.MaxValue / 2)  // Need room for reverse arcs
        {
            throw new OutOfMemoryException("Graph capacity would exceed maximum size");
        }
        
        return (int)newCapacity;
    }
}