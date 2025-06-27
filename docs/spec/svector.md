# SVector Data Structure Specification

## 1. Overview

### 1.1 Purpose
SVector (Symmetric Vector) is a specialized container that provides bidirectional indexing with both negative and positive indices. It is designed for scenarios where data naturally has a symmetric structure around a central point, such as graph algorithms with forward/reverse arcs.

### 1.2 Core Concept
- Valid indices range from `-size` to `size-1` (inclusive)
- Total capacity for elements: `2 * size`
- Negative indices: `[-size, -1]` 
- Positive indices: `[0, size-1]`
- The structure grows symmetrically at both ends

## 2. Data Model

### 2.1 Internal State
The implementation must maintain:
- **size**: Number of elements on each side (non-negative integer)
- **capacity**: Reserved space on each side (non-negative integer)
- **data storage**: Contiguous memory to hold `2 * capacity` elements

### 2.2 Invariants
1. `0 ≤ size ≤ capacity`
2. `capacity ≤ MAX_CAPACITY` where `MAX_CAPACITY` is implementation-defined
3. Valid indices are exactly those in range `[-size, size)`
4. Elements outside the valid range are uninitialized or default-initialized

### 2.3 Memory Layout
```
[unused capacity | negative elements | positive elements | unused capacity]
                  ^                    ^
                  -size               0
```

## 3. Operations Specification

### 3.1 Construction and Initialization

#### 3.1.1 Default Constructor
- **Behavior**: Creates an empty SVector
- **Post-conditions**: 
  - `size() == 0`
  - `capacity() == 0`
- **Complexity**: O(1)

#### 3.1.2 Copy Constructor
- **Behavior**: Creates a deep copy of another SVector
- **Post-conditions**:
  - `size() == other.size()`
  - All elements are copied: `this[i] == other[i]` for all valid `i`
- **Complexity**: O(n) where n = `other.size()`

#### 3.1.3 Move Constructor
- **Behavior**: Transfers ownership from another SVector
- **Post-conditions**:
  - `this` contains all data from `other`
  - `other.size() == 0` after move
  - No element copying occurs
- **Complexity**: O(1)

### 3.2 Element Access

#### 3.2.1 Index Operator `operator[]`
- **Input**: Index `i` where `-size ≤ i < size`
- **Output**: Reference to element at index `i`
- **Error Handling**: 
  - Debug mode: Assert/abort on invalid index
  - Release mode: Undefined behavior for invalid index
- **Complexity**: O(1)

### 3.3 Capacity Management

#### 3.3.1 `size()`
- **Returns**: Current number of elements on each side
- **Complexity**: O(1)

#### 3.3.2 `capacity()`
- **Returns**: Current reserved space on each side
- **Complexity**: O(1)

#### 3.3.3 `reserve(n)`
- **Behavior**: Ensures capacity ≥ n
- **Post-conditions**:
  - `capacity() ≥ n`
  - All existing elements preserved
  - No change if `n ≤ capacity()`
- **Complexity**: 
  - O(1) if no reallocation needed
  - O(size) if reallocation required

#### 3.3.4 `resize(n)`
- **Behavior**: Changes size to exactly n
- **Post-conditions**:
  - `size() == n`
  - If growing: new elements are default-initialized
  - If shrinking: excess elements are destroyed
  - Existing elements in range `[-min(n,old_size), min(n,old_size))` are preserved
- **Complexity**: O(|n - old_size|)

### 3.4 Modification Operations

#### 3.4.1 `grow(left_value, right_value)`
- **Behavior**: Adds one element at each end
- **Post-conditions**:
  - `size() == old_size + 1`
  - `this[-size()] == left_value`
  - `this[size()-1] == right_value`
  - All existing elements preserved with indices shifted
- **Special Case**: If `left_value` or `right_value` reference existing elements, they must be copied before reallocation
- **Complexity**: O(1) amortized

#### 3.4.2 `clear()`
- **Behavior**: Removes all elements
- **Post-conditions**:
  - `size() == 0`
  - Capacity unchanged
- **Complexity**: O(size)

#### 3.4.3 `swap(other)`
- **Behavior**: Exchanges contents with another SVector
- **Post-conditions**:
  - `this` contains what `other` had
  - `other` contains what `this` had
- **Complexity**: O(1)

### 3.5 Memory Management

#### 3.5.1 Growth Strategy
- **Growth Factor**: 1.3x (approximately)
- **Formula**: `new_capacity = max(1.3 * current_capacity, current_capacity + required_growth)`
- **Constraints**: Must handle integer overflow appropriately

#### 3.5.2 `clear_and_dealloc()`
- **Behavior**: Clears all elements and releases memory
- **Post-conditions**:
  - `size() == 0`
  - `capacity() == 0`
  - All memory deallocated
- **Complexity**: O(size) + deallocation time

## 4. Iterator/Enumeration Support

### 4.1 Iteration Order
- Default iteration: From index `-size` to `size-1`
- Must provide mechanism to iterate over:
  - All elements (negative to positive)
  - Positive range only `[0, size)`
  - Negative range only `[-size, 0)`

### 4.2 Iterator Invalidation
Any operation that may cause reallocation invalidates all iterators:
- `grow()`
- `reserve()` (if it causes reallocation)
- `resize()` (if it causes reallocation)

## 5. Performance Requirements

### 5.1 Time Complexity Guarantees

| Operation | Complexity | Notes |
|-----------|------------|-------|
| Construction (default) | O(1) | |
| Construction (copy) | O(n) | |
| Construction (move) | O(1) | |
| Element access | O(1) | |
| size() | O(1) | |
| capacity() | O(1) | |
| grow() | O(1) amortized | O(n) worst case during reallocation |
| resize() | O(\|delta\|) | delta = new_size - old_size |
| reserve() | O(n) or O(1) | O(n) only if reallocation needed |
| clear() | O(n) | Must destruct elements |
| swap() | O(1) | |

### 5.2 Space Complexity
- **Memory overhead**: O(capacity - size) unused elements
- **Total memory**: O(capacity) 
- **Memory layout**: Must use contiguous storage for cache efficiency

## 6. Type Requirements

### 6.1 Index Type Requirements
- Must be integral or convertible to integral
- Must support:
  - Comparison operators
  - Arithmetic operations (+, -, unary negation)
  - Conversion to/from standard integer types

### 6.2 Element Type Requirements
- Must be default-constructible (for resize operations)
- Must be copy-constructible (for copy operations)
- Must be move-constructible (for C++11 and similar)
- Must be destructible

## 7. Error Handling

### 7.1 Index Validation
- **Invalid index access**: 
  - Debug builds: Hard error (assertion/exception)
  - Release builds: Undefined behavior (for performance)

### 7.2 Capacity Limits
- **Exceeding maximum capacity**: Implementation-defined behavior
- **Memory allocation failure**: Throw exception or return error code

### 7.3 Special Cases
- **Self-referential growth**: `grow(this[i], this[j])` must work correctly
- **Self-assignment**: `this[i] = this[j]` must work correctly
- **Self-swap**: `swap(*this)` must be a no-op

## 8. Thread Safety

- **Default**: Not thread-safe
- **Const operations**: Safe for concurrent read-only access
- **Mutable operations**: Require external synchronization

## 9. Acceptance Criteria

Based on the C++ test suite, an implementation must pass the following tests:

### 9.1 Basic Operations
1. **Empty State**
   - New SVector has size 0, capacity 0
   
2. **Growth**
   - Can grow from empty state
   - Capacity increases appropriately
   - Elements accessible at correct indices

3. **Indexing**
   - Positive indices `[0, size)` work correctly
   - Negative indices `[-size, -1]` work correctly
   - Out-of-bounds access handled appropriately

4. **Copy Semantics**
   - Copy constructor creates independent copy
   - Assignment operator works correctly
   - Copied elements equal original

5. **Move Semantics** (if applicable)
   - Move constructor transfers ownership
   - Source object left in valid empty state
   - No element copying occurs

### 9.2 Resize Operations
1. **Grow Size**
   - New elements are default-initialized
   - Existing elements preserved

2. **Shrink Size**
   - Excess elements destroyed
   - Remaining elements preserved

3. **Resize to Zero**
   - Results in empty vector
   - Can resize back up

### 9.3 Memory Management
1. **Reserve**
   - Increases capacity without changing size
   - Preserves all elements
   - No-op if requested capacity ≤ current

2. **Growth Pattern**
   - Capacity grows by ~1.3x factor
   - Amortized O(1) insertion

3. **Clear and Deallocate**
   - Releases all memory
   - Object still usable after

### 9.4 Complex Types
1. **String Elements**
   - Proper construction/destruction
   - No memory leaks

2. **Move-Only Types**
   - Can store non-copyable types
   - Proper move semantics

3. **Tracked Objects**
   - Correct constructor/destructor counts
   - No double-destruction
   - No leaks

### 9.5 Edge Cases
1. **Self-Referential Operations**
   - `grow(this[i], this[j])` works correctly

2. **Large Sizes**
   - Handle sizes up to implementation limits

3. **Integral Type Optimization**
   - Efficient copying for POD types

## 10. Implementation Guidelines

### 10.1 Memory Layout Strategy
```
Logical view:  [-size ... -1] [0 ... size-1]
Physical view: [buffer + capacity - size ... buffer + capacity + size - 1]
```

### 10.2 Optimization Opportunities
1. **Type Traits**: Optimize for POD types using memcpy
2. **Small Size Optimization**: Consider inline storage for small sizes
3. **Growth Strategy**: Balance between memory usage and reallocation frequency

### 10.3 Debugging Support
1. Provide clear string representation
2. Include bounds checking in debug builds
3. Support for debugging iterators

## 11. Example Usage Patterns

```
// Symmetric data storage
sv.grow(negative_value, positive_value);

// Bidirectional algorithms
distances[-node] = backward_distance;
distances[node] = forward_distance;

// Time series with past/future
temps[-hour] = historical_temperature;
temps[hour] = predicted_temperature;
```

## 12. Platform Considerations

1. **Integer Overflow**: Handle capacity calculations safely
2. **Memory Alignment**: Ensure proper alignment for element type
3. **Exception Safety**: Provide appropriate guarantees
4. **Allocator Support**: Consider custom memory allocation

## 13. Test Suite Requirements

A conforming implementation must provide tests for:

### 13.1 Type Combinations
Test with various index/element type combinations:
- (int, int)
- (int, complex_type)
- (strong_int, int)
- (strong_int, strong_type)

### 13.2 Stress Tests
- Large sizes (10K+ elements)
- Repeated grow/shrink cycles
- Memory leak detection
- Performance benchmarks

### 13.3 Iterator Tests
- Forward iteration
- Partial range iteration
- Iterator invalidation
- Const correctness

## 14. Performance Benchmarks

Implementations should benchmark against:
- Standard vector/array with offset calculation
- Hash map with integer keys
- Two separate arrays for positive/negative

Key metrics:
- Random access time
- Sequential iteration time
- Growth operation time
- Memory usage

## 15. Reference Implementation Notes

The original C++ implementation uses:
- `malloc/free` for memory management
- Placement new for construction
- Manual destruction for non-POD types
- `memcpy` optimization for integral types

Language-specific implementations may use:
- Native memory management
- Language-specific optimizations
- Standard library containers as building blocks

This specification provides a complete blueprint for implementing SVector in any programming language while maintaining the essential characteristics and performance guarantees of the original C++ implementation.