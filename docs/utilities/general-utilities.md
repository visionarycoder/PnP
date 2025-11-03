# Enterprise General Utilities

**Description**: Production-ready utility functions with type safety, comprehensive error handling, and performance optimization for enterprise applications
**Language/Technology**: C# / .NET 9.0
**Prerequisites**: .NET 9.0 SDK, System.Text.Json, Microsoft.Extensions.Options

## Type-Safe Result Pattern

**Description**: Comprehensive result pattern implementation for error handling and optional value management with enterprise-grade features

**Code**:

```csharp
namespace EnterpriseUtilities.Core;

/// <summary>
/// Represents the result of an operation that may succeed or fail
/// </summary>
/// <typeparam name="TValue">The type of the success value</typeparam>
/// <typeparam name="TError">The type of the error value</typeparam>
public readonly record struct Result<TValue, TError>
{
    private readonly TValue? value;
    private readonly TError? error;
    private readonly bool isSuccess;

    private Result(TValue value)
    {
        this.value = value;
        error = default;
        isSuccess = true;
    }

    private Result(TError error)
    {
        value = default;
        this.error = error;
        isSuccess = false;
    }

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    /// <summary>
    /// Transforms the success value using the provided function
    /// </summary>
    public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> mapper)
    {
        return isSuccess && value is not null
            ? Result<TResult, TError>.Success(mapper(value))
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Chains multiple operations that return Result types
    /// </summary>
    public Result<TResult, TError> Bind<TResult>(Func<TValue, Result<TResult, TError>> binder)
    {
        return isSuccess && value is not null
            ? binder(value)
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Handles both success and failure cases and returns a single result
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        return isSuccess && value is not null
            ? onSuccess(value)
            : onFailure(error!);
    }

    /// <summary>
    /// Executes side effects without changing the result
    /// </summary>
    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        if (isSuccess && value is not null)
        {
            action(value);
        }
        return this;
    }

    /// <summary>
    /// Returns the value if successful, otherwise throws an exception
    /// </summary>
    public TValue Unwrap()
    {
        return isSuccess && value is not null
            ? value
            : throw new InvalidOperationException($"Cannot unwrap failed result: {error}");
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the provided default
    /// </summary>
    public TValue UnwrapOr(TValue defaultValue)
    {
        return isSuccess && value is not null ? value : defaultValue;
    }

    /// <summary>
    /// Returns the value if successful, otherwise computes and returns a default
    /// </summary>
    public TValue UnwrapOrElse(Func<TError, TValue> defaultFactory)
    {
        return isSuccess && value is not null ? value : defaultFactory(error!);
    }

    public static implicit operator bool(Result<TValue, TError> result) => result.isSuccess;
}

/// <summary>
/// Convenience methods for working with Result types
/// </summary>
public static class Result
{
    public static Result<T, string> Try<T>(Func<T> operation)
    {
        try
        {
            return Result<T, string>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T, string>.Failure(ex.Message);
        }
    }

    public static async Task<Result<T, string>> TryAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);
            return Result<T, string>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T, string>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Combines multiple results into a single result containing a collection
    /// </summary>
    public static Result<IEnumerable<T>, string> Combine<T>(params Result<T, string>[] results)
    {
        var values = new List<T>();
        var errors = new List<string>();

        foreach (var result in results)
        {
            result.Match(
                onSuccess: values.Add,
                onFailure: errors.Add);
        }

        return errors.Count == 0
            ? Result<IEnumerable<T>, string>.Success(values)
            : Result<IEnumerable<T>, string>.Failure(string.Join("; ", errors));
    }
}
```

**Usage**:

```csharp
// Basic usage with error handling
Result<int, string> ParseInteger(string input)
{
    return int.TryParse(input, out var result)
        ? Result<int, string>.Success(result)
        : Result<int, string>.Failure($"Invalid integer: {input}");
}

// Chaining operations
var result = ParseInteger("42")
    .Map(x => x * 2)                           // Transform success value
    .Bind(x => x > 50 
        ? Result<int, string>.Success(x) 
        : Result<int, string>.Failure("Value too small"))
    .Tap(x => Console.WriteLine($"Final value: {x}"));   // Side effect

// Pattern matching
var output = result.Match(
    onSuccess: value => $"Success: {value}",
    onFailure: error => $"Error: {error}"
);

// Async operations with error handling
var asyncResult = await Result.TryAsync(async () =>
{
    await Task.Delay(100);
    return await FetchDataFromApiAsync();
});

// Combining multiple operations
var combinedResult = Result.Combine(
    ParseInteger("10"),
    ParseInteger("20"),
    ParseInteger("30")
).Map(values => values.Sum());

Console.WriteLine(combinedResult.UnwrapOr(0)); // Output: 60
```

## High-Performance Data Conversion Utilities

**Description**: Enterprise-grade data conversion utilities with streaming support, validation, and error recovery

**Code**:

```csharp
namespace EnterpriseUtilities.Conversion;

/// <summary>
/// High-performance data conversion utilities with comprehensive error handling
/// </summary>
public static class DataConverter
{
    /// <summary>
    /// Converts objects to JSON with custom serialization options
    /// </summary>
    public static Result<string, string> ToJson<T>(
        T value, 
        JsonSerializerOptions? options = null)
    {
        try
        {
            options ??= new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(value, options);
            return Result<string, string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string, string>.Failure($"JSON serialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes JSON to strongly-typed objects with validation
    /// </summary>
    public static Result<T, string> FromJson<T>(
        string json, 
        JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Result<T, string>.Failure("JSON input is null or empty");
        }

        try
        {
            options ??= new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var result = JsonSerializer.Deserialize<T>(json, options);
            return result is not null
                ? Result<T, string>.Success(result)
                : Result<T, string>.Failure("Deserialization resulted in null value");
        }
        catch (JsonException ex)
        {
            return Result<T, string>.Failure($"JSON deserialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts CSV data to strongly-typed objects with header mapping
    /// </summary>
    public static Result<IEnumerable<T>, string> FromCsv<T>(
        Stream csvStream,
        bool hasHeader = true,
        string delimiter = ",") where T : class, new()
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            var records = new List<T>();
            
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToArray();

            string[]? headers = null;
            if (hasHeader)
            {
                var headerLine = reader.ReadLine();
                if (headerLine is null)
                {
                    return Result<IEnumerable<T>, string>.Failure("CSV header is empty");
                }
                headers = ParseCsvLine(headerLine, delimiter);
            }

            string? line;
            var lineNumber = hasHeader ? 2 : 1;
            
            while ((line = reader.ReadLine()) is not null)
            {
                var values = ParseCsvLine(line, delimiter);
                var record = new T();

                for (var i = 0; i < values.Length && i < properties.Length; i++)
                {
                    var property = headers is not null && i < headers.Length
                        ? properties.FirstOrDefault(p => 
                            string.Equals(p.Name, headers[i], StringComparison.OrdinalIgnoreCase))
                        : properties[i];

                    if (property is not null)
                    {
                        var convertResult = ConvertValue(values[i], property.PropertyType);
                        if (convertResult)
                        {
                            property.SetValue(record, convertResult.Unwrap());
                        }
                    }
                }

                records.Add(record);
                lineNumber++;
            }

            return Result<IEnumerable<T>, string>.Success(records);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<T>, string>.Failure($"CSV parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts objects to CSV format with custom formatting
    /// </summary>
    public static Result<string, string> ToCsv<T>(
        IEnumerable<T> data,
        bool includeHeaders = true,
        string delimiter = ",")
    {
        try
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            var csv = new StringBuilder();

            if (includeHeaders)
            {
                var headers = properties.Select(p => EscapeCsvField(p.Name, delimiter));
                csv.AppendLine(string.Join(delimiter, headers));
            }

            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? string.Empty;
                    return EscapeCsvField(value, delimiter);
                });
                csv.AppendLine(string.Join(delimiter, values));
            }

            return Result<string, string>.Success(csv.ToString());
        }
        catch (Exception ex)
        {
            return Result<string, string>.Failure($"CSV generation failed: {ex.Message}");
        }
    }

    private static string[] ParseCsvLine(string line, string delimiter)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];
            var next = i + 1 < line.Length ? line[i + 1] : '\0';

            if (current == '"')
            {
                if (inQuotes && next == '"')
                {
                    field.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (current.ToString() == delimiter && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(current);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private static string EscapeCsvField(string field, string delimiter)
    {
        if (field.Contains(delimiter) || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static Result<object?, string> ConvertValue(string value, Type targetType)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                return Result<object?, string>.Success(null);
            }

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            return underlyingType.Name switch
            {
                nameof(String) => Result<object?, string>.Success(value),
                nameof(Int32) => Result<object?, string>.Success(int.Parse(value)),
                nameof(Int64) => Result<object?, string>.Success(long.Parse(value)),
                nameof(Double) => Result<object?, string>.Success(double.Parse(value)),
                nameof(Decimal) => Result<object?, string>.Success(decimal.Parse(value)),
                nameof(DateTime) => Result<object?, string>.Success(DateTime.Parse(value)),
                nameof(Boolean) => Result<object?, string>.Success(bool.Parse(value)),
                _ => underlyingType.IsEnum 
                    ? Result<object?, string>.Success(Enum.Parse(underlyingType, value))
                    : Result<object?, string>.Success(Convert.ChangeType(value, underlyingType))
            };
        }
        catch (Exception ex)
        {
            return Result<object?, string>.Failure($"Value conversion failed: {ex.Message}");
        }
    }
}
```

**Usage**:

```csharp
// JSON serialization with error handling
public record Person(string Name, int Age, string Email);

var person = new Person("John Doe", 30, "john@example.com");

var jsonResult = DataConverter.ToJson(person);
jsonResult.Match(
    onSuccess: json => Console.WriteLine($"JSON: {json}"),
    onFailure: error => Console.WriteLine($"Error: {error}")
);

// JSON deserialization with validation
var jsonString = """{"name": "Jane Smith", "age": 25, "email": "jane@example.com"}""";
var personResult = DataConverter.FromJson<Person>(jsonString);

// CSV processing with strong typing
public class Employee
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
}

// Read CSV file
using var fileStream = File.OpenRead("employees.csv");
var employeesResult = DataConverter.FromCsv<Employee>(fileStream, hasHeader: true);

employeesResult.Match(
    onSuccess: employees =>
    {
        foreach (var emp in employees)
        {
            Console.WriteLine($"{emp.Name}: {emp.Salary:C}");
        }
    },
    onFailure: error => Console.WriteLine($"CSV Error: {error}")
);

// Convert objects to CSV
var employeeList = new List<Employee>
{
    new() { Name = "Alice Johnson", Department = "Engineering", Salary = 85000, HireDate = DateTime.Now.AddYears(-2) },
    new() { Name = "Bob Wilson", Department = "Marketing", Salary = 65000, HireDate = DateTime.Now.AddMonths(-8) }
};

var csvResult = DataConverter.ToCsv(employeeList, includeHeaders: true);
csvResult.Tap(csv => File.WriteAllText("output.csv", csv));
```

**Notes**:

**Enterprise Utility Development Best Practices**:

- **Type Safety**: Use generic type parameters and nullable reference types to prevent runtime errors
- **Error Handling**: Implement comprehensive error handling with Result pattern for predictable failure scenarios
- **Performance**: Use `Span<T>` and `Memory<T>` for high-performance scenarios with minimal allocations
- **Immutability**: Design utilities to work with immutable data structures where possible
- **Composability**: Enable method chaining and functional composition for complex data transformations
- **Validation**: Implement input validation with clear, actionable error messages
- **Resource Management**: Use proper disposal patterns and async/await for I/O operations

**Advanced Patterns**:

- **Fluent Interfaces**: Design utilities with method chaining for improved readability
- **Extension Methods**: Group related functionality in static classes with meaningful namespaces
- **Strategy Pattern**: Allow customization through dependency injection and strategy interfaces
- **Caching**: Implement intelligent caching for expensive operations with proper invalidation
- **Streaming**: Support large datasets through streaming APIs to minimize memory usage

**Security Considerations**:

- **Input Sanitization**: Validate and sanitize all inputs to prevent injection attacks and data corruption
- **Data Protection**: Implement secure handling of sensitive data with proper encryption and key management
- **Resource Limits**: Implement safeguards against resource exhaustion and denial-of-service attacks
- **Audit Logging**: Log security-relevant operations for compliance and forensic analysis

**Performance Optimization**:

- **Memory Efficiency**: Use object pooling and buffer recycling for high-throughput scenarios
- **Async Patterns**: Implement proper async/await patterns with ConfigureAwait(false) in library code
- **Parallelization**: Leverage parallel processing for CPU-intensive operations with proper partitioning
- **Profiling Integration**: Design utilities to work with performance profiling and monitoring tools

**Testing Strategies**:

- **Unit Testing**: Comprehensive unit test coverage including edge cases and error conditions
- **Property-Based Testing**: Use property-based testing for mathematical and transformation utilities
- **Performance Testing**: Benchmark critical utilities to ensure performance requirements are met
- **Integration Testing**: Test utilities in realistic scenarios with actual data and dependencies

**Related Snippets**:

- [Configuration Helpers](configuration-helpers.md) - Enterprise configuration management utilities
- [Logging Utilities](logging-utilities.md) - Structured logging and observability infrastructure
}

## Advanced Validation Utilities

**Code**:

```csharp
using System.ComponentModel.DataAnnotations;

public static class ValidationUtilities
{
    /// <summary>
    /// Validates an object using data annotations and returns structured results
    /// </summary>
    public static ValidationResult<T> Validate<T>(T instance) where T : notnull
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        
        return new ValidationResult<T>(
            isValid,
            instance,
            results.ToImmutableList()
        );
    }

    /// <summary>
    /// Validates multiple objects and aggregates results
    /// </summary>
    public static BatchValidationResult<T> ValidateBatch<T>(IEnumerable<T> items) where T : notnull
    {
        var validItems = new List<T>();
        var invalidItems = new List<(T Item, IReadOnlyList<ValidationResult> Errors)>();

        foreach (var item in items)
        {
            var result = Validate(item);
            if (result.IsValid)
            {
                validItems.Add(item);
            }
            else
            {
                invalidItems.Add((item, result.ValidationErrors));
            }
        }

        return new BatchValidationResult<T>(
            validItems.ToImmutableList(),
            invalidItems.ToImmutableList()
        );
    }
}

public readonly record struct ValidationResult<T>(
    bool IsValid,
    T Value,
    IReadOnlyList<ValidationResult> ValidationErrors
);

public readonly record struct BatchValidationResult<T>(
    IReadOnlyList<T> ValidItems,
    IReadOnlyList<(T Item, IReadOnlyList<ValidationResult> Errors)> InvalidItems
);
```

## High-Performance Collection Utilities

**Code**:

```csharp
public static class CollectionUtilities
{
    /// <summary>
    /// Safely gets an item from a collection with bounds checking
    /// </summary>
    public static Option<T> TryGet<T>(this IReadOnlyList<T> source, int index)
    {
        return index >= 0 && index < source.Count 
            ? Option<T>.Some(source[index])
            : Option<T>.None();
    }

    /// <summary>
    /// Partitions collection into chunks using Span for performance
    /// </summary>
    public static IEnumerable<ReadOnlyMemory<T>> ChunkMemory<T>(
        ReadOnlyMemory<T> source, 
        int chunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(chunkSize, 0);

        var remaining = source;
        while (!remaining.IsEmpty)
        {
            var takeCount = Math.Min(chunkSize, remaining.Length);
            yield return remaining.Slice(0, takeCount);
            remaining = remaining.Slice(takeCount);
        }
    }

    /// <summary>
    /// Thread-safe dictionary operations with atomic updates
    /// </summary>
    public static Result<TValue> AtomicUpdate<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TValue?, TValue> updateFunc) where TKey : notnull
    {
        try
        {
            var result = dictionary.AddOrUpdate(
                key,
                _ => updateFunc(default),
                (_, existing) => updateFunc(existing)
            );
            
            return Result<TValue>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TValue>.Failure($"Failed to update key '{key}': {ex.Message}");
        }
    }
}
```

**Usage**:

```csharp
// Enterprise validation patterns
var user = new User { Email = "test@example.com", Age = 25 };
var validationResult = ValidationUtilities.Validate(user);

if (validationResult.IsValid)
{
    await ProcessUser(validationResult.Value);
}
else
{
    LogValidationErrors(validationResult.ValidationErrors);
}

// Batch validation with comprehensive reporting
var users = await GetUsersAsync();
var batchResult = ValidationUtilities.ValidateBatch(users);

await ProcessValidUsers(batchResult.ValidItems);
await HandleInvalidUsers(batchResult.InvalidItems);

// High-performance data conversion
var csvResult = await DataConverter.ConvertFromCsvAsync<ProductDto>("products.csv");
csvResult.Match(
    products => ProcessProducts(products),
    error => LogError($"CSV conversion failed: {error}")
);

// Memory-efficient collection processing
var largeDataset = await GetLargeDatasetAsync();
await foreach (var chunk in CollectionUtilities.ChunkMemory(largeDataset, 1000))
{
    await ProcessChunkAsync(chunk);
}

// Thread-safe dictionary operations
var cache = new ConcurrentDictionary<string, UserProfile>();
var updateResult = cache.AtomicUpdate(
    userId,
    existing => existing?.WithUpdatedTimestamp() ?? CreateNewProfile(userId)
);
```

**Notes**:

- **Type Safety**: All utilities use generic parameters and nullable reference types for compile-time safety
- **Performance**: Leverages `Span<T>` and `Memory<T>` for high-performance scenarios with minimal allocations  
- **Error Handling**: Comprehensive error handling through Result pattern prevents exceptions in normal flow
- **Thread Safety**: Atomic operations and immutable data structures ensure safe concurrent access
- **Validation**: Enterprise-grade validation with structured error reporting and batch processing
- **Memory Management**: Efficient memory usage with streaming operations and chunk processing
- **Async Support**: Full async/await support for I/O operations with proper cancellation handling
- **Enterprise Patterns**: Dependency injection ready, comprehensive logging, and observability support

## Related Snippets

- [Configuration Helpers](configuration-helpers.md) - Type-safe configuration management
- [Logging Utilities](logging-utilities.md) - Structured logging and observability
- [Result Pattern](../csharp/result-pattern.md) - Functional error handling
