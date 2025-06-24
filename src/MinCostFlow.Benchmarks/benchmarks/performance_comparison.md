# Performance Comparison Report
Generated: 2025-06-24 13:03:31 UTC

## Summary Table

| Problem | Nodes | Arcs | NetworkSimplex (ms) | OR-Tools (ms) | Winner | Speedup |
|---------|------:|-----:|--------------------:|--------------:|--------|---------|
| Small_Grid2x2 | 4 | 4 | 0.34 | 0.37 | NetworkSimplex | 1.08× |
| Small_Diamond | 4 | 4 | 0.47 | 0.38 | OR-Tools | 1.23× |
| Small_Simple4 | 4 | 5 | 0.35 | 0.40 | NetworkSimplex | 1.14× |
| Small_Path5 | 5 | 4 | 0.42 | 0.31 | OR-Tools | 1.35× |
| Small_Star | 5 | 8 | 0.42 | 0.40 | OR-Tools | 1.04× |
| Small_Transport2x3 | 5 | 6 | 0.52 | 0.49 | OR-Tools | 1.06× |
| Small_Cycle | 6 | 7 | 0.45 | 0.53 | NetworkSimplex | 1.17× |
| Small_Assignment3x3 | 6 | 9 | 0.47 | 0.52 | NetworkSimplex | 1.11× |
| Small_Path10 | 10 | 9 | 0.27 | 0.27 | NetworkSimplex | 1.01× |
| LEMON_Test12 | 12 | 21 | 0.29 | 0.36 | NetworkSimplex | 1.27× |
| Transport_100 | 20 | 100 | 0.60 | INFEASIBLE | NetworkSimplex | N/A |
| Transport_500 | 44 | 484 | 0.79 | INFEASIBLE | NetworkSimplex | N/A |
| Transport_5000 | 142 | 5,041 | 2.07 | INFEASIBLE | NetworkSimplex | N/A |
| DIMACS_Netgen8_08a | 256 | 2,048 | 1.92 | 4.72 | NetworkSimplex | 2.46× |
| Circulation_1000 | 1,000 | 49,950 | 229.61 | 82.33 | OR-Tools | 2.79× |
| DIMACS_Netgen8_10a | 1,024 | 8,192 | 6.67 | 17.96 | NetworkSimplex | 2.69× |
| Path_10000 | 10,000 | 9,999 | 889.75 | 4275.65 | NetworkSimplex | 4.81× |
| Grid_100x100 | 10,000 | 39,600 | 137.26 | 118.70 | OR-Tools | 1.16× |

## Summary Statistics
- NetworkSimplex wins: 9/15 (60.0%)
- OR-Tools wins: 6/15 (40.0%)
- All optimal solutions match: ✓

## Detailed Results

### Small_Grid2x2
- Nodes: 4
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.34 (0.25-0.38) | 6 | 75 |
| OrTools | Optimal | 0.37 (0.28-0.45) | 13 | 75 |

### Small_Diamond
- Nodes: 4
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.47 (0.34-0.55) | 5 | 80 |
| OrTools | Optimal | 0.38 (0.28-0.48) | 8 | 80 |

### Small_Simple4
- Nodes: 4
- Arcs: 5

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.35 (0.30-0.44) | 8 | 30 |
| OrTools | Optimal | 0.40 (0.33-0.54) | 8 | 30 |

### Small_Path5
- Nodes: 5
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.42 (0.24-1.05) | 8 | 100 |
| OrTools | Optimal | 0.31 (0.24-0.48) | 12 | 100 |

### Small_Star
- Nodes: 5
- Arcs: 8

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.42 (0.22-0.62) | 11 | 90 |
| OrTools | Optimal | 0.40 (0.24-0.53) | 11 | 90 |

### Small_Transport2x3
- Nodes: 5
- Arcs: 6

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.52 (0.47-0.58) | 10 | 85 |
| OrTools | Optimal | 0.49 (0.43-0.52) | 10 | 85 |

### Small_Cycle
- Nodes: 6
- Arcs: 7

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.45 (0.38-0.54) | 8 | 50 |
| OrTools | Optimal | 0.53 (0.51-0.58) | 11 | 50 |

### Small_Assignment3x3
- Nodes: 6
- Arcs: 9

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.47 (0.38-0.57) | 10 | 5 |
| OrTools | Optimal | 0.52 (0.46-0.57) | 12 | 5 |

### Small_Path10
- Nodes: 10
- Arcs: 9

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.27 (0.23-0.40) | 13 | 900 |
| OrTools | Optimal | 0.27 (0.24-0.32) | 13 | 900 |

### LEMON_Test12
- Nodes: 12
- Arcs: 21

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.29 (0.22-0.37) | 6 | 5,240 |
| OrTools | Optimal | 0.36 (0.33-0.41) | 19 | 5,240 |

### Transport_100
- Nodes: 20
- Arcs: 100

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.60 (0.48-0.71) | 16 | 6,700 |

### Transport_500
- Nodes: 44
- Arcs: 484

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.79 (0.72-0.84) | 40 | 6,300 |

### Transport_5000
- Nodes: 142
- Arcs: 5,041

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 2.07 (1.11-5.40) | 272 | 2,450 |

### DIMACS_Netgen8_08a
- Nodes: 256
- Arcs: 2,048

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 1.92 (1.61-2.47) | 140 | 142,274,536 |
| OrTools | Optimal | 4.72 (3.81-7.66) | 707 | 142,274,536 |

### Circulation_1000
- Nodes: 1,000
- Arcs: 49,950

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 229.61 (223.82-239.73) | 2527 | -21,815,005 |
| OrTools | Optimal | 82.33 (76.12-93.69) | 10311 | -21,815,005 |

### DIMACS_Netgen8_10a
- Nodes: 1,024
- Arcs: 8,192

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 6.67 (4.47-11.16) | 531 | 369,269,289 |
| OrTools | Optimal | 17.96 (15.08-25.06) | 1468 | 369,269,289 |

### Path_10000
- Nodes: 10,000
- Arcs: 9,999

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 889.75 (790.77-1070.99) | 1697 | 54,780,000 |
| OrTools | Optimal | 4275.65 (3676.09-4962.10) | 3039 | 54,780,000 |

### Grid_100x100
- Nodes: 10,000
- Arcs: 39,600

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 137.26 (121.94-162.46) | 3113 | 562,000 |
| OrTools | Optimal | 118.70 (109.37-127.25) | 13082 | 562,000 |

## Scalability Analysis

- NetworkSimplex scaling: ~0.004829 ms per arc
- OR-Tools scaling: ~0.005463 ms per arc
- Better scaling: NetworkSimplex

### What This Means

The scalability analysis uses linear regression to estimate how solution time increases with problem size (number of arcs):

- **NetworkSimplex**: Each additional arc adds approximately 0.005 ms to solution time
- **OR-Tools**: Each additional arc adds approximately 0.005 ms to solution time

**NetworkSimplex** scales better, with approximately 1.1× better performance growth as problem size increases.

**Practical Example**: For a problem with 100,000 arcs:
- NetworkSimplex estimated time: 483 ms (0.5 seconds)
- OR-Tools estimated time: 546 ms (0.5 seconds)

*Note: This is a simplified linear model. Actual performance may vary based on problem structure, 
density, and other characteristics. Network flow algorithms typically have polynomial complexity.*
