---
description: Design pattern implementation and architectural best practices
applyTo: '**/design-patterns/**,**/DesignPatterns.*/**,**/*pattern*'
---

# Design Patterns Instructions

## Scope
Applies to design pattern implementations, architectural patterns, and software design principles.

## Pattern Implementation Guidelines
- Understand the problem the pattern solves before implementation.
- Follow established pattern structure and terminology.
- Document when and why to use each pattern.
- Provide clear examples with realistic use cases.
- Consider modern language features that might simplify patterns.

## Creational Patterns
- Singleton: Use dependency injection instead of static instances when possible.
- Factory Method: Prefer dependency injection over factory methods.
- Abstract Factory: Use for families of related objects.
- Builder: Use for complex object construction with many parameters.
- Prototype: Implement proper deep cloning for mutable objects.

## Structural Patterns
- Adapter: Use to integrate incompatible interfaces.
- Bridge: Separate abstraction from implementation.
- Composite: Implement uniform interface for tree structures.
- Decorator: Prefer composition over inheritance for behavior extension.
- Facade: Simplify complex subsystem interfaces.

## Behavioral Patterns
- Observer: Use event-driven patterns and weak references.
- Strategy: Use dependency injection for algorithm selection.
- Command: Implement undo/redo functionality properly.
- State: Use state machines for complex state transitions.
- Template Method: Use virtual methods for customization points.

## Modern Pattern Adaptations
- Use generics to make patterns type-safe.
- Leverage async/await for asynchronous pattern implementations.
- Use functional programming concepts where appropriate.
- Consider LINQ and expression trees for query patterns.
- Use attributes and reflection for metadata-driven patterns.

## SOLID Principles Application
- Single Responsibility: Each class should have one reason to change.
- Open/Closed: Open for extension, closed for modification.
- Liskov Substitution: Subtypes must be substitutable for base types.
- Interface Segregation: Many specific interfaces are better than one general interface.
- Dependency Inversion: Depend on abstractions, not concretions.

## Anti-Patterns to Avoid
- God Object: Classes that do too much.
- Spaghetti Code: Complex and tangled control flow.
- Golden Hammer: Using same solution for every problem.
- Copy-Paste Programming: Duplicating code instead of abstracting.
- Magic Numbers: Using unnamed numerical constants.

## Pattern Testing Strategies
- Test pattern behavior, not implementation details.
- Use mocking to isolate pattern components.
- Test edge cases and error conditions.
- Verify pattern constraints and invariants.
- Performance test patterns with realistic data sizes.

## Documentation Standards
- Document pattern intent and motivation clearly.
- Provide class diagrams and interaction diagrams.
- Include code examples with explanations.
- Document known uses and related patterns.
- Explain trade-offs and alternative solutions.

## Performance Considerations
- Measure performance impact of pattern overhead.
- Use appropriate data structures for pattern implementation.
- Consider memory usage and garbage collection impact.
- Optimize hot paths while maintaining pattern integrity.
- Profile patterns under realistic conditions.

## Thread Safety
- Document thread safety guarantees clearly.
- Use appropriate synchronization primitives.
- Consider lock-free implementations where possible.
- Test concurrent access scenarios thoroughly.
- Use immutable objects where appropriate.

## Architectural Patterns
- MVC/MVP/MVVM: Separate concerns appropriately.
- Repository: Abstract data access layer properly.
- Unit of Work: Manage transactional boundaries.
- Service Layer: Encapsulate business logic.
- Domain-Driven Design: Model business domain accurately.