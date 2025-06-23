# Minimum Cost Flow Solver Requirements Specification

## Business Context

### Domain: Rental Logistics
- **Industry**: Asset rental management
- **Network Scale**: 10 rental depots
- **Time Horizon**: One year
- **Activities**: ~10,000 rental activities/transactions

### Network Characteristics
- **Type**: Time-expanded network
- **Nodes**: Temporal nodes representing depot states at different times
- **Arcs**: 
  - Asset movements between depots
  - Carry-forward arcs (assets staying at location)
  - Back arcs/delays (idle time representation)
- **Dynamics**: Frequent modifications from real-world events

## Functional Requirements

### Core Solver Capabilities

1. **Minimum Cost Flow Solution**
   - Solve minimum cost flow problems on directed graphs
   - Handle networks with ~10,000 activities
   - Support both initial solving and re-solving

2. **Incremental Modifications**
   - Modify network without full reconstruction
   - Support arc cost changes
   - Support capacity modifications
   - Add/remove arcs dynamically
   - Supply/demand changes at network edges only

3. **What-If Analysis**
   - Evaluate potential modifications without committing them
   - Rapid impact assessment for:
     - New rental reservations
     - Cancellations
     - Asset repair requirements
   - Return cost/feasibility of proposed changes

4. **State Management**
   - Maintain solver state between solves
   - Access internal structures for analysis
   - Utilize solver state for shortest path computations
   - Support warm-start from previous solutions

### Transactional Event Handling

**Primary Events:**
1. **New Rental Reservation**
   - Add arc or modify capacities
   - Evaluate routing feasibility
   - Calculate marginal cost impact

2. **Cancellation**
   - Remove demand or modify capacities
   - Recalculate optimal flows
   - Identify freed capacity

3. **Asset Repair Requirement**
   - Temporarily remove asset from network
   - Reroute affected flows
   - Calculate service impact

## Performance Requirements

### Solve Times
| Operation | Target | Priority |
|-----------|--------|----------|
| Initial Solve | < 1 second | Acceptable |
| Re-solve (with warm start) | ~100ms | Desirable |
| Modification Evaluation | < 50ms | Required |
| Single Arc Update | < 10ms | Optimal |

### Scalability
- Handle year-long time horizons efficiently
- Memory usage < 200MB for typical networks
- Support for sparse time-expanded networks

## Technical Requirements

### Architecture Constraints

1. **Implementation Language**: C#
   - Leverage modern C# features for performance
   - Use .NET optimization capabilities

2. **Dependencies**
   - No commercial libraries or tools
   - Open-source components with permissive licenses only
   - Commercial use must be permitted
   - No licensing costs

3. **Transparency**
   - Internal structures must be accessible
   - No black-box implementations
   - Ability to inspect and modify solver state

### Algorithm Requirements

1. **Algorithm Selection**
   - Phase 1: Best general-purpose approach
   - Phase 2: Dynamic selection based on problem characteristics
   - Must support warm-starting

2. **Data Structure Requirements**
   - Mutable graph representation
   - Efficient incremental updates
   - Memory-efficient for sparse networks

### Integration Requirements

1. **State Persistence**
   - Serialization/deserialization of solver state
   - Support for checkpointing
   - Recovery from saved states

2. **Solver Interface**
   - Clear API for modifications
   - Separate evaluation from commitment
   - Transaction-style operations

## Quality Attributes

### Maintainability
- Clean, well-documented code
- Modular architecture
- Comprehensive test suite
- Performance benchmarks

### Reliability
- Numerical stability
- Handling of degenerate cases
- Graceful degradation for infeasible problems

### Extensibility
- Pluggable algorithm selection
- Customizable for logistics-specific optimizations
- Support for future constraint types

## Development Constraints

### Team Capabilities
- Expertise in numerical algorithms available
- Preference for maintainable solutions
- Willing to implement from scratch if needed

### Implementation Approach
- Can use open-source implementations as inspiration
- May build on existing graph libraries
- Performance is critical (avoid generic solutions)

## Known Limitations of Existing Solutions

### OR-Tools Issues
- Non-transparent internal structures
- Immutable network representation
- No modification/re-solve capability
- Lacks warm-start support
- Black-box implementation

### General Library Issues
- Often too generic for maximum performance
- Not optimized for incremental updates
- Limited support for time-expanded networks

## Success Criteria

1. **Performance**: Meets all timing requirements
2. **Functionality**: Supports all transactional events
3. **Reliability**: Produces correct solutions consistently
4. **Usability**: Clear API for logistics domain
5. **Maintainability**: Team can extend and optimize

## Future Considerations

1. **Distributed Solving**: For very large networks
2. **Real-time Updates**: Sub-second response for all operations  
3. **Machine Learning Integration**: Predictive warm-starting
4. **Multi-objective Optimization**: Beyond just cost

## Risk Factors

1. **Performance**: Achieving <50ms modification evaluation
2. **Memory**: Scaling to full year horizon
3. **Numerical**: Stability with incremental updates
4. **Complexity**: Balancing features with maintainability