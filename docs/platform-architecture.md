# Platform and Architecture Guide

This document covers platform-specific considerations, architectural decisions, and deployment strategies for the Network Simplex implementation.

## C# vs C++ Considerations

### Language Feature Mapping

| C++ Feature | C# Equivalent | Notes |
|------------|---------------|-------|
| Templates | Generics | Use constraints for better optimization |
| Pointers | Unsafe code / Span<T> | Prefer Span<T> when possible |
| STL containers | .NET collections | Consider performance characteristics |
| RAII | IDisposable / using | Deterministic cleanup |
| Macros | Constants / Methods | No preprocessing needed |
| Header files | Single file | Simpler compilation model |

### Performance Characteristics
- **JIT Compilation**: First run slower, subsequent runs optimized
- **Garbage Collection**: Minimize allocations in hot paths
- **Bounds Checking**: Can be disabled with unsafe code
- **Virtual Dispatch**: Avoid in performance-critical code
- **Struct Layout**: Control with StructLayout attribute if needed

### Memory Model Differences
```csharp
// C++ style (not available in C#)
// int* array = new int[size];
// delete[] array;

// C# managed arrays
int[] array = new int[size]; // GC managed

// C# with explicit control
int[] array = ArrayPool<int>.Shared.Rent(size);
try
{
    // Use array
}
finally
{
    ArrayPool<int>.Shared.Return(array);
}
```

## Language Features Utilization

### Modern C# Features

1. **Span<T> for Slicing**
```csharp
Span<int> subtree = _nodeList.AsSpan(start, count);
foreach (int node in subtree)
{
    _potential[node] += delta;
}
```

2. **ValueTask for Async**
```csharp
public ValueTask<ProblemType> RunAsync()
{
    if (IsTrivial())
        return new ValueTask<ProblemType>(ProblemType.Optimal);
    
    return new ValueTask<ProblemType>(RunAsyncCore());
}
```

3. **Pattern Matching**
```csharp
public static bool IsOptimal(ProblemType result) => result switch
{
    ProblemType.Optimal => true,
    ProblemType.Infeasible => false,
    ProblemType.Unbounded => false,
    _ => throw new ArgumentException("Unknown problem type")
};
```

4. **Local Functions**
```csharp
private void UpdatePotentials(int root, int delta)
{
    void UpdateSubtree(int node)
    {
        _potential[node] += delta;
        // No allocation for delegate
    }
    
    TraverseSubtree(root, UpdateSubtree);
}
```

### Generic Constraints
```csharp
public class NetworkSimplex<TFlow, TCost>
    where TFlow : struct, INumber<TFlow>
    where TCost : struct, INumber<TCost>
{
    // Enables better JIT optimization
    // Prevents boxing
    // Allows math operations
}
```

## Architecture Decisions

### Modular Design
```
Core Algorithm
    ├── Graph Abstraction
    ├── Solver Implementation
    └── Optimization Modules
         ├── SIMD Module
         ├── Unsafe Module
         └── Memory Pool Module
```

### Extension Points
1. **Custom Graph Implementations** - Interface-based design
2. **Pivot Rule Plugins** - Strategy pattern for pivot selection
3. **Cost Computation** - Pluggable cost functions
4. **Progress Reporting** - Event-based notifications

### Thread Safety Model
- **Immutable Graph** - Graph structure is read-only during solve
- **Solver State** - Not thread-safe, create per-thread instances
- **Memory Pools** - Thread-safe with minimal contention
- **Results** - Immutable once computed

## Deployment Scenarios

### Desktop Applications
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

### Server Applications
- Use Server GC for better throughput
- Pre-JIT critical paths on startup
- Monitor memory pool usage
- Consider CPU affinity for large problems

### Cloud Functions
```csharp
public class MinCostFlowFunction
{
    private static readonly SolverMemoryPool Pool = new();
    
    static MinCostFlowFunction()
    {
        // Pre-warm pools
        Pool.WarmUp(expectedSize: 10000);
    }
    
    public async Task<Result> Solve(Problem problem)
    {
        var solver = new NetworkSimplex<int, int>(problem.Graph)
            .SetMemoryPool(Pool);
        
        return await solver.RunAsync();
    }
}
```

### Container Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
ENV DOTNET_gcServer=1
ENV DOTNET_GCHeapCount=4

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . .
RUN dotnet publish -c Release -r linux-x64

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["./MinCostFlow.App"]
```

## Platform Considerations

### Cross-Platform Testing
1. **Windows** - Primary development platform
2. **Linux** - Server deployment target
3. **macOS** - Developer machines
4. **ARM64** - Growing server platform

### Native AOT
```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <OptimizationPreference>Speed</OptimizationPreference>
</PropertyGroup>
```

Benefits:
- Instant startup
- Lower memory usage
- Predictable performance
- No JIT overhead

Limitations:
- Larger binary size
- Limited reflection
- Compile-time generic instantiation

### Performance Tuning by Platform

#### Windows
```csharp
if (OperatingSystem.IsWindows())
{
    // Use Windows-specific optimizations
    Process.GetCurrentProcess().PriorityClass = 
        ProcessPriorityClass.High;
}
```

#### Linux
```csharp
if (OperatingSystem.IsLinux())
{
    // Consider NUMA nodes for large problems
    // Use transparent huge pages
}
```

## Future Scalability

### Parallelization Opportunities
1. **Independent Subtree Updates** - Parallel potential updates
2. **Block Search Parallelization** - Evaluate blocks concurrently
3. **Multi-Start Strategies** - Solve from different initial bases
4. **Problem Decomposition** - Split large problems

### GPU Acceleration Potential
- Reduced cost calculation for all arcs
- Matrix operations for specialized variants
- Parallel graph algorithms
- Consider CUDA.NET or OpenCL.NET

### Distributed Computing
```csharp
public interface IDistributedSolver
{
    Task<ProblemType> SolvePartition(GraphPartition partition);
    Task<Solution> CombineSolutions(IEnumerable<Solution> partials);
}
```

## Monitoring and Observability

### Performance Metrics
```csharp
public class SolverMetrics
{
    public Counter IterationCount { get; }
    public Histogram IterationDuration { get; }
    public Gauge ActiveMemoryPools { get; }
    
    public void RecordIteration(TimeSpan duration)
    {
        IterationCount.Inc();
        IterationDuration.Observe(duration.TotalMilliseconds);
    }
}
```

### Integration with APM
- OpenTelemetry support
- Application Insights integration
- Custom ETW events for detailed tracing
- Performance counters for monitoring

## Key Architectural Principles

1. **Modularity** - Optimizations are optional and pluggable
2. **Performance** - Zero-cost abstractions where possible
3. **Usability** - Simple API for common cases
4. **Extensibility** - Support advanced scenarios
5. **Portability** - Run anywhere .NET runs