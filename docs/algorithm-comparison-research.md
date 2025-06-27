# Algorithm Comparison Research: NetworkSimplex vs OR-Tools

## Executive Summary

Our NetworkSimplex implementation is currently **9x slower** than Google OR-Tools for large problems (718ms vs 80ms on Circulation_1000). This research identifies the fundamental algorithmic differences and proposes implementing a cost-scaling push-relabel algorithm to close this performance gap while maintaining NetworkSimplex's warm-start advantages.

### Key Findings
- **Algorithmic difference**: OR-Tools uses cost-scaling push-relabel (polynomial complexity) vs our primal network simplex (exponential worst-case)
- **Data layout**: OR-Tools leverages contiguous arrays with high memory locality
- **Operation efficiency**: Push-relabel avoids explicit pivoting, reducing per-iteration overhead
- **Solution**: Implement cost-scaling algorithm for initial solves, retain NetworkSimplex for warm-start scenarios

## Algorithm Analysis

### OR-Tools: Cost-Scaling Push-Relabel
- **Algorithm**: Cost-scaling push-relabel with polynomial complexity O(n²m log(nK))
- **Operations**: Pushes flow from active nodes, uses "Push Look-Ahead" heuristics
- **Memory**: Trades space for speed with auxiliary structures (excess, potential, active lists)
- **Reduced costs**: Maintains dual prices and checks in batches during scaling phases
- **Parallelism**: Single-threaded but highly optimized for single-core efficiency

### Our Implementation: Primal Network Simplex
- **Algorithm**: Primal simplex with exponential worst-case but good average performance
- **Operations**: Pivots on spanning tree, updates flows and potentials each iteration
- **Memory**: Optimized for memory efficiency, structure-of-arrays pattern
- **Reduced costs**: Updates potentials each pivot, touching many nodes frequently
- **Warm start**: Can reuse previous solutions (major advantage)

## Performance Gap Root Causes

### 1. Fundamental Algorithm Complexity
- **Cost-scaling**: O(n²m log(nK)) polynomial bound
- **Network simplex**: Can require O(nm) pivots in practice
- On 1M+ arc problems, the iteration count difference becomes significant

### 2. Data Layout and Memory Access
- **OR-Tools**: Contiguous C++ arrays with predictable access patterns
- **Our implementation**: Good structure-of-arrays design, but C# introduces some overhead
- Cache efficiency critical at scale

### 3. Pivot vs Push Efficiency
- **Network simplex**: Each pivot requires:
  - Finding entering arc (16% of runtime after optimization)
  - Updating spanning tree (28% of runtime - now the bottleneck)
  - Updating potentials (21% of runtime)
- **Push-relabel**: Avoids tree maintenance, pushes flow in bulk

### 4. Reduced Cost Maintenance
- **OR-Tools**: Batch updates during scaling phases
- **Our approach**: Per-pivot updates, attempted caching (failed due to overhead)
- The sparse, irregular access pattern makes caching ineffective

### 5. Scaling Behavior
- For Circulation_1000 (dense network):
  - NetworkSimplex: ~125k iterations
  - Expected with cost-scaling: ~10-20k push/relabel operations

## Current Optimization Results

We achieved 29% improvement through:
- **SmallBlocksForDense**: Reduced block size for dense networks
- **AdaptiveBlockSize**: Dynamic adjustment based on hit rate
- **Removed ineffective optimizations**: SIMD potential updates made things worse

But fundamental algorithmic differences limit further gains.

## Optimization Opportunities

### For NetworkSimplex
1. **Parallelization**: 
   - Parallel search for negative reduced cost arcs
   - Parallel reduced cost updates
   - Challenge: Maintaining thread safety with spanning tree

2. **Tree operation optimization** (current bottleneck at 28%):
   - Better memory layout for tree traversal
   - Batch updates where possible
   - Consider alternative tree representations

3. **Warm start optimization**:
   - This is NetworkSimplex's key advantage
   - Further optimize for re-solve scenarios

### For Cost-Scaling Implementation
1. **Memory-efficient implementation**:
   - Learn from OR-Tools' space/time tradeoff
   - But optimize for C# memory model

2. **SIMD opportunities**:
   - Batch reduced cost checks
   - Parallel excess updates
   - Learn from our failed SIMD attempt (avoid gather/scatter)

3. **Hybrid warm-start**:
   - Extract spanning tree from cost-scaling solution
   - Initialize NetworkSimplex near optimality

## Proposed Solution: Hybrid Solver Approach

### Phase 1: Implement Cost-Scaling Push-Relabel
- Port LEMON's proven implementation
- Target 5-10x speedup for initial solves
- Three methods: PUSH, AUGMENT, PARTIAL_AUGMENT

### Phase 2: Optimize for C#/.NET
- Leverage unsafe code where beneficial
- Optimize data structures for .NET memory model
- Careful SIMD usage (avoiding previous pitfalls)

### Phase 3: Create Hybrid Solver
```csharp
public class HybridMinCostFlowSolver : IMinCostFlowSolver
{
    // Use CostScaling for cold starts
    // Use NetworkSimplex for warm starts
    // Decision based on availability of previous solution
}
```

### Phase 4: Advanced Optimizations
- Explore parallelization opportunities
- Problem-specific algorithm selection
- GPU acceleration for very large problems

## Implementation Roadmap

### Near Term (1-2 weeks)
1. Create `CostScaling.cs` implementing `IMinCostFlowSolver`
2. Port core algorithm from LEMON
3. Integrate with existing test infrastructure
4. Benchmark against OR-Tools

### Medium Term (3-4 weeks)
1. Optimize data structures and memory layout
2. Implement hybrid solver
3. Add warm-start conversion
4. Performance tuning

### Long Term (1-2 months)
1. Parallel algorithm exploration
2. Advanced heuristics
3. GPU investigation
4. Production hardening

## Risk Analysis

### Technical Risks
1. **Complexity**: Cost-scaling is algorithmically complex
   - Mitigation: Thorough testing against LEMON/OR-Tools
2. **Performance**: May not immediately match OR-Tools
   - Mitigation: Iterative optimization, profiling
3. **Integration**: Ensuring clean interfaces
   - Mitigation: Well-defined solver interface already exists

### Benefits
1. **Performance**: 5-10x speedup for initial solves expected
2. **Flexibility**: Best algorithm for each scenario
3. **Validation**: Two independent implementations
4. **Future-proof**: Foundation for more algorithms

## Conclusion

The 9x performance gap between our NetworkSimplex and OR-Tools is primarily due to algorithmic differences rather than implementation quality. While we've optimized NetworkSimplex well (29% improvement), fundamental limitations remain.

Implementing cost-scaling push-relabel will:
1. Provide competitive performance for initial solves
2. Maintain NetworkSimplex advantages for warm-start scenarios
3. Create a robust, flexible solver framework

This hybrid approach leverages the strengths of both algorithms, providing optimal performance across different use cases.