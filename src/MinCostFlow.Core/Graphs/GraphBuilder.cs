using System;
using System.Collections.Generic;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Core.Graphs;

/// <summary>
/// Helper class for building graphs with a fluent API.
/// </summary>
public sealed class GraphBuilder : IDisposable
{
    private readonly CompactDigraph _graph;
    private readonly Dictionary<int, Node> _nodeMap;
    private int _nextNodeId;
    
    /// <summary>
    /// Initializes a new instance of the GraphBuilder class.
    /// </summary>
    public GraphBuilder()
    {
        _graph = new CompactDigraph();
        _nodeMap = new Dictionary<int, Node>();
        _nextNodeId = 0;
    }
    
    /// <summary>
    /// Adds a node with an optional external ID.
    /// </summary>
    public GraphBuilder AddNode(int? externalId = null)
    {
        int id = externalId ?? _nextNodeId++;
        if (_nodeMap.ContainsKey(id))
        {
            throw new ArgumentException($"Node with ID {id} already exists");
        }
        
        Node node = _graph.AddNode();
        _nodeMap[id] = node;
        return this;
    }
    
    /// <summary>
    /// Adds multiple nodes at once.
    /// </summary>
    public GraphBuilder AddNodes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            AddNode();
        }
        return this;
    }
    
    /// <summary>
    /// Adds an arc between two nodes identified by their external IDs.
    /// </summary>
    public GraphBuilder AddArc(int sourceId, int targetId)
    {
        if (!_nodeMap.TryGetValue(sourceId, out Node source))
        {
            throw new ArgumentException($"Source node {sourceId} not found");
        }
        
        if (!_nodeMap.TryGetValue(targetId, out Node target))
        {
            throw new ArgumentException($"Target node {targetId} not found");
        }
        
        _graph.AddArc(source, target);
        return this;
    }
    
    /// <summary>
    /// Gets the internal node for an external ID.
    /// </summary>
    public Node GetNode(int externalId)
    {
        if (!_nodeMap.TryGetValue(externalId, out Node node))
        {
            throw new ArgumentException($"Node {externalId} not found");
        }
        return node;
    }
    
    /// <summary>
    /// Builds and returns the graph.
    /// </summary>
    public CompactDigraph Build()
    {
        return _graph;
    }
    
    /// <summary>
    /// Gets a read-only view of the node mapping.
    /// </summary>
    public IReadOnlyDictionary<int, Node> NodeMap => _nodeMap;
    
    /// <summary>
    /// Disposes the resources used by this GraphBuilder.
    /// </summary>
    public void Dispose()
    {
        _graph?.Dispose();
    }
}