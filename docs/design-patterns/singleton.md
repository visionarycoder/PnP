# Singleton Pattern

**Description**: Ensures a class has only one instance and provides global access to it. Thread-safe implementation using lazy initialization. **Note**: Consider dependency injection instead of static instances when possible.

**Language/Technology**: C# / .NET 8.0

**Code**:

```csharp
using System;

/// <summary>
/// Thread-safe Singleton implementation using Lazy<T>
/// NOTE: Consider using dependency injection instead for better testability
/// </summary>
public sealed class Singleton
{
    private static readonly Lazy<Singleton> instance = new(() => new Singleton());
    
    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static Singleton Instance => instance.Value;
    
    // Private constructor prevents instantiation from outside
    private Singleton() => InitializeResources();
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    
    private void InitializeResources()
    {
        // Initialize expensive resources here
        Console.WriteLine("Singleton instance created");
    }
    
    public void DoWork() => Console.WriteLine("Singleton is doing work...");
    
    // Example property
    public string Name { get; set; } = "DefaultSingleton";
}

/// <summary>
/// Generic Singleton base class for inheritance
/// </summary>
public abstract class SingletonBase<T> where T : class, new()
{
    private static readonly Lazy<T> instance = new(() => new T());
    
    public static T Instance => instance.Value;
    
    protected SingletonBase() { }
}

/// <summary>
/// Example of inheriting from generic singleton
/// </summary>
public class ConfigurationManager : SingletonBase<ConfigurationManager>
{
    public string ConnectionString { get; set; } = "DefaultConnection";
    
    public void LoadConfiguration()
    {
        Console.WriteLine("Loading configuration...");
    }
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        // Basic Singleton usage
        var singleton1 = Singleton.Instance;
        var singleton2 = Singleton.Instance;
        
        Console.WriteLine($"Same instance: {ReferenceEquals(singleton1, singleton2)}");
        // Output: Same instance: True
        
        singleton1.Name = "Modified";
        Console.WriteLine($"singleton2.Name: {singleton2.Name}");
        // Output: singleton2.Name: Modified
        
        singleton1.DoWork();
        // Output: Singleton is doing work...
        
        // Generic Singleton usage
        var config1 = ConfigurationManager.Instance;
        var config2 = ConfigurationManager.Instance;
        
        Console.WriteLine($"Config same instance: {ReferenceEquals(config1, config2)}");
        // Output: Config same instance: True
        
        config1.LoadConfiguration();
        // Output: Loading configuration...
    }
}
```

**Notes**:

- Targets .NET 8.0 SDK with modern C# features
- Thread-safe using `Lazy<T>` which handles synchronization internally
- Sealed class prevents inheritance issues and potential threading problems
- Private constructor ensures single instantiation path
- Generic base class allows easy singleton implementation for any class
- Lazy initialization - instance created only when first accessed
- **Prefer dependency injection over Singleton pattern** for better testability and maintainability
- Uses expression-bodied members where appropriate for conciseness
- Consider using `[MethodImpl(MethodImplOptions.NoInlining)]` for initialization methods
- Related patterns: [Factory Pattern](factory.md), [Dependency Injection](../integration/dependency-injection.md)

**Prerequisites**:

- .NET 8.0 SDK (uses modern C# syntax and patterns)

**References**:

- [Microsoft Docs: Lazy\<T\>](https://docs.microsoft.com/en-us/dotnet/api/system.lazy-1)
- Gang of Four Design Patterns book

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #singleton #creational #thread-safe #lazy*
