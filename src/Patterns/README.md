# Patterns

A collection of common design patterns and utilities for C#/.NET applications.

## Features

### 1. Lazy-Loading Singleton Pattern

A thread-safe singleton implementation using .NET's `Lazy<T>` class that defers instantiation until first access.

#### Usage

```csharp
using Patterns;

// Define your singleton class
public class ConfigurationManager : LazySingleton<ConfigurationManager>
{
    public string AppName { get; set; } = "MyApp";
    
    public void LoadConfiguration()
    {
        // Load configuration logic
    }
}

// Access the singleton instance
var config = ConfigurationManager.Instance;
config.LoadConfiguration();

// Always returns the same instance
var config2 = ConfigurationManager.Instance;
Console.WriteLine(config == config2); // True
```

### 2. Constants Dictionary Builder

A utility that builds dictionaries from class constants using reflection. Supports const fields, static readonly fields, and static properties.

#### Usage

```csharp
using Patterns;

// Define a class with constants
public static class HttpStatusCodes
{
    public const int Ok = 200;
    public const int Created = 201;
    public const int BadRequest = 400;
    public const int NotFound = 404;
    public static readonly int InternalServerError = 500;
}

// Build dictionary from constants
var statusCodes = ConstantsDictionaryBuilder.BuildFromConstants<HttpStatusCodes>();

foreach (var kvp in statusCodes)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
// Output:
// Ok: 200
// Created: 201
// BadRequest: 400
// NotFound: 404
// InternalServerError: 500
```

#### Available Methods

- `BuildFromConstants<T>()` - Builds dictionary from const and static readonly fields
- `BuildFromProperties<T>()` - Builds dictionary from static properties
- `BuildFromAll<T>()` - Builds dictionary from both fields and properties

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```
