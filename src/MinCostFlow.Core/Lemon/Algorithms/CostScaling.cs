using System;
using System.Collections.Generic;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon.Algorithms;

/// <summary>
/// Implementation of the Cost Scaling algorithm for finding minimum cost flows.
/// This is a push-relabel based algorithm with polynomial time complexity.
/// Based on LEMON's highly optimized implementation.
/// </summary>
public sealed class CostScaling(IGraph graph) : IMinCostFlowSolver
{
    /// <summary>
    /// Internal method for the cost scaling algorithm.
    /// </summary>
    public enum Method
    {
        /// <summary>
        /// Local push operations - flow is moved only on one admissible arc at once.
        /// </summary>
        Push,
        
        /// <summary>
        /// Augment operations - flow is moved on admissible paths from excess to deficit nodes.
        /// </summary>
        Augment,
        
        /// <summary>
        /// Partial augment operations - limited length paths (most efficient).
        /// </summary>
        PartialAugment
    }
    
    // Constants
    private const int MAX_PARTIAL_PATH_LENGTH = 4;
    private const int PRICE_REFINEMENT_LIMIT = 2;
    private const double GLOBAL_UPDATE_FACTOR = 2.0;
    
    // Graph reference
    private readonly IGraph _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    private readonly int _nodeCount = graph.NodeCount;
    private readonly int _arcCount = graph.ArcCount;
    private int _resNodeCount;  // Residual graph node count (includes artificial root)
    private int _resArcCount;   // Residual graph arc count
    private int _root;          // Artificial root node
    
    // Problem parameters
    private bool _hasLower = false;
    private long _sumSupply = 0;
    private int _supNodeCount = 0;  // Number of supply nodes
    
    // Node and arc data for residual network
    private int[] _firstOut;      // First outgoing arc for each node
    private bool[] _forward;      // Direction of arc (forward/backward)
    private int[] _source;        // Source node of each arc
    private int[] _target;        // Target node of each arc
    private int[] _reverse;       // Reverse arc index
    
    // Original problem data
    private long[] _lower;        // Lower bounds
    private long[] _upper;        // Upper bounds (capacity)
    private long[] _cost;         // Arc costs
    private long[] _supply;       // Node supplies
    
    // Algorithm state
    private long[] _resCap;       // Residual capacities
    private long[] _resCost;      // Residual arc costs
    private long[] _pi;           // Node potentials (dual variables)
    private long[] _excess;       // Node excess
    private int[] _nextOut;       // Next outgoing arc to check
    private readonly Queue<int> _activeNodes = new();  // Active nodes (with positive excess)
    
    // Arc mapping from residual to original
    private int[] _arcRef;        // Maps residual arc to original arc (-1 for artificial)
    
    // Scaling parameters
    private long _epsilon;        // Current scaling factor
    private int _alpha;           // Scaling parameter (typically 4-16)
    private int epsPhaseCount = 0;  // Track epsilon phases for initialization
    private long _costScaling;    // Cost scaling factor (n+1)

    public SolverStatus Status { get; private set; } = SolverStatus.NotSolved;

    /// <summary>
    /// Sets the supply value for a node.
    /// </summary>
    public CostScaling SetNodeSupply(Node node, long supply)
    {
        if (!_graph.IsValidNode(node))
        {
            throw new ArgumentException("Invalid node", nameof(node));
        }

        _supply ??= new long[_nodeCount];

        _supply[node.Id] = supply;
        return this;
    }
    
    /// <summary>
    /// Sets the bounds for an arc.
    /// </summary>
    public CostScaling SetArcBounds(Arc arc, long lower, long upper)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        if (lower > upper)
        {
            throw new ArgumentException("Lower bound cannot exceed upper bound");
        }

        if (_lower == null)
        {
            _lower = new long[_arcCount];
            _upper = new long[_arcCount];
            // Initialize with default bounds
            for (int i = 0; i < _arcCount; i++)
            {
                _upper[i] = long.MaxValue;
            }
        }
        
        _lower[arc.Id] = lower;
        _upper[arc.Id] = upper;
        
        if (lower != 0)
        {
            _hasLower = true;
        }

        return this;
    }
    
    /// <summary>
    /// Sets the cost for an arc.
    /// </summary>
    public CostScaling SetArcCost(Arc arc, long cost)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        _cost ??= new long[_arcCount];

        _cost[arc.Id] = cost;
        return this;
    }
    
    /// <summary>
    /// Runs the algorithm with the specified method.
    /// </summary>
    public SolverStatus Solve(Method method = Method.PartialAugment, int scalingFactor = 8)
    {
        _alpha = scalingFactor;
        
        // Initialize the algorithm
        if (!Initialize())
        {
            Status = SolverStatus.Infeasible;
            return Status;
        }
        
        // Build the residual network
        BuildResidualNetwork();
        
        // Initialize node potentials
        InitializePotentials();
        
        // Main algorithm
        Start(method);
        
        // Debug: Print final residual capacities
        /*Console.WriteLine("\nFinal residual network:");
        for (int a = 0; a < _resArcCount; a++)
        {
            Console.WriteLine($"Arc {a}: {_source[a]}->{_target[a]}, forward={_forward[a]}, resCap={_resCap[a]}, arcRef={_arcRef[a]}");
        }*/
        
        Status = SolverStatus.Optimal;
        return Status;
    }
    
    public SolverStatus Solve()
    {
        return Solve(Method.PartialAugment, 8);
    }
    
    /// <summary>
    /// Gets the flow value on an arc.
    /// </summary>
    public long GetFlow(Arc arc)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        if (Status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not available");
        }

        // In the residual network, we need to find the backward arc for this original arc
        // and the flow = backward_arc_residual_capacity + lower_bound

        int arcId = arc.Id;
        int u = _graph.Source(arc).Id;
        int v = _graph.Target(arc).Id;
        
        // Find the backward arc in the residual network
        // The backward arc goes from v to u and has the same arcRef
        for (int a = _firstOut[v]; a < _firstOut[v + 1]; a++)
        {
            if (!_forward[a] && _target[a] == u && _arcRef[a] == arcId)
            {
                // Flow = lower_bound + (backward_residual_capacity - lower_bound)
                // = backward_residual_capacity
                return _resCap[a];
            }
        }
        
        // If no backward arc found, flow equals lower bound
        // This should not happen in a properly constructed residual network
        return _lower[arcId];
    }
    
    /// <summary>
    /// Gets the potential (dual variable) of a node.
    /// </summary>
    public long GetPotential(Node node)
    {
        if (!_graph.IsValidNode(node))
        {
            throw new ArgumentException("Invalid node", nameof(node));
        }

        if (Status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not available");
        }

        // Potentials need to be unscaled
        return _pi[node.Id] / _costScaling;
    }
    
    /// <summary>
    /// Gets the total cost of the solution.
    /// </summary>
    public long GetTotalCost()
    {
        if (Status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not available");
        }

        long totalCost = 0;
        for (int i = 0; i < _arcCount; i++)
        {
            var arc = new Arc(i);
            long flow = GetFlow(arc);
            totalCost += flow * _cost[i];
        }
        
        return totalCost;
    }
    
    private bool Initialize()
    {
        // Initialize arrays if not already done
        _supply ??= new long[_nodeCount];

        _cost ??= new long[_arcCount];

        if (_lower == null)
        {
            _lower = new long[_arcCount];
            _upper = new long[_arcCount];
            for (int i = 0; i < _arcCount; i++)
            {
                _upper[i] = long.MaxValue;
            }
        }
        
        // Check supply balance and count supply nodes
        _sumSupply = 0;
        _supNodeCount = 0;
        for (int i = 0; i < _nodeCount; i++)
        {
            _sumSupply += _supply[i];
            if (_supply[i] > 0)
            {
                _supNodeCount++;
            }
        }
        
        // Check feasibility
        if (_sumSupply > 0)
        {
            return false;  // Infeasible - more supply than demand
        }

        // Check bounds
        for (int i = 0; i < _arcCount; i++)
        {
            if (_lower[i] > _upper[i])
            {
                return false;  // Infeasible bounds
            }
        }
        
        // Apply cost scaling by (n+1) to ensure integer optimality
        _costScaling = _nodeCount + 1;
        
        return true;
    }
    
    private void FindInitialFlow()
    {
        // Simple algorithm to find initial feasible flow
        // For now, just handle the case without lower bounds
        if (!_hasLower)
        {
            // Initialize all flows to 0 (already done by default)
            return;
        }
        
        // With lower bounds, we need to satisfy them
        // This would require a more complex initialization
        throw new NotImplementedException("Lower bounds not yet supported in CostScaling");
    }
    
    private void BuildResidualNetwork()
    {
        // Residual network has an artificial root node
        _root = _nodeCount;
        _resNodeCount = _nodeCount + 1;
        
        // Count residual arcs (forward + backward + arcs to/from root)
        _resArcCount = 2 * _arcCount;
        int supplyArcCount = 0;
        for (int i = 0; i < _nodeCount; i++)
        {
            if (_supply[i] != 0)
            {
                supplyArcCount++;
            }
        }
        _resArcCount += supplyArcCount;
        
        // Allocate arrays
        _firstOut = new int[_resNodeCount + 1];
        _forward = new bool[_resArcCount];
        _source = new int[_resArcCount];
        _target = new int[_resArcCount];
        _reverse = new int[_resArcCount];
        _resCap = new long[_resArcCount];
        _resCost = new long[_resArcCount];
        _arcRef = new int[_resArcCount];
        
        // Build temporary arc lists for counting
        var outArcs = new List<int>[_resNodeCount];
        for (int i = 0; i < _resNodeCount; i++)
        {
            outArcs[i] = [];
        }

        int arcIndex = 0;
        
        // Add forward and backward arcs for each original arc
        for (int i = 0; i < _arcCount; i++)
        {
            var arc = new Arc(i);
            int u = _graph.Source(arc).Id;
            int v = _graph.Target(arc).Id;
            
            // Forward arc
            int fwdArc = arcIndex;
            _source[arcIndex] = u;
            _target[arcIndex] = v;
            _forward[arcIndex] = true;
            _resCap[arcIndex] = _upper[i] - _lower[i];
            _resCost[arcIndex] = _cost[i] * _costScaling;  // Apply cost scaling
            _arcRef[arcIndex] = i;
            outArcs[u].Add(arcIndex);
            arcIndex++;
            
            // Backward arc
            int bwdArc = arcIndex;
            _source[arcIndex] = v;
            _target[arcIndex] = u;
            _forward[arcIndex] = false;
            _resCap[arcIndex] = _lower[i];  // Initial flow equals lower bound
            _resCost[arcIndex] = -_cost[i] * _costScaling;  // Apply cost scaling
            _arcRef[arcIndex] = i;
            outArcs[v].Add(arcIndex);
            arcIndex++;
            
            // Set reverse pointers
            _reverse[fwdArc] = bwdArc;
            _reverse[bwdArc] = fwdArc;
        }
        
        // Add supply/demand arcs
        for (int u = 0; u < _nodeCount; u++)
        {
            if (_supply[u] > 0)
            {
                // Arc from root to supply node
                _source[arcIndex] = _root;
                _target[arcIndex] = u;
                _forward[arcIndex] = true;
                _resCap[arcIndex] = _supply[u];
                _resCost[arcIndex] = 0;
                _arcRef[arcIndex] = -1;
                _reverse[arcIndex] = -1;  // No reverse for artificial arcs
                outArcs[_root].Add(arcIndex);
                arcIndex++;
            }
            else if (_supply[u] < 0)
            {
                // Arc from demand node to root
                _source[arcIndex] = u;
                _target[arcIndex] = _root;
                _forward[arcIndex] = true;
                _resCap[arcIndex] = -_supply[u];
                _resCost[arcIndex] = 0;
                _arcRef[arcIndex] = -1;
                _reverse[arcIndex] = -1;  // No reverse for artificial arcs
                outArcs[u].Add(arcIndex);
                arcIndex++;
            }
        }
        
        // Build firstOut array from temporary lists
        // First, we need to reorder the arcs properly
        var tempSource = new int[_resArcCount];
        var tempTarget = new int[_resArcCount];
        var tempForward = new bool[_resArcCount];
        var tempResCap = new long[_resArcCount];
        var tempResCost = new long[_resArcCount];
        var tempArcRef = new int[_resArcCount];
        var tempReverse = new int[_resArcCount];
        
        // Copy existing data to temp arrays
        Array.Copy(_source, tempSource, _resArcCount);
        Array.Copy(_target, tempTarget, _resArcCount);
        Array.Copy(_forward, tempForward, _resArcCount);
        Array.Copy(_resCap, tempResCap, _resArcCount);
        Array.Copy(_resCost, tempResCost, _resArcCount);
        Array.Copy(_arcRef, tempArcRef, _resArcCount);
        Array.Copy(_reverse, tempReverse, _resArcCount);
        
        // Rebuild in proper order
        int currentPos = 0;
        var arcMapping = new int[_resArcCount]; // Maps old position to new position
        
        for (int u = 0; u < _resNodeCount; u++)
        {
            _firstOut[u] = currentPos;
            
            foreach (int oldArcPos in outArcs[u])
            {
                // Copy arc data to new position
                _source[currentPos] = tempSource[oldArcPos];
                _target[currentPos] = tempTarget[oldArcPos];
                _forward[currentPos] = tempForward[oldArcPos];
                _resCap[currentPos] = tempResCap[oldArcPos];
                _resCost[currentPos] = tempResCost[oldArcPos];
                _arcRef[currentPos] = tempArcRef[oldArcPos];
                
                // Track mapping for reverse arc updates
                arcMapping[oldArcPos] = currentPos;
                currentPos++;
            }
        }
        _firstOut[_resNodeCount] = currentPos;
        
        // Update reverse arc pointers
        for (int a = 0; a < _resArcCount; a++)
        {
            if (tempReverse[a] >= 0)
            {
                _reverse[arcMapping[a]] = arcMapping[tempReverse[a]];
            }
            else
            {
                _reverse[arcMapping[a]] = -1;
            }
        }
        
        // Debug: Print residual network structure
        /*Console.WriteLine("Initial residual network structure:");
        for (int a = 0; a < _resArcCount; a++)
        {
            Console.WriteLine($"Arc {a}: {_source[a]}->{_target[a]}, forward={_forward[a]}, resCap={_resCap[a]}, arcRef={_arcRef[a]}, reverse={_reverse[a]}");
        }*/
        
        // Adjust for lower bounds on supplies
        if (_hasLower)
        {
            // Create adjusted excess values
            _excess = new long[_resNodeCount];
            for (int i = 0; i < _nodeCount; i++)
            {
                _excess[i] = _supply[i];
            }
            
            // Adjust for lower bounds
            for (int i = 0; i < _arcCount; i++)
            {
                if (_lower[i] != 0)
                {
                    var arc = new Arc(i);
                    int u = _graph.Source(arc).Id;
                    int v = _graph.Target(arc).Id;
                    _excess[u] -= _lower[i];
                    _excess[v] += _lower[i];
                }
            }
        }
        else
        {
            _excess = new long[_resNodeCount];
            // Don't copy supplies - in the residual network, supplies are handled via root arcs
        }
        
        // Initialize algorithm arrays
        _pi = new long[_resNodeCount];
        _nextOut = new int[_resNodeCount];
    }
    
    
    private void InitializePotentials()
    {
        // Simple initialization - set all potentials to 0
        // More sophisticated initialization could use shortest paths
        Array.Clear(_pi, 0, _pi.Length);
        
        // Initialize epsilon to maximum SCALED cost in the residual network
        // OR-Tools pattern: epsilon = max(|scaled_cost|) where scaled_cost = cost * (n+1)
        _epsilon = 1; // Start with 1 as minimum
        for (int a = 0; a < _resArcCount; a++)
        {
            _epsilon = Math.Max(_epsilon, Math.Abs(_resCost[a]));
        }
    }
    
    private void Start(Method method)
    {
        switch (method)
        {
            case Method.Push:
                StartPush();
                break;
            case Method.Augment:
                StartAugment(_resNodeCount - 1);
                break;
            case Method.PartialAugment:
                StartAugment(MAX_PARTIAL_PATH_LENGTH);
                break;
        }
        
        // Debug: Print epsilon phases
        
        // Note: With proper cost scaling and epsilon termination at 1,
        // the algorithm achieves exact optimality without needing a cleanup phase
    }
    
    private void StartPush()
    {
        int globalUpdateSkip = (int)(GLOBAL_UPDATE_FACTOR * (_resNodeCount + _supNodeCount * _supNodeCount));
        int nextGlobalUpdateLimit = globalUpdateSkip;
        int relabelCount = 0;
        
        // Hyper node tracking for push-look-ahead heuristic
        bool[] hyper = new bool[_resNodeCount];
        long[] hyperCost = new long[_resNodeCount];
        
        // Perform cost scaling phases
        // OR-Tools pattern: divide epsilon first, then refine
        do
        {
            // Divide epsilon by alpha at the start of each iteration (as OR-Tools does)
            _epsilon = Math.Max(_epsilon / _alpha, 1);
            
            epsPhaseCount++;
            
            // Debug output for tracking progress (disabled)
            // if (epsPhaseCount % 5 == 1)
            // {
            //     Console.WriteLine($"CostScaling: Phase {epsPhaseCount}, epsilon = {_epsilon}");
            // }
            
            // Prevent infinite loops
            if (epsPhaseCount > 100)
            {
                throw new InvalidOperationException($"Too many epsilon phases: {epsPhaseCount}, epsilon: {_epsilon}");
            }
            
            // Price refinement heuristic
            if (epsPhaseCount >= PRICE_REFINEMENT_LIMIT)
            {
                if (PriceRefinement())
                {
                    continue;
                }
            }
            
            // Initialize current phase
            InitPhase();
            
            // Main push-relabel loop
            int iterCount = 0;
            int lastActiveCount = 0;
            while (_activeNodes.Count > 0)
            {
                iterCount++;
                
                // Debug progress for large problems (disabled)
                // if (iterCount % 100000 == 0 && _nodeCount > 1000)
                // {
                //     Console.WriteLine($"  Iteration {iterCount}: active nodes = {_activeNodes.Count}");
                // }
                
                if (iterCount > 10000000)  // Increase limit for large problems
                {
                    throw new InvalidOperationException($"Too many iterations in phase {epsPhaseCount}");
                }
                
                // Select an active node (FIFO selection)
                int n = _activeNodes.Peek();
                int lastOut = _firstOut[n + 1];
                long piN = _pi[n];
                
                // Perform push operations if there are admissible arcs
                if (_excess[n] > 0)
                {
                    for (int a = _nextOut[n]; a < lastOut; a++)
                    {
                        if (_resCap[a] > 0)
                        {
                            long rc = GetReducedCost(a, piN);
                            if (rc <= -_epsilon)
                            {
                                int t = _target[a];
                                long delta = Math.Min(_resCap[a], _excess[n]);
                                
                                // Push-look-ahead heuristic
                                long ahead = -_excess[t];
                                int lastOutT = _firstOut[t + 1];
                                long piT = _pi[t];
                                
                                for (int ta = _nextOut[t]; ta < lastOutT; ta++)
                                {
                                    if (_resCap[ta] > 0 && GetReducedCost(ta, piT) < -_epsilon)
                                    {
                                        ahead += _resCap[ta];
                                        if (ahead >= delta)
                                        {
                                            break;
                                        }
                                    }
                                }
                                
                                if (ahead < 0)
                                {
                                    ahead = 0;
                                }

                                // Push flow along the arc
                                if (ahead < delta && ahead > 0 && !hyper[t])
                                {
                                    // Push only 'ahead' amount and make node hyper
                                    Push(a, ahead);
                                    if (_excess[t] > 0)
                                    {
                                        _activeNodes.Enqueue(t);
                                        hyper[t] = true;
                                        hyperCost[t] = rc;
                                    }
                                    _nextOut[n] = a;
                                    break;  // Go to next active node
                                }
                                else
                                {
                                    // Regular push
                                    bool wasActive = _excess[t] > 0;
                                    Push(a, delta);
                                    if (!wasActive && _excess[t] > 0)
                                    {
                                        _activeNodes.Enqueue(t);
                                    }
                                }
                                
                                if (_excess[n] == 0)
                                {
                                    _nextOut[n] = a;
                                    goto removeNodes;
                                }
                            }
                        }
                    }
                    _nextOut[n] = lastOut;
                }
                
                // Relabel the node if it is still active (or hyper)
                if (_excess[n] > 0 || hyper[n])
                {
                    Relabel(n, hyper[n], hyperCost[n]);
                    hyper[n] = false;
                    relabelCount++;
                }
                
                removeNodes:
                // Remove nodes that are not active nor hyper
                while (_activeNodes.Count > 0 && 
                       _excess[_activeNodes.Peek()] <= 0 && 
                       !hyper[_activeNodes.Peek()])
                {
                    _activeNodes.Dequeue();
                }
                
                // Global update heuristic
                if (relabelCount >= nextGlobalUpdateLimit)
                {
                    GlobalUpdate();
                    Array.Clear(hyper, 0, hyper.Length);
                    nextGlobalUpdateLimit += globalUpdateSkip;
                }
            }
            
            // Continue until epsilon = 1 (matching OR-Tools termination)
        } while (_epsilon != 1);
    }
    
    private long GetReducedCost(int arc, long piSource)
    {
        // Reduced cost = cost + pi[source] - pi[target]
        return _resCost[arc] + piSource - _pi[_target[arc]];
    }
    
    private void Push(int arc, long delta)
    {
        _resCap[arc] -= delta;
        if (_reverse[arc] >= 0)
        {
            _resCap[_reverse[arc]] += delta;
        }
        _excess[_source[arc]] -= delta;
        _excess[_target[arc]] += delta;
    }
    
    private void Relabel(int node, bool isHyper, long hyperCost)
    {
        long minRedCost = isHyper ? -hyperCost : long.MaxValue;
        int firstOut = _firstOut[node];
        int lastOut = _firstOut[node + 1];
        long piNode = _pi[node];
        
        // Find minimum reduced cost among admissible arcs
        for (int a = firstOut; a < lastOut; a++)
        {
            if (_resCap[a] > 0)
            {
                long rc = GetReducedCost(a, piNode);
                if (rc < minRedCost)
                {
                    minRedCost = rc;
                }
            }
        }
        
        // Update potential
        _pi[node] -= minRedCost + _epsilon;
        _nextOut[node] = firstOut;
    }
    
    private void InitPhase()
    {
        // Initialize excess from root node arcs
        if (epsPhaseCount == 1)
        {
            // First phase - push all flow from root
            for (int a = _firstOut[_root]; a < _firstOut[_root + 1]; a++)
            {
                if (_resCap[a] > 0)
                {
                    Push(a, _resCap[a]);
                }
            }
        }
        
        // Saturate arcs not satisfying the optimality condition
        for (int u = 0; u < _resNodeCount; u++)
        {
            int lastOut = _firstOut[u + 1];
            long piU = _pi[u];
            
            for (int a = _firstOut[u]; a < lastOut; a++)
            {
                long delta = _resCap[a];
                if (delta > 0)
                {
                    long rc = GetReducedCost(a, piU);
                    if (rc <= -_epsilon)
                    {
                        Push(a, delta);
                    }
                }
            }
        }
        
        // Find active nodes (nodes with positive excess)
        _activeNodes.Clear();
        int activeCount = 0;
        for (int u = 0; u < _resNodeCount; u++)
        {
            if (_excess[u] > 0 && u != _root)  // Root should not be active
            {
                _activeNodes.Enqueue(u);
                activeCount++;
            }
        }
        
        // Debug for large problems (disabled)
        // if (_nodeCount > 1000 && epsPhaseCount == 1)
        // {
        //     Console.WriteLine($"  Initial active nodes: {activeCount}");
        // }
        
        // Initialize next arcs
        for (int u = 0; u < _resNodeCount; u++)
        {
            _nextOut[u] = _firstOut[u];
        }
    }
    
    private static bool PriceRefinement()
    {
        // Simplified version - always return false for now
        // Full implementation would involve topological sorting and bucket-based refinement
        return false;
    }
    
    private void GlobalUpdate()
    {
        // Simplified version - recompute potentials using shortest paths
        // Full implementation would use Bellman-Ford or similar
        // For now, just reset next_out pointers
        for (int u = 0; u < _resNodeCount; u++)
        {
            _nextOut[u] = _firstOut[u];
        }
    }
    
    private void StartAugment(int maxPathLength)
    {
        const int PRICE_REFINEMENT_LIMIT = 2;
        const double GLOBAL_UPDATE_FACTOR = 1.0;
        int globalUpdateSkip = (int)(GLOBAL_UPDATE_FACTOR * (_resNodeCount + _supNodeCount * _supNodeCount));
        int nextGlobalUpdateLimit = globalUpdateSkip;
        
        // Path data structures
        List<int> path = [];
        bool[] pathArc = new bool[_resArcCount];
        int relabelCount = 0;
        
        // Perform cost scaling phases
        // OR-Tools pattern: divide epsilon first, then refine
        do
        {
            // Divide epsilon by alpha at the start of each iteration (as OR-Tools does)
            _epsilon = Math.Max(_epsilon / _alpha, 1);
            
            epsPhaseCount++;
            
            // Debug output for tracking progress (disabled)
            // if (epsPhaseCount % 5 == 1)
            // {
            //     Console.WriteLine($"CostScaling: Phase {epsPhaseCount}, epsilon = {_epsilon}");
            // }
            
            // Prevent infinite loops
            if (epsPhaseCount > 100)
            {
                throw new InvalidOperationException($"Too many epsilon phases: {epsPhaseCount}, epsilon: {_epsilon}");
            }
            
            // Price refinement heuristic
            if (epsPhaseCount >= PRICE_REFINEMENT_LIMIT)
            {
                if (PriceRefinement())
                {
                    continue;
                }
            }
            
            // Initialize current phase
            InitPhase();
            
            // Perform partial augment and relabel operations
            int iterCount = 0;
            while (true)
            {
                iterCount++;
                
                // Debug progress for large problems (disabled)
                // if (iterCount % 100000 == 0 && _nodeCount > 1000)
                // {
                //     Console.WriteLine($"  Augment iteration {iterCount}: active nodes = {_activeNodes.Count}");
                // }
                
                if (iterCount > 10000000)
                {
                    throw new InvalidOperationException($"Too many augment iterations in phase {epsPhaseCount}");
                }
                
                // Select an active node (FIFO selection)
                while (_activeNodes.Count > 0 && _excess[_activeNodes.Peek()] <= 0)
                {
                    _activeNodes.Dequeue();
                }
                
                if (_activeNodes.Count == 0)
                {
                    break;
                }

                int start = _activeNodes.Peek();
                
                // Find an augmenting path from the start node
                int tip = start;
                while (path.Count < maxPathLength && _excess[tip] >= 0)
                {
                    int u;
                    long minRedCost = long.MaxValue;
                    long piTip = _pi[tip];
                    int lastOut = _firstOut[tip + 1];
                    
                    // Look for admissible arc
                    for (int a = _nextOut[tip]; a < lastOut; a++)
                    {
                        if (_resCap[a] > 0)
                        {
                            u = _target[a];
                            long rc = _resCost[a] + piTip - _pi[u];
                            if (rc <= -_epsilon)
                            {
                                path.Add(a);
                                _nextOut[tip] = a;
                                if (pathArc[a])
                                {
                                    // Cycle found, augment immediately
                                    goto augment;
                                }
                                tip = u;
                                pathArc[a] = true;
                                goto next_step;
                            }
                            else if (rc < minRedCost)
                            {
                                minRedCost = rc;
                            }
                        }
                    }
                    
                    // Relabel tip node
                    if (tip != start)
                    {
                        int ra = _reverse[path[^1]];
                        if (ra >= 0 && _resCap[ra] > 0)
                        {
                            long rc = _resCost[ra] + piTip - _pi[_target[ra]];
                            minRedCost = Math.Min(minRedCost, rc);
                        }
                    }
                    
                    // Check remaining arcs for minimum reduced cost
                    lastOut = _nextOut[tip];
                    for (int a = _firstOut[tip]; a < lastOut; a++)
                    {
                        if (_resCap[a] > 0)
                        {
                            long rc = _resCost[a] + piTip - _pi[_target[a]];
                            if (rc < minRedCost)
                            {
                                minRedCost = rc;
                            }
                        }
                    }
                    
                    _pi[tip] -= minRedCost + _epsilon;
                    _nextOut[tip] = _firstOut[tip];
                    relabelCount++;
                    
                    // Step back
                    if (tip != start)
                    {
                        int pa = path[^1];
                        pathArc[pa] = false;
                        tip = _source[pa];
                        path.RemoveAt(path.Count - 1);
                    }
                    
                    next_step: ;
                }
                
                // Augment along the found path
                augment:
                long delta;
                int v = start;
                for (int i = 0; i < path.Count; i++)
                {
                    int pa = path[i];
                    int u = v;
                    v = _target[pa];
                    pathArc[pa] = false;
                    delta = Math.Min(_resCap[pa], _excess[u]);
                    _resCap[pa] -= delta;
                    if (_reverse[pa] >= 0)
                    {
                        _resCap[_reverse[pa]] += delta;
                    }
                    _excess[u] -= delta;
                    _excess[v] += delta;
                    if (_excess[v] > 0 && _excess[v] <= delta)
                    {
                        _activeNodes.Enqueue(v);
                    }
                }
                path.Clear();
                
                // Global update heuristic
                if (relabelCount >= nextGlobalUpdateLimit)
                {
                    GlobalUpdate();
                    nextGlobalUpdateLimit += globalUpdateSkip;
                }
            }
            
            // Continue until epsilon = 1 (matching OR-Tools termination)
        } while (_epsilon != 1);
    }
}