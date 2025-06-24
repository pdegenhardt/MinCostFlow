# Benchmark Data Directory

This directory contains test problems for validating and benchmarking the MinCostFlow solver.

## Directory Structure

- `small/` - Problems with < 1,000 nodes (for quick testing)
- `medium/` - Problems with 1,000 - 10,000 nodes
- `large/` - Problems with > 10,000 nodes
- `dimacs/` - Standard DIMACS benchmark problems
- `lemon/` - Problems converted from LEMON test suite

## Problem Format

All problems are in DIMACS minimum cost flow format:

```
c Comment lines start with 'c'
p min NODES ARCS
n NODE_ID SUPPLY
a FROM TO LOWER UPPER COST
```

## Known Solutions

Each problem file may have a corresponding `.sol` file containing:
- Optimal objective value
- Solve time from reference solver
- Problem characteristics

## Problem Sources

1. **LEMON Test Suite**: Converted from LEMON's test cases
2. **DIMACS Benchmarks**: Standard benchmark problems
3. **Generated Problems**: Synthetic problems with known structure

## Usage

To run benchmarks on these problems:

```bash
dotnet run --project src/MinCostFlow.Benchmarks -- --problem-dir benchmarks/data
```

To validate solutions:

```bash
dotnet run --project src/MinCostFlow.Validation -- benchmarks/data/small/test1.min
```