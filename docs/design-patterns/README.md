# Design Pattern Snippets

Collection of comprehensive Gang of Four design pattern examples implemented in C#.

## Pattern Index

### Creational Patterns

- [Singleton Pattern](singleton.md) - Ensure a class has only one instance
- [Factory Method Pattern](factory-method.md) - Create objects without specifying exact classes
- [Abstract Factory Pattern](abstract-factory.md) - Create families of related objects
- [Builder Pattern](builder.md) - Construct complex objects step by step
- [Prototype Pattern](prototype.md) - Create objects by cloning existing instances

### Structural Patterns

- [Adapter Pattern](adapter.md) - Allow incompatible interfaces to work together
- [Bridge Pattern](bridge.md) - Separate abstraction from implementation *(Coming Soon)*
- [Composite Pattern](composite.md) - Compose objects into tree structures *(Coming Soon)*
- [Decorator Pattern](decorator.md) - Add behavior to objects dynamically *(Coming Soon)*
- [Facade Pattern](facade.md) - Provide unified interface to subsystem *(Coming Soon)*
- [Flyweight Pattern](flyweight.md) - Share objects efficiently *(Coming Soon)*
- [Proxy Pattern](proxy.md) - Provide placeholder/surrogate for another object *(Coming Soon)*

### Behavioral Patterns

- [Observer Pattern](observer.md) - Define one-to-many dependency between objects
- [Strategy Pattern](strategy.md) - Define family of algorithms and make them interchangeable
- [Command Pattern](command.md) - Encapsulate requests as objects *(Coming Soon)*
- [Chain of Responsibility](chain-of-responsibility.md) - Pass requests along chain of handlers *(Coming Soon)*
- [Mediator Pattern](mediator.md) - Define object interaction through mediator *(Coming Soon)*
- [Memento Pattern](memento.md) - Capture and restore object state *(Coming Soon)*
- [State Pattern](state.md) - Allow object to alter behavior when state changes *(Coming Soon)*
- [Template Method Pattern](template-method.md) - Define skeleton of algorithm in base class *(Coming Soon)*
- [Visitor Pattern](visitor.md) - Define operations on object structure elements *(Coming Soon)*
- [Iterator Pattern](iterator.md) - Provide way to access elements sequentially *(Coming Soon)*
- [Interpreter Pattern](interpreter.md) - Define grammar and interpreter for language *(Coming Soon)*

## Pattern Categories Overview

### Creational Pattern Overview

Focus on object creation mechanisms, trying to create objects in a manner suitable to the situation. They help make a system independent of how its objects are created, composed, and represented.

- **Singleton** - When you need exactly one instance (logging, caching, thread pools)
- **Factory Method** - When you need to create objects without specifying exact classes
- **Abstract Factory** - When you need to create families of related objects
- **Builder** - When you need to construct complex objects with many optional parts
- **Prototype** - When creating objects is expensive and you need copies

### Structural Pattern Overview

Deal with object composition and typically identify simple ways to realize relationships between different objects.

- **Adapter** - Integrate classes with incompatible interfaces
- **Bridge** - Separate abstraction from implementation
- **Composite** - Build complex objects from simple ones in tree structures
- **Decorator** - Add functionality to objects without altering structure
- **Facade** - Simplify interface to complex subsystem
- **Flyweight** - Minimize memory usage when dealing with large numbers of objects
- **Proxy** - Control access to another object

### Behavioral Pattern Overview

Focus on communication between objects and the assignment of responsibilities between objects.

- **Observer** - Notify multiple objects about state changes
- **Strategy** - Choose algorithms at runtime
- **Command** - Turn requests into objects for queuing, logging, undo operations
- **Chain of Responsibility** - Pass requests through chain of potential handlers
- **Mediator** - Reduce coupling between communicating objects
- **Memento** - Save and restore object state
- **State** - Change object behavior based on internal state
- **Template Method** - Define common algorithm structure in base class
- **Visitor** - Perform operations on object structure without changing classes
- **Iterator** - Traverse collections without exposing internal structure
- **Interpreter** - Implement specialized computer language or expression evaluator
