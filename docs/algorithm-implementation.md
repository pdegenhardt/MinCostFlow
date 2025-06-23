# Algorithm Implementation Details

This document covers the core algorithm implementation details, data structures, and debugging techniques for the Network Simplex solver.

## Thread Index Structure

The thread index is the most complex and critical part of the Network Simplex implementation. It enables O(1) tree updates by maintaining a preorder traversal of the spanning tree as a doubly-linked circular list.

### Key Invariants
- `Thread[u]` points to the next node in preorder traversal
- `RevThread[u]` points to the previous node
- `Thread[LastSucc[u]]` points to the node after u's subtree
- `SuccNum[u]` = size of subtree rooted at u
- The thread forms a cycle: following Thread eventually returns to start

### Why Thread Index?
- **O(1) subtree identification** - Can iterate subtree without recursion
- **O(1) tree updates** - Rethreading is local operation (vs O(n) for parent-pointer trees)
- **Efficient potential updates** - Natural iteration order for tree operations
- **Cache-friendly traversal** - Sequential access pattern

## Algorithm Flow

### 1. Initialization Phase
```
1. Transform problem (handle lower bounds)
   - Shift flows to make lower bounds zero
   - Adjust supplies accordingly

2. Create artificial root node
   - Index = _nodeCount
   - Connected to all nodes via artificial arcs

3. Build initial spanning tree with artificial arcs
   - Nodes with supply > 0: arc from node to root
   - Nodes with supply ≤ 0: arc from root to node
   - Set artificial arc costs based on sum of all costs

4. Initialize thread indices
   - Thread links nodes in preorder traversal
   - RevThread provides backward links
   - Set SuccNum for subtree sizes

5. Set initial flows and potentials
   - Artificial arc flows = absolute supply values
   - Potentials computed from artificial arc costs
```

### 2. Main Simplex Loop
```
while (entering arc exists):
    1. Find entering arc (negative reduced cost)
       - Use selected pivot rule (Block/First/Best)
       - Identify non-basic arc violating optimality

    2. Find join node (LCA of entering arc endpoints)
       - Traverse from both endpoints toward root
       - First common node is the join

    3. Find leaving arc (minimum ratio test)
       - Compute flow change limits along cycle
       - Select arc that reaches bound first

    4. Update flows along cycle
       - Add/subtract flow change along forward/backward arcs
       - Update arc states (basic/non-basic)

    5. Update spanning tree structure
       - Remove leaving arc from tree
       - Add entering arc to tree
       - Rethread affected subtrees

    6. Update node potentials
       - Only nodes in modified subtree need updates
       - Use thread structure for efficient traversal
```

### 3. Termination Check
```
if (all artificial arcs have zero flow):
    return OPTIMAL
else:
    return INFEASIBLE
```

## Data Structure Design

### Structure of Arrays Pattern
```csharp
// Instead of:
class Arc {
    int Source, Target, Flow, Capacity, Cost;
}
Arc[] arcs;

// We use:
int[] _arcSource;
int[] _arcTarget; 
int[] _arcFlow;
int[] _arcCapacity;
int[] _arcCost;
```

**Benefits:**
- Better cache locality for large problems
- Efficient SIMD operations possible
- Reduced memory fragmentation
- Matches LEMON's proven design

### Tree Maintenance Arrays
- `Parent[]`: Parent node in spanning tree
- `PredArc[]`: Arc from parent to node
- `Thread[]`: Next node in preorder traversal
- `RevThread[]`: Previous node in preorder traversal
- `SuccNum[]`: Size of subtree rooted at node
- `LastSucc[]`: Last descendant in subtree

## Common Implementation Pitfalls

### 1. Array Bounds
- **Issue**: Root node needs space in arrays
- **Solution**: Allocate arrays with size `_nodeCount + 1`

### 2. Thread Initialization
- **Issue**: Off-by-one errors in circular linking
- **Solution**: Careful validation with cycle detection

### 3. State Consistency
- **Issue**: Arc states must match tree structure
- **Solution**: Update State array atomically with tree changes

### 4. Potential Updates
- **Issue**: Must traverse exact subtree affected by pivot
- **Solution**: Use thread structure to iterate subtree nodes

### 5. Numerical Precision
- **Issue**: Floating-point arithmetic in costs
- **Solution**: Use integer costs when possible, or careful epsilon comparisons

## Debugging Techniques

### Thread Structure Validation
```csharp
bool ValidateThread()
{
    var visited = new bool[_nodeCount + 1];
    int count = 0;
    int u = _root;
    do
    {
        if (visited[u]) return false; // Cycle detected
        visited[u] = true;
        count++;
        u = _tree.Thread[u];
    } while (u != _root);
    return count == _nodeCount + 1; // Should visit all nodes + root
}
```

### Tree Consistency Check
```csharp
bool ValidateTree()
{
    // Check SuccNum consistency
    for (int u = 0; u <= _nodeCount; u++)
    {
        int subtreeSize = CountSubtree(u);
        if (subtreeSize != _tree.SuccNum[u]) return false;
    }
    
    // Check parent-child relationships
    for (int u = 0; u < _nodeCount; u++)
    {
        if (_tree.Parent[u] >= 0)
        {
            int p = _tree.Parent[u];
            bool found = false;
            // Verify u is in parent's subtree via thread
            int v = p;
            for (int i = 0; i < _tree.SuccNum[p]; i++)
            {
                v = _tree.Thread[v];
                if (v == u) { found = true; break; }
            }
            if (!found) return false;
        }
    }
    return true;
}
```

### Complementary Slackness Verification
At optimality, these conditions must hold for each arc:
```csharp
bool CheckOptimality()
{
    for (int a = 0; a < _arcCount; a++)
    {
        int reducedCost = ComputeReducedCost(a);
        
        // If flow < capacity, then reduced cost ≥ 0
        if (_flow[a] < _upperBound[a] && reducedCost < -EPSILON)
            return false;
            
        // If flow > 0, then reduced cost ≤ 0  
        if (_flow[a] > 0 && reducedCost > EPSILON)
            return false;
            
        // Tree arcs have reduced cost = 0
        if (_state[a] == State.Tree && Math.Abs(reducedCost) > EPSILON)
            return false;
    }
    return true;
}
```

## C# vs C++ Implementation Considerations

### Memory Management
- Use `Span<T>` for stack-allocated temporary arrays
- Array pooling for heap allocations in hot paths
- Value types (struct) for small objects like Node/Arc IDs

### Performance Patterns
- Properties vs fields: Use fields in performance-critical structures
- Virtual calls: Avoid in hot paths, use concrete types
- Generics: Constrain to avoid boxing, enable JIT optimizations

### Safety Features
- Bounds checking: Disable in release builds for hot paths
- Nullable references: Use for API clarity
- Readonly structs: For immutable value types

## Algorithm Complexity

### Time Complexity
- Initialization: O(n + m) where n = nodes, m = arcs
- Per iteration: O(m) for pivot search, O(n) for tree update
- Total iterations: O(nm) theoretical, O(m) typical
- Overall: O(n²m) worst case, O(nm) expected

### Space Complexity
- Primary storage: O(n + m) for graph structure
- Working memory: O(n) for tree arrays
- Temporary allocations: O(1) with pooling

## References

- LEMON source: `lemon-1.3.1/lemon/network_simplex.h`
- Ahuja, Magnanti, Orlin: "Network Flows" (1993)
- Kelly & O'Neill: "The Minimum Cost Flow Problem and The Network Simplex Method" (1991)