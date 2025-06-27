# Generic Max Flow Algorithm Specification

## 1. Overview

This specification defines a generic maximum flow algorithm implementation using the push-relabel method (Goldberg-Tarjan algorithm). The algorithm finds the maximum flow from a source node to a sink node in a directed graph with capacitated arcs.

### 1.1 Algorithm Overview

The push-relabel algorithm works with preflows instead of flows and uses two main operations:
- **Push**: Send flow along an admissible arc
- **Relabel**: Increase the height of a node when no admissible arcs exist

### 1.2 Key Concepts

- **Preflow**: A relaxed flow where inflow can exceed outflow at nodes
- **Excess**: The difference between inflow and outflow at a node (sum of incoming flow)
- **Height/Potential**: A label on each node used to guide flow direction
- **Active Node**: A node with positive excess (excluding source and sink)
- **Admissible Arc**: An arc with residual capacity where the tail's height equals the head's height plus one
- **Residual Capacity**: For arc: capacity[arc] - flow[arc]; For reverse arc: flow[arc]

## 2. Dependencies

### 2.1 Graph Structure Requirements

The algorithm requires a directed graph implementation with the following properties:

#### Required Graph Interface:
- `num_nodes()`: Returns the number of nodes
- `num_arcs()`: Returns the number of arcs  
- `node_capacity()`: Returns the reserved node capacity
- `arc_capacity()`: Returns the reserved arc capacity
- `Head(arc)`: Returns the destination node of an arc
- `Tail(arc)`: Returns the source node of an arc
- `OppositeArc(arc)` or `Opposite(arc)`: Returns the reverse arc
- `OutgoingArcs(node)`: Returns outgoing arcs from a node
- `OutgoingOrOppositeIncomingArcs(node)`: Returns all incident arcs (both directions)
- `OutgoingOrOppositeIncomingArcsStartingFrom(node, arc)`: Resume iteration from specific arc
- `IsNodeValid(node)`: Validates node index
- `IsArcValid(arc)`: Validates arc index
- `Build(permutation*)`: Finalize graph construction (may reorder arcs, returns permutation)

#### Special Constants:
- `kNilArc`: Represents invalid/non-existent arc

#### Graph Properties:
The graph must support one of two reverse arc models:

1. **Negative Reverse Arcs Model** (`kHasNegativeReverseArcs = true`):
   - Direct arcs: indices in range [0, num_arcs)
   - Reverse arcs: indices in range [-num_arcs, 0)
   - Only direct arcs can have positive initial capacity
   - Opposite(arc) = ~arc for direct arcs

2. **Standard Model** (`kHasNegativeReverseArcs = false`):
   - All arcs: indices in range [0, num_arcs)
   - Both arc and its reverse can have positive capacity
   - Must store initial capacities separately

### 2.2 Priority Queue with Restricted Push

A specialized priority queue implementation with:
- Elements can only be pushed with priority ≥ (highest_priority - 1)
- All operations are O(1)
- Elements with same priority are retrieved in LIFO order
- Uses two internal queues for even/odd priorities

#### Interface:
- `IsEmpty()`: Check if queue is empty
- `Clear()`: Remove all elements
- `Push(element, priority)`: Add element with given priority (must satisfy restriction)
- `Pop()`: Remove and return highest priority element

#### Implementation Detail:
- Maintains two vectors: one for even priorities, one for odd priorities
- Both vectors remain sorted due to the push restriction
- Pop returns from whichever vector has the highest back element

### 2.3 Optional: ZVector Type

For graphs with negative reverse arcs, a symmetric vector that can be indexed with negative values:
- Valid indices: [min_index, max_index]
- Used for residual capacities when reverse arcs have negative indices

### 2.4 Type Requirements

#### Index Types:
- **NodeIndex**: Integer type for node indices
- **ArcIndex**: Integer type for arc indices (must be signed if negative reverse arcs)
- **NodeHeight**: Type for node heights (typically same as NodeIndex)

#### Flow Types:
- **ArcFlowType**: Flow quantity per arc (can be unsigned)
- **FlowSumType**: Sum of flows (must be signed, bit width ≥ ArcFlowType)

## 3. Core Algorithm Specification

### 3.1 Data Structures

The implementation must maintain:

1. **Node Arrays** (size = node_capacity):
   - `node_excess`: Excess flow at each node (FlowSumType)
   - `node_potential`: Height/potential of each node (NodeHeight)
   - `first_admissible_arc`: Cached first admissible arc for each node (ArcIndex)

2. **Arc Arrays**:
   - `residual_arc_capacity`: Residual capacity for each arc (ArcFlowType)
     - For negative reverse arcs: indices from -arc_capacity to arc_capacity-1
     - Otherwise: indices from 0 to arc_capacity-1
   - `initial_capacity`: Initial arc capacities (only if `kHasNegativeReverseArcs = false`)

3. **Active Node Management**:
   - Priority queue of active nodes ordered by height (PriorityQueueWithRestrictedPush)

4. **BFS Support** (for GlobalUpdate):
   - `node_in_bfs_queue`: Boolean array tracking visited nodes
   - `bfs_queue`: Vector of nodes for BFS traversal

### 3.2 Algorithm Phases

#### 3.2.1 Initialization (`InitializePreflow`)
1. Set all node excesses to 0
2. Set all node potentials to 0, except source = num_nodes
3. Initialize residual capacities:
   - If negative reverse arcs: move all capacity to direct arcs, set reverse to 0
   - Otherwise: copy from initial capacities
4. Set first admissible arc for each node to first outgoing/incoming arc

#### 3.2.2 Main Algorithm (`Solve`)
1. Handle invalid source/sink (≥ num_nodes): return OPTIMAL with 0 flow
2. Initialize preflow
3. Execute RefineWithGlobalUpdate loop:
   - While can saturate outgoing arcs from source:
     - Push flow from source (limited by kMaxFlowSum)
     - Perform global updates and discharge active nodes
     - Push excess back to source
4. Check for integer overflow condition
5. Return status

#### 3.2.3 RefineWithGlobalUpdate
Implements two-phase algorithm:
1. Phase 1: Process nodes that can reach sink
   - Uses skip heuristic: if node height increases by >1, may skip it
   - Performs global updates when nodes are skipped
2. Phase 2: Push excess back to source

#### 3.2.4 SaturateOutgoingArcsFromSource
- Push flow on admissible outgoing arcs from source
- Limit total flow to kMaxFlowSum to prevent overflow
- Skip arcs where head height ≥ num_nodes
- Returns true if any flow was pushed

#### 3.2.5 Discharge Operation
For an active node:
1. While node has excess:
   - Start from cached first_admissible_arc
   - Find admissible arc and push flow
   - If no admissible arc exists, relabel node
   - If height ≥ num_nodes after relabel, stop (unreachable)

#### 3.2.6 Push Operations

**PushFlow(flow, tail, arc)**:
- Decrease residual_arc_capacity[arc] by flow
- Increase residual_arc_capacity[opposite_arc] by flow
- Decrease node_excess[tail] by flow
- Increase node_excess[head] by flow

**PushAsMuchFlowAsPossible(tail, arc, node_excess_array)**:
- Push min(node_excess[tail], residual_arc_capacity[arc])
- Optimized version using cached array pointer

#### 3.2.7 Relabel Operation (Relaxed Version)
1. Find minimum height among neighbors with positive residual capacity
2. Set node height = min_height + 1
3. Cache the arc that achieves minimum as first_admissible_arc
4. If current height already = min_height + 1, stop early (relaxed condition)

#### 3.2.8 Global Update
Reverse BFS from sink in residual graph:
1. Mark source and sink as visited
2. Start BFS from sink
3. For each node reached:
   - Set height = distance from sink
   - Steal excess from neighbors during traversal (optimization)
4. Unreached nodes get height = 2 * num_nodes - 1
5. Rebuild active node queue in height order

#### 3.2.9 PushFlowExcessBackToSource
Depth-first traversal to return excess to source:
1. Build DFS tree of nodes with positive flow paths
2. Detect and cancel flow on cycles using Tarjan's algorithm
3. Push excess back in reverse topological order
4. Ensures all non-source/sink nodes have 0 excess

### 3.3 Special Arc Handling

1. **Self-loops**: SetArcCapacity ignores self-loops (head == tail)
2. **Multiple arcs**: Each arc between same nodes tracked independently
3. **Arc permutation**: Graph may reorder arcs during Build()

## 4. Public Interface

### 4.1 Constructor
```
GenericMaxFlow(graph, source, sink)
```
- `graph`: The graph structure
- `source`: Source node index
- `sink`: Sink node index

### 4.2 Configuration Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `SetArcCapacity` | arc, capacity | Set capacity for direct arc (ignored for self-loops) |

### 4.3 Solver Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Solve` | boolean | Run algorithm, returns true (sets status internally) |
| `status` | Status enum | Get solution status |

### 4.4 Result Query Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetOptimalFlow` | - | FlowSumType | Total flow (equals node_excess[sink]) |
| `Flow` | arc | FlowSumType | Signed flow on arc (positive = forward) |
| `Capacity` | arc | ArcFlowType | Initial capacity of arc |
| `GetSourceSideMinCut` | result vector | - | Nodes reachable from source in residual |
| `GetSinkSideMinCut` | result vector | - | Nodes reaching sink in reverse residual |
| `AugmentingPathExists` | - | boolean | Check if more flow possible |
| `CreateFlowModel` | - | FlowModelProto | Export to protocol buffer format |

### 4.5 Graph Access

| Method | Returns | Description |
|--------|---------|-------------|
| `graph` | Graph* | Get the associated graph |
| `GetSourceNodeIndex` | NodeIndex | Get source node |
| `GetSinkNodeIndex` | NodeIndex | Get sink node |

### 4.6 Flow Calculation

For negative reverse arcs model:
- Flow(direct_arc) = residual_capacity[opposite_arc]
- Flow(reverse_arc) = -residual_capacity[arc]

For standard model:
- Flow(arc) = initial_capacity[arc] - residual_capacity[arc]

## 5. Status Codes

| Status | Description |
|--------|-------------|
| `NOT_SOLVED` | Algorithm not run or problem modified |
| `OPTIMAL` | Optimal solution found |
| `INT_OVERFLOW` | Flow exceeds FlowSumType::max() |

## 6. Special Cases and Edge Conditions

### 6.1 Self-loops
- SetArcCapacity returns immediately for self-loops
- Capacity treated as 0

### 6.2 Multiple Arcs
- Each arc maintains independent capacity and flow
- No consolidation of parallel arcs

### 6.3 Disconnected Graphs
- Returns OPTIMAL with flow = 0
- Min-cut separates reachable components

### 6.4 Integer Overflow
- Detected when optimal flow = kMaxFlowSum AND augmenting path exists
- Returns INT_OVERFLOW status
- May require multiple iterations of main loop

### 6.5 Invalid Source/Sink
- If source or sink ≥ num_nodes: returns OPTIMAL with flow = 0
- Handled before main algorithm

### 6.6 Height Overflow Prevention
- Nodes with height ≥ num_nodes cannot reach sink
- Height ≥ 2*num_nodes for completely disconnected nodes

## 7. Performance Characteristics

### 7.1 Time Complexity
- Theoretical: O(n² √m) with highest-level selection
- Practice: Much faster due to heuristics

### 7.2 Space Complexity
- Node arrays: O(node_capacity)
- Arc arrays: O(arc_capacity)
- BFS queue: O(num_nodes)

### 7.3 Key Optimizations
1. **Global update**: Exact distance computation via BFS
2. **First admissible arc caching**: Avoid repeated searches
3. **Excess stealing**: During global update traversal
4. **Two-phase algorithm**: Separate flow finding and cleanup
5. **Skip heuristic**: Avoid nodes with large height increases
6. **Memory pre-allocation**: Based on graph capacities

## 8. Implementation Requirements

### 8.1 Memory Management
- Pre-allocate arrays using graph capacity methods
- Uninitialized allocation acceptable for performance
- ZVector for symmetric indexing if needed

### 8.2 Numerical Requirements
- Detect integer overflow conditions
- Maintain flow antisymmetry exactly
- Handle maximum representable values

### 8.3 Algorithm Invariants
1. node_excess[v] ≥ 0 for v ≠ source
2. node_excess[source] ≤ 0
3. node_excess[source] + node_excess[sink] + Σ(other nodes) = 0
4. residual_arc_capacity[arc] ≥ 0 for all arcs
5. node_potential[source] = num_nodes (constant)
6. node_potential[sink] = 0 (constant)
7. residual_arc_capacity[arc] + residual_arc_capacity[opposite_arc] = initial_capacity

## 9. Test Requirements

### 9.1 Basic Feasibility Tests

Each test should verify:
- Expected total flow
- Expected flow on each arc
- Min-cut correctness (optional)

Test cases:
1. **Simple Path** (4 nodes, 3 arcs)
   - Linear chain: 0→1→2→3
   - Capacities: [8,10,8]
   - Expected flow: 8 on all arcs

2. **Multiple Paths** (6 nodes, 9 arcs)
   - Diamond-like structure with multiple paths
   - Tests flow distribution

3. **Multiple Arcs** (5 nodes, 8 arcs)
   - Duplicate arcs between node pairs
   - Each maintains independent flow

4. **Direct Source-Sink Arc**
   - Arc directly from source to sink
   - Combined with other paths

### 9.2 Capacity Limit Tests

1. **Huge Capacity**
   - Use maximum representable value for ArcFlowType
   - Verify no overflow in normal case

2. **Flow Overflow Limit**
   - Total capacity = FlowSumType::max()
   - Should return OPTIMAL

3. **Flow Overflow**
   - Total capacity > FlowSumType::max()
   - Must return INT_OVERFLOW

### 9.3 Graph Structure Tests

1. **Disconnected Variants**
   - Source disconnected from sink
   - Components separated
   - Must return 0 flow

2. **Empty Graph**
   - No arcs (or no nodes)
   - Edge case handling

3. **Single Node**
   - Source = Sink
   - Degenerate case

### 9.4 Type Flexibility Tests

1. **Custom Integer Types**
   - Test with wrapped uint16_t or similar
   - Verify all operations preserve type safety

2. **Small Flow Types**
   - ArcFlowType smaller than FlowSumType
   - Compare results with standard types

### 9.5 Randomized Tests

Generate test graphs:
1. **Complete Bipartite**
   - All left nodes → all right nodes
   - Add source/sink connections

2. **Partial Random**
   - Each node connects to k random nodes
   - Parameterized density

3. **Random Capacities**
   - Uniform random in range
   - Test capacity modifications

### 9.6 Validation Requirements

For each solution:
1. **Flow Conservation**: Σ(in) = Σ(out) at each node (except source/sink)
2. **Capacity Constraints**: 0 ≤ flow ≤ capacity
3. **Antisymmetry**: flow[arc] = -flow[opposite_arc]
4. **Optimality**: No augmenting path exists (unless INT_OVERFLOW)
5. **Min-Cut**: Cut value = max flow value

### 9.7 Performance Benchmarks

Test configurations:
- Full assignment: O(n²) arcs
- Partial assignment: O(n) arcs  
- Random flows: Variable density
- Sizes: 100-10,000 nodes

Measure:
- Nodes processed per second
- Scaling behavior
- Memory usage

## 10. Error Handling

### 10.1 Input Validation
- Negative capacities: Undefined (may assert in debug)
- Invalid arc indices: Undefined behavior
- Source/sink validation: Handled gracefully

### 10.2 Overflow Detection
- Check when GetOptimalFlow() = kMaxFlowSum
- Verify via AugmentingPathExists()
- Set INT_OVERFLOW status

### 10.3 Debug Support
- CheckResult() validates all invariants
- DebugString() provides arc details
- Statistics tracking (optional)

## 11. Protocol Buffer Support

The implementation should support export to FlowModelProto with:
- Problem type: MAX_FLOW
- Node list with supplies (+1 for source, -1 for sink)
- Arc list with tail, head, capacity

## 12. Implementation Notes

### 12.1 Critical Details
1. Status cleared on each Solve() call
2. Build() may reorder arcs - use permutation array
3. Source always has height = num_nodes
4. Memory allocated but not initialized for performance
5. Skip heuristic threshold: height increase > 1

### 12.2 Language Adaptations
- Use appropriate integer overflow detection
- Implement efficient priority queue as specified
- Consider cache-friendly memory layout
- Provide strong typing where supported