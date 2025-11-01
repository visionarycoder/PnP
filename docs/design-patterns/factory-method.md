# Factory Method Pattern

**Description**: Creates objects without specifying their exact class, delegating the creation to subclasses. Useful when you need to create different types of objects based on parameters.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;

/// <summary>
/// Abstract product interface
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogError(string error);
}

/// <summary>
/// Concrete product: File logger
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _filePath;
    
    public FileLogger(string filePath = "app.log")
    {
        _filePath = filePath;
    }
    
    public void Log(string message)
    {
        Console.WriteLine($"[FILE LOG] {DateTime.Now}: {message}");
        // In real implementation, write to file
    }
    
    public void LogError(string error)
    {
        Console.WriteLine($"[FILE ERROR] {DateTime.Now}: {error}");
    }
}

/// <summary>
/// Concrete product: Console logger
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[CONSOLE] {message}");
    }
    
    public void LogError(string error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CONSOLE ERROR] {error}");
        Console.ResetColor();
    }
}

/// <summary>
/// Concrete product: Database logger
/// </summary>
public class DatabaseLogger : ILogger
{
    private readonly string _connectionString;
    
    public DatabaseLogger(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public void Log(string message)
    {
        Console.WriteLine($"[DATABASE] Logged to DB: {message}");
        // In real implementation, insert into database
    }
    
    public void LogError(string error)
    {
        Console.WriteLine($"[DATABASE ERROR] Error logged to DB: {error}");
    }
}

/// <summary>
/// Abstract creator class
/// </summary>
public abstract class LoggerFactory
{
    /// <summary>
    /// Factory method - subclasses implement this
    /// </summary>
    public abstract ILogger CreateLogger();
    
    /// <summary>
    /// Template method that uses the factory method
    /// </summary>
    public void LogMessage(string message)
    {
        var logger = CreateLogger();
        logger.Log($"Factory: {GetType().Name} - {message}");
    }
}

/// <summary>
/// Concrete creator: File logger factory
/// </summary>
public class FileLoggerFactory : LoggerFactory
{
    private readonly string _filePath;
    
    public FileLoggerFactory(string filePath = "app.log")
    {
        _filePath = filePath;
    }
    
    public override ILogger CreateLogger()
    {
        return new FileLogger(_filePath);
    }
}

/// <summary>
/// Concrete creator: Console logger factory
/// </summary>
public class ConsoleLoggerFactory : LoggerFactory
{
    public override ILogger CreateLogger()
    {
        return new ConsoleLogger();
    }
}

/// <summary>
/// Concrete creator: Database logger factory
/// </summary>
public class DatabaseLoggerFactory : LoggerFactory
{
    private readonly string _connectionString;
    
    public DatabaseLoggerFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public override ILogger CreateLogger()
    {
        return new DatabaseLogger(_connectionString);
    }
}

/// <summary>
/// Simple factory helper for common scenarios
/// </summary>
public static class LoggerFactoryHelper
{
    public static ILogger CreateLogger(LoggerType type, string parameter = null)
    {
        return type switch
        {
            LoggerType.File => new FileLogger(parameter ?? "app.log"),
            LoggerType.Console => new ConsoleLogger(),
            LoggerType.Database => new DatabaseLogger(parameter ?? "DefaultConnection"),
            _ => throw new ArgumentException($"Unknown logger type: {type}")
        };
    }
}

public enum LoggerType
{
    File,
    Console,
    Database
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        // Factory Method Pattern usage
        LoggerFactory fileFactory = new FileLoggerFactory("custom.log");
        LoggerFactory consoleFactory = new ConsoleLoggerFactory();
        LoggerFactory dbFactory = new DatabaseLoggerFactory("Server=localhost;Database=Logs");
        
        // Use factories to create loggers
        var fileLogger = fileFactory.CreateLogger();
        var consoleLogger = consoleFactory.CreateLogger();
        var dbLogger = dbFactory.CreateLogger();
        
        // Use the loggers
        fileLogger.Log("File log message");
        consoleLogger.Log("Console log message");
        dbLogger.LogError("Database error message");
        
        // Use template method
        fileFactory.LogMessage("Template method message");
        // Output: [FILE LOG] 2025-10-31: Factory: FileLoggerFactory - Template method message
        
        // Simple factory usage
        var logger1 = LoggerFactoryHelper.CreateLogger(LoggerType.Console);
        var logger2 = LoggerFactoryHelper.CreateLogger(LoggerType.File, "debug.log");
        var logger3 = LoggerFactoryHelper.CreateLogger(LoggerType.Database, "ProductionDB");
        
        logger1.Log("Simple factory console message");
        logger2.Log("Simple factory file message");
        logger3.LogError("Simple factory database error");
        
        // Factory pattern allows easy switching
        ProcessData(new ConsoleLoggerFactory());
        ProcessData(new FileLoggerFactory("process.log"));
    }
    
    static void ProcessData(LoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger();
        logger.Log("Processing started...");
        // Process data here
        logger.Log("Processing completed.");
    }
}
```

**Notes**:

- Separates object creation from object usage
- Makes code more flexible and maintainable
- Easy to add new logger types without modifying existing code
- Factory method delegates creation to subclasses
- Simple factory pattern included for basic scenarios
- Template method pattern combined with factory method for common operations
- Consider using dependency injection for more advanced scenarios
- Related patterns: [Abstract Factory](abstract-factory.md), [Builder Pattern](builder.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of inheritance and polymorphism

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Factory Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/factory)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #factory #creational #polymorphism #solid*
