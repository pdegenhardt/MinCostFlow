# Development Guide

This guide covers development practices, testing strategies, and code organization principles for the Network Simplex implementation.

## Porting from C++

### Core Principles
1. **Preserve algorithmic structure** - Don't try to "improve" the algorithm during porting
2. **Memory layout matters** - Structure-of-arrays pattern from LEMON is worth preserving
3. **Integer indices over references** - Using int indices is more cache-friendly than object references
4. **Batch operations** - Process multiple elements together when possible

### Translation Patterns
```cpp
// C++ LEMON pattern
for (int a = _first_out[n]; a != _first_out[n+1]; ++a) {
    // Process arc a
}
```

```csharp
// C# equivalent
for (int a = _firstOut[n]; a < _firstOut[n + 1]; a++) {
    // Process arc a
}
```

### Key Differences
- No pointer arithmetic - use array indexing
- No macros - use const fields or methods
- Exception safety by default - no manual cleanup needed
- Generics instead of templates - similar but with constraints

## Testing Strategy

### Test Categories

1. **Unit Tests** - Test individual components
   - Graph operations (add/remove nodes/arcs)
   - Tree operations (thread updates, LCA)
   - Pivot rules (correctness of selection)

2. **Integration Tests** - Test algorithm behavior
   - Small hand-crafted problems with known solutions
   - LEMON test case ports for validation
   - Edge cases (disconnected graphs, zero capacity)

3. **Property-Based Tests** - Generate random problems
   - Random graph generation with valid supplies
   - Compare against reference implementation
   - Verify optimality conditions

4. **Performance Tests** - Regression prevention
   - Benchmark standard problem sizes
   - Track memory allocations
   - Monitor optimization effectiveness

### Test Implementation
```csharp
[Fact]
public void TestOptimalityConditions()
{
    // Arrange
    var (graph, solver) = CreateTestProblem();
    
    // Act
    var result = solver.Run();
    
    // Assert
    Assert.Equal(ProblemType.Optimal, result);
    AssertComplementarySlackness(solver);
    AssertFlowConservation(solver);
    AssertCapacityConstraints(solver);
}
```

## API Design

### Fluent Configuration
```csharp
var solver = new NetworkSimplex<int, int>(graph)
    .LowerMap(arc => 0)
    .UpperMap(arc => capacities[arc])
    .CostMap(arc => costs[arc])
    .SupplyMap(node => supplies[node])
    .EnableOptimizedPivot()
    .EnableOptimizedPotentialUpdate();
```

### Error Handling
```csharp
public NetworkSimplex<TFlow, TCost> SupplyMap(Func<int, TFlow> map)
{
    if (map == null)
        throw new ArgumentNullException(nameof(map));
        
    // Validate supply feasibility
    TFlow totalSupply = default;
    for (int i = 0; i < _nodeCount; i++)
    {
        var supply = map(i);
        totalSupply = AddFlow(totalSupply, supply);
    }
    
    if (!IsZero(totalSupply))
        throw new InvalidOperationException(
            "Total supply must equal total demand");
            
    _supplyMap = map;
    return this;
}
```

### Performance Documentation
```csharp
/// <summary>
/// Finds entering arc using block search pivot rule.
/// </summary>
/// <remarks>
/// Time complexity: O(m/k) average where k is block size.
/// Space complexity: O(1).
/// This method is called once per simplex iteration.
/// </remarks>
private int FindEnteringArcBlockSearch() { ... }
```

## Code Organization

### Project Structure
```
MinCostFlow/
├── src/
│   ├── MinCostFlow.Core/           # Main library
│   │   ├── Algorithms/             # Algorithm implementations
│   │   │   ├── NetworkSimplex.cs
│   │   │   └── Internal/          # Non-public implementations
│   │   ├── Graphs/                # Graph data structures
│   │   └── Utils/                 # Utilities (memory pools, etc)
│   ├── MinCostFlow.Tests/         # Test project
│   └── MinCostFlow.Benchmarks/    # Performance benchmarks
└── docs/                          # Documentation
```

### Internal Organization
- **Internal namespace** - Hide implementation details from public API
- **Partial classes** - Separate algorithm phases into logical files
- **Nested classes** - Keep related types together (e.g., TreeStructure)
- **Extension methods** - Optional functionality without cluttering main API

### Separation of Concerns
```csharp
// Main algorithm class - public API
public partial class NetworkSimplex<TFlow, TCost>
{
    public NetworkSimplex<TFlow, TCost> EnableOptimizedPivot() { ... }
}

// Internal optimization implementations
internal static class BlockSearchPivotOptimized
{
    public static unsafe int FindEnteringArc(...) { ... }
}

// Utility classes
public sealed class SolverMemoryPool { ... }
```

## Development Process

### Incremental Development
1. **Start with correctness** - Get a working implementation before optimizing
2. **Profile before optimizing** - Measure to find actual bottlenecks
3. **Optimize incrementally** - Add one optimization at a time
4. **Maintain baseline** - Keep unoptimized version for comparison

### Code Review Checklist
- [ ] Algorithm correctness verified
- [ ] Unit tests cover new functionality
- [ ] Performance impact measured
- [ ] Documentation updated
- [ ] No memory leaks (dispose patterns followed)
- [ ] Thread safety considered
- [ ] API consistency maintained

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Solves the minimum cost flow problem.
/// </summary>
/// <returns>
/// The problem type (Optimal, Infeasible, or Unbounded).
/// </returns>
/// <exception cref="InvalidOperationException">
/// Thrown if the problem is not properly configured.
/// </exception>
/// <remarks>
/// This method implements the primal Network Simplex algorithm.
/// Time complexity: O(n²m) worst case, O(nm) expected.
/// Space complexity: O(n + m).
/// </remarks>
public ProblemType Run() { ... }
```

#### Code Comments
```csharp
// Find entering arc using block search
// We examine sqrt(m) candidates per iteration for efficiency
private int FindEnteringArcBlockSearch()
{
    // Initialize block size based on Goldfarb & Reid's recommendation
    int blockSize = Math.Max(1, (int)Math.Sqrt(_arcCount));
    
    // Start from where we left off in previous iteration
    // This gives better empirical performance
    int startArc = _nextArc;
    
    // ... implementation
}
```

## Best Practices Summary

1. **Keep public API minimal** - Expose only what users need
2. **Validate inputs early** - Fail fast with clear messages
3. **Document performance** - Big-O complexity and practical considerations
4. **Test edge cases** - Empty graphs, single nodes, zero flows
5. **Benchmark everything** - Measure impact of changes
6. **Maintain compatibility** - Don't break existing users
7. **Follow C# conventions** - PascalCase, async patterns, etc.