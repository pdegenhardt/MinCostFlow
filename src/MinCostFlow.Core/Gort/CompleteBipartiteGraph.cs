using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Implicit representation of complete bipartite graphs.
/// Left nodes: [0, LeftNodes)
/// Right nodes: [LeftNodes, LeftNodes + RightNodes)
/// Only left nodes have outgoing arcs to all right nodes.
/// Memory usage: O(1) - constant space.
/// </summary>
public class CompleteBipartiteGraph : IGraphBase
{
    private int _leftNodes;
    private int _rightNodes;
    private bool _capacitiesFrozen;

    public CompleteBipartiteGraph(int leftNodes = 0, int rightNodes = 0)
    {
        Debug.Assert(leftNodes >= 0, "Number of left nodes must be non-negative");
        Debug.Assert(rightNodes >= 0, "Number of right nodes must be non-negative");
        
        _leftNodes = leftNodes;
        _rightNodes = rightNodes;
        _capacitiesFrozen = false;
    }

    public int LeftNodes => _leftNodes;
    public int RightNodes => _rightNodes;
    
    public int NumNodes => _leftNodes + _rightNodes;
    public int NumArcs => _leftNodes * _rightNodes;  // Each left node connects to each right node
    public int NodeCapacity => NumNodes;
    public int ArcCapacity => NumArcs;

    public bool IsNodeValid(int node) => node >= 0 && node < NumNodes;

    public bool IsArcValid(int arc) => arc >= 0 && arc < NumArcs;

    public bool IsLeftNode(int node) => node >= 0 && node < _leftNodes;
    
    public bool IsRightNode(int node) => node >= _leftNodes && node < NumNodes;

    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < NumNodes; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllLeftNodes()
    {
        for (int i = 0; i < _leftNodes; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllRightNodes()
    {
        for (int i = _leftNodes; i < NumNodes; i++)
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
        
        // For simplicity, add all extra nodes as right nodes
        if (bound > NumNodes)
        {
            _rightNodes += bound - NumNodes;
        }
    }

    public void ReserveArcs(int bound)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        
        // Cannot directly reserve arcs in bipartite graph
        // Would need to know how to distribute between left and right nodes
        if (bound > NumArcs)
        {
            throw new InvalidOperationException(
                "Cannot reserve arcs directly in CompleteBipartiteGraph. " +
                "Use SetNodeCounts or ReserveNodes instead.");
        }
    }

    public void Reserve(int nodeCapacity, int arcCapacity)
    {
        ReserveNodes(nodeCapacity);
        if (arcCapacity > NumArcs)
        {
            throw new InvalidOperationException(
                "Cannot reserve arcs directly in CompleteBipartiteGraph. " +
                "Use SetNodeCounts or ReserveNodes instead.");
        }
    }

    public void FreezeCapacities()
    {
        _capacitiesFrozen = true;
    }

    public void AddNode(int node)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        Debug.Assert(node >= 0, "Node index must be non-negative");
        
        if (node >= NumNodes)
        {
            // Add as right nodes
            _rightNodes = node - _leftNodes + 1;
        }
    }

    public int AddArc(int tail, int head)
    {
        Debug.Assert(IsNodeValid(tail), $"Invalid tail node: {tail}");
        Debug.Assert(IsNodeValid(head), $"Invalid head node: {head}");
        
        if (!IsLeftNode(tail) || !IsRightNode(head))
        {
            throw new InvalidOperationException(
                $"In bipartite graph, arcs must go from left nodes to right nodes. " +
                $"Tail {tail} must be in [0, {_leftNodes}), head {head} must be in [{_leftNodes}, {NumNodes})");
        }
        
        // Arc already exists implicitly
        return GetArcIndex(tail, head);
    }

    public int[]? Build()
    {
        // CompleteBipartiteGraph doesn't require build
        return null;
    }

    public int Head(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        // Arc index = leftNode * _rightNodes + (rightNode - _leftNodes)
        // So: rightNode - _leftNodes = arc % _rightNodes
        return _leftNodes + arc % _rightNodes;
    }

    public int Tail(int arc)
    {
        Debug.Assert(IsArcValid(arc), $"Invalid arc: {arc}");
        // Arc index = leftNode * _rightNodes + (rightNode - _leftNodes)
        // So: leftNode = arc / _rightNodes
        return arc / _rightNodes;
    }

    public int OutDegree(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        if (IsLeftNode(node))
        {
            return _rightNodes;  // Left nodes connect to all right nodes
        }
        else
        {
            return 0;  // Right nodes have no outgoing arcs
        }
    }

    public IEnumerable<int> OutgoingArcs(int node)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        
        if (IsLeftNode(node))
        {
            int startArc = node * _rightNodes;
            for (int i = 0; i < _rightNodes; i++)
            {
                yield return startArc + i;
            }
        }
        // Right nodes have no outgoing arcs
    }

    public IEnumerable<int> OutgoingArcsStartingFrom(int node, int startArc)
    {
        Debug.Assert(IsNodeValid(node), $"Invalid node: {node}");
        Debug.Assert(IsArcValid(startArc), $"Invalid arc: {startArc}");
        
        if (!IsLeftNode(node))
        {
            yield break;  // Right nodes have no outgoing arcs
        }
        
        int nodeStart = node * _rightNodes;
        int nodeEnd = (node + 1) * _rightNodes;
        
        Debug.Assert(startArc >= nodeStart && startArc < nodeEnd, 
            $"Arc {startArc} does not belong to node {node}");
        
        for (int arc = startArc; arc < nodeEnd; arc++)
        {
            yield return arc;
        }
    }

    /// <summary>
    /// Sets the number of left and right nodes.
    /// </summary>
    public void SetNodeCounts(int leftNodes, int rightNodes)
    {
        Debug.Assert(!_capacitiesFrozen, "Capacities are frozen");
        Debug.Assert(leftNodes >= 0, "Number of left nodes must be non-negative");
        Debug.Assert(rightNodes >= 0, "Number of right nodes must be non-negative");
        
        _leftNodes = leftNodes;
        _rightNodes = rightNodes;
    }

    /// <summary>
    /// Gets the arc index for an arc from a left node to a right node.
    /// </summary>
    public int GetArcIndex(int leftNode, int rightNode)
    {
        Debug.Assert(IsLeftNode(leftNode), $"Node {leftNode} is not a left node");
        Debug.Assert(IsRightNode(rightNode), $"Node {rightNode} is not a right node");
        
        return leftNode * _rightNodes + (rightNode - _leftNodes);
    }

    /// <summary>
    /// Checks if an arc exists between two nodes.
    /// </summary>
    public bool HasArc(int tail, int head)
    {
        return IsLeftNode(tail) && IsRightNode(head);
    }
}