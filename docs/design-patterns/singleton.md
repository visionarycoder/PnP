# Singleton Pattern

**Description**: Ensures a class has only one instance and provides global access to it. Thread-safe implementation using lazy initialization.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;

/// <summary>
/// Thread-safe Singleton implementation using Lazy<T>
/// </summary>
public sealed class Singleton
{
    private static readonly Lazy<Singleton> _instance = new(() => new Singleton());
    
    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static Singleton Instance => _instance.Value;
    
    // Private constructor prevents instantiation from outside
    private Singleton()
    {
        InitializeResources();
    }
    
    private void InitializeResources()
    {
        // Initialize expensive resources here
        Console.WriteLine("Singleton instance created");
    }
    
    public void DoWork()
    {
        Console.WriteLine("Singleton is doing work...");
    }
    
    // Example property
    public string Name { get; set; } = "DefaultSingleton";
}

/// <summary>
/// Generic Singleton base class for inheritance
/// </summary>
public abstract class SingletonBase<T> where T : class, new()
{
    private static readonly Lazy<T> _instance = new(() => new T());
    
    public static T Instance => _instance.Value;
    
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

- Thread-safe using `Lazy&lt;T&gt;` which handles synchronization
- Sealed class prevents inheritance issues
- Private constructor ensures single instantiation
- Generic base class allows easy singleton implementation for any class
- Lazy initialization - instance created only when first accessed
- Consider dependency injection instead of Singleton for better testability
- Related patterns: [Factory Pattern](factory.md), [Multiton Pattern](multiton.md)

**Prerequisites**:

- .NET Framework 4.0+ or .NET Core (for `Lazy&lt;T&gt;`)

**References**:

- [Microsoft Docs: Lazy&lt;T&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.lazy-1)
- Gang of Four Design Patterns book

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #singleton #creational #thread-safe #lazy*
