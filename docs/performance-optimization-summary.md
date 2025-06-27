# NetworkSimplex Performance Optimization Summary

## Date: 2025-06-24

## Problem Statement
The NetworkSimplex solver showed severe performance degradation on dense networks with high arc counts:
- Circulation_1000 (49,950 arcs): 265ms vs OR-Tools 80ms (3.3× slower)
- Circulation_5000 (1.25M arcs): 34.5s vs OR-Tools 2.8s (12× slower)  
- Circulation_6000 (1.8M arcs): 61.4s vs OR-Tools 3.7s (17× slower)

## Root Cause Analysis
The primary bottleneck was identified in the pivot search phase:
1. **Block size too large**: For dense networks, block size of √m (√49,950 ≈ 225) meant checking hundreds of arcs before early termination
2. **No adaptation**: Fixed block size didn't adjust to problem characteristics
3. **Poor cache locality**: Accessing node potentials for many different nodes caused cache misses

## Implementation Summary

### 1. Feature Flags Framework
Created `OptimizationFlags` enum with options:
- `AdaptiveBlockSize`: Dynamically adjust block size based on hit rate
- `SmallBlocksForDense`: Use smaller initial blocks for dense networks
- `ReducedCostCaching`: (planned) Cache reduced costs
- `CandidateListPivot`: (planned) Maintain list of promising arcs

### 2. Profiling Infrastructure
Added comprehensive metrics tracking:
- Time breakdown by phase (pivot search, tree update, potential update)
- Iterations count
- Block size evolution
- Average arcs checked per pivot

### 3. Optimizations Implemented

#### SmallBlocksForDense
- For networks with density > 10 arcs/node, use block_size = min(50, √m/4)
- Reduces initial block size from 225 to 50 for Circulation_1000

#### AdaptiveBlockSize  
- Track hit rate (1/arcs_checked_to_find_negative)
- If hit rate < 10%, reduce block size by 50%
- If hit rate > 50%, increase block size by 150%
- Clamp between MinBlockSize (10) and MaxBlockSize (100)

### 4. Quick Benchmark Tool
Created `--quick-opt` command for rapid iteration:
```bash
dotnet run -- --quick-opt
```

## Results

### Initial Test Results
```
Configuration       Time(ms)  Iter  BlockSize    AvgArcs/Pivot
Baseline             1039.2  96258  225->225           233
SmallBlocks           760.8 124916   50->50             56
AdaptiveBlocks        803.4 191451  225->10             16
Both                  819.0 193157   50->10             15
```

### Key Findings
1. **SmallBlocks optimization**: 27% speedup (1039ms → 761ms)
   - Reduces average arcs checked from 233 to 56 per pivot
   - Slight increase in iterations due to less greedy pivot selection

2. **AdaptiveBlocks**: Mixed results
   - Successfully adapts block size (225→10)
   - Dramatically reduces arcs checked per pivot (233→16)
   - But causes ~2x more iterations, suggesting overly aggressive reduction

3. **Combined approach**: Needs tuning
   - Both optimizations together don't stack well
   - Suggests need for better adaptive parameters

## Next Steps

### Immediate Actions
1. **Tune adaptive parameters**:
   - Adjust hit rate thresholds (currently 10%/50%)
   - Use gentler growth/shrink factors (currently 1.5x/0.5x)
   - Consider minimum arcs checked before adapting

2. **Fix timing resolution**:
   - Use higher precision timers (Stopwatch.GetTimestamp)
   - Accumulate timing over multiple iterations

3. **Implement reduced cost caching**:
   - Expected 20-30% additional improvement
   - Avoid recalculating for all 50k arcs each iteration

### Medium Term
1. **Candidate list pivot**: Maintain list of promising arcs
2. **Better heuristics**: Use problem structure to guide initial block size
3. **Profile-guided optimization**: Auto-tune parameters based on problem type

### Long Term
1. **Parallel pivot search** for very large problems
2. **SIMD optimizations** for reduced cost calculations
3. **GPU acceleration** for problems with >1M arcs

## Lessons Learned
1. **Feature flags are essential**: Allow testing optimizations independently
2. **Metrics matter**: Detailed profiling revealed unexpected behavior
3. **One size doesn't fit all**: Different problem structures need different strategies
4. **Iteration count matters**: Faster pivot search can lead to more iterations

## Conclusion
We've built a solid foundation for performance optimization with:
- Feature flags framework for A/B testing
- Comprehensive profiling infrastructure  
- Quick iteration benchmark tool
- Initial optimizations showing 27% improvement

While we haven't yet matched OR-Tools performance, we have a clear path forward with tuning and additional optimizations. The adaptive approach shows promise but needs refinement to balance pivot quality with search efficiency.