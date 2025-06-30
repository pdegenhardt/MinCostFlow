using MinCostFlow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Generic maximum flow algorithm implementation using the push-relabel method (Goldberg-Tarjan algorithm).
/// Finds the maximum flow from a source node to a sink node in a directed graph with capacitated arcs.
/// </summary>
/// <typeparam name="TGraph">The graph type that implements IMaxFlowGraph</typeparam>
/// <typeparam name="TArcFlowType">The type for arc capacities and flows (can be unsigned)</typeparam>
/// <typeparam name="TFlowSumType">The type for flow sums (must be signed, bit width ≥ TArcFlowType)</typeparam>
public class GenericMaxFlow<TGraph, TArcFlowType, TFlowSumType>
    where TGraph : IMaxFlowGraph
    where TArcFlowType : unmanaged, INumber<TArcFlowType>
    where TFlowSumType : unmanaged, INumber<TFlowSumType>
{
    #region Constants and Fields

    // Maximum manageable flow (to detect overflow)
    private readonly TFlowSumType MaxFlowSum;
    
    // Graph and problem definition
    private readonly TGraph _graph;
    private readonly int _sourceNodeIndex;
    private readonly int _sinkNodeIndex;
    
    // Node arrays (size = node_capacity)
    private readonly TFlowSumType[] _nodeExcess;
    private readonly int[] _nodePotential;
    private readonly int[] _firstAdmissibleArc;
    
    // Arc arrays
    private readonly ZVector<TArcFlowType>? _residualArcCapacityNegative; // For negative reverse arcs
    private readonly TArcFlowType[]? _residualArcCapacityStandard; // For standard graphs
    private readonly TArcFlowType[]? _initialCapacity; // Initial capacities for all graphs
    
    // Active node management
    private readonly PriorityQueueWithRestrictedPush<int, int> _activeNodeByHeight;
    
    // BFS support
    private readonly bool[] _nodeInBfsQueue;
    private readonly List<int> _bfsQueue;
    
    // Status
    private Status _status;
    
    // Logger
    private readonly ILogger _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new max flow solver for the given graph and source/sink nodes.
    /// </summary>
    public GenericMaxFlow(TGraph graph, int source, int sink, ILogger? logger = null)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _sourceNodeIndex = source;
        _sinkNodeIndex = sink;
        _logger = logger ?? new NoOpLogger();
        
        // Initialize MaxFlowSum to the maximum value for the flow sum type
        // This represents the limit beyond which we consider overflow
        if (typeof(TFlowSumType) == typeof(int))
        {
            MaxFlowSum = (TFlowSumType)(object)int.MaxValue;
        }
        else if (typeof(TFlowSumType) == typeof(long))
        {
            MaxFlowSum = (TFlowSumType)(object)long.MaxValue;
        }
        else if (typeof(TFlowSumType) == typeof(uint))
        {
            MaxFlowSum = (TFlowSumType)(object)uint.MaxValue;
        }
        else if (typeof(TFlowSumType) == typeof(ulong))
        {
            MaxFlowSum = (TFlowSumType)(object)ulong.MaxValue;
        }
        else
        {
            // Generic fallback using numeric abstractions
            try
            {
                // Try to get MaxValue if available
                var maxValueProperty = typeof(TFlowSumType).GetProperty("MaxValue");
                if (maxValueProperty != null)
                {
                    MaxFlowSum = (TFlowSumType)maxValueProperty.GetValue(null)!;
                }
                else
                {
                    // Fallback to a large value
                    MaxFlowSum = TFlowSumType.CreateChecked(1000000000);
                }
            }
            catch
            {
                // Last resort fallback
                MaxFlowSum = TFlowSumType.CreateChecked(10000);
            }
        }
        
        var nodeCapacity = graph.NodeCapacity;
        var arcCapacity = graph.ArcCapacity;
        
        // Initialize node arrays
        _nodeExcess = new TFlowSumType[nodeCapacity];
        _nodePotential = new int[nodeCapacity];
        _firstAdmissibleArc = new int[nodeCapacity];
        
        // Initialize arc arrays based on graph type
        if (graph.HasNegativeReverseArcs)
        {
            // Use ZVector for symmetric indexing with negative arcs
            _residualArcCapacityNegative = ZVector<TArcFlowType>.ForArcs(arcCapacity);
            _initialCapacity = new TArcFlowType[arcCapacity];
        }
        else
        {
            // Use standard arrays
            _residualArcCapacityStandard = new TArcFlowType[arcCapacity];
            _initialCapacity = new TArcFlowType[arcCapacity];
        }
        
        // Initialize data structures
        _activeNodeByHeight = new PriorityQueueWithRestrictedPush<int, int>();
        _nodeInBfsQueue = new bool[nodeCapacity];
        _bfsQueue = new List<int>(nodeCapacity);
        
        _status = Status.NOT_SOLVED;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the graph associated with this solver.
    /// </summary>
    public TGraph Graph => _graph;

    /// <summary>
    /// Gets the source node index.
    /// </summary>
    public int GetSourceNodeIndex() => _sourceNodeIndex;

    /// <summary>
    /// Gets the sink node index.
    /// </summary>
    public int GetSinkNodeIndex() => _sinkNodeIndex;

    /// <summary>
    /// Gets the current solution status.
    /// </summary>
    public Status status => _status;

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the capacity for the given arc.
    /// Self-loops are ignored.
    /// </summary>
    public void SetArcCapacity(int arc, TArcFlowType capacity)
    {
        if (!_graph.IsArcValid(arc))
            throw new ArgumentException($"Invalid arc: {arc}");
            
        // Ignore self-loops
        if (_graph.Head(arc) == _graph.Tail(arc))
            return;
            
        if (_graph.HasNegativeReverseArcs)
        {
            // For negative reverse arcs, only direct arcs can have positive capacity
            if (arc > 0)
            {
                _residualArcCapacityNegative![arc] = capacity;
                _initialCapacity![arc] = capacity;
            }
        }
        else
        {
            // Store initial capacity for standard graphs
            _initialCapacity![arc] = capacity;
        }
        
        _status = Status.NOT_SOLVED;
    }

    /// <summary>
    /// Runs the max flow algorithm and returns true on success.
    /// </summary>
    public bool Solve()
    {
        _status = Status.NOT_SOLVED;
        
        // Initialize preflow
        InitializePreflow();
        
        // Handle invalid source/sink after initialization
        int numNodes = _graph.NumNodes;
        if (_sinkNodeIndex >= numNodes || _sourceNodeIndex >= numNodes)
        {
            // Behave like a normal graph where source and sink are disconnected
            _status = Status.OPTIMAL;
            return true;
        }
        
        // Main algorithm following OR-Tools pattern
        RefineWithGlobalUpdate();
        
        _status = Status.OPTIMAL;
        
        // Check for overflow - only report overflow if we've hit the max and still have augmenting paths
        // This means we can't represent the true max flow value
        if (GetOptimalFlow().CompareTo(MaxFlowSum) >= 0 && AugmentingPathExists())
        {
            _status = Status.INT_OVERFLOW;
        }
        
        return true;
    }

    /// <summary>
    /// Gets the optimal flow value (equals node_excess[sink]).
    /// </summary>
    public TFlowSumType GetOptimalFlow()
    {
        // Handle invalid sink index
        if (_sinkNodeIndex < 0 || _sinkNodeIndex >= _nodeExcess.Length)
            return default(TFlowSumType);
            
        return _nodeExcess[_sinkNodeIndex];
    }

    /// <summary>
    /// Gets the signed flow on the given arc.
    /// Positive values indicate forward flow.
    /// </summary>
    public TFlowSumType Flow(int arc)
    {
        if (!_graph.IsArcValid(arc))
            throw new ArgumentException($"Invalid arc: {arc}");
            
        if (_graph.HasNegativeReverseArcs)
        {
            if (arc > 0)
            {
                // Direct arc: flow = residual_capacity[opposite_arc]
                var oppositeArc = ~arc;
                var residualCapacity = _residualArcCapacityNegative![oppositeArc];
                // _logger.Log($"Flow({arc}): opposite arc={oppositeArc}, residual capacity on opposite={residualCapacity}");
                return TFlowSumType.CreateChecked(residualCapacity);
            }
            else
            {
                // Reverse arc: flow = -residual_capacity[arc]
                var flow = TFlowSumType.CreateChecked(_residualArcCapacityNegative![arc]);
                return -flow;
            }
        }
        else
        {
            // Standard model: flow = initial_capacity - residual_capacity
            var initial = TFlowSumType.CreateChecked(_initialCapacity![arc]);
            var residual = TFlowSumType.CreateChecked(_residualArcCapacityStandard![arc]);
            return initial - residual;
        }
    }

    /// <summary>
    /// Gets the initial capacity of the given arc.
    /// </summary>
    public TArcFlowType Capacity(int arc)
    {
        if (!_graph.IsArcValid(arc))
            throw new ArgumentException($"Invalid arc: {arc}");
            
        // Only forward arcs have capacities
        if (_graph.HasNegativeReverseArcs && arc < 0)
            return default(TArcFlowType);
            
        return _initialCapacity![arc > 0 ? arc : -arc];
    }

    /// <summary>
    /// Gets the nodes reachable from source in the residual graph.
    /// </summary>
    public void GetSourceSideMinCut(List<int> result)
    {
        result.Clear();
        
        // BFS from source in residual graph
        Array.Clear(_nodeInBfsQueue, 0, _graph.NumNodes);
        _bfsQueue.Clear();
        
        _bfsQueue.Add(_sourceNodeIndex);
        _nodeInBfsQueue[_sourceNodeIndex] = true;
        
        for (int i = 0; i < _bfsQueue.Count; i++)
        {
            int node = _bfsQueue[i];
            result.Add(node);
            
            foreach (var arc in _graph.OutgoingOrOppositeIncomingArcs(node))
            {
                if (GetResidualCapacity(arc).CompareTo(default(TArcFlowType)) > 0)
                {
                    int head = _graph.Head(arc);
                    if (!_nodeInBfsQueue[head])
                    {
                        _nodeInBfsQueue[head] = true;
                        _bfsQueue.Add(head);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the nodes that can reach sink in the residual graph.
    /// </summary>
    public void GetSinkSideMinCut(List<int> result)
    {
        result.Clear();
        
        // Reverse BFS from sink in residual graph
        Array.Clear(_nodeInBfsQueue, 0, _graph.NumNodes);
        _bfsQueue.Clear();
        
        _bfsQueue.Add(_sinkNodeIndex);
        _nodeInBfsQueue[_sinkNodeIndex] = true;
        
        for (int i = 0; i < _bfsQueue.Count; i++)
        {
            int node = _bfsQueue[i];
            result.Add(node);
            
            foreach (var arc in _graph.OutgoingOrOppositeIncomingArcs(node))
            {
                int oppositeArc = _graph.OppositeArc(arc);
                if (GetResidualCapacity(oppositeArc).CompareTo(default(TArcFlowType)) > 0)
                {
                    int tail = _graph.Tail(arc);
                    if (!_nodeInBfsQueue[tail])
                    {
                        _nodeInBfsQueue[tail] = true;
                        _bfsQueue.Add(tail);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if an augmenting path exists from source to sink.
    /// </summary>
    public bool AugmentingPathExists()
    {
        var sourceReachable = new List<int>();
        GetSourceSideMinCut(sourceReachable);
        return sourceReachable.Contains(_sinkNodeIndex);
    }

    #endregion

    #region Private Methods - Algorithm Core

    private void InitializePreflow()
    {
        // Initialize node data
        int numNodes = _graph.NumNodes;
        for (int i = 0; i < numNodes; i++)
        {
            _nodeExcess[i] = default(TFlowSumType);
            _nodePotential[i] = 0;
            _firstAdmissibleArc[i] = IGraphBase.NilArc;
        }
        
        // Set source potential
        if (_sourceNodeIndex >= 0 && _sourceNodeIndex < numNodes)
            _nodePotential[_sourceNodeIndex] = numNodes;
        
        // Initialize residual capacities
        if (_graph.HasNegativeReverseArcs)
        {
            _logger.Log("InitializePreflow: Setting up residual capacities for negative reverse arcs");
            // For negative reverse arcs, capacities are already set via SetArcCapacity
            // Just ensure all reverse arcs have 0 capacity
            foreach (var arc in _graph.AllForwardArcs())
            {
                _residualArcCapacityNegative![~arc] = default(TArcFlowType);
                _logger.Log($"  Arc {arc}: capacity={_residualArcCapacityNegative[arc]}, reverse arc {~arc} capacity={_residualArcCapacityNegative[~arc]}");
            }
        }
        else
        {
            _logger.Log("InitializePreflow: Copying initial capacities for standard graph");
            // Copy from initial capacities
            Array.Copy(_initialCapacity!, _residualArcCapacityStandard!, _graph.NumArcs);
        }
        
        // Initialize first admissible arc cache
        for (int node = 0; node < numNodes; node++)
        {
            var arcs = _graph.OutgoingOrOppositeIncomingArcs(node);
            _firstAdmissibleArc[node] = arcs.FirstOrDefault(IGraphBase.NilArc);
        }
        
        // Clear active nodes
        _activeNodeByHeight.Clear();
    }

    private void RefineWithGlobalUpdate()
    {
        _logger.Log("RefineWithGlobalUpdate: Starting");
        
        // Follow OR-Tools pattern: loop while we can saturate outgoing arcs from source
        while (SaturateOutgoingArcsFromSource())
        {
            _logger.Log("RefineWithGlobalUpdate: Saturated arcs from source, starting main loop");
            
            // Perform global update to set exact distances
            GlobalUpdate();
            
            _logger.Log($"RefineWithGlobalUpdate: Active nodes count = {_activeNodeByHeight.Count}");
            
            // Main discharge loop
            while (!_activeNodeByHeight.IsEmpty())
            {
                int node = _activeNodeByHeight.Pop();
                
                _logger.Log($"  Discharging node {node} with excess {_nodeExcess[node]}");
                
                // Skip if no excess
                if (_nodeExcess[node].CompareTo(default(TFlowSumType)) <= 0)
                {
                    _logger.Log($"    Skipping - no excess");
                    continue;
                }
                    
                Discharge(node);
            }
            
            // Push flow excess back to source (two-phase algorithm)
            PushFlowExcessBackToSource();
        }
        
        _logger.Log("RefineWithGlobalUpdate: Done");
    }

    private bool SaturateOutgoingArcsFromSource()
    {
        int numNodes = _graph.NumNodes;
        
        // If sink or source already have MaxFlowSum, avoid overflow
        if (_nodeExcess[_sinkNodeIndex].Equals(MaxFlowSum)) return false;
        if (_nodeExcess[_sourceNodeIndex].Equals(-MaxFlowSum)) return false;
        
        bool flowPushed = false;
        _logger.Log($"SaturateOutgoingArcsFromSource: Starting");
        
        foreach (var arc in _graph.OutgoingArcs(_sourceNodeIndex))
        {
            var flow = GetResidualCapacity(arc);
            int head = _graph.Head(arc);
            
            _logger.Log($"  Checking arc {arc} to node {head}, flow={flow}, potential={_nodePotential[head]}");
            
            // This is the special IsAdmissible() condition for the source (OR-Tools line 1113)
            if (flow.CompareTo(default(TArcFlowType)) == 0 || _nodePotential[head] >= numNodes)
            {
                _logger.Log($"    Skipping: flow={flow}, potential={_nodePotential[head]}, numNodes={numNodes}");
                continue;
            }
            
            // Handle overflow protection (OR-Tools lines 1117-1131)
            var currentFlowOutOfSource = -_nodeExcess[_sourceNodeIndex];
            var cappedFlow = MaxFlowSum - currentFlowOutOfSource;
            var flowAsSum = TFlowSumType.CreateChecked(flow);
            
            _logger.Log($"    Current flow out of source: {currentFlowOutOfSource}");
            _logger.Log($"    Capped flow: {cappedFlow}");
            _logger.Log($"    Flow as sum: {flowAsSum}");
            
            if (cappedFlow.CompareTo(flowAsSum) < 0)
            {
                // Push as much as we can to reach MaxFlowSum
                if (cappedFlow.CompareTo(default(TFlowSumType)) == 0) return true;
                PushFlow(cappedFlow, _sourceNodeIndex, arc);
                return true;
            }
            
            // Push all the flow
            PushFlow(flowAsSum, _sourceNodeIndex, arc);
            flowPushed = true;
            
            _logger.Log($"    Pushed {flow} flow to node {head}");
        }
        
        _logger.Log($"SaturateOutgoingArcsFromSource: Flow pushed = {flowPushed}");
        return flowPushed;
    }

    private void Discharge(int node)
    {
        _logger.Log($"Discharge: Node {node} with excess {_nodeExcess[node]} and potential {_nodePotential[node]}");
        
        while (_nodeExcess[node].CompareTo(default(TFlowSumType)) > 0)
        {
            // Try to push along admissible arcs
            bool pushed = false;
            
            foreach (var arc in _graph.OutgoingOrOppositeIncomingArcsStartingFrom(node, _firstAdmissibleArc[node]))
            {
                // Check if arc is admissible
                if (IsAdmissible(node, arc))
                {
                    _firstAdmissibleArc[node] = arc;
                    
                    // Push flow
                    var flowToPush = PushAsMuchFlowAsPossible(node, arc);
                    if (flowToPush.CompareTo(default(TFlowSumType)) > 0)
                    {
                        pushed = true;
                        
                        // Activate head if necessary
                        int head = _graph.Head(arc);
                        if (head != _sourceNodeIndex && head != _sinkNodeIndex && 
                            _nodeExcess[head].CompareTo(default(TFlowSumType)) > 0)
                        {
                            _activeNodeByHeight.Push(head, _nodePotential[head]);
                        }
                        
                        if (_nodeExcess[node].CompareTo(default(TFlowSumType)) <= 0)
                            break;
                    }
                }
            }
            
            if (!pushed)
            {
                // No admissible arc found, relabel
                Relabel(node);
                
                // Check if node became unreachable
                if (_nodePotential[node] >= _graph.NumNodes)
                    break;
            }
        }
    }

    private void Relabel(int node)
    {
        int minHeight = int.MaxValue;
        int minArc = IGraphBase.NilArc;
        
        foreach (var arc in _graph.OutgoingOrOppositeIncomingArcs(node))
        {
            if (GetResidualCapacity(arc).CompareTo(default(TArcFlowType)) > 0)
            {
                int head = _graph.Head(arc);
                if (_nodePotential[head] < minHeight)
                {
                    minHeight = _nodePotential[head];
                    minArc = arc;
                }
            }
        }
        
        if (minHeight < int.MaxValue)
        {
            // Relaxed relabel - only increase if necessary
            if (_nodePotential[node] <= minHeight)
            {
                _nodePotential[node] = minHeight + 1;
                _firstAdmissibleArc[node] = minArc;
            }
        }
        else
        {
            // No outgoing residual arcs, node is disconnected
            _nodePotential[node] = 2 * _graph.NumNodes - 1;
            _firstAdmissibleArc[node] = IGraphBase.NilArc;
        }
    }

    private void GlobalUpdate()
    {
        _logger.Log("GlobalUpdate: Starting");
        
        int numNodes = _graph.NumNodes;
        
        // Initialize BFS
        Array.Clear(_nodeInBfsQueue, 0, numNodes);
        _bfsQueue.Clear();
        _activeNodeByHeight.Clear();
        
        // Mark source and sink as visited
        _nodeInBfsQueue[_sourceNodeIndex] = true;
        _nodeInBfsQueue[_sinkNodeIndex] = true;
        
        // Start BFS from sink
        _bfsQueue.Add(_sinkNodeIndex);
        _nodePotential[_sinkNodeIndex] = 0;
        
        // BFS to compute exact distances
        _logger.Log($"GlobalUpdate: BFS from sink (node {_sinkNodeIndex})");
        for (int i = 0; i < _bfsQueue.Count; i++)
        {
            int node = _bfsQueue[i];
            int candidateDistance = _nodePotential[node] + 1;
            
            _logger.Log($"  Processing BFS node {node} with height {_nodePotential[node]}");
            
            // Debug: Log all arcs for this node
            var allArcs = _graph.OutgoingOrOppositeIncomingArcs(node).ToList();
            _logger.Log($"    Node {node} has {allArcs.Count} arcs: {string.Join(", ", allArcs)}");
            
            foreach (var arc in _graph.OutgoingOrOppositeIncomingArcs(node))
            {
                int head = _graph.Head(arc);
                
                // Skip if head already visited
                if (_nodeInBfsQueue[head])
                {
                    _logger.Log($"    Skipping arc {arc} to node {head} - already visited");
                    continue;
                }
                
                // Check if we can traverse backwards via opposite arc
                int oppositeArc = _graph.OppositeArc(arc);
                var oppositeResidual = GetResidualCapacity(oppositeArc);
                
                _logger.Log($"    Checking arc {arc} (tail={node}, head={head}), opposite arc {oppositeArc}, opposite residual={oppositeResidual}");
                
                if (oppositeResidual.CompareTo(default(TArcFlowType)) > 0)
                {
                    _logger.Log($"      Can reach node {head} via opposite arc {oppositeArc} with residual {oppositeResidual}");
                    
                    // If head has excess, steal it during BFS
                    // This is a key optimization from OR-Tools
                    if (_nodeExcess[head].CompareTo(default(TFlowSumType)) > 0)
                    {
                        var excess = _nodeExcess[head];
                        var residual = TFlowSumType.CreateChecked(GetResidualCapacity(oppositeArc));
                        var flowToPush = Min(excess, residual);
                        
                        if (flowToPush.CompareTo(default(TFlowSumType)) > 0)
                        {
                            PushFlow(flowToPush, head, oppositeArc);
                            _logger.Log($"      Stole {flowToPush} flow from node {head}");
                        }
                        
                        // If arc became saturated, skip adding head
                        if (GetResidualCapacity(oppositeArc).CompareTo(default(TArcFlowType)) <= 0)
                            continue;
                    }
                    
                    // Add head to BFS queue
                    _nodePotential[head] = candidateDistance;
                    _nodeInBfsQueue[head] = true;
                    _bfsQueue.Add(head);
                    _logger.Log($"      Added node {head} to BFS with height {candidateDistance}");
                }
            }
        }
        
        // Set unreached nodes to high potential
        for (int node = 0; node < numNodes; node++)
        {
            if (!_nodeInBfsQueue[node])
            {
                _nodePotential[node] = 2 * numNodes - 1;
            }
        }
        
        // Reset the active nodes. Doing it like this pushes the nodes in increasing
        // order of height. Note that bfs_queue[0] is the sink so we skip it.
        for (int i = 1; i < _bfsQueue.Count; i++)
        {
            int node = _bfsQueue[i];
            if (_nodeExcess[node].CompareTo(default(TFlowSumType)) > 0)
            {
                _activeNodeByHeight.Push(node, _nodePotential[node]);
            }
        }
        
        // Log final potentials
        _logger.Log("GlobalUpdate: Final potentials:");
        for (int node = 0; node < numNodes; node++)
        {
            _logger.Log($"  Node {node}: potential={_nodePotential[node]}, excess={_nodeExcess[node]}");
        }
        _logger.Log($"GlobalUpdate: Done, active nodes={_activeNodeByHeight.Count}");
    }

    private void PushFlowExcessBackToSource()
    {
        // This is a simplified version - in production, implement Tarjan's cycle cancellation
        // For now, just push excess back greedily
        
        _logger.Log("PushFlowExcessBackToSource: Starting");
        
        var processed = new bool[_graph.NumNodes];
        var toProcess = new List<int>();
        
        // Find nodes with excess
        for (int node = 0; node < _graph.NumNodes; node++)
        {
            if (node != _sourceNodeIndex && node != _sinkNodeIndex && 
                _nodeExcess[node].CompareTo(default(TFlowSumType)) > 0)
            {
                toProcess.Add(node);
                _logger.Log($"  Node {node} has excess {_nodeExcess[node]}");
            }
        }
        
        // Process in reverse topological order (approximation)
        toProcess.Sort((a, b) => _nodePotential[b].CompareTo(_nodePotential[a]));
        
        _logger.Log($"  Processing {toProcess.Count} nodes with excess");
        
        foreach (var node in toProcess)
        {
            _logger.Log($"  Processing node {node} with excess {_nodeExcess[node]} and potential {_nodePotential[node]}");
            
            // Debug: Show all arcs from this node
            var allArcs = _graph.OutgoingOrOppositeIncomingArcs(node).ToList();
            _logger.Log($"    Node {node} has {allArcs.Count} arcs: {string.Join(", ", allArcs)}");
            
            while (_nodeExcess[node].CompareTo(default(TFlowSumType)) > 0)
            {
                bool pushed = false;
                
                foreach (var arc in _graph.OutgoingOrOppositeIncomingArcs(node))
                {
                    int oppositeArc = _graph.OppositeArc(arc);
                    var oppositeResidual = GetResidualCapacity(oppositeArc);
                    
                    _logger.Log($"    Checking arc {arc}: opposite arc={oppositeArc}");
                    _logger.Log($"      Arc residual={GetResidualCapacity(arc)}, opposite residual={oppositeResidual}");
                    
                    if (oppositeResidual.CompareTo(default(TArcFlowType)) > 0)
                    {
                        int head = _graph.Head(arc);
                        _logger.Log($"    Arc {arc} to node {head}: opposite residual={oppositeResidual}, head potential={_nodePotential[head]}");
                        
                        if (_nodePotential[head] < _nodePotential[node] || head == _sourceNodeIndex)
                        {
                            var flowPushed = PushAsMuchFlowAsPossible(node, arc);
                            _logger.Log($"      Pushed {flowPushed} flow");
                            pushed = true;
                            
                            if (_nodeExcess[node].CompareTo(default(TFlowSumType)) <= 0)
                                break;
                        }
                    }
                }
                
                if (!pushed)
                {
                    _logger.Log($"    WARNING: Cannot push excess from node {node} - node may be disconnected from source");
                    // If we can't push excess back, the node is disconnected from source
                    // This is valid - just break and continue
                    break;
                }
            }
        }
        
        _logger.Log("PushFlowExcessBackToSource: Done");
    }

    #endregion

    #region Private Methods - Flow Operations

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushFlow(TFlowSumType flow, int tail, int arc)
    {
        int head = _graph.Head(arc);
        int oppositeArc = _graph.OppositeArc(arc);
        
        // Update residual capacities
        DecreaseResidualCapacity(arc, TArcFlowType.CreateChecked(flow));
        IncreaseResidualCapacity(oppositeArc, TArcFlowType.CreateChecked(flow));
        
        // Update excesses
        _nodeExcess[tail] = _nodeExcess[tail] - flow;
        _nodeExcess[head] = _nodeExcess[head] + flow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TFlowSumType PushAsMuchFlowAsPossible(int tail, int arc)
    {
        var excess = _nodeExcess[tail];
        var residual = TFlowSumType.CreateChecked(GetResidualCapacity(arc));
        var flow = Min(excess, residual);
        
        if (flow.CompareTo(default(TFlowSumType)) > 0)
        {
            PushFlow(flow, tail, arc);
        }
        
        return flow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAdmissible(int tail, int arc)
    {
        if (GetResidualCapacity(arc).CompareTo(default(TArcFlowType)) <= 0)
            return false;
            
        int head = _graph.Head(arc);
        return _nodePotential[tail] == _nodePotential[head] + 1;
    }

    #endregion

    #region Private Methods - Capacity Management

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TArcFlowType GetResidualCapacity(int arc)
    {
        if (_graph.HasNegativeReverseArcs)
        {
            return _residualArcCapacityNegative![arc];
        }
        else
        {
            return _residualArcCapacityStandard![arc];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecreaseResidualCapacity(int arc, TArcFlowType amount)
    {
        if (_graph.HasNegativeReverseArcs)
        {
            _residualArcCapacityNegative![arc] = _residualArcCapacityNegative[arc] - amount;
        }
        else
        {
            _residualArcCapacityStandard![arc] = _residualArcCapacityStandard[arc] - amount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncreaseResidualCapacity(int arc, TArcFlowType amount)
    {
        if (_graph.HasNegativeReverseArcs)
        {
            _residualArcCapacityNegative![arc] = _residualArcCapacityNegative[arc] + amount;
        }
        else
        {
            _residualArcCapacityStandard![arc] = _residualArcCapacityStandard[arc] + amount;
        }
    }

    #endregion

    #region Private Methods - Arithmetic Helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Min<T>(T a, T b) where T : unmanaged, IComparable<T>
    {
        return a.CompareTo(b) <= 0 ? a : b;
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Solution status codes.
    /// </summary>
    public enum Status
    {
        /// <summary>Algorithm not run or problem modified.</summary>
        NOT_SOLVED,
        
        /// <summary>Optimal solution found.</summary>
        OPTIMAL,
        
        /// <summary>Flow exceeds maximum representable value.</summary>
        INT_OVERFLOW
    }

    #endregion
}