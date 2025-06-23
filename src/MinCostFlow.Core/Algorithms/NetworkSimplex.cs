using System;
using System.Runtime.CompilerServices;
using MinCostFlow.Core.DataStructures;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using MinCostFlow.Core.Algorithms.Internal;

namespace MinCostFlow.Core.Algorithms;

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
    private bool _useOptimizedPotentialUpdate = false;
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
    
    // Pivot rule
    private PivotRule _pivotRule = PivotRule.BlockSearch;
    private IFindEnteringArc? _enteringArcFinder;
    
    // Maximum values
    private readonly long MAX;
    private readonly long INF;
    private long ART_COST;
    
    // Solver state
    private SolverStatus _status = SolverStatus.NotSolved;
    
    // Internal properties for optimized implementations
    internal int SearchArcNum => _searchArcNum;
    internal ArcLists ArcLists => _arcLists;
    internal SpanningTree Tree => _tree;
    
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
            throw new ArgumentException("Invalid arc", nameof(arc));
        
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
            throw new ArgumentException("Invalid arc", nameof(arc));
        
        _cost[arc.Id] = cost;
        return this;
    }
    
    /// <summary>
    /// Sets the supply value for a node.
    /// </summary>
    public NetworkSimplex SetNodeSupply(Node node, long supply)
    {
        if (!_graph.IsValidNode(node))
            throw new ArgumentException("Invalid node", nameof(node));
        
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
        // Check bounds
        if (!CheckBounds())
        {
            _status = SolverStatus.Infeasible;
            return _status;
        }
        
        // Transform to standard form
        TransformToStandardForm();
        
        // Initialize the algorithm
        if (!Initialize())
        {
            _status = SolverStatus.Infeasible;
            return _status;
        }
        
        // Create pivot rule finder
        _enteringArcFinder = CreatePivotRuleFinder();
        
        // Main simplex loop
        int iterations = 0;
        while (_enteringArcFinder.FindEnteringArc())
        {
            iterations++;
            if (iterations > 10000) // Safety check
            {
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
                UpdateTreeStructure();
                UpdatePotentials();
            }
        }
        
        // Check feasibility
        if (CheckFeasibility())
        {
            _status = SolverStatus.Optimal;
        }
        else
        {
            _status = SolverStatus.Infeasible;
        }
        
        return _status;
    }
    
    /// <summary>
    /// Gets the flow value on an arc.
    /// </summary>
    public long GetFlow(Arc arc)
    {
        if (_status != SolverStatus.Optimal)
            throw new InvalidOperationException("Solution not optimal");
        if (!_graph.IsValidArc(arc))
            throw new ArgumentException("Invalid arc", nameof(arc));
        
        // Add back the lower bound that was transformed away
        return _flow[arc.Id] + _origLower[arc.Id];
    }
    
    /// <summary>
    /// Gets the potential (dual value) of a node.
    /// </summary>
    public long GetPotential(Node node)
    {
        if (_status != SolverStatus.Optimal)
            throw new InvalidOperationException("Solution not optimal");
        if (!_graph.IsValidNode(node))
            throw new ArgumentException("Invalid node", nameof(node));
        
        return _pi[node.Id];
    }
    
    /// <summary>
    /// Gets the total cost of the solution.
    /// </summary>
    public long GetTotalCost()
    {
        if (_status != SolverStatus.Optimal)
            throw new InvalidOperationException("Solution not optimal");
        
        long totalCost = 0;
        for (int i = 0; i < _arcCount; i++)
        {
            // Use actual flow (including lower bounds) for cost calculation
            totalCost += (_flow[i] + _origLower[i]) * _cost[i];
        }
        return totalCost;
    }
    
    /// <summary>
    /// Gets the current solver status.
    /// </summary>
    public SolverStatus Status => _status;
    
    /// <summary>
    /// Enables optimized pivot rules using unsafe code.
    /// </summary>
    public void EnableOptimizedPivot(bool enable = true)
    {
        _useOptimizedPivot = enable;
    }
    
    /// <summary>
    /// Enables optimized potential updates using SIMD.
    /// </summary>
    public void EnableOptimizedPotentialUpdate(bool enable = true)
    {
        _useOptimizedPotentialUpdate = enable;
    }
    
    /// <summary>
    /// Sets a memory pool for reducing allocations.
    /// </summary>
    public void SetMemoryPool(Utils.SolverMemoryPool? pool)
    {
        _memoryPool = pool;
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
                return false;
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
        
        return _pivotRule switch
        {
            PivotRule.BlockSearch => new BlockSearchPivot(this),
            PivotRule.FirstEligible => new FirstEligiblePivot(this),
            PivotRule.BestEligible => new BestEligiblePivot(this),
            _ => throw new NotImplementedException($"Pivot rule {_pivotRule} not implemented yet")
        };
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
                (_flow[_tree.Pred[_uOut]] == 0) ? STATE_LOWER : STATE_UPPER;
        }
        else
        {
            _tree.State[_inArc] = (sbyte)(-_tree.State[_inArc]);
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
            _tree.PredDir[_uIn] = (sbyte)(_uIn == _arcLists.Source[_inArc] ? DIR_UP : DIR_DOWN);

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
                _tree.PredDir[u] = (sbyte)(-_tree.PredDir[p]);
                tmpSc += _tree.SuccNum[u] - _tree.SuccNum[p];
                _tree.SuccNum[u] = tmpSc;
                _tree.LastSucc[p] = tmpLs;
            }
            _tree.Pred[_uIn] = _inArc;
            _tree.PredDir[_uIn] = (sbyte)(_uIn == _arcLists.Source[_inArc] ? DIR_UP : DIR_DOWN);
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
        
        if (_useOptimizedPotentialUpdate)
        {
            unsafe
            {
                fixed (long* piPtr = _pi)
                fixed (int* threadPtr = _tree.Thread)
                fixed (int* lastSuccPtr = _tree.LastSucc)
                {
                    PotentialUpdateOptimized.UpdatePotentials(
                        piPtr, threadPtr, lastSuccPtr, _uIn, sigma);
                }
            }
        }
        else
        {
            int end = _tree.Thread[_tree.LastSucc[_uIn]];
            for (int u = _uIn; u != end; u = _tree.Thread[u])
            {
                _pi[u] += sigma;
            }
        }
    }
    
    private bool CheckFeasibility()
    {
        // Check if all artificial arcs have zero flow
        for (int e = _arcCount; e < _allArcNum; e++)
        {
            if (_flow[e] != 0)
                return false;
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
        private readonly int _blockSize;
        private int _nextArc;
        
        public BlockSearchPivot(NetworkSimplex ns)
        {
            _ns = ns;
            int blockSize = (int)Math.Sqrt(ns._searchArcNum);
            _blockSize = Math.Max(blockSize, MIN_BLOCK_SIZE);
            _nextArc = 0;
        }
        
        public bool FindEnteringArc()
        {
            long min = 0;
            int cnt = _blockSize;
            int e;
            int startArc = _nextArc;
            
            for (e = _nextArc; e < _ns._searchArcNum; e++)
            {
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < min)
                {
                    min = c;
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0) goto search_end;
                    cnt = _blockSize;
                }
            }
            
            for (e = 0; e < _nextArc; e++)
            {
                long c = _ns._tree.State[e] * 
                    (_ns._cost[e] + _ns._pi[_ns._arcLists.Source[e]] - _ns._pi[_ns._arcLists.Target[e]]);
                if (c < min)
                {
                    min = c;
                    _ns._inArc = e;
                }
                if (--cnt == 0)
                {
                    if (min < 0) goto search_end;
                    cnt = _blockSize;
                }
            }
            
            if (min >= 0) return false;
            
        search_end:
            _nextArc = e;
            return true;
        }
    }
    
    // First Eligible pivot rule implementation
    private sealed class FirstEligiblePivot : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns;
        private int _nextArc;
        
        public FirstEligiblePivot(NetworkSimplex ns)
        {
            _ns = ns;
            _nextArc = 0;
        }
        
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
    private sealed class BestEligiblePivot : IFindEnteringArc
    {
        private readonly NetworkSimplex _ns;
        
        public BestEligiblePivot(NetworkSimplex ns)
        {
            _ns = ns;
        }
        
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