using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Implicit representation of complete graphs (all nodes connected).
/// Memory usage: O(1) - constant space regardless of graph size.
/// Arc indices are deterministic: arc from i to j (i ≠ j) has index i × (NumNodes - 1) + j - (j > i ? 1 : 0).
/// </summary>
public class CompleteGraph : IGraphBase
{
    private int _numNodes;
    private bool _capacitiesFrozen;

    public CompleteGraph(int numNodes = 0)
    {
        Debug.Assert(numNodes >= 0, "Number of nodes must be non-negative");
        _numNodes = numNodes;
        _capacitiesFrozen = false;
    }

    public int NumNodes => _numNodes;
    public int NumArcs => _numNodes * (_numNodes - 1);  // Every node connects to every other node (no self-loops)
    public int NodeCapacity => _numNodes;
    public int ArcCapacity => NumArcs;

    public bool IsNodeValid(int node) => node >= 0 && node < _numNodes;

    public bool IsArcValid(int arc) => arc >= 0 && arc < NumArcs;

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < _numNodes; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllForwardArcs()
    {
        for (int i = 0; i < NumArcs; i++)
        {
            yield return i;
        }
    }

    public void ReserveNodes(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        
        if (bound > _numNodes)
        {
            _numNodes = bound;
        }
    }

    public void ReserveArcs(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        
        // For complete graph, arc capacity is determined by node count
        // Find minimum number of nodes needed for the requested arc count
        int requiredNodes = (int)Math.Ceiling(Math.Sqrt(bound));
        if (requiredNodes > _numNodes)
        {
            _numNodes = requiredNodes;
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
        Debug.Assert(node >= 0, "Node index must be non-negative");
        
        if (node >= _numNodes)
        {
            Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
            _numNodes = node + 1;
        }
    }

    public int AddArc(int tail, int head)
    {
        Debug.Assert(IsNodeValid(tail), $"Invalid tail node: {tail}");
        Debug.Assert(IsNodeValid(head), $"Invalid head node: {head}");
        
        // Arc already exists implicitly
        return GetArcIndex(tail, head);
    }

    public int[]? Build()
    {
        // CompleteGraph doesn't require build
        return null;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        if (_numNodes <= 1) return 0;
        
        int tail = arc / (_numNodes - 1);
        int head = arc % (_numNodes - 1);
        
        // Adjust for skipped self-loop
        if (head >= tail) head++;
        
        return head;
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        if (_numNodes <= 1) return 0;
        return arc / (_numNodes - 1);
    }

    public int OutDegree(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        return _numNodes - 1;  // Each node connects to all other nodes (no self-loops)
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        int startArc = node * (_numNodes - 1);
        for (int i = 0; i < _numNodes - 1; i++)
        {
            yield return startArc + i;
        }
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int startArc)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        Debug.Assert(IsArcValid(startArc), $"Invalid arc: {startArc}");
        
        int nodeStart = node * (_numNodes - 1);
        int nodeEnd = (node + 1) * (_numNodes - 1);
        
        Debug.Assert(startArc >= nodeStart && startArc < nodeEnd, 
            $"Arc {startArc} does not belong to node {node}");
        
        for (int arc = startArc; arc < nodeEnd; arc++)
        {
            yield return arc;
        }
    }

    /// <summary>
    /// Gets the arc index for an arc from tail to head.
    /// </summary>
    public int GetArcIndex(int tail, int head)
    {
        Debug.Assert(IsNodeValid(tail), $"Invalid tail node: {tail}");
        Debug.Assert(IsNodeValid(head), $"Invalid head node: {head}");
        Debug.Assert(tail != head, "Self-loops are not allowed in complete graphs");
        
        // Without self-loops: skip the diagonal
        if (head > tail)
            return tail * (_numNodes - 1) + head - 1;
        else
            return tail * (_numNodes - 1) + head;
    }

    /// <summary>
    /// Checks if an arc exists between two nodes (always true for complete graph).
    /// </summary>
    public bool HasArc(int tail, int head)
    {
        return IsNodeValid(tail) && IsNodeValid(head) && tail != head;
    }
}