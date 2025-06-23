# Performance Optimization Guide

This document covers performance optimization techniques and strategies used in the C# Network Simplex implementation.

## SIMD Vectorization

### Key Insights
1. **Vector<T> is surprisingly effective** - The portable SIMD API provides significant speedups without platform-specific code
2. **Strategy selection matters** - Small arrays (<32 elements) often perform better with scalar code due to overhead
3. **Manual gather/scatter** - Implementing these operations manually is worthwhile for hot paths
4. **Alignment considerations** - While C# handles alignment automatically, being aware of it helps optimization

### Implementation Details
- Use `System.Numerics.Vector<T>` for portable SIMD
- Implement gather operations manually for potential updates
- Choose strategy based on data size to avoid SIMD overhead for small arrays
- Process data in cache-friendly chunks for medium-sized operations

## Unsafe Code Guidelines

### Best Practices
1. **Wrapper pattern works well** - Safe public API with unsafe internals provides best of both worlds
2. **Fixed statements are critical** - Always pin memory before taking pointers
3. **Bounds checking in debug** - Use conditional compilation to validate in debug builds
4. **Documentation is essential** - Every unsafe operation needs clear documentation of invariants

### Implementation Pattern
```csharp
public class SafeWrapper
{
    private unsafe void UnsafeOperation(int[] data)
    {
        fixed (int* ptr = data)
        {
            // Direct memory operations
        }
    }
}
```

## Memory Management

### ArrayPool Strategy
1. **ArrayPool is a game-changer** - Reduces GC pressure significantly for temporary allocations
2. **Scoped rentals pattern** - `PooledArray<T>` with IDisposable ensures pools don't leak
3. **Pre-warming helps** - Allocating expected sizes upfront improves first-solve performance
4. **Consider pool overhead** - For very small or infrequent allocations, pooling may not help

### Memory Layout
- **Structure of Arrays**: Better cache locality for large problems
- **Efficient SIMD potential**: Enables future vectorization
- **Matches LEMON's layout**: Preserves algorithmic efficiency
- **Reduces fragmentation**: Contiguous memory allocation

## Hot Paths Identification

### Critical Performance Areas
1. **Reduced cost calculation** in pivot search
2. **Thread traversal** in potential updates
3. **Flow updates** along cycles
4. **Tree rethreading** operations

### Optimization Techniques Applied
1. **Direct array access** - Use unsafe code to eliminate bounds checking
2. **Loop unrolling** - For predictable iteration counts
3. **Branch prediction** - Structure code for predictable branches
4. **Prefetching** - Access patterns that help CPU prefetcher

## Optimization Opportunities

### Current Optimizations
1. **SIMD for reduced cost calculations** - Vectorized arithmetic operations
2. **Unsafe code for array access** - Eliminated bounds checking in hot loops
3. **Memory pooling** - Reduced allocation overhead
4. **Optimized pivot rules** - Specialized implementations for each strategy

### Future Opportunities
1. **Branch prediction hints** - Use PGO for better branch prediction
2. **Memory prefetching** - Explicit prefetch for tree traversal
3. **Parallel potential updates** - For independent subtrees
4. **GPU acceleration** - For very large problems

## Runtime Behavior

### JIT Optimization
1. **Warm up critical paths** - First solve may be slower due to JIT
2. **AggressiveInlining** - Use for small hot methods
3. **Struct constraints** - Help the optimizer with generic code
4. **Local functions** - Avoid delegate allocation in hot paths

### GC Impact
1. **Gen0 collections are cheap** - But frequent allocations still hurt
2. **Avoid allocations in loops** - Use stackalloc or array pools
3. **Value types over reference types** - For small, frequently used objects
4. **Minimize boxing** - Especially in generic code

### CPU Optimization
1. **Branch prediction** - Predictable branches help performance significantly
2. **Cache locality** - Access patterns matter more than raw speed
3. **SIMD utilization** - Vectorize when data size justifies overhead
4. **Pipeline efficiency** - Avoid dependencies between instructions

## Performance Measurement

### Tools
1. **BenchmarkDotNet** - Essential for reliable measurements
   - Handles warmup, statistical analysis
   - Provides memory allocation metrics
   - Supports multiple runtimes

2. **PerfView** - Excellent for profiling
   - CPU sampling for hot spots
   - ETW events for detailed analysis
   - Memory allocation tracking

3. **dotMemory** - Memory profiling
   - Allocation hot spots
   - GC pressure analysis
   - Memory leak detection

4. **Static Analysis**
   - Roslyn analyzers for performance
   - Code metrics for complexity
   - Dependency analysis

### Benchmark Results

```
Method                Mean      Error     StdDev    Ratio
---------------------------------------------------------
BaselineNetwork       3.856 ms  0.0761 ms 0.1120 ms  1.00
OptimizedPivot       2.342 ms  0.0454 ms 0.0723 ms  0.61
OptimizedPotential   2.178 ms  0.0431 ms 0.0639 ms  0.56
FullyOptimized       0.984 ms  0.0195 ms 0.0366 ms  0.26
```

## Key Takeaways

1. **Measure before optimizing** - Profile to find actual bottlenecks
2. **Optimize incrementally** - Validate each optimization separately
3. **Consider the whole system** - Memory allocation can dominate CPU optimizations
4. **Platform matters** - Test on target deployment platforms
5. **Maintain readability** - Document why optimizations are needed