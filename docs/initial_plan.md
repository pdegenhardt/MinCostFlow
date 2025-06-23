# LEMON Network Simplex C# Port Implementation Plan

## Project Overview

Port LEMON's high-performance Network Simplex algorithm to C# for solving minimum cost flow problems on time-expanded networks with ~10,000 nodes. Target performance: <1s initial solve, ~100ms re-solve, <50ms modification evaluation.

## Phase 1: Foundation Setup (Week 1)

### 1.1 LEMON Source Analysis

**Primary Reference Files:**
- `lemon/network_simplex.h` (main algorithm, ~3000 lines)
- `lemon/core.h` (graph interfaces)
- `lemon/list_graph.h` (mutable graph implementation)
- `test/min_cost_flow_test.cc` (test cases and validation data)

**Team Assignment:** Senior developer familiar with C++ and graph algorithms

**Deliverables:**
- Algorithm flow diagram
- Key data structure documentation
- Performance-critical sections identified

### 1.2 C# Project Structure

```
MinCostFlow/
├── Core/
│   ├── IGraph.cs
│   ├── IMinCostFlowSolver.cs
│   ├── GraphBuilder.cs
│   └── Types/
│       ├── Node.cs
│       ├── Arc.cs
│       └── FlowValue.cs
├── Algorithms/
│   ├── NetworkSimplex.cs
│   ├── NetworkSimplexOptions.cs
│   └── Internal/
│       ├── BlockSearchPivot.cs
│       ├── FirstEligiblePivot.cs
│       └── BestEligiblePivot.cs
├── DataStructures/
│   ├── CompactGraph.cs
│   ├── ArcLists.cs
│   └── SpanningTree.cs
├── Utils/
│   ├── MemoryPool.cs
│   ├── MathHelpers.cs
│   └── Validation.cs
└── Tests/
    ├── CorrectnessTests.cs
    ├── PerformanceTests.cs
    └── TestData/
```

### 1.3 Core Interfaces

```csharp
public interface IGraph
{
    int NodeCount { get; }
    int ArcCount { get; }
    ReadOnlySpan<int> GetOutgoingArcs(int node);
    ReadOnlySpan<int> GetIncomingArcs(int node);
}

public interface IMinCostFlowSolver<TFlow, TCost>
    where TFlow : struct, IComparable<TFlow>
    where TCost : struct, IComparable<TCost>
{
    SolverStatus Solve();
    TFlow GetFlow(int arc);
    TCost GetReducedCost(int arc);
    TCost GetPotential(int node);
    void ModifyArc(int arc, TFlow capacity, TCost cost);
    void WarmStart(SolverState<TFlow, TCost> previousState);
}
```

## Phase 2: Data Structures (Week 1-2)

### 2.1 Memory-Efficient Graph Representation

**Key Design from LEMON:**
- Forward/backward arc lists for efficient traversal
- Compact node/arc indexing (0-based integers)
- Separate arrays for properties (capacity, cost, flow)

```csharp
public sealed class CompactGraph
{
    // Node data
    private int[] _firstOut;  // First outgoing arc for each node
    private int[] _firstIn;   // First incoming arc for each node
    
    // Arc data (Structure of Arrays for cache efficiency)
    private int[] _source;    // Source node of each arc
    private int[] _target;    // Target node of each arc
    private int[] _nextOut;   // Next outgoing arc in linked list
    private int[] _nextIn;    // Next incoming arc in linked list
    
    // Use ArrayPool for temporary allocations
    private readonly ArrayPool<int> _pool = ArrayPool<int>.Shared;
}
```

### 2.2 Spanning Tree Structure

**Critical for Network Simplex performance:**

```csharp
internal struct SpanningTree
{
    // LEMON's thread index structure for O(1) updates
    public int[] Parent;     // Parent node in tree
    public int[] Thread;     // Thread index for tree traversal
    public int[] Depth;      // Depth of node in tree
    public bool[] Forward;   // Arc direction in tree
    
    // Efficient update operations
    public void UpdateThread(int enteringArc, int leavingArc);
    public int FindJoinNode(int u, int v);
}
```

## Phase 3: Network Simplex Core (Week 2-3)

### 3.1 Main Algorithm Structure

**Port from LEMON's implementation pattern:**

```csharp
public sealed class NetworkSimplex<TFlow, TCost>
    where TFlow : struct, INumber<TFlow>
    where TCost : struct, INumber<TCost>
{
    // State arrays (following LEMON's layout)
    private TFlow[] _flow;       // Current flow on each arc
    private TCost[] _cost;       // Cost of each arc
    private TFlow[] _capacity;   // Capacity of each arc
    private TFlow[] _supply;     // Supply/demand at each node
    private TCost[] _potential;  // Node potentials (dual variables)
    private int[] _state;        // Arc state (basic/non-basic)
    
    // Spanning tree structure
    private SpanningTree _tree;
    
    // Pivot strategies
    private readonly IPivotRule _pivotRule;
}
```

### 3.2 Block Search Pivot Implementation

**LEMON's key optimization - examine √m candidates:**

```csharp
internal sealed class BlockSearchPivot : IPivotRule
{
    private readonly int _blockSize;
    private int _nextArc;
    
    public BlockSearchPivot(int arcCount)
    {
        _blockSize = (int)Math.Sqrt(arcCount);
    }
    
    public unsafe int FindEnteringArc(
        TCost* reducedCosts, 
        int* state, 
        int arcCount)
    {
        // Direct port of LEMON's block search
        // Uses unsafe code for maximum performance
    }
}
```

### 3.3 Strongly Feasible Basis Maintenance

**Prevents cycling, reduces iterations:**

```csharp
private void MaintainStronglyFeasibleBasis(int enteringArc)
{
    // Port LEMON's approach:
    // 1. Find join node
    // 2. Identify leaving arc maintaining feasibility
    // 3. Update spanning tree efficiently
}
```

## Phase 4: Performance Optimizations (Week 3-4)

### 4.1 SIMD Optimizations

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static unsafe void UpdatePotentials(
    Span<TCost> potentials, 
    TCost delta, 
    ReadOnlySpan<int> nodes)
{
    if (Vector.IsHardwareAccelerated && nodes.Length >= Vector<TCost>.Count)
    {
        // Vectorized update for groups of nodes
        fixed (TCost* pPotentials = potentials)
        fixed (int* pNodes = nodes)
        {
            // SIMD implementation
        }
    }
    else
    {
        // Scalar fallback
    }
}
```

### 4.2 Memory Pooling Strategy

```csharp
public sealed class SolverMemoryPool
{
    private readonly ArrayPool<int> _intPool;
    private readonly ArrayPool<double> _doublePool;
    
    // Pre-allocate common sizes based on network size
    public void PreAllocate(int nodeCount, int arcCount)
    {
        // Warm up pools with expected sizes
    }
}
```

### 4.3 Cache-Friendly Iterations

```csharp
// Process arcs in cache-friendly blocks
private const int CacheLineSize = 64;
private const int IntsPerCacheLine = CacheLineSize / sizeof(int);

private void ProcessArcsInBlocks(Action<int> processArc)
{
    for (int block = 0; block < _arcCount; block += IntsPerCacheLine)
    {
        int end = Math.Min(block + IntsPerCacheLine, _arcCount);
        for (int arc = block; arc < end; arc++)
        {
            processArc(arc);
        }
    }
}
```

## Phase 5: Incremental Updates (Week 4-5)

### 5.1 Warm Start Implementation

```csharp
public void WarmStart(NetworkSimplexState<TFlow, TCost> state)
{
    // Restore previous solution
    state.Flow.CopyTo(_flow);
    state.Potential.CopyTo(_potential);
    state.Tree.CopyTo(_tree);
    
    // Verify and repair if needed
    if (!IsStronglyFeasible())
    {
        RepairBasis();
    }
}
```

### 5.2 Fast Arc Modifications

```csharp
public void ModifyArcCost(int arc, TCost newCost)
{
    TCost oldCost = _cost[arc];
    _cost[arc] = newCost;
    
    if (_state[arc] == ArcState.Basic)
    {
        // Update potentials incrementally
        TCost delta = newCost - oldCost;
        UpdatePotentialTree(_tree.GetSubtree(arc), delta);
    }
    
    // Local reoptimization
    PerformLocalPivots(arc, maxPivots: 10);
}
```

## Phase 6: Testing and Validation (Week 5-6)

### 6.1 Test Data Sources

1. **LEMON test cases**: Port test data from `test/min_cost_flow_test.cc`
2. **DIMACS benchmarks**: Download from http://dimacs.rutgers.edu/
3. **Custom logistics tests**: Time-expanded networks with your specific patterns

### 6.2 Correctness Tests

```csharp
[TestClass]
public class NetworkSimplexCorrectnessTests
{
    [TestMethod]
    public void TestComplementarySlackness()
    {
        // Verify optimality conditions
    }
    
    [TestMethod]
    public void TestFlowConservation()
    {
        // Verify flow balance at each node
    }
    
    [TestMethod]
    public void CompareWithLemonResults()
    {
        // Run same problems through LEMON and compare
    }
}
```

### 6.3 Performance Benchmarks

```csharp
[Benchmark]
public void SolveLogisticsNetwork10K()
{
    var solver = new NetworkSimplex<int, double>(_graph10K);
    solver.Solve();
}

[Benchmark]
public void WarmStartResolve()
{
    _solver.ModifyArcCost(_randomArc, _newCost);
    _solver.Solve();
}
```

## Implementation Checklist

### Week 1 Tasks
- [ ] Clone LEMON repository and study network_simplex.h
- [ ] Set up C# project structure
- [ ] Implement basic graph interfaces
- [ ] Create memory-efficient graph data structure
- [ ] Port spanning tree structure from LEMON

### Week 2 Tasks
- [ ] Implement basic network simplex without optimizations
- [ ] Port block search pivot rule
- [ ] Implement strongly feasible basis maintenance
- [ ] Create initial test suite with small problems

### Week 3 Tasks
- [ ] Add SIMD optimizations for potential updates
- [ ] Implement memory pooling
- [ ] Optimize cache access patterns
- [ ] Add unsafe code optimizations in critical paths

### Week 4 Tasks
- [ ] Implement warm start functionality
- [ ] Add incremental arc modification
- [ ] Create time-expanded network helpers
- [ ] Benchmark against OR-Tools

### Week 5 Tasks
- [ ] Comprehensive correctness testing
- [ ] Performance profiling and optimization
- [ ] Documentation and code review
- [ ] Production hardening

## Key LEMON Concepts to Preserve

1. **Block Search Size**: √m candidates per iteration
2. **Strongly Feasible Basis**: Prevents cycling
3. **Thread Index**: O(1) tree updates
4. **First Eligible Fallback**: When block search fails
5. **Dual Simplex Option**: For reoptimization

## Performance Targets

| Operation | Target Time | Network Size |
|-----------|------------|--------------|
| Initial Solve | < 1s | 10,000 nodes |
| Re-solve (warm) | < 100ms | 10,000 nodes |
| Arc Modification | < 50ms | 10,000 nodes |
| Memory Usage | < 200MB | 10,000 nodes |

## Resources

1. **LEMON Source**: https://lemon.cs.elte.hu/trac/lemon
2. **Network Simplex Paper**: Orlin, R.K. (1997). "A polynomial time primal network simplex algorithm for minimum cost flows"
3. **Test Data**: http://lime.cs.elte.hu/~kpeter/data/mcf/
4. **C# Performance**: https://github.com/adamsitnik/awesome-dot-net-performance

## Success Criteria

1. Passes all LEMON test cases
2. Achieves performance targets on reference problems
3. Supports incremental modifications efficiently
4. Memory usage remains bounded
5. Code is maintainable and well-documented

## Next Steps After Implementation

1. Integration with your time-expanded network builder
2. Specialized optimizations for rental logistics patterns
3. Distributed solving for very large problems
4. Real-time monitoring and debugging tools