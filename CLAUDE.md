# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains a C# port implementation plan for LEMON's Network Simplex algorithm for solving minimum cost flow problems. The main components are:

- `/lemon-1.3.1/`: LEMON C++ library source code (reference implementation)
- `/docs/initial_plan.md`: Detailed implementation plan for porting to C#

## Key LEMON Source Files

When implementing the C# port, reference these critical files:

- `lemon-1.3.1/lemon/network_simplex.h`: Main Network Simplex algorithm implementation (~3000 lines)
- `lemon-1.3.1/lemon/core.h`: Core graph interfaces
- `lemon-1.3.1/lemon/circulation.h`: Related flow algorithms
- `lemon-1.3.1/test/min_cost_flow_test.cc`: Test cases and validation data

## Build Commands

### LEMON C++ Library (for reference/testing)
```bash
cd lemon-1.3.1
mkdir build && cd build
cmake ..
make
make test  # Run all tests
./test/min_cost_flow_test  # Run specific test
```

### C# Project (when created)
The C# project structure should follow the plan in `/docs/initial_plan.md`. Expected commands:
```bash
dotnet build
dotnet test
dotnet run --project MinCostFlow.Benchmarks
```

## Architecture Overview

### LEMON's Network Simplex Key Concepts

1. **Block Search Pivot Rule**: Examines √m arc candidates per iteration for efficiency
2. **Strongly Feasible Basis**: Maintains feasibility to prevent cycling
3. **Thread Index Structure**: Enables O(1) spanning tree updates
4. **Memory Layout**: Structure-of-arrays pattern for cache efficiency

### Critical Performance Optimizations to Port

1. **Compact Graph Representation**: 
   - Forward/backward arc lists
   - 0-based integer indexing
   - Separate arrays for properties

2. **Spanning Tree Operations**:
   - Thread index for fast updates
   - Depth tracking for LCA queries
   - Efficient subtree operations

3. **Pivot Strategies**:
   - Block search (default)
   - First eligible (fallback)
   - Best eligible (for small problems)

## Performance Targets

From `/docs/initial_plan.md`:
- Initial solve: <1s for 10,000 nodes
- Re-solve with warm start: <100ms
- Arc modification evaluation: <50ms
- Memory usage: <200MB

## Testing Approach

1. Port test cases from `lemon-1.3.1/test/min_cost_flow_test.cc`
2. Use DIMACS benchmark problems
3. Create time-expanded network test cases
4. Validate against LEMON results for correctness

## Development Workflow

When implementing the C# port:

1. Study the corresponding LEMON source file
2. Follow the structure outlined in `/docs/initial_plan.md`
3. Maintain LEMON's algorithmic optimizations
4. Use unsafe code and SIMD where beneficial for performance
5. Implement comprehensive tests comparing with LEMON output