# Factory Patterns

**Description**: Comprehensive factory patterns including Simple Factory, Factory Method, and Abstract Factory patterns. These patterns provide flexible object creation mechanisms without exposing the underlying instantiation logic to the client.

**Language/Technology**: C#, .NET 9.0

**Code**:

## Simple Factory Pattern

```csharp
namespace DesignPatterns.Factory;

/// <summary>
/// Product interface for all loggers
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogError(string error);
}

/// <summary>
/// Console logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Log(string message) => 
        Console.WriteLine($"[CONSOLE] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");

    public void LogError(string error) => 
        Console.WriteLine($"[CONSOLE ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {error}");
}

/// <summary>
/// File logger implementation
/// </summary>
public class FileLogger(string filePath = "application.log") : ILogger
{
    public void Log(string message) => 
        File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n");

    public void LogError(string error) => 
        File.AppendAllText(filePath, $"ERROR {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {error}\n");
}

/// <summary>
/// Database logger implementation  
/// </summary>
public class DatabaseLogger(string connectionString) : ILogger
{
    public void Log(string message)
    {
        // Implementation would save to database
        Console.WriteLine($"[DATABASE] Saved: {message}");
    }

    public void LogError(string error)
    {
        Console.WriteLine($"[DATABASE ERROR] Saved: {error}");
    }
}

/// <summary>
/// Simple factory for creating loggers based on type
/// </summary>
public static class LoggerFactory
{
    /// <summary>
    /// Creates logger instances based on the specified type
    /// </summary>
    /// <param name="loggerType">Type of logger to create</param>
    /// <param name="configuration">Optional configuration parameters</param>
    /// <returns>Configured logger instance</returns>
    /// <exception cref="ArgumentException">Thrown for unsupported logger types</exception>
    public static ILogger CreateLogger(LoggerType loggerType, string? configuration = null)
    {
        return loggerType switch
        {
            LoggerType.Console => new ConsoleLogger(),
            LoggerType.File => new FileLogger(configuration ?? "application.log"),
            LoggerType.Database => new DatabaseLogger(configuration ?? "DefaultConnection"),
            _ => throw new ArgumentException($"Unsupported logger type: {loggerType}", nameof(loggerType))
        };
    }

    /// <summary>
    /// Creates multiple loggers for composite logging
    /// </summary>
    public static ILogger CreateCompositeLogger(params LoggerType[] types) =>
        new CompositeLogger(types.Select(t => CreateLogger(t)).ToArray());
}

/// <summary>
/// Supported logger types for factory creation
/// </summary>
public enum LoggerType
{
    Console,
    File,
    Database
}

/// <summary>
/// Composite logger that delegates to multiple loggers
/// </summary>
public class CompositeLogger(ILogger[] loggers) : ILogger
{
    public void Log(string message)
    {
        foreach (var logger in loggers)
        {
            logger.Log(message);
        }
    }

    public void LogError(string error)
    {
        foreach (var logger in loggers)
        {
            logger.LogError(error);
        }
    }
}
```

**Usage**:

```csharp
// Simple factory usage
var consoleLogger = LoggerFactory.CreateLogger(LoggerType.Console);
var fileLogger = LoggerFactory.CreateLogger(LoggerType.File, "logs/app.log");
var dbLogger = LoggerFactory.CreateLogger(LoggerType.Database, "Server=.;Database=Logs");

// Composite logger
var compositeLogger = LoggerFactory.CreateCompositeLogger(
    LoggerType.Console, 
    LoggerType.File, 
    LoggerType.Database);

consoleLogger.Log("Application started");
fileLogger.LogError("Configuration file not found");
compositeLogger.Log("This will be logged to all registered loggers");
```

**Notes**:
- **Simple Factory**: Encapsulates object creation logic in a single method
- **Flexibility**: Easy to add new logger types without changing client code
- **Configuration**: Supports parameterized creation for different configurations
- **Composite Pattern Integration**: Demonstrates how factory patterns work with other design patterns
- **Modern C# Features**: Uses primary constructors, expression-bodied members, and switch expressions
- **Error Handling**: Proper exception handling for unsupported types
- **Performance**: Minimal overhead with direct instantiation
- **Testability**: Easy to mock and unit test individual logger implementations

**Related Patterns**:
- Strategy Pattern: For interchangeable logging strategies
- Abstract Factory: For creating families of related logging components
- Builder Pattern: For complex logger configuration