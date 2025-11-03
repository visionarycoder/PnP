---
description: Utility functions and helper library development best practices
applyTo: '**/*util*,**/*helper*,**/common/**,**/shared/**,**/extensions/**'
---

# Utilities Instructions

## Scope
Applies to utility functions, helper classes, common libraries, and shared components.

## Design Principles
- Design for reusability across multiple contexts.
- Keep utilities focused on single responsibility.
- Make functions pure when possible (no side effects).
- Use immutable data structures where appropriate.
- Design for composability and functional chaining.

## Naming Conventions
- Use descriptive names that clearly indicate purpose.
- Use verbs for functions that perform actions.
- Use nouns for functions that return computed values.
- Prefix boolean-returning functions with `is`, `has`, or `can`.
- Use consistent naming patterns across related utilities.

## Error Handling
- Use explicit error types rather than generic exceptions.
- Validate input parameters and throw meaningful errors.
- Document expected exceptions in function documentation.
- Use result types or nullable returns for expected failures.
- Provide error context for debugging purposes.

## Performance Considerations
- Optimize for common use cases.
- Use lazy evaluation for expensive computations.
- Implement caching for frequently computed values.
- Avoid unnecessary object allocations in hot paths.
- Consider memory usage for utility functions used in loops.

## Type Safety and Generics
- Use strong typing to prevent runtime errors.
- Leverage generics for type-safe, reusable utilities.
- Provide type constraints where appropriate.
- Use discriminated unions for complex return types.
- Implement proper null safety patterns.

## Extension Methods and Helpers
- Group related extensions in static classes.
- Use meaningful namespaces to avoid conflicts.
- Implement fluent interfaces for better usability.
- Provide overloads for common parameter combinations.
- Consider method chaining for data transformations.

## Configuration and Customization
- Use dependency injection for configurable behavior.
- Provide sensible defaults for optional parameters.
- Use options pattern for complex configurations.
- Allow customization through strategy patterns.
- Document configuration requirements clearly.

## Testing Strategies
- Write comprehensive unit tests for edge cases.
- Test with various input combinations and types.
- Test error conditions and exception handling.
- Use property-based testing for mathematical utilities.
- Test performance characteristics for critical utilities.

## Documentation Standards
- Provide clear examples for common use cases.
- Document parameter constraints and expected ranges.
- Include performance characteristics and complexity.
- Document thread safety guarantees.
- Provide migration guides for breaking changes.

## Async and Threading
- Use async/await patterns consistently.
- Implement cancellation token support for long-running operations.
- Ensure thread safety for utilities used in concurrent contexts.
- Use appropriate synchronization primitives.
- Document threading requirements and guarantees.

## Validation and Contracts
- Implement input validation with clear error messages.
- Use guard clauses for precondition checking.
- Implement postcondition validation where appropriate.
- Use contracts or assertions for invariants.
- Provide helpful error messages for invalid inputs.

## Compatibility and Portability
- Design for cross-platform compatibility when possible.
- Minimize dependencies on platform-specific features.
- Use abstraction layers for platform-specific code.
- Test on multiple platforms and runtime versions.
- Document platform-specific limitations or requirements.