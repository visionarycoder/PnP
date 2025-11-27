# Copilot Instructions: C# Design Patterns

## Purpose

Ensure generated C# design pattern examples are modern, reproducible, and educational.

## Guidelines

- Use C# 12 / .NET 8+ syntax, forward-compatible with .NET 10.
- Show **intent**, **structure**, **code**, **usage**, and **notes**.
- Prefer interfaces, records, async/await.
- Avoid outdated constructs (ArrayList, Task.Result).
- Provide unit-testable examples.

## Categories

- **Creational**: Factory, Builder, Singleton, Prototype
- **Structural**: Adapter, Decorator, Facade, Proxy
- **Behavioral**: Strategy, Observer, Mediator, Command, State

## Example Output Format

**Intent**: One-sentence purpose.  
**Structure**: Key classes/interfaces.  
**Code**: Minimal compilable example.  
**Usage**: Short demo snippet.  
**Notes**: Pitfalls, modern alternatives.

## Example: Strategy Pattern

```csharp
public interface ISortingStrategy
{
    Task<IEnumerable<int>> SortAsync(IEnumerable<int> data);
}

public class QuickSortStrategy : ISortingStrategy
{
    public Task<IEnumerable<int>> SortAsync(IEnumerable<int> data) =>
        Task.FromResult(data.OrderBy(x => x));
}

public class Sorter
{
    private readonly ISortingStrategy _strategy;
    public Sorter(ISortingStrategy strategy) => _strategy = strategy;
    public Task<IEnumerable<int>> SortAsync(IEnumerable<int> data) => _strategy.SortAsync(data);
}

//Usage

var sorter = new Sorter(new QuickSortStrategy());
var result = await sorter.SortAsync(new[] { 5, 2, 9 });
```

## Notes

- Prefer DI for strategy injection.
- LINQ covers many cases, but Strategy is useful for pluggable algorithms.
