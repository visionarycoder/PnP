---
description: Algorithm implementation and data structure best practices
applyTo: '**/algorithms/**,**/Algorithm.*/**,**/*algorithm*,**/*search*,**/*sort*'
---

# Algorithms Instructions

## Scope
Applies to algorithm implementations, data structures, and computational problem solving.

## Algorithm Design Principles
- Choose appropriate time and space complexity for the use case.
- Document Big O notation for time and space complexity.
- Implement clear, readable code over micro-optimizations.
- Use descriptive variable names that reflect algorithm concepts.
- Follow established algorithmic patterns and conventions.

## Data Structure Implementation
- Use appropriate data structures for specific problems.
- Implement proper encapsulation and data hiding.
- Provide clear interfaces with well-defined contracts.
- Handle edge cases (empty collections, single elements, etc.).
- Implement proper equality and comparison methods.

## Performance Optimization
- Profile code to identify actual bottlenecks.
- Use appropriate algorithmic complexity for problem size.
- Consider cache efficiency and memory access patterns.
- Implement lazy evaluation where beneficial.
- Use vectorization and SIMD operations when appropriate.

## Testing Strategies
- Test with various input sizes and edge cases.
- Use property-based testing for mathematical correctness.
- Benchmark performance with different data sets.
- Test boundary conditions and error cases.
- Validate correctness with known test cases.

## Documentation Standards
- Document algorithm purpose and use cases clearly.
- Include complexity analysis (time and space).
- Provide examples with input/output samples.
- Reference original papers or sources when applicable.
- Document any assumptions or limitations.

## Error Handling
- Validate input parameters and constraints.
- Handle overflow and underflow conditions.
- Use appropriate exception types for different error cases.
- Provide meaningful error messages for debugging.
- Consider graceful degradation for performance-critical code.

## Sorting Algorithms
- Choose appropriate sorting algorithm for data characteristics.
- Implement stable sorts when order preservation matters.
- Use in-place algorithms to minimize memory usage.
- Handle duplicate elements correctly.
- Provide comparison function customization.

## Searching Algorithms
- Use appropriate search strategy for data organization.
- Implement early termination for optimization.
- Handle not-found cases consistently.
- Support custom comparison functions.
- Consider probabilistic algorithms for large datasets.

## Graph Algorithms
- Use appropriate graph representation (adjacency list/matrix).
- Handle both directed and undirected graphs.
- Implement proper cycle detection.
- Use efficient data structures for priority queues.
- Handle disconnected components appropriately.

## Dynamic Programming
- Identify optimal substructure and overlapping subproblems.
- Choose between memoization and tabulation approaches.
- Optimize space complexity when possible.
- Handle base cases and boundary conditions.
- Document recurrence relations clearly.

## String Algorithms
- Handle Unicode and character encoding properly.
- Use appropriate string matching algorithms for use case.
- Consider preprocessing for multiple queries.
- Handle case sensitivity and locale considerations.
- Optimize for common string operations.