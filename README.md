# MinCostFlow - C# Network Simplex Implementation

A high-performance C# implementation of the Network Simplex algorithm for solving Minimum Cost Flow (MCF) problems, based on the LEMON C++ library.

## Overview

This library provides an efficient solver for minimum cost flow problems in directed graphs. The implementation is a port of LEMON's highly optimized Network Simplex algorithm to C#, maintaining the performance characteristics while providing a modern .NET API.

### Features

- **High Performance**: Optimized implementation matching C++ performance
- **Multiple Pivot Rules**: Block Search, First Eligible, and Best Eligible strategies
- **Flexible API**: Support for various problem formulations
- **Memory Efficient**: Structure-of-arrays design for cache efficiency
- **.NET Integration**: Works seamlessly with modern .NET applications
- **Performance Optimizations**: SIMD vectorization, unsafe code paths, and memory pooling
- **Warm Start Support**: (Coming in Week 3) Reuse previous solutions
- **Incremental Updates**: (Coming in Week 3) Efficient arc modification

## Installation

```bash
dotnet add package MinCostFlow.Core
```

## Quick Start

```csharp
using MinCostFlow.Core;
using MinCostFlow.Core.Algorithms;

// Create a graph
var graph = new Graph();

// Add nodes
for (int i = 0; i < 4; i++)
{
    graph.AddNode();
}

// Add arcs with (from, to, capacity, cost)
var arc1 = graph.AddArc(0, 1, 10, 2);  // capacity: 10, cost: 2
var arc2 = graph.AddArc(0, 2, 8, 3);
var arc3 = graph.AddArc(1, 3, 5, 1);
var arc4 = graph.AddArc(2, 3, 10, 4);

// Create solver with optimizations
var solver = new NetworkSimplex<int, int>(graph)
    .EnableOptimizedPivot()
    .EnableOptimizedPotentialUpdate();

// Set supply/demand
solver.SupplyMap(node => node == 0 ? 15 : node == 3 ? -15 : 0);

// Solve
var result = solver.Run();

if (result == NetworkSimplex<int, int>.ProblemType.Optimal)
{
    Console.WriteLine($"Total cost: {solver.TotalCost()}");
    
    // Get flow on each arc
    foreach (var arc in graph.Arcs)
    {
        Console.WriteLine($"Flow on arc {arc}: {solver.Flow(arc)}");
    }
}
```

## Performance

The implementation achieves excellent performance through various optimizations:

### Benchmark Results (10,000 nodes, 30,000 arcs)
| Configuration | Time | Relative |
|--------------|------|----------|
| Baseline | 3.86ms | 1.00x |
| Optimized Pivot | 2.34ms | 1.65x |
| Optimized Potential | 2.18ms | 1.77x |
| Fully Optimized | 0.98ms | 3.94x |

**Key Achievement**: Solving 10,000 node problems in <1ms (target was <1000ms)

## Project Status

### Completed (Weeks 1-2)
- ✅ Core Network Simplex algorithm
- ✅ Graph data structures  
- ✅ Three pivot strategies
- ✅ SIMD optimizations
- ✅ Unsafe code optimizations
- ✅ Memory pooling
- ✅ Comprehensive test suite

### In Progress (Week 3)
- 🚧 Warm start functionality
- 🚧 Incremental arc modifications
- 🚧 Time-expanded network helpers
- 🚧 OR-Tools performance comparison

## Documentation

- [API Reference](docs/api-reference.md)
- [Implementation Plan](docs/initial_plan.md)
- [Week 2 Progress Report](docs/week2-progress.md)
- [Performance Optimization](docs/performance-optimization.md)
- [Algorithm Implementation](docs/algorithm-implementation.md)
- [Development Guide](docs/development-guide.md)
- [Platform & Architecture](docs/platform-architecture.md)

## Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/MinCostFlow.git
cd MinCostFlow

# Build
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run --project src/MinCostFlow.Benchmarks -c Release
```

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests to our repository.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

This implementation is based on the LEMON C++ library's Network Simplex algorithm. LEMON is a C++ template library for efficient modeling and optimization in networks.