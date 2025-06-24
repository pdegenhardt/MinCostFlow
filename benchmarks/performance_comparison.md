# Performance Comparison Report
Generated: 2025-06-24 13:23:37 UTC

## Summary Table

| Problem | Nodes | Arcs | NetworkSimplex (ms) | OR-Tools (ms) | Winner | Speedup |
|---------|------:|-----:|--------------------:|--------------:|--------|---------|
| Small_Grid2x2 | 4 | 4 | 0.17 | 0.17 | OR-Tools | 1.04× |
| Small_Diamond | 4 | 4 | 0.23 | 0.33 | NetworkSimplex | 1.43× |
| Small_Simple4 | 4 | 5 | 0.17 | 0.19 | NetworkSimplex | 1.08× |
| Small_Path5 | 5 | 4 | 0.23 | 0.26 | NetworkSimplex | 1.16× |
| Small_Star | 5 | 8 | 0.19 | 0.16 | OR-Tools | 1.19× |
| Small_Transport2x3 | 5 | 6 | 0.22 | 0.26 | NetworkSimplex | 1.16× |
| Small_Cycle | 6 | 7 | 0.15 | 0.18 | NetworkSimplex | 1.24× |
| Small_Assignment3x3 | 6 | 9 | 0.16 | 0.20 | NetworkSimplex | 1.30× |
| Small_Path10 | 10 | 9 | 0.24 | 0.20 | OR-Tools | 1.21× |
| LEMON_Test12 | 12 | 21 | 0.40 | 0.35 | OR-Tools | 1.14× |
| Transport_100 | 20 | 100 | 0.36 | 0.44 | NetworkSimplex | 1.19× |
| Transport_500 | 44 | 484 | 0.70 | 0.82 | NetworkSimplex | 1.16× |
| Transport_5000 | 142 | 5,041 | 1.23 | 2.10 | NetworkSimplex | 1.71× |
| DIMACS_Netgen8_08a | 256 | 2,048 | 1.52 | 3.02 | NetworkSimplex | 1.98× |
| Circulation_1000 | 1,000 | 49,950 | 265.01 | 79.59 | OR-Tools | 3.33× |
| DIMACS_Netgen8_10a | 1,024 | 8,192 | 6.19 | 15.01 | NetworkSimplex | 2.42× |
| Circulation_5000 | 5,000 | 1,249,750 | 34509.02 | 2833.39 | OR-Tools | 12.18× |
| Circulation_6000 | 6,000 | 1,799,700 | 61440.75 | 3667.54 | OR-Tools | 16.75× |
| Path_10000 | 10,000 | 9,999 | 890.63 | 3371.47 | NetworkSimplex | 3.79× |
| Grid_100x100 | 10,000 | 39,600 | 122.99 | 90.72 | OR-Tools | 1.36× |

## Summary Statistics
- NetworkSimplex wins: 12/20 (60.0%)
- OR-Tools wins: 8/20 (40.0%)
- All optimal solutions match: ✓

## Detailed Results

### Small_Grid2x2
- Nodes: 4
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.17 (0.14-0.26) | 6 | 75 |
| OrTools | Optimal | 0.17 (0.16-0.18) | 13 | 75 |

### Small_Diamond
- Nodes: 4
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.23 (0.15-0.31) | 7 | 80 |
| OrTools | Optimal | 0.33 (0.17-0.64) | 13 | 80 |

### Small_Simple4
- Nodes: 4
- Arcs: 5

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.17 (0.13-0.22) | 10 | 30 |
| OrTools | Optimal | 0.19 (0.16-0.23) | 16 | 30 |

### Small_Path5
- Nodes: 5
- Arcs: 4

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.23 (0.17-0.31) | 8 | 100 |
| OrTools | Optimal | 0.26 (0.18-0.33) | 12 | 100 |

### Small_Star
- Nodes: 5
- Arcs: 8

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.19 (0.14-0.24) | 16 | 90 |
| OrTools | Optimal | 0.16 (0.15-0.18) | 16 | 90 |

### Small_Transport2x3
- Nodes: 5
- Arcs: 6

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.22 (0.19-0.24) | 13 | 85 |
| OrTools | Optimal | 0.26 (0.21-0.31) | 16 | 85 |

### Small_Cycle
- Nodes: 6
- Arcs: 7

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.15 (0.14-0.16) | 16 | 50 |
| OrTools | Optimal | 0.18 (0.16-0.26) | 16 | 50 |

### Small_Assignment3x3
- Nodes: 6
- Arcs: 9

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.16 (0.14-0.18) | 14 | 5 |
| OrTools | Optimal | 0.20 (0.16-0.29) | 16 | 5 |

### Small_Path10
- Nodes: 10
- Arcs: 9

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.24 (0.15-0.30) | 13 | 900 |
| OrTools | Optimal | 0.20 (0.17-0.28) | 13 | 900 |

### LEMON_Test12
- Nodes: 12
- Arcs: 21

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.40 (0.22-0.95) | 11 | 5,240 |
| OrTools | Optimal | 0.35 (0.27-0.45) | 19 | 5,240 |

### Transport_100
- Nodes: 20
- Arcs: 100

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.36 (0.31-0.44) | 16 | 7,400 |
| OrTools | Optimal | 0.44 (0.38-0.49) | 47 | 7,400 |

### Transport_500
- Nodes: 44
- Arcs: 484

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 0.70 (0.59-0.88) | 40 | 6,300 |
| OrTools | Optimal | 0.82 (0.60-1.31) | 170 | 6,300 |

### Transport_5000
- Nodes: 142
- Arcs: 5,041

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 1.23 (0.92-2.11) | 273 | 2,576 |
| OrTools | Optimal | 2.10 (1.77-2.85) | 1477 | 2,576 |

### DIMACS_Netgen8_08a
- Nodes: 256
- Arcs: 2,048

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 1.52 (1.28-1.95) | 144 | 142,274,536 |
| OrTools | Optimal | 3.02 (2.55-3.79) | 708 | 142,274,536 |

### Circulation_1000
- Nodes: 1,000
- Arcs: 49,950

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 265.01 (248.70-274.44) | 2527 | -21,815,005 |
| OrTools | Optimal | 79.59 (70.67-86.91) | 9852 | -21,815,005 |

### DIMACS_Netgen8_10a
- Nodes: 1,024
- Arcs: 8,192

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 6.19 (4.33-11.35) | 531 | 369,269,289 |
| OrTools | Optimal | 15.01 (13.26-19.01) | 1467 | 369,269,289 |

### Circulation_5000
- Nodes: 5,000
- Arcs: 1,249,750

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 34509.02 (34454.33-34554.49) | 60415 | -571,982,353 |
| OrTools | Optimal | 2833.39 (2745.19-2935.15) | 143979 | -571,982,353 |

### Circulation_6000
- Nodes: 6,000
- Arcs: 1,799,700

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 61440.75 (60418.39-62205.91) | 86850 | -827,519,645 |
| OrTools | Optimal | 3667.54 (3559.64-3769.71) | 298318 | -827,519,645 |

### Path_10000
- Nodes: 10,000
- Arcs: 9,999

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 890.63 (822.07-1000.47) | 1696 | 54,780,000 |
| OrTools | Optimal | 3371.47 (3238.49-3468.09) | 3037 | 54,780,000 |

### Grid_100x100
- Nodes: 10,000
- Arcs: 39,600

| Solver | Status | Time (ms) | Memory (KB) | Cost |
|--------|--------|-----------|-------------|------|
| NetworkSimplex | Optimal | 122.99 (116.96-131.17) | 3113 | 562,000 |
| OrTools | Optimal | 90.72 (87.72-96.21) | 13081 | 562,000 |

## Scalability Analysis

- NetworkSimplex scaling: ~0.032141 ms per arc
- OR-Tools scaling: ~0.001996 ms per arc
- Better scaling: OR-Tools

### What This Means

The scalability analysis uses linear regression to estimate how solution time increases with problem size (number of arcs):

- **NetworkSimplex**: Each additional arc adds approximately 0.032 ms to solution time
- **OR-Tools**: Each additional arc adds approximately 0.002 ms to solution time

**OR-Tools** scales better, with approximately 16.1× better performance growth as problem size increases.

**Practical Example**: For a problem with 100,000 arcs:
- NetworkSimplex estimated time: 3214 ms (3.2 seconds)
- OR-Tools estimated time: 200 ms (0.2 seconds)

*Note: This is a simplified linear model. Actual performance may vary based on problem structure, 
density, and other characteristics. Network flow algorithms typically have polynomial complexity.*
