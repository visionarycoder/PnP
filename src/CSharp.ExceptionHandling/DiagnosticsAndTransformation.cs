using System.Diagnostics;
using System.Collections.Concurrent;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Collects and manages diagnostic information for error analysis.
/// </summary>
public class DiagnosticCollector
{
    private readonly ConcurrentDictionary<string, object> diagnosticData = new();
    private readonly List<DiagnosticEvent> events = new();
    private readonly object eventsLock = new();

    public string CorrelationId { get; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public void AddData(string key, object value)
    {
        diagnosticData.AddOrUpdate(key, value, (k, v) => value);
    }

    public T? GetData<T>(string key)
    {
        if (diagnosticData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddEvent(string eventType, string message, Exception? exception = null)
    {
        var diagnosticEvent = new DiagnosticEvent(eventType, message, exception, DateTime.UtcNow);
        
        lock (eventsLock)
        {
            events.Add(diagnosticEvent);
        }
    }

    public IReadOnlyList<DiagnosticEvent> GetEvents()
    {
        lock (eventsLock)
        {
            return events.ToList();
        }
    }

    public Dictionary<string, object> GetAllData()
    {
        return new Dictionary<string, object>(diagnosticData);
    }

    public DiagnosticSummary CreateSummary()
    {
        return new DiagnosticSummary(
            CorrelationId,
            CreatedAt,
            GetAllData(),
            GetEvents());
    }
}

/// <summary>
/// Represents a diagnostic event with timestamp and context.
/// </summary>
public record DiagnosticEvent(
    string EventType,
    string Message,
    Exception? Exception,
    DateTime Timestamp);

/// <summary>
/// Summary of diagnostic information for an operation.
/// </summary>
public record DiagnosticSummary(
    string CorrelationId,
    DateTime CreatedAt,
    Dictionary<string, object> Data,
    IReadOnlyList<DiagnosticEvent> Events);

/// <summary>
/// Context manager for maintaining diagnostic information across operation chains.
/// </summary>
public class DiagnosticContext : IDisposable
{
    private static readonly AsyncLocal<DiagnosticContext?> current = new();
    private readonly DiagnosticCollector collector;
    private readonly DiagnosticContext? parent;

    public static DiagnosticContext? Current => current.Value;

    public DiagnosticContext(string? operationName = null)
    {
        collector = new DiagnosticCollector();
        parent = current.Value;
        current.Value = this;

        if (!string.IsNullOrEmpty(operationName))
        {
            collector.AddData("OperationName", operationName);
        }

        collector.AddEvent("ContextCreated", $"Diagnostic context created for operation: {operationName}");
    }

    public void AddData(string key, object value) => collector.AddData(key, value);
    public void AddEvent(string eventType, string message, Exception? exception = null) => collector.AddEvent(eventType, message, exception);
    public DiagnosticSummary GetSummary() => collector.CreateSummary();

    public void Dispose()
    {
        collector.AddEvent("ContextDisposed", "Diagnostic context disposed");
        current.Value = parent;
    }
}

/// <summary>
/// Exception handler that captures rich diagnostic information.
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// Handles an exception with rich diagnostic information collection.
    /// </summary>
    public static EnrichedExceptionInfo HandleException(Exception exception, string? operationName = null)
    {
        var diagnostics = DiagnosticContext.Current?.GetSummary() ?? CreateEmptyDiagnostics();
        var stackTrace = new StackTrace(exception, true);
        var environmentInfo = CollectEnvironmentInfo();

        return new EnrichedExceptionInfo(
            exception,
            operationName ?? "Unknown Operation",
            diagnostics,
            stackTrace,
            environmentInfo,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Executes an operation with automatic exception handling and diagnostics.
    /// </summary>
    public static async Task<T> HandleAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        Action<EnrichedExceptionInfo>? onError = null)
    {
        using var context = new DiagnosticContext(operationName);
        
        try
        {
            context.AddEvent("OperationStarted", $"Starting operation: {operationName}");
            var result = await operation();
            context.AddEvent("OperationCompleted", $"Operation completed successfully: {operationName}");
            return result;
        }
        catch (Exception ex)
        {
            context.AddEvent("OperationFailed", $"Operation failed: {operationName}", ex);
            var enrichedInfo = HandleException(ex, operationName);
            onError?.Invoke(enrichedInfo);
            throw;
        }
    }

    private static DiagnosticSummary CreateEmptyDiagnostics()
    {
        return new DiagnosticSummary(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            new Dictionary<string, object>(),
            new List<DiagnosticEvent>());
    }

    private static Dictionary<string, object> CollectEnvironmentInfo()
    {
        return new Dictionary<string, object>
        {
            ["MachineName"] = Environment.MachineName,
            ["ProcessId"] = Environment.ProcessId,
            ["ThreadId"] = Environment.CurrentManagedThreadId,
            ["WorkingSet"] = Environment.WorkingSet,
            ["TickCount"] = Environment.TickCount64,
            ["OSVersion"] = Environment.OSVersion.ToString(),
            ["CLRVersion"] = Environment.Version.ToString()
        };
    }
}

/// <summary>
/// Rich exception information with diagnostic context.
/// </summary>
public record EnrichedExceptionInfo(
    Exception Exception,
    string OperationName,
    DiagnosticSummary Diagnostics,
    StackTrace StackTrace,
    Dictionary<string, object> EnvironmentInfo,
    DateTime CapturedAt);

/// <summary>
/// Exception transformation utilities for converting between exception types.
/// </summary>
public static class ExceptionTransformer
{
    /// <summary>
    /// Transforms a general exception into a domain-specific exception.
    /// </summary>
    public static DomainException ToDomainException(Exception exception, string? context = null)
    {
        return exception switch
        {
            DomainException domain => domain,
            ArgumentException arg => new ValidationException(
                $"Invalid argument: {arg.Message}",
                new[] { new ValidationError(arg.ParamName ?? "Unknown", arg.Message, null, "INVALID_ARGUMENT") }),
            InvalidOperationException invalid => new BusinessRuleException(
                "InvalidOperation", 
                invalid.Message, 
                invalid),
            TimeoutException timeout => new ExternalServiceException(
                context ?? "Unknown Service",
                $"Operation timed out: {timeout.Message}",
                408, // Request Timeout
                null,
                timeout),
            HttpRequestException http => new ExternalServiceException(
                context ?? "HTTP Service",
                http.Message,
                null,
                null,
                http),
            _ => new BusinessRuleException(
                "UnhandledException",
                $"An unexpected error occurred: {exception.Message}",
                exception)
        };
    }

    /// <summary>
    /// Flattens nested exceptions for easier analysis.
    /// </summary>
    public static IEnumerable<Exception> FlattenExceptions(Exception exception)
    {
        var current = exception;
        
        while (current != null)
        {
            yield return current;
            
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.SelectMany(FlattenExceptions))
                {
                    yield return inner;
                }
                break;
            }
            
            current = current.InnerException;
        }
    }
}