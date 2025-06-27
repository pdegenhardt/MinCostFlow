# Graph Data Structure Specification

## 1. Overview

This specification defines a family of graph data structures optimized for performance and memory efficiency. The design emphasizes fast construction, minimal memory overhead, and efficient iteration over graph elements.

### Core Design Principles

1. **Integer-based representation**: Nodes and arcs are represented by integer indices
2. **External annotations**: Node/arc properties (weights, labels, etc.) are stored in external arrays indexed by node/arc IDs
3. **Construction-first design**: Graphs are primarily designed to be constructed once and then queried multiple times
4. **Memory efficiency**: Each implementation optimizes for specific memory/performance trade-offs

## 2. Type System

### 2.1 Index Types

- **NodeIndex**: Integer type for node indices (typically 32-bit signed integer)
- **ArcIndex**: Integer type for arc indices (typically 32-bit signed integer)
  - Must be signed for graphs with reverse arcs
  - Can be unsigned for forward-only graphs

### 2.2 Special Constants

- **NilNode**: Maximum value of NodeIndex type, represents invalid/non-existent node
- **NilArc**: Maximum value of ArcIndex type, represents invalid/non-existent arc

## 3. Base Graph Interface

All graph implementations must provide the following interface:

### 3.1 Core Properties

| Property | Type | Description |
|----------|------|-------------|
| NumNodes | NodeIndex | Number of valid nodes in the graph |
| Size | NodeIndex | Alias for NumNodes (for collection compatibility) |
| NumArcs | ArcIndex | Number of valid forward arcs in the graph |
| NodeCapacity | NodeIndex | Reserved capacity for nodes (always ≥ NumNodes) |
| ArcCapacity | ArcIndex | Reserved capacity for arcs (always ≥ NumArcs) |

### 3.2 Validation Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| IsNodeValid | NodeIndex | Boolean | Returns true if node ∈ [0, NumNodes) |
| IsArcValid | ArcIndex | Boolean | Returns true if arc is valid (see note below) |

**Arc Validity Ranges:**
- Forward-only graphs: arc ∈ [0, NumArcs)
- Graphs with reverse arcs: arc ∈ [-NumArcs, NumArcs) where arc ≠ 0

### 3.3 Iteration Support

| Method | Returns | Description |
|--------|---------|-------------|
| AllNodes | Range of NodeIndex | Returns iterable range [0, NumNodes) |
| AllForwardArcs | Range of ArcIndex | Returns iterable range [0, NumArcs) |

### 3.4 Capacity Management

| Method | Parameters | Description |
|--------|------------|-------------|
| ReserveNodes | NodeIndex bound | Pre-allocate space for at least 'bound' nodes |
| ReserveArcs | ArcIndex bound | Pre-allocate space for at least 'bound' arcs |
| Reserve | NodeIndex nodeCapacity, ArcIndex arcCapacity | Combined reservation |
| FreezeCapacities | None | Prevent future capacity changes (enforced in debug mode) |

### 3.5 Graph Construction

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| AddNode | NodeIndex node | None | Ensures node is valid (extends graph if needed) |
| AddArc | NodeIndex tail, NodeIndex head | ArcIndex | Adds arc and returns its index |
| Build | Optional: permutation array | None | Finalizes graph construction (required for some implementations) |

### 3.6 Arc Properties

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| Head | ArcIndex | NodeIndex | Returns destination node of arc |
| Tail | ArcIndex | NodeIndex | Returns source node of arc |
| OutDegree | NodeIndex | ArcIndex | Number of outgoing arcs from node |

### 3.7 Arc Iteration

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| OutgoingArcs | NodeIndex | Arc iterator | Iterate over arcs leaving the node |
| OutgoingArcsStartingFrom | NodeIndex, ArcIndex | Arc iterator | Resume iteration from specific arc |

### 3.8 Convenience Operators

| Operator | Parameters | Returns | Description |
|----------|------------|---------|-------------|
| [] | NodeIndex | Node iterator | Iterate over head nodes of outgoing arcs |

## 4. Graph Implementation Variants

### 4.1 ListGraph

**Purpose**: Simple linked-list based implementation with fast construction

**Data Structure**:
- Array indexed by NodeIndex storing first outgoing arc
- Array indexed by ArcIndex storing next arc in linked list
- Array indexed by ArcIndex storing head node
- Optional: Array indexed by ArcIndex storing tail node

**Memory Usage**: 
- Without tail array: 1 × NodeCapacity + 2 × ArcCapacity integer values
- With tail array: 1 × NodeCapacity + 3 × ArcCapacity integer values

**Performance Characteristics**:
- AddArc: O(1)
- Build: O(1) - no operation needed
- OutgoingArcs iteration: O(degree)
- Head: O(1)
- Tail: O(1) if tail array present
- OutDegree: O(degree) - requires iteration

**Key Features**:
- Does not require Build() call
- Preserves original arc indices
- Slightly slower iteration than static variants

### 4.2 StaticGraph

**Purpose**: Memory-efficient implementation with fast iteration

**Data Structure**:
- Array indexed by NodeIndex+1 storing start positions (cumulative arc counts)
- Array indexed by ArcIndex storing head nodes
- Array indexed by ArcIndex storing tail nodes

**Memory Usage**: 
- 1 × (NodeCapacity + 1) + 2 × ArcCapacity integer values

**Performance Characteristics**:
- AddArc: O(1) during construction
- Build: O(n + m) where n = nodes, m = arcs
- OutgoingArcs iteration: O(degree) with excellent cache locality
- Head: O(1)
- Tail: O(1)
- OutDegree: O(1)

**Key Features**:
- Requires Build() before use
- May reorder arcs during Build() (permutation provided)
- Most memory-efficient forward-only implementation
- Fastest iteration due to contiguous storage

### 4.3 ReverseArcListGraph

**Purpose**: Linked-list implementation supporting reverse arc queries

**Data Structure**:
- All ListGraph structures plus:
- Array indexed by NodeIndex storing first incoming reverse arc
- Uses signed arithmetic for reverse arc indices

**Memory Usage**:
- 2 × NodeCapacity + 4 × ArcCapacity integer values

**Performance Characteristics**:
- Similar to ListGraph for forward operations
- IncomingArcs iteration: O(in-degree)
- OppositeArc: O(1) - computed as ~arc

**Additional Methods**:

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| InDegree | NodeIndex | ArcIndex | Number of incoming arcs to node |
| OppositeArc | ArcIndex | ArcIndex | Returns reverse of given arc |
| IncomingArcs | NodeIndex | Arc iterator | Iterate over arcs entering the node |
| OppositeIncomingArcs | NodeIndex | Arc iterator | Iterate over reverse arcs leaving the node |
| OutgoingOrOppositeIncomingArcs | NodeIndex | Arc iterator | Combined iteration |

### 4.4 ReverseArcStaticGraph

**Purpose**: Memory-efficient static implementation with reverse arc support

**Data Structure**:
- All StaticGraph structures plus:
- Array indexed by NodeIndex+1 storing reverse arc start positions
- Array indexed by ArcIndex storing opposite arc indices

**Memory Usage**:
- 2 × (NodeCapacity + 1) + 3 × ArcCapacity integer values

**Performance Characteristics**:
- Build: O(n + m) - more complex than StaticGraph
- OutDegree/InDegree: O(1)
- All iterations have excellent cache locality
- Reverse arcs from a node are sorted by head

### 4.5 CompleteGraph

**Purpose**: Implicit representation of complete graphs (all nodes connected)

**Data Structure**:
- Only stores number of nodes
- Arcs are computed on-demand using arithmetic

**Memory Usage**:
- O(1) - constant space regardless of graph size

**Performance Characteristics**:
- All operations O(1)
- No Build() required
- Arc indices are deterministic: arc from i to j has index i × NumNodes + j

### 4.6 CompleteBipartiteGraph

**Purpose**: Implicit representation of complete bipartite graphs

**Data Structure**:
- Stores number of left nodes and right nodes
- Left nodes: [0, LeftNodes)
- Right nodes: [LeftNodes, LeftNodes + RightNodes)

**Memory Usage**:
- O(1) - constant space

**Performance Characteristics**:
- Similar to CompleteGraph
- Only left nodes have outgoing arcs
- GetArc method computes arc index from node pair

## 6. Utility Functions

### 6.1 Permute

**Purpose**: Reorder array elements according to a permutation

**Parameters**:
- Permutation array where element i moves to position Permutation[i]
- Array to be permuted (modified in-place)

**Requirements**:
- Permutation must be valid (each index appears exactly once)
- Creates temporary copy of elements (not in-place algorithm)

## 7. Performance Requirements

### 7.1 Construction Performance

- **ListGraph**: Should handle random arc insertion efficiently
- **StaticGraph**: Build() should be optimized for:
  - Already-sorted arcs (O(n+m) single pass)
  - Random arcs (O(n+m) with sorting)

### 7.2 Memory Allocation

- Implementations should minimize allocations
- Reserve methods should perform at most one allocation
- Growth should use exponential strategy (typical factor: 1.3-2.0)

### 7.3 Cache Efficiency

- Static variants must store outgoing arcs contiguously
- Iteration should access memory sequentially when possible

## 8. Testing Requirements

### 8.1 Functional Tests

1. **Empty Graph**: Verify correct behavior with no nodes/arcs
2. **Single Node**: Graph with nodes but no arcs
3. **Arc Ordering**: Verify iteration order matches specification
4. **Capacity Management**: Test reservation and growth
5. **Build Permutation**: Verify arc reordering is correctly reported

### 8.2 Edge Cases

1. **Maximum Indices**: Test with maximum valid node/arc values
2. **Self-loops**: Arcs where Head == Tail
3. **Multiple Arcs**: Multiple arcs between same node pair
4. **Reverse Arc Sign**: Correct handling of negative indices

### 8.3 Performance Tests

1. **Random Arc Construction**: Benchmark with random tail/head pairs
2. **Ordered Arc Construction**: Benchmark with arcs added in tail order
3. **Iteration Performance**: Measure arc traversal speed
4. **Memory Usage**: Verify actual memory matches theoretical

### 8.4 Stress Tests

1. **Large Graphs**: 10M+ nodes, 50M+ arcs
2. **High Degree Nodes**: Nodes with thousands of connections
3. **Memory Pressure**: Operation under limited memory

## 9. Implementation Notes

### 9.1 Memory Management

- Use specialized allocators where beneficial
- Consider memory pooling for linked-list nodes
- Ensure proper cleanup in destructors

### 9.2 Iterator Design

- Iterators should be input/forward iterator compatible
- Support range-based iteration patterns
- Invalid iterators should fail safely in debug mode

### 9.3 Thread Safety

- Construction phase is not thread-safe
- After Build(), multiple readers are safe
- No concurrent modification support

### 9.4 Error Handling

- Invalid inputs in debug mode should assert/crash
- Release mode may skip validation for performance
- Capacity overflow should be detected

## 10. Acceptance Criteria

A conforming implementation must:

1. **Correctness**:
   - Pass all functional tests
   - Handle all edge cases correctly
   - Maintain invariants after each operation

2. **Performance**:
   - Meet or exceed reference implementation benchmarks
   - Scale linearly with graph size for construction
   - Achieve stated complexity bounds

3. **Memory**:
   - Use no more memory than specified formulas
   - Properly release all allocated memory
   - Handle out-of-memory conditions gracefully

4. **Interface**:
   - Implement all required methods
   - Support generic programming patterns
   - Provide appropriate type aliases

5. **Quality**:
   - No memory leaks or undefined behavior
   - Clean compilation without warnings
   - Comprehensive test coverage (>95%)