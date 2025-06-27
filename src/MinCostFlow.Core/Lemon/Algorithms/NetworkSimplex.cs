using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MinCostFlow.Core.Analysis;
using MinCostFlow.Core.Lemon.Algorithms.Internal;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.DataStructures;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon.Algorithms;

/// <summary>
/// Implementation of the primal Network Simplex algorithm for finding minimum cost flows.
/// Based on LEMON's highly optimized implementation.
/// </summary>
public sealed class NetworkSimplex : IMinCostFlowSolver
{
    // Constants matching LEMON
    internal const int MIN_BLOCK_SIZE = 10;
    private const sbyte STATE_UPPER = SpanningTree.STATE_UPPER;
    private const sbyte STATE_TREE = SpanningTree.STATE_TREE;
    private const sbyte STATE_LOWER = SpanningTree.STATE_LOWER;
    private const sbyte DIR_DOWN = SpanningTree.DIR_DOWN;
    private const sbyte DIR_UP = SpanningTree.DIR_UP;

    // Graph reference
    private readonly IGraph _graph;
    private readonly int _nodeCount;
    private readonly int _arcCount;
    private int _allArcNum;
    private int _searchArcNum;
    
    // Optimization settings
    private bool _useOptimizedPivot = false;
    private Utils.SolverMemoryPool? _memoryPool;
    
    // Problem parameters
    private SupplyType _supplyType = SupplyType.Geq;
    private long _sumSupply;
    
    // Node and arc data (Structure of Arrays)
    private long[] _lower;      // Lower bounds
    private long[] _upper;      // Upper bounds (capacity)
    private long[] _cost;       // Arc costs
    private long[] _supply;     // Node supplies
    private long[] _flow;       // Current flow values
    private long[] _pi;         // Node potentials (dual variables)
    private long[] _origLower;  // Original lower bounds (before transformation)
    
    // Arc structure arrays
    private readonly ArcLists _arcLists;
    
    // Spanning tree structure
    private SpanningTree _tree;
    private int _root;
    
    // Temporary data for pivot operations
    private int _inArc;
    private int _join;
    private int _uIn, _vIn, _uOut, _vOut;
    private long _delta;
    
    // Reduced cost caching
    private long[]? _reducedCosts;
    private bool _reducedCostsDirty = true;
    
    // Adjacency lists for efficient reduced cost updates
    private int[][]? _outgoingArcs; // Arcs where node is source
    private int[][]? _incomingArcs; // Arcs where node is target
    
    // Optimization: track which nodes need reduced cost updates
    private bool[]? _dirtyNodes;
    private int[]? _dirtyNodeList;
    private int _dirtyNodeCount = 0;
    
    // Pivot rule
    private PivotRule _pivotRule = PivotRule.BlockSearch;
    private IFindEnteringArc? _enteringArcFinder;
    
    // Maximum values
    private readonly long MAX;
    private readonly long INF;
    private long ART_COST;
    
    // Solver state
    private SolverStatus _status = SolverStatus.NotSolved;
    
    // Optimization configuration
    private OptimizationConfig _optimizationConfig = new OptimizationConfig();
    private bool _useAutoConfiguration = true;
    private ProblemCharacteristics? _problemCharacteristics;
    
    // Performance metrics
    private SolverMetrics _metrics = new SolverMetrics();
    private readonly Stopwatch _totalTimer = new Stopwatch();
    private readonly Stopwatch _pivotTimer = new Stopwatch();
    private readonly Stopwatch _treeTimer = new Stopwatch();
    private readonly Stopwatch _potentialTimer = new Stopwatch();
    private long _arcsCheckedInCurrentPivot = 0;
    
    // High precision timing helpers
    private static readonly double TicksPerMicrosecond = Stopwatch.Frequency / 1_000_000.0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double TicksToMicroseconds(long ticks)
    {
        return ticks / TicksPerMicrosecond;
    }
    
    // Internal properties for optimized implementations
    internal int SearchArcNum => _searchArcNum;
    internal ArcLists ArcLists => _arcLists;
    internal SpanningTree Tree => _tree;
    internal OptimizationConfig OptimizationConfig => _optimizationConfig;
    
    /// <summary>
    /// Initializes a new instance of the NetworkSimplex algorithm.
    /// </summary>
    public NetworkSimplex(IGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _nodeCount = graph.NodeCount;
        _arcCount = graph.ArcCount;
        
        // Initialize constants
        MAX = long.MaxValue;
        INF = long.MaxValue / 2; // Avoid overflow in computations
        
        // Allocate arrays
        int maxArcNum = _arcCount + 2 * _nodeCount; // Space for artificial arcs
        
        _lower = new long[maxArcNum];
        _upper = new long[maxArcNum];
        _cost = new long[maxArcNum];
        _supply = new long[_nodeCount];
        _flow = new long[maxArcNum];
        _pi = new long[_nodeCount + 1]; // Add 1 for root node
        _origLower = new long[_arcCount]; // Only need for original arcs
        
        // Initialize arc lists
        _arcLists = new ArcLists(maxArcNum);
        
        // Initialize spanning tree (add 1 for root node)
        _tree = new SpanningTree(_nodeCount + 1, maxArcNum);
        
        // Copy graph structure
        InitializeGraphStructure();
    }
    
    /// <summary>
    /// Sets the lower and upper bounds for an arc.
    /// </summary>
    public NetworkSimplex SetArcBounds(Arc arc, long lower, long upper)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        _lower[arc.Id] = lower;
        _upper[arc.Id] = upper;
        _origLower[arc.Id] = lower; // Save original
        return this;
    }
    
    /// <summary>
    /// Sets the cost for an arc.
    /// </summary>
    public NetworkSimplex SetArcCost(Arc arc, long cost)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        _cost[arc.Id] = cost;
        return this;
    }
    
    /// <summary>
    /// Sets the supply value for a node.
    /// </summary>
    public NetworkSimplex SetNodeSupply(Node node, long supply)
    {
        if (!_graph.IsValidNode(node))
        {
            throw new ArgumentException("Invalid node", nameof(node));
        }

        _supply[node.Id] = supply;
        return this;
    }
    
    /// <summary>
    /// Sets the supply type (GEQ or LEQ).
    /// </summary>
    public NetworkSimplex SetSupplyType(SupplyType type)
    {
        _supplyType = type;
        return this;
    }
    
    /// <summary>
    /// Sets the pivot rule to use.
    /// </summary>
    public NetworkSimplex SetPivotRule(PivotRule rule)
    {
        _pivotRule = rule;
        return this;
    }
    
    /// <summary>
    /// Runs the Network Simplex algorithm.
    /// </summary>
    public SolverStatus Solve()
    {
        // Reset metrics
        _metrics = new SolverMetrics();
        _totalTimer.Restart();
        
        // Reset individual timer accumulations
        long pivotSearchTicks = 0;
        long treeUpdateTicks = 0;
        long potentialUpdateTicks = 0;
        
        // Check bounds
        if (!CheckBounds())
        {
            _status = SolverStatus.Infeasible;
            return _status;
        }
        
        // Transform to standard form
        TransformToStandardForm();
        
        // Analyze problem and auto-configure optimizations if enabled
        if (_useAutoConfiguration)
        {
            var characteristics = AnalyzeProblem();
            var autoConfig = OptimizationSelector.SelectConfiguration(characteristics);
            _optimizationConfig = autoConfig;
            
            // Log configuration choice if verbose logging is enabled
            if (Environment.GetEnvironmentVariable("MCF_VERBOSE") == "1")
            {
                Console.WriteLine($"Auto-configured optimizations for problem:");
                Console.WriteLine($"  {characteristics}");
                Console.WriteLine($"  Selected flags: {autoConfig.Flags}");
            }
        }
        
        // Initialize the algorithm
        if (!Initialize())
        {
            _status = SolverStatus.Infeasible;
            return _status;
        }
        
        // Create pivot rule finder
        _enteringArcFinder = CreatePivotRuleFinder();
        
        // Record initial block size if using block search
        if (_enteringArcFinder is BlockSearchPivot blockPivot)
        {
            _metrics.InitialBlockSize = blockPivot.BlockSize;
        }
        else if (_enteringArcFinder is CachedBlockSearchPivot cachedBlockPivot)
        {
            _metrics.InitialBlockSize = cachedBlockPivot.BlockSize;
        }
        
        // Main simplex loop
        int iterations = 0;
        
        // Calculate expected iterations for monitoring
        int expectedIterations = (int)(Math.Sqrt(_searchArcNum) * _nodeCount * 0.5);
        int warningThreshold = (int)(expectedIterations * 1.5);
        bool iterationWarningShown = false;
        
        long maxIterations = Math.Max(1000000L, (long)_nodeCount * _arcCount); // Allow more iterations for larger problems
        
        while (true)
        {
            // Time pivot search
            _pivotTimer.Restart();
            _arcsCheckedInCurrentPivot = 0;
            bool found = _enteringArcFinder.FindEnteringArc();
            _pivotTimer.Stop();
            pivotSearchTicks += _pivotTimer.ElapsedTicks;
            _metrics.TotalArcsChecked += _arcsCheckedInCurrentPivot;
            
            if (!found)
            {
                break;
            }

            iterations++;
            
            // Check for excessive iterations
            if (!iterationWarningShown && iterations > warningThreshold)
            {
                Console.WriteLine($"WARNING: Iteration count ({iterations:N0}) exceeds expected threshold ({warningThreshold:N0})");
                Console.WriteLine($"  Expected: ~{expectedIterations:N0}, Current: {iterations:N0} ({(double)iterations/expectedIterations:F1}x)");
                if ((_optimizationConfig.Flags & OptimizationFlags.AdaptiveBlockSize) != 0)
                {
                    Console.WriteLine("  Consider disabling AdaptiveBlockSize optimization");
                }
                iterationWarningShown = true;
            }
            
            if (iterations > maxIterations) // Safety check
            {
                // Log diagnostic information
                Console.WriteLine($"ERROR: Network Simplex hit iteration limit of {maxIterations:N0} (nodes: {_nodeCount}, arcs: {_arcCount})");
                _status = SolverStatus.Infeasible;
                return _status;
            }
            
            FindJoinNode();
            bool change = FindLeavingArc();
            if (!change && _delta == 0)
            {
                _status = SolverStatus.Unbounded;
                return _status;
            }
            ChangeFlow(change);
            if (change)
            {
                // Time tree structure update
                _treeTimer.Restart();
                UpdateTreeStructure();
                _treeTimer.Stop();
                treeUpdateTicks += _treeTimer.ElapsedTicks;
                
                // Time potential update
                _potentialTimer.Restart();
                UpdatePotentials();
                _potentialTimer.Stop();
                potentialUpdateTicks += _potentialTimer.ElapsedTicks;
            }
        }
        
        // Update metrics
        _metrics.Iterations = iterations;
        _metrics.BaselineIterations = expectedIterations;
        _metrics.IterationRatio = expectedIterations > 0 ? (double)iterations / expectedIterations : 1.0;
        
        if (_enteringArcFinder is BlockSearchPivot finalBlockPivot)
        {
            _metrics.FinalBlockSize = finalBlockPivot.BlockSize;
        }
        else if (_enteringArcFinder is CachedBlockSearchPivot finalCachedBlockPivot)
        {
            _metrics.FinalBlockSize = finalCachedBlockPivot.BlockSize;
        }
        _metrics.AverageArcsCheckedPerPivot = iterations > 0 ? 
            (double)_metrics.TotalArcsChecked / iterations : 0;
        
        // Check feasibility
        if (CheckFeasibility())
        {
            _status = SolverStatus.Optimal;
            
            // Transform the solution back to the original form if we had lower bounds
            bool hasLowerBounds = false;
            for (int i = 0; i < _arcCount; i++)
            {
                if (_origLower[i] != 0)
                {
                    hasLowerBounds = true;
                    break;
                }
            }
            
            if (hasLowerBounds)
            {
                // Add back the lower bounds to get actual flow values
                for (int i = 0; i < _arcCount; i++)
                {
                    if (_origLower[i] != 0)
                    {
                        _flow[i] += _origLower[i];
                        // Restore original supplies
                        _supply[_arcLists.Source[i]] += _origLower[i];
                        _supply[_arcLists.Target[i]] -= _origLower[i];
                    }
                }
            }
        }
        else
        {
            _status = SolverStatus.Infeasible;
        }
        
        // Complete metrics
        _totalTimer.Stop();
        _metrics.TotalSolveTimeMs = _totalTimer.ElapsedMilliseconds;
        _metrics.TotalSolveTimeMicros = TicksToMicroseconds(_totalTimer.ElapsedTicks);
        
        // Convert accumulated ticks to microseconds
        _metrics.PivotSearchTimeMicros = TicksToMicroseconds(pivotSearchTicks);
        _metrics.TreeUpdateTimeMicros = TicksToMicroseconds(treeUpdateTicks);
        _metrics.PotentialUpdateTimeMicros = TicksToMicroseconds(potentialUpdateTicks);
        
        // Also update millisecond values for compatibility
        _metrics.PivotSearchTimeMs = (long)(_metrics.PivotSearchTimeMicros / 1000.0);
        _metrics.TreeUpdateTimeMs = (long)(_metrics.TreeUpdateTimeMicros / 1000.0);
        _metrics.PotentialUpdateTimeMs = (long)(_metrics.PotentialUpdateTimeMicros / 1000.0);
        
        return _status;
    }
    
    /// <summary>
    /// Gets the flow value on an arc.
    /// </summary>
    public long GetFlow(Arc arc)
    {
        if (_status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not optimal");
        }

        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        return _flow[arc.Id];
    }
    
    /// <summary>
    /// Gets the potential (dual value) of a node.
    /// </summary>
    public long GetPotential(Node node)
    {
        if (_status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not optimal");
        }

        if (!_graph.IsValidNode(node))
        {
            throw new ArgumentException("Invalid node", nameof(node));
        }

        return _pi[node.Id];
    }
    
    /// <summary>
    /// Gets the total cost of the solution.
    /// </summary>
    public long GetTotalCost()
    {
        if (_status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException("Solution not optimal");
        }

        long totalCost = 0;
        for (int i = 0; i < _arcCount; i++)
        {
            totalCost += _flow[i] * _cost[i];
        }
        return totalCost;
    }
    
    /// <summary>
    /// Gets the current solver status.
    /// </summary>
    public SolverStatus Status => _status;
    
    /// <summary>
    /// Gets the supply type (GEQ or LEQ).
    /// </summary>
    public SupplyType SupplyType => _supplyType;
    
    /// <summary>
    /// Gets the supply value for a node.
    /// </summary>
    public long GetNodeSupply(Node node)
    {
        if (!_graph.IsValidNode(node))
        {
            throw new ArgumentException("Invalid node", nameof(node));
        }

        return _supply[node.Id];
    }
    
    /// <summary>
    /// Gets the cost of an arc.
    /// </summary>
    public long GetArcCost(Arc arc)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        return _cost[arc.Id];
    }
    
    /// <summary>
    /// Gets the lower bound of an arc.
    /// </summary>
    public long GetArcLowerBound(Arc arc)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        return _origLower[arc.Id];
    }
    
    /// <summary>
    /// Gets the upper bound of an arc.
    /// </summary>
    public long GetArcUpperBound(Arc arc)
    {
        if (!_graph.IsValidArc(arc))
        {
            throw new ArgumentException("Invalid arc", nameof(arc));
        }

        return _upper[arc.Id];
    }
    
    /// <summary>
    /// Enables optimized pivot rules using unsafe code.
    /// </summary>
    public void EnableOptimizedPivot(bool enable = true)
    {
        _useOptimizedPivot = enable;
    }
    
    
    /// <summary>
    /// Sets a memory pool for reducing allocations.
    /// </summary>
    public void SetMemoryPool(Utils.SolverMemoryPool? pool)
    {
        _memoryPool = pool;
    }
    
    /// <summary>
    /// Enables optimization flags.
    /// </summary>
    public void EnableOptimizations(OptimizationFlags flags)
    {
        _optimizationConfig.Flags = flags;
    }
    
    /// <summary>
    /// Sets the optimization configuration.
    /// </summary>
    public void SetOptimizationConfig(OptimizationConfig config)
    {
        _optimizationConfig = config ?? throw new ArgumentNullException(nameof(config));
        _useAutoConfiguration = false; // Disable auto-configuration when manually set
    }
    
    /// <summary>
    /// Enables or disables automatic optimization configuration based on problem characteristics.
    /// Default is true.
    /// </summary>
    public void SetAutoConfiguration(bool enable)
    {
        _useAutoConfiguration = enable;
    }
    
    /// <summary>
    /// Gets the problem characteristics from the last analysis.
    /// Returns null if Solve() has not been called yet.
    /// </summary>
    public ProblemCharacteristics? GetProblemCharacteristics()
    {
        return _problemCharacteristics;
    }
    
    /// <summary>
    /// Gets the solver metrics from the last solve.
    /// </summary>
    public SolverMetrics GetMetrics()
    {
        return _metrics;
    }
    
    /// <summary>
    /// Analyzes the problem and returns its characteristics.
    /// This is automatically called during Solve() if auto-configuration is enabled.
    /// </summary>
    public ProblemCharacteristics AnalyzeProblem()
    {
        if (_lower == null || _upper == null || _cost == null || _supply == null)
        {
            throw new InvalidOperationException("Problem data not initialized. Call SetCosts, SetUpperBounds, etc. first.");
        }
        
        _problemCharacteristics = ProblemAnalyzer.Analyze(_graph, _lower, _upper, _cost, _supply);
        return _problemCharacteristics;
    }
    
    
    private void InitializeGraphStructure()
    {
        // Copy arc structure from graph
        for (var arc = _graph.FirstArc(); arc.IsValid; arc = _graph.NextArc(arc))
        {
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            _arcLists.SetArc(arc.Id, source.Id, target.Id);
            
            // Default bounds and costs
            _lower[arc.Id] = 0;
            _upper[arc.Id] = INF;
            _cost[arc.Id] = 0;
        }
        
        // Default supplies
        Array.Fill(_supply, 0);
    }
    
    private bool CheckBounds()
    {
        for (int i = 0; i < _arcCount; i++)
        {
            if (_upper[i] < _lower[i])
            {
                return false;
            }
        }
        return true;
    }
    
    private void TransformToStandardForm()
    {
        // Handle lower bounds by transforming the problem
        for (int i = 0; i < _arcCount; i++)
        {
            if (_lower[i] != 0)
            {
                // Transform: x'[i] = x[i] - lower[i]
                // This affects the supplies
                int u = _arcLists.Source[i];
                int v = _arcLists.Target[i];
                _supply[u] -= _lower[i];
                _supply[v] += _lower[i];
                _upper[i] -= _lower[i];
                // Set lower to 0 after transformation
                _lower[i] = 0;
            }
        }
        
        // Calculate sum of supplies
        _sumSupply = 0;
        for (int i = 0; i < _nodeCount; i++)
        {
            _sumSupply += _supply[i];
        }
        
        // Calculate artificial cost
        long maxCost = 0;
        for (int i = 0; i < _arcCount; i++)
        {
            maxCost = Math.Max(maxCost, Math.Abs(_cost[i]));
        }
        ART_COST = (maxCost + 1) * _nodeCount;
    }
    
    private bool Initialize()
    {
        // Choose root node
        _root = _nodeCount;
        
        // Initialize spanning tree structure
        _tree.Parent[_root] = -1;
        _tree.Pred[_root] = -1;
        _tree.Thread[_root] = 0;
        _tree.RevThread[0] = _root;
        _tree.SuccNum[_root] = _nodeCount + 1;
        _tree.LastSucc[_root] = _nodeCount - 1; // Last node, not root
        _tree.PredDir[_root] = 0;
        
        // Initialize node data
        InitializeNodes();
        
        // Set arc counts
        _allArcNum = _searchArcNum;
        
        // Initialize reduced cost caching if enabled
        if ((_optimizationConfig.Flags & OptimizationFlags.ReducedCostCaching) != 0)
        {
            _reducedCosts = new long[_allArcNum];
            _reducedCostsDirty = true;
        }
        
        return true;
    }
    
    private void InitializeNodes()
    {
        if (_supplyType == SupplyType.Geq)
        {
            InitializeGEQ();
        }
        else
        {
            InitializeLEQ();
        }
    }
    
    private void InitializeGEQ()
    {
        // Initialize states for original arcs
        for (int i = 0; i < _arcCount; i++)
        {
            _tree.State[i] = STATE_LOWER;
            _flow[i] = 0;
        }
        
        _searchArcNum = _arcCount + _nodeCount;
        int f = _arcCount + _nodeCount;
        
        // First set up thread array to form a cyclic list
        for (int u = 0; u < _nodeCount; u++)
        {
            _tree.Thread[u] = u + 1;
        }
        _tree.Thread[_nodeCount - 1] = _root; // Close the cycle back to root
        
        // Set up reverse thread
        for (int u = 0; u < _nodeCount; u++)
        {
            _tree.RevThread[_tree.Thread[u]] = u;
        }
        
        for (int u = 0, e = _arcCount; u < _nodeCount; u++, e++)
        {
            _tree.Parent[u] = _root;
            _tree.SuccNum[u] = 1;
            _tree.LastSucc[u] = u;
            
            if (_supply[u] <= 0)
            {
                _tree.PredDir[u] = DIR_DOWN;
                _pi[u] = 0;
                _tree.Pred[u] = e;
                _arcLists.SetArc(e, _root, u);
                _upper[e] = INF;
                _flow[e] = -_supply[u];
                _cost[e] = 0;
                _tree.State[e] = STATE_TREE;
            }
            else
            {
                _tree.PredDir[u] = DIR_UP;
                _pi[u] = -ART_COST;
                _tree.Pred[u] = f;
                _arcLists.SetArc(f, u, _root);
                _upper[f] = INF;
                _flow[f] = _supply[u];
                _tree.State[f] = STATE_TREE;
                _cost[f] = ART_COST;
                _arcLists.SetArc(e, _root, u);
                _upper[e] = INF;
                _flow[e] = 0;
                _cost[e] = 0;
                _tree.State[e] = STATE_LOWER;
                f++;
            }
        }
        _allArcNum = f;
        
        // Complete the thread cycle properly
        _tree.Thread[_nodeCount - 1] = _root;
        _tree.RevThread[_root] = _nodeCount - 1;
    }
    
    private void InitializeLEQ()
    {
        // Initialize states for original arcs
        for (int i = 0; i < _arcCount; i++)
        {
            _tree.State[i] = STATE_LOWER;
            _flow[i] = 0;
        }
        
        _searchArcNum = _arcCount + _nodeCount;
        int f = _arcCount + _nodeCount;
        
        // First set up thread array to form a cyclic list
        for (int u = 0; u < _nodeCount; u++)
        {
            _tree.Thread[u] = u + 1;
        }
        _tree.Thread[_nodeCount - 1] = _root; // Close the cycle back to root
        
        // Set up reverse thread
        for (int u = 0; u < _nodeCount; u++)
        {
            _tree.RevThread[_tree.Thread[u]] = u;
        }
        
        for (int u = 0, e = _arcCount; u < _nodeCount; u++, e++)
        {
            _tree.Parent[u] = _root;
            _tree.SuccNum[u] = 1;
            _tree.LastSucc[u] = u;
            
            if (_supply[u] >= 0)
            {
                _tree.PredDir[u] = DIR_UP;
                _pi[u] = 0;
                _tree.Pred[u] = e;
                _arcLists.SetArc(e, u, _root);
                _upper[e] = INF;
                _flow[e] = _supply[u];
                _cost[e] = 0;
                _tree.State[e] = STATE_TREE;
            }
            else
            {
                _tree.PredDir[u] = DIR_DOWN;
                _pi[u] = ART_COST;
                _tree.Pred[u] = f;
                _arcLists.SetArc(f, _root, u);
                _upper[f] = INF;
                _flow[f] = -_supply[u];
                _tree.State[f] = STATE_TREE;
                _cost[f] = ART_COST;
                _arcLists.SetArc(e, u, _root);
                _upper[e] = INF;
                _flow[e] = 0;
                _cost[e] = 0;
                _tree.State[e] = STATE_LOWER;
                f++;
            }
        }
        _allArcNum = f;
        
        // Complete the thread cycle properly
        _tree.Thread[_nodeCount - 1] = _root;
        _tree.RevThread[_root] = _nodeCount - 1;
    }
    
    private IFindEnteringArc CreatePivotRuleFinder()
    {
        if (_useOptimizedPivot)
        {
            return new OptimizedPivotWrapper(this, _pivotRule);
        }
        
        // Check if we should use cached pivot
        bool useCache = (_optimizationConfig.Flags & OptimizationFlags.ReducedCostCaching) != 0;
        
        // Initialize reduced cost caching infrastructure if needed
        if (useCache && _reducedCosts == null)
        {
            _reducedCosts = new long[_searchArcNum];
            _reducedCostsDirty = true;
            
            // Only use caching for sparse networks where it's beneficial
            double density = (double)_searchArcNum / (_nodeCount * _nodeCount);
            if (density < 0.01 && _searchArcNum < 10000) // Very sparse network only
            {
                BuildAdjacencyLists();
                _dirtyNodes = new bool[_nodeCount];
                _dirtyNodeList = new int[_nodeCount];
            }
            else
            {
                // For dense networks, disable caching as it's slower
                _reducedCosts = null;
                useCache = false;
            }
        }
        
        return _pivotRule switch
        {
            PivotRule.BlockSearch => useCache ? new CachedBlockSearchPivot(this) : new BlockSearchPivot(this),
            PivotRule.FirstEligible => new FirstEligiblePivot(this),
            PivotRule.BestEligible => new BestEligiblePivot(this),
            _ => throw new NotImplementedException($"Pivot rule {_pivotRule} not implemented yet")
        };
    }
    
    private void BuildAdjacencyLists()
    {
        // Count arcs per node (only for original arcs, not artificial ones)
        var outDegree = new int[_nodeCount];
        var inDegree = new int[_nodeCount];
        
        // Only include original arcs in adjacency lists
        int maxArc = Math.Min(_searchArcNum, _arcCount);
        
        for (int e = 0; e < maxArc; e++)
        {
            outDegree[_arcLists.Source[e]]++;
            inDegree[_arcLists.Target[e]]++;
        }
        
        // Allocate adjacency lists
        _outgoingArcs = new int[_nodeCount][];
        _incomingArcs = new int[_nodeCount][];
        
        for (int i = 0; i < _nodeCount; i++)
        {
            _outgoingArcs[i] = new int[outDegree[i]];
            _incomingArcs[i] = new int[inDegree[i]];
            outDegree[i] = 0; // Reset for use as index
            inDegree[i] = 0;
        }
        
        // Fill adjacency lists
        for (int e = 0; e < maxArc; e++)
        {
            int u = _arcLists.Source[e];
            int v = _arcLists.Target[e];
            _outgoingArcs[u][outDegree[u]++] = e;
            _incomingArcs[v][inDegree[v]++] = e;
        }
    }
    
    private void FindJoinNode()
    {
        int u = _arcLists.Source[_inArc];
        int v = _arcLists.Target[_inArc];
        while (u != v)
        {
            if (_tree.SuccNum[u] < _tree.SuccNum[v])
            {
                u = _tree.Parent[u];
            }
            else
            {
                v = _tree.Parent[v];
            }
        }
        _join = u;
    }
    
    private bool FindLeavingArc()
    {
        // Initialize first and second nodes according to the direction
        // of the cycle
        int first, second;
        if (_tree.State[_inArc] == STATE_LOWER)
        {
            first = _arcLists.Source[_inArc];
            second = _arcLists.Target[_inArc];
        }
        else
        {
            first = _arcLists.Target[_inArc];
            second = _arcLists.Source[_inArc];
        }
        _delta = _upper[_inArc];
        int result = 0;
        long d;
        int e;
        
        // Search the cycle from the first node to the join node
        for (int u = first; u != _join; u = _tree.Parent[u])
        {
            e = _tree.Pred[u];
            d = _flow[e];
            if (_tree.PredDir[u] == DIR_DOWN)
            {
                long c = _upper[e];
                d = c >= MAX ? INF : c - d;
            }
            if (d < _delta)
            {
                _delta = d;
                _uOut = u;
                result = 1;
            }
        }
        
        // Search the cycle from the second node to the join node
        for (int u = second; u != _join; u = _tree.Parent[u])
        {
            e = _tree.Pred[u];
            d = _flow[e];
            if (_tree.PredDir[u] == DIR_UP)
            {
                long c = _upper[e];
                d = c >= MAX ? INF : c - d;
            }
            if (d <= _delta)
            {
                _delta = d;
                _uOut = u;
                result = 2;
            }
        }
        
        if (result == 1)
        {
            _uIn = first;
            _vIn = second;
        }
        else
        {
            _uIn = second;
            _vIn = first;
        }
        return result != 0;
    }
    
    private void ChangeFlow(bool change)
    {
        // Augment along the cycle
        if (_delta > 0)
        {
            long val = _tree.State[_inArc] * _delta;
            _flow[_inArc] += val;
            for (int u = _arcLists.Source[_inArc]; u != _join; u = _tree.Parent[u])
            {
                _flow[_tree.Pred[u]] -= _tree.PredDir[u] * val;
            }
            for (int u = _arcLists.Target[_inArc]; u != _join; u = _tree.Parent[u])
            {
                _flow[_tree.Pred[u]] += _tree.PredDir[u] * val;
            }
        }
        
        // Update the state of the entering and leaving arcs
        if (change)
        {
            _tree.State[_inArc] = STATE_TREE;
            _tree.State[_tree.Pred[_uOut]] = 
                _flow[_tree.Pred[_uOut]] == 0 ? STATE_LOWER : STATE_UPPER;
        }
        else
        {
            _tree.State[_inArc] = (sbyte)-_tree.State[_inArc];
        }
    }
    
    private void UpdateTreeStructure()
    {
        int oldRevThread = _tree.RevThread[_uOut];
        int oldSuccNum = _tree.SuccNum[_uOut];
        int oldLastSucc = _tree.LastSucc[_uOut];
        _vOut = _tree.Parent[_uOut];

        // Check if u_in and u_out coincide
        if (_uIn == _uOut)
        {
            // Update parent, pred, pred_dir
            _tree.Parent[_uIn] = _vIn;
            _tree.Pred[_uIn] = _inArc;
            _tree.PredDir[_uIn] = _uIn == _arcLists.Source[_inArc] ? DIR_UP : DIR_DOWN;

            // Update thread and rev_thread
            if (_tree.Thread[_vIn] != _uOut)
            {
                int after = _tree.Thread[oldLastSucc];
                _tree.Thread[oldRevThread] = after;
                _tree.RevThread[after] = oldRevThread;
                after = _tree.Thread[_vIn];
                _tree.Thread[_vIn] = _uOut;
                _tree.RevThread[_uOut] = _vIn;
                _tree.Thread[oldLastSucc] = after;
                _tree.RevThread[after] = oldLastSucc;
            }
        }
        else
        {
            // Handle the case when old_rev_thread equals to v_in
            // (it also means that join and v_out coincide)
            int threadContinue = oldRevThread == _vIn ?
                _tree.Thread[oldLastSucc] : _tree.Thread[_vIn];

            // Update thread and parent along the stem nodes (i.e. the nodes
            // between u_in and u_out, whose parent have to be changed)
            int stem = _uIn;              // the current stem node
            int parStem = _vIn;          // the new parent of stem
            int nextStem;                // the next stem node
            int last = _tree.LastSucc[_uIn];  // the last successor of stem
            int before, after = _tree.Thread[last];
            _tree.Thread[_vIn] = _uIn;
            Span<int> dirtyRevs = stackalloc int[_nodeCount];
            dirtyRevs[0] = _vIn;
            int dirtyCount = 1;
            
            while (stem != _uOut)
            {
                // Insert the next stem node into the thread list
                nextStem = _tree.Parent[stem];
                _tree.Thread[last] = nextStem;
                dirtyRevs[dirtyCount++] = last;

                // Remove the subtree of stem from the thread list
                before = _tree.RevThread[stem];
                _tree.Thread[before] = after;
                _tree.RevThread[after] = before;

                // Change the parent node and shift stem nodes
                _tree.Parent[stem] = parStem;
                parStem = stem;
                stem = nextStem;

                // Update last and after
                last = _tree.LastSucc[stem] == _tree.LastSucc[parStem] ?
                    _tree.RevThread[parStem] : _tree.LastSucc[stem];
                after = _tree.Thread[last];
            }
            _tree.Parent[_uOut] = parStem;
            _tree.Thread[last] = threadContinue;
            _tree.RevThread[threadContinue] = last;
            _tree.LastSucc[_uOut] = last;

            // Remove the subtree of u_out from the thread list except for
            // the case when old_rev_thread equals to v_in
            if (oldRevThread != _vIn)
            {
                _tree.Thread[oldRevThread] = after;
                _tree.RevThread[after] = oldRevThread;
            }

            // Update rev_thread using the new thread values
            for (int i = 0; i < dirtyCount; ++i)
            {
                int u = dirtyRevs[i];
                _tree.RevThread[_tree.Thread[u]] = u;
            }

            // Update pred, pred_dir, last_succ and succ_num for the
            // stem nodes from u_out to u_in
            int tmpSc = 0, tmpLs = _tree.LastSucc[_uOut];
            for (int u = _uOut, p = _tree.Parent[u]; u != _uIn; u = p, p = _tree.Parent[u])
            {
                _tree.Pred[u] = _tree.Pred[p];
                _tree.PredDir[u] = (sbyte)-_tree.PredDir[p];
                tmpSc += _tree.SuccNum[u] - _tree.SuccNum[p];
                _tree.SuccNum[u] = tmpSc;
                _tree.LastSucc[p] = tmpLs;
            }
            _tree.Pred[_uIn] = _inArc;
            _tree.PredDir[_uIn] = _uIn == _arcLists.Source[_inArc] ? DIR_UP : DIR_DOWN;
            _tree.SuccNum[_uIn] = oldSuccNum;
        }

        // Update last_succ from v_in towards the root
        int upLimitOut = _tree.LastSucc[_join] == _vIn ? _join : -1;
        int lastSuccOut = _tree.LastSucc[_uOut];
        for (int u = _vIn; u != -1 && _tree.LastSucc[u] == _vIn; u = _tree.Parent[u])
        {
            _tree.LastSucc[u] = lastSuccOut;
        }

        // Update last_succ from v_out towards the root
        if (_join != oldRevThread && _vIn != oldRevThread)
        {
            for (int u = _vOut; u != upLimitOut && _tree.LastSucc[u] == oldLastSucc;
                 u = _tree.Parent[u])
            {
                _tree.LastSucc[u] = oldRevThread;
            }
        }
        else if (lastSuccOut != oldLastSucc)
        {
            for (int u = _vOut; u != upLimitOut && _tree.LastSucc[u] == oldLastSucc;
                 u = _tree.Parent[u])
            {
                _tree.LastSucc[u] = lastSuccOut;
            }
        }

        // Update succ_num from v_in to join
        for (int u = _vIn; u != _join; u = _tree.Parent[u])
        {
            _tree.SuccNum[u] += oldSuccNum;
        }
        // Update succ_num from v_out to join
        for (int u = _vOut; u != _join; u = _tree.Parent[u])
        {
            _tree.SuccNum[u] -= oldSuccNum;
        }
    }
    
    private void UpdatePotentials()
    {
        long sigma = _pi[_vIn] - _pi[_uIn] - 
            _tree.PredDir[_uIn] * _cost[_inArc];
        
        // Simple, efficient potential update using thread index
        int end = _tree.Thread[_tree.LastSucc[_uIn]];
        for (int u = _uIn; u != end; u = _tree.Thread[u])
        {
            _pi[u] += sigma;
            
            // Mark nodes as dirty for batch reduced cost update if caching is enabled
            if (_reducedCosts != null && _dirtyNodes != null && !_dirtyNodes[u])
            {
                _dirtyNodes[u] = true;
                _dirtyNodeList![_dirtyNodeCount++] = u;
            }
        }
        
        // If reduced cost caching is enabled but we don't have dirty tracking, mark all as dirty
        if (_reducedCosts != null && _dirtyNodes == null)
        {
            _reducedCostsDirty = true;
        }
    }
    
    private void UpdateReducedCosts()
    {
        if (_reducedCosts == null)
        {
            return;
        }

        // If we have dirty nodes, update only affected arcs
        if (_dirtyNodes != null && _dirtyNodeCount > 0 && !_reducedCostsDirty)
        {
            // Batch update reduced costs for dirty nodes
            for (int i = 0; i < _dirtyNodeCount; i++)
            {
                int node = _dirtyNodeList![i];
                
                // Update outgoing arcs
                foreach (int e in _outgoingArcs![node])
                {
                    if (_tree.State[e] != STATE_TREE)
                    {
                        _reducedCosts[e] = _tree.State[e] * 
                            (_cost[e] + _pi[_arcLists.Source[e]] - _pi[_arcLists.Target[e]]);
                    }
                }
                
                // Update incoming arcs
                foreach (int e in _incomingArcs![node])
                {
                    if (_tree.State[e] != STATE_TREE)
                    {
                        _reducedCosts[e] = _tree.State[e] * 
                            (_cost[e] + _pi[_arcLists.Source[e]] - _pi[_arcLists.Target[e]]);
                    }
                }
                
                _dirtyNodes[node] = false;
            }
            
            _dirtyNodeCount = 0;
        }
        else if (_reducedCostsDirty)
        {
            // Full update if needed
            int maxArc = Math.Min(_searchArcNum, _arcCount); // Only update original arcs
            for (int e = 0; e < maxArc; e++)
            {
                if (_tree.State[e] != STATE_TREE)
                {
                    _reducedCosts[e] = _tree.State[e] * 
                        (_cost[e] + _pi[_arcLists.Source[e]] - _pi[_arcLists.Target[e]]);
                }
                else
                {
                    _reducedCosts[e] = 0; // Tree arcs have zero reduced cost by definition
                }
            }
            
            _reducedCostsDirty = false;
        }
    }
    
    private bool CheckFeasibility()
    {
        // Check if all artificial arcs have zero flow
        for (int e = _arcCount; e < _allArcNum; e++)
        {
            if (_flow[e] != 0)
            {
                return false;
            }
        }
        return true;
    }
    
    // Interface for pivot rule implementations
    private interface IFindEnteringArc
    {
        bool FindEnteringArc();
    }
    
    // Block Search pivot rule implementation
    private sealed class BlockSearchPivot : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns;
        private int _blockSize;
        private int _nextArc;
        private int _consecutiveLowHits = 0;
        private int _consecutiveHighHits = 0;
        private readonly int _baseBlockSize;
        private readonly int _dynamicMinBlockSize;
        
        public int BlockSize => _blockSize;
        
        public BlockSearchPivot(NetworkSimplex ns)
        {
            _ns = ns;
            
            // Calculate base block size
            _baseBlockSize = (int)Math.Sqrt(ns._searchArcNum);
            
            // Calculate dynamic minimum based on configuration
            _dynamicMinBlockSize = Math.Max(
                _ns._optimizationConfig.MinBlockSize,
                (int)(_baseBlockSize * _ns._optimizationConfig.MinBlockSizeRatio)
            );
            
            // Check if we should use small blocks for dense networks
            if ((_ns._optimizationConfig.Flags & OptimizationFlags.SmallBlocksForDense) != 0)
            {
                double density = (double)ns._searchArcNum / ns._nodeCount;
                if (density > 10) // Dense network threshold
                {
                    _blockSize = Math.Min(50, _baseBlockSize / 4);
                }
                else
                {
                    _blockSize = _baseBlockSize;
                }
            }
            else
            {
                _blockSize = _baseBlockSize;
            }
            
            _blockSize = Math.Max(_blockSize, _dynamicMinBlockSize);
            _nextArc = 0;
        }
        
        public bool FindEnteringArc()
        {
            long min = 0;
            int cnt = _blockSize;
            int e;
            int startArc = _nextArc;
            int arcsChecked = 0;
            
            for (e = _nextArc; e < _ns._searchArcNum; e++)
            {
                arcsChecked++;
                _ns._arcsCheckedInCurrentPivot++;
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < min)
                {
                    min = c;
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0)
                    {
                        goto search_end;
                    }

                    cnt = _blockSize;
                }
            }
            
            for (e = 0; e < _nextArc; e++)
            {
                arcsChecked++;
                _ns._arcsCheckedInCurrentPivot++;
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < min)
                {
                    min = c;
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0)
                    {
                        goto search_end;
                    }

                    cnt = _blockSize;
                }
            }
            
            if (min >= 0)
            {
                return false;
            }

            search_end:
            _nextArc = e;
            
            // Implement adaptive block size if enabled
            if ((_ns._optimizationConfig.Flags & OptimizationFlags.AdaptiveBlockSize) != 0)
            {
                double hitRate = arcsChecked > 0 ? 1.0 / arcsChecked : 0;
                
                if (hitRate < _ns._optimizationConfig.LowHitRateThreshold)
                {
                    // Too many arcs checked
                    _consecutiveHighHits = 0;
                    _consecutiveLowHits++;
                    
                    if (_consecutiveLowHits >= _ns._optimizationConfig.ConsecutiveHitsBeforeAdapt)
                    {
                        // Reduce block size
                        int newSize = (int)(_blockSize * _ns._optimizationConfig.BlockSizeShrinkFactor);
                        _blockSize = Math.Max(_dynamicMinBlockSize, newSize);
                        _consecutiveLowHits = 0;
                    }
                }
                else if (hitRate > _ns._optimizationConfig.HighHitRateThreshold)
                {
                    // Found very quickly
                    _consecutiveLowHits = 0;
                    _consecutiveHighHits++;
                    
                    if (_consecutiveHighHits >= _ns._optimizationConfig.ConsecutiveHitsBeforeAdapt)
                    {
                        // Increase block size
                        int newSize = (int)(_blockSize * _ns._optimizationConfig.BlockSizeGrowthFactor);
                        _blockSize = Math.Min(_ns._optimizationConfig.MaxBlockSize, newSize);
                        _consecutiveHighHits = 0;
                    }
                }
                else
                {
                    // Normal hit rate, reset counters
                    _consecutiveLowHits = 0;
                    _consecutiveHighHits = 0;
                }
            }
            
            return true;
        }
    }
    
    // Cached Block Search pivot rule implementation
    private sealed class CachedBlockSearchPivot : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns;
        private int _blockSize;
        private int _nextArc;
        private int _consecutiveLowHits = 0;
        private int _consecutiveHighHits = 0;
        private readonly int _baseBlockSize;
        private readonly int _dynamicMinBlockSize;
        
        public int BlockSize => _blockSize;
        
        public CachedBlockSearchPivot(NetworkSimplex ns)
        {
            _ns = ns;
            
            // Calculate base block size
            _baseBlockSize = (int)Math.Sqrt(ns._searchArcNum);
            
            // Calculate dynamic minimum based on configuration
            _dynamicMinBlockSize = Math.Max(
                _ns._optimizationConfig.MinBlockSize,
                (int)(_baseBlockSize * _ns._optimizationConfig.MinBlockSizeRatio)
            );
            
            // Check if we should use small blocks for dense networks
            if ((_ns._optimizationConfig.Flags & OptimizationFlags.SmallBlocksForDense) != 0)
            {
                double density = (double)ns._searchArcNum / ns._nodeCount;
                if (density > 10) // Dense network threshold
                {
                    _blockSize = Math.Min(50, _baseBlockSize / 4);
                }
                else
                {
                    _blockSize = _baseBlockSize;
                }
            }
            else
            {
                _blockSize = _baseBlockSize;
            }
            
            _blockSize = Math.Max(_blockSize, _dynamicMinBlockSize);
            _nextArc = 0;
        }
        
        public bool FindEnteringArc()
        {
            // Update reduced costs if needed
            _ns.UpdateReducedCosts();
            
            long min = 0;
            int cnt = _blockSize;
            int e;
            int startArc = _nextArc;
            int arcsChecked = 0;
            
            // Use cached reduced costs
            var reducedCosts = _ns._reducedCosts!;
            
            for (e = _nextArc; e < _ns._searchArcNum; e++)
            {
                arcsChecked++;
                _ns._arcsCheckedInCurrentPivot++;
                
                if (reducedCosts[e] < min)
                {
                    min = reducedCosts[e];
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0)
                    {
                        goto search_end;
                    }

                    cnt = _blockSize;
                }
            }
            
            for (e = 0; e < _nextArc; e++)
            {
                arcsChecked++;
                _ns._arcsCheckedInCurrentPivot++;
                
                if (reducedCosts[e] < min)
                {
                    min = reducedCosts[e];
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0)
                    {
                        goto search_end;
                    }

                    cnt = _blockSize;
                }
            }
            
            if (min >= 0)
            {
                return false;
            }

            search_end:
            _nextArc = e;
            
            // Implement adaptive block size if enabled
            if ((_ns._optimizationConfig.Flags & OptimizationFlags.AdaptiveBlockSize) != 0)
            {
                double hitRate = arcsChecked > 0 ? 1.0 / arcsChecked : 0;
                
                if (hitRate < _ns._optimizationConfig.LowHitRateThreshold)
                {
                    // Too many arcs checked
                    _consecutiveHighHits = 0;
                    _consecutiveLowHits++;
                    
                    if (_consecutiveLowHits >= _ns._optimizationConfig.ConsecutiveHitsBeforeAdapt)
                    {
                        // Reduce block size
                        int newSize = (int)(_blockSize * _ns._optimizationConfig.BlockSizeShrinkFactor);
                        _blockSize = Math.Max(_dynamicMinBlockSize, newSize);
                        _consecutiveLowHits = 0;
                    }
                }
                else if (hitRate > _ns._optimizationConfig.HighHitRateThreshold)
                {
                    // Found very quickly
                    _consecutiveLowHits = 0;
                    _consecutiveHighHits++;
                    
                    if (_consecutiveHighHits >= _ns._optimizationConfig.ConsecutiveHitsBeforeAdapt)
                    {
                        // Increase block size
                        int newSize = (int)(_blockSize * _ns._optimizationConfig.BlockSizeGrowthFactor);
                        _blockSize = Math.Min(_ns._optimizationConfig.MaxBlockSize, newSize);
                        _consecutiveHighHits = 0;
                    }
                }
                else
                {
                    // Normal hit rate, reset counters
                    _consecutiveLowHits = 0;
                    _consecutiveHighHits = 0;
                }
            }
            
            return true;
        }
    }
    
    // First Eligible pivot rule implementation
    private sealed class FirstEligiblePivot(NetworkSimplex ns) : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns = ns;
        private int _nextArc = 0;

        public bool FindEnteringArc()
        {
            // Search from current position
            for (int e = _nextArc; e < _ns._searchArcNum; e++)
            {
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < 0)
                {
                    _ns._inArc = e;
                    _nextArc = e + 1;
                    return true;
                }
            }
            
            // Wrap around
            for (int e = 0; e < _nextArc; e++)
            {
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < 0)
                {
                    _ns._inArc = e;
                    _nextArc = e + 1;
                    return true;
                }
            }
            
            return false;
        }
    }
    
    // Best Eligible pivot rule implementation
    private sealed class BestEligiblePivot(NetworkSimplex ns) : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns = ns;

        public bool FindEnteringArc()
        {
            long min = 0;
            int bestArc = -1;
            
            for (int e = 0; e < _ns._searchArcNum; e++)
            {
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < min)
                {
                    min = c;
                    bestArc = e;
                }
            }
            
            if (min < 0)
            {
                _ns._inArc = bestArc;
                return true;
            }
            
            return false;
        }
    }
    
    // Wrapper for optimized pivot rules
    private sealed class OptimizedPivotWrapper : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns;
        private readonly object _optimizedPivot;
        private readonly PivotRule _rule;
        
        public unsafe OptimizedPivotWrapper(NetworkSimplex ns, PivotRule rule)
        {
            _ns = ns;
            _rule = rule;
            
            // Pin arrays and create optimized pivot
            fixed (long* costPtr = ns._cost)
            fixed (long* piPtr = ns._pi)
            fixed (int* sourcePtr = ns._arcLists.Source)
            fixed (int* targetPtr = ns._arcLists.Target)
            fixed (sbyte* statePtr = ns._tree.State)
            {
                _optimizedPivot = rule switch
                {
                    PivotRule.BlockSearch => new BlockSearchPivotOptimized(ns, costPtr, piPtr, sourcePtr, targetPtr, statePtr),
                    PivotRule.FirstEligible => new FirstEligiblePivotOptimized(ns, costPtr, piPtr, sourcePtr, targetPtr, statePtr),
                    PivotRule.BestEligible => new BestEligiblePivotOptimized(ns, costPtr, piPtr, sourcePtr, targetPtr, statePtr),
                    _ => throw new NotImplementedException($"Optimized pivot rule {rule} not implemented")
                };
            }
        }
        
        public unsafe bool FindEnteringArc()
        {
            fixed (long* costPtr = _ns._cost)
            fixed (long* piPtr = _ns._pi)
            fixed (int* sourcePtr = _ns._arcLists.Source)
            fixed (int* targetPtr = _ns._arcLists.Target)
            fixed (sbyte* statePtr = _ns._tree.State)
            {
                int enteringArc = -1;
                bool found = _rule switch
                {
                    PivotRule.BlockSearch => ((BlockSearchPivotOptimized)_optimizedPivot).FindEnteringArc(out enteringArc),
                    PivotRule.FirstEligible => ((FirstEligiblePivotOptimized)_optimizedPivot).FindEnteringArc(out enteringArc),
                    PivotRule.BestEligible => ((BestEligiblePivotOptimized)_optimizedPivot).FindEnteringArc(out enteringArc),
                    _ => false
                };
                
                if (found)
                {
                    _ns._inArc = enteringArc;
                }
                
                return found;
            }
        }
    }
}