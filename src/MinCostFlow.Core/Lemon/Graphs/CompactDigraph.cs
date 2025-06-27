using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon.Graphs;

/// <summary>
/// A memory-efficient directed graph implementation based on LEMON's ListDigraph.
/// Uses Structure of Arrays (SoA) pattern for cache efficiency.
/// </summary>
public sealed class CompactDigraph : IGraph, IDisposable
{
    // Node data structures
    private int[] _firstOut;    // First outgoing arc for each node
    private int[] _firstIn;     // First incoming arc for each node
    
    // Arc data structures (Structure of Arrays for cache efficiency)
    private int[] _source;      // Source node of each arc
    private int[] _target;      // Target node of each arc
    private int[] _nextOut;     // Next outgoing arc in linked list
    private int[] _nextIn;      // Next incoming arc in linked list
    
    // Temporary buffers for returning arc lists
    private Arc[]? _tempOutArcs;
    private Arc[]? _tempInArcs;
    private readonly ArrayPool<Arc> _arcPool = ArrayPool<Arc>.Shared;
    
    private int _nodeCount;
    private int _arcCount;
    private int _nodeCapacity;
    private int _arcCapacity;
    
    private const int INITIAL_NODE_CAPACITY = 16;
    private const int INITIAL_ARC_CAPACITY = 32;
    private const int INVALID_ID = -1;
    
    /// <summary>
    /// Creates a new empty directed graph.
    /// </summary>
    public CompactDigraph() : this(INITIAL_NODE_CAPACITY, INITIAL_ARC_CAPACITY)
    {
    }
    
    /// <summary>
    /// Creates a new directed graph with specified initial capacities.
    /// </summary>
    public CompactDigraph(int nodeCapacity, int arcCapacity)
    {
        _nodeCapacity = Math.Max(nodeCapacity, INITIAL_NODE_CAPACITY);
        _arcCapacity = Math.Max(arcCapacity, INITIAL_ARC_CAPACITY);
        
        // Allocate node arrays
        _firstOut = new int[_nodeCapacity];
        _firstIn = new int[_nodeCapacity];
        Array.Fill(_firstOut, INVALID_ID);
        Array.Fill(_firstIn, INVALID_ID);
        
        // Allocate arc arrays
        _source = new int[_arcCapacity];
        _target = new int[_arcCapacity];
        _nextOut = new int[_arcCapacity];
        _nextIn = new int[_arcCapacity];
        
        _nodeCount = 0;
        _arcCount = 0;
    }
    
    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodeCount;
    /// <summary>
    /// Gets the number of arcs in the graph.
    /// </summary>
    public int ArcCount => _arcCount;
    
    /// <summary>
    /// Adds a new node to the graph.
    /// </summary>
    public Node AddNode()
    {
        if (_nodeCount >= _nodeCapacity)
        {
            GrowNodeArrays();
        }
        
        int id = _nodeCount++;
        _firstOut[id] = INVALID_ID;
        _firstIn[id] = INVALID_ID;
        
        return new Node(id);
    }
    
    /// <summary>
    /// Adds a new arc from source to target.
    /// </summary>
    public Arc AddArc(Node source, Node target)
    {
        if (!IsValidNode(source) || !IsValidNode(target))
        {
            throw new ArgumentException("Invalid source or target node");
        }
        
        if (_arcCount >= _arcCapacity)
        {
            GrowArcArrays();
        }
        
        int id = _arcCount++;
        int sourceId = source.Id;
        int targetId = target.Id;
        
        // Set arc data
        _source[id] = sourceId;
        _target[id] = targetId;
        
        // Insert into outgoing list of source
        _nextOut[id] = _firstOut[sourceId];
        _firstOut[sourceId] = id;
        
        // Insert into incoming list of target
        _nextIn[id] = _firstIn[targetId];
        _firstIn[targetId] = id;
        
        return new Arc(id);
    }
    
    /// <summary>
    /// Gets the source node of an arc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node Source(Arc arc) => new Node(_source[arc.Id]);
    
    /// <summary>
    /// Gets the target node of an arc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node Target(Arc arc) => new Node(_target[arc.Id]);
    
    /// <summary>
    /// Gets all outgoing arcs from a node.
    /// </summary>
    public ReadOnlySpan<Arc> GetOutArcs(Node node)
    {
        if (!IsValidNode(node))
        {
            return ReadOnlySpan<Arc>.Empty;
        }
        
        // Count outgoing arcs
        int count = 0;
        for (int arcId = _firstOut[node.Id]; arcId != INVALID_ID; arcId = _nextOut[arcId])
        {
            count++;
        }
        
        if (count == 0)
        {
            return ReadOnlySpan<Arc>.Empty;
        }
        
        // Rent buffer if needed
        if (_tempOutArcs == null || _tempOutArcs.Length < count)
        {
            if (_tempOutArcs != null)
            {
                _arcPool.Return(_tempOutArcs);
            }
            _tempOutArcs = _arcPool.Rent(count);
        }
        
        // Fill buffer
        int index = 0;
        for (int arcId = _firstOut[node.Id]; arcId != INVALID_ID; arcId = _nextOut[arcId])
        {
            _tempOutArcs[index++] = new Arc(arcId);
        }
        
        return new ReadOnlySpan<Arc>(_tempOutArcs, 0, count);
    }
    
    /// <summary>
    /// Gets all incoming arcs to a node.
    /// </summary>
    public ReadOnlySpan<Arc> GetInArcs(Node node)
    {
        if (!IsValidNode(node))
        {
            return ReadOnlySpan<Arc>.Empty;
        }
        
        // Count incoming arcs
        int count = 0;
        for (int arcId = _firstIn[node.Id]; arcId != INVALID_ID; arcId = _nextIn[arcId])
        {
            count++;
        }
        
        if (count == 0)
        {
            return ReadOnlySpan<Arc>.Empty;
        }
        
        // Rent buffer if needed
        if (_tempInArcs == null || _tempInArcs.Length < count)
        {
            if (_tempInArcs != null)
            {
                _arcPool.Return(_tempInArcs);
            }
            _tempInArcs = _arcPool.Rent(count);
        }
        
        // Fill buffer
        int index = 0;
        for (int arcId = _firstIn[node.Id]; arcId != INVALID_ID; arcId = _nextIn[arcId])
        {
            _tempInArcs[index++] = new Arc(arcId);
        }
        
        return new ReadOnlySpan<Arc>(_tempInArcs, 0, count);
    }
    
    /// <summary>
    /// Checks if a node is valid in this graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidNode(Node node) => node.Id >= 0 && node.Id < _nodeCount;
    
    /// <summary>
    /// Checks if an arc is valid in this graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidArc(Arc arc) => arc.Id >= 0 && arc.Id < _arcCount;
    
    /// <summary>
    /// Gets the first node for iteration.
    /// </summary>
    public Node FirstNode() => _nodeCount > 0 ? new Node(0) : Node.Invalid;
    
    /// <summary>
    /// Gets the next node for iteration.
    /// </summary>
    public Node NextNode(Node node)
    {
        int nextId = node.Id + 1;
        return nextId < _nodeCount ? new Node(nextId) : Node.Invalid;
    }
    
    /// <summary>
    /// Gets the first arc for iteration.
    /// </summary>
    public Arc FirstArc() => _arcCount > 0 ? new Arc(0) : Arc.Invalid;
    
    /// <summary>
    /// Gets the next arc for iteration.
    /// </summary>
    public Arc NextArc(Arc arc)
    {
        int nextId = arc.Id + 1;
        return nextId < _arcCount ? new Arc(nextId) : Arc.Invalid;
    }
    
    private void GrowNodeArrays()
    {
        _nodeCapacity *= 2;
        Array.Resize(ref _firstOut, _nodeCapacity);
        Array.Resize(ref _firstIn, _nodeCapacity);
        
        // Initialize new elements
        Array.Fill(_firstOut, INVALID_ID, _nodeCount, _nodeCapacity - _nodeCount);
        Array.Fill(_firstIn, INVALID_ID, _nodeCount, _nodeCapacity - _nodeCount);
    }
    
    private void GrowArcArrays()
    {
        _arcCapacity *= 2;
        Array.Resize(ref _source, _arcCapacity);
        Array.Resize(ref _target, _arcCapacity);
        Array.Resize(ref _nextOut, _arcCapacity);
        Array.Resize(ref _nextIn, _arcCapacity);
    }
    
    /// <summary>
    /// Releases the resources used by this graph.
    /// </summary>
    public void Dispose()
    {
        if (_tempOutArcs != null)
        {
            _arcPool.Return(_tempOutArcs);
            _tempOutArcs = null;
        }
        
        if (_tempInArcs != null)
        {
            _arcPool.Return(_tempInArcs);
            _tempInArcs = null;
        }
    }
}