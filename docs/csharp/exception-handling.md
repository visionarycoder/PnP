# Exception Handling Patterns and Structured Error Management

**Description**: Comprehensive exception handling patterns including structured error management, exception transformation, error boundaries, fault tolerance strategies, exception propagation patterns, and diagnostic information collection for building robust and maintainable applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Base exception hierarchy for structured error handling
[Serializable]
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public string ErrorCategory { get; }
    public Dictionary<string, object> ErrorData { get; }
    public DateTime Timestamp { get; }
    public string CorrelationId { get; }

    protected DomainException(
        string errorCode, 
        string errorCategory, 
        string message, 
        Exception innerException = null,
        string correlationId = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        ErrorCategory = errorCategory ?? throw new ArgumentNullException(nameof(errorCategory));
        ErrorData = new Dictionary<string, object>();
        Timestamp = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    protected DomainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode)) ?? "";
        ErrorCategory = info.GetString(nameof(ErrorCategory)) ?? "";
        Timestamp = info.GetDateTime(nameof(Timestamp));
        CorrelationId = info.GetString(nameof(CorrelationId)) ?? "";
        
        var errorDataJson = info.GetString(nameof(ErrorData));
        ErrorData = string.IsNullOrEmpty(errorDataJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(errorDataJson) ?? new Dictionary<string, object>();
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(ErrorCategory), ErrorCategory);
        info.AddValue(nameof(Timestamp), Timestamp);
        info.AddValue(nameof(CorrelationId), CorrelationId);
        info.AddValue(nameof(ErrorData), JsonSerializer.Serialize(ErrorData));
    }

    public DomainException WithData(string key, object value)
    {
        ErrorData[key] = value;
        return this;
    }

    public T GetData<T>(string key, T defaultValue = default)
    {
        if (ErrorData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}

// Specific domain exception types
[Serializable]
public class ValidationException : DomainException
{
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public ValidationException(
        string message, 
        IEnumerable<ValidationError> validationErrors = null,
        Exception innerException = null,
        string correlationId = null)
        : base("VALIDATION_ERROR", "Validation", message, innerException, correlationId)
    {
        ValidationErrors = validationErrors?.ToList() ?? new List<ValidationError>();
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        var errorsJson = info.GetString(nameof(ValidationErrors));
        ValidationErrors = string.IsNullOrEmpty(errorsJson)
            ? new List<ValidationError>()
            : JsonSerializer.Deserialize<List<ValidationError>>(errorsJson) ?? new List<ValidationError>();
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ValidationErrors), JsonSerializer.Serialize(ValidationErrors));
    }
}

[Serializable]
public class BusinessLogicException : DomainException
{
    public BusinessLogicException(
        string errorCode,
        string message, 
        Exception innerException = null,
        string correlationId = null)
        : base(errorCode, "BusinessLogic", message, innerException, correlationId)
    {
    }

    protected BusinessLogicException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
public class ExternalServiceException : DomainException
{
    public string ServiceName { get; }
    public string OperationName { get; }
    public int? HttpStatusCode { get; }

    public ExternalServiceException(
        string serviceName,
        string operationName,
        string message,
        int? httpStatusCode = null,
        Exception innerException = null,
        string correlationId = null)
        : base("EXTERNAL_SERVICE_ERROR", "ExternalService", message, innerException, correlationId)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        HttpStatusCode = httpStatusCode;
    }

    protected ExternalServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ServiceName = info.GetString(nameof(ServiceName)) ?? "";
        OperationName = info.GetString(nameof(OperationName)) ?? "";
        HttpStatusCode = info.GetValue(nameof(HttpStatusCode), typeof(int?)) as int?;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ServiceName), ServiceName);
        info.AddValue(nameof(OperationName), OperationName);
        info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
    }
}

[Serializable]
public class ResourceNotFoundException : DomainException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ResourceNotFoundException(
        string resourceType,
        string resourceId,
        string message = null,
        Exception innerException = null,
        string correlationId = null)
        : base("RESOURCE_NOT_FOUND", "NotFound", 
               message ?? $"{resourceType} with ID '{resourceId}' was not found", 
               innerException, correlationId)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
    }

    protected ResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ResourceType = info.GetString(nameof(ResourceType)) ?? "";
        ResourceId = info.GetString(nameof(ResourceId)) ?? "";
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ResourceType), ResourceType);
        info.AddValue(nameof(ResourceId), ResourceId);
    }
}

// Validation error model
public record ValidationError(string PropertyName, string ErrorMessage, object AttemptedValue = null);

// Exception transformation and mapping
public interface IExceptionTransformer
{
    Exception Transform(Exception exception, ExceptionContext context);
    bool CanTransform(Exception exception, ExceptionContext context);
}

public class ExceptionContext
{
    public string OperationName { get; set; }
    public string CorrelationId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public CancellationToken CancellationToken { get; set; }
}

public class ExceptionTransformationEngine
{
    private readonly List<IExceptionTransformer> transformers = new List<IExceptionTransformer>();
    private readonly ILogger<ExceptionTransformationEngine> logger;

    public ExceptionTransformationEngine(ILogger<ExceptionTransformationEngine> logger = null)
    {
        this.logger = logger;
    }

    public void AddTransformer(IExceptionTransformer transformer)
    {
        transformers.Add(transformer ?? throw new ArgumentNullException(nameof(transformer)));
    }

    public Exception Transform(Exception exception, ExceptionContext context)
    {
        if (exception == null) return null;

        var currentException = exception;
        
        foreach (var transformer in transformers)
        {
            try
            {
                if (transformer.CanTransform(currentException, context))
                {
                    var transformed = transformer.Transform(currentException, context);
                    if (transformed != null)
                    {
                        logger?.LogDebug("Transformed {OriginalType} to {NewType} using {TransformerType}",
                            currentException.GetType().Name, transformed.GetType().Name, transformer.GetType().Name);
                        currentException = transformed;
                    }
                }
            }
            catch (Exception transformerEx)
            {
                logger?.LogError(transformerEx, "Exception transformer {TransformerType} failed", 
                    transformer.GetType().Name);
            }
        }

        return currentException;
    }
}

// Common exception transformers
public class HttpExceptionTransformer : IExceptionTransformer
{
    public bool CanTransform(Exception exception, ExceptionContext context)
    {
        return exception is HttpRequestException || 
               exception is TaskCanceledException ||
               (exception is System.Net.WebException);
    }

    public Exception Transform(Exception exception, ExceptionContext context)
    {
        return exception switch
        {
            HttpRequestException httpEx => new ExternalServiceException(
                "HttpService", 
                context.OperationName ?? "Unknown", 
                httpEx.Message, 
                null, 
                httpEx, 
                context.CorrelationId),
            
            TaskCanceledException tcEx when tcEx.CancellationToken.IsCancellationRequested => 
                new BusinessLogicException(
                    "OPERATION_CANCELLED", 
                    "Operation was cancelled", 
                    tcEx, 
                    context.CorrelationId),
            
            TaskCanceledException tcEx => new ExternalServiceException(
                "HttpService", 
                context.OperationName ?? "Unknown", 
                "Request timed out", 
                408, 
                tcEx, 
                context.CorrelationId),
            
            System.Net.WebException webEx => new ExternalServiceException(
                "WebService", 
                context.OperationName ?? "Unknown", 
                webEx.Message, 
                null, 
                webEx, 
                context.CorrelationId),
            
            _ => exception
        };
    }
}

public class DatabaseExceptionTransformer : IExceptionTransformer
{
    public bool CanTransform(Exception exception, ExceptionContext context)
    {
        var typeName = exception.GetType().Name;
        return typeName.Contains("Sql") || 
               typeName.Contains("Database") || 
               typeName.Contains("Connection") ||
               exception is InvalidOperationException && exception.Message.Contains("connection");
    }

    public Exception Transform(Exception exception, ExceptionContext context)
    {
        if (exception.Message.Contains("timeout"))
        {
            return new ExternalServiceException(
                "Database", 
                context.OperationName ?? "Unknown", 
                "Database operation timed out", 
                null, 
                exception, 
                context.CorrelationId);
        }

        if (exception.Message.Contains("connection"))
        {
            return new ExternalServiceException(
                "Database", 
                context.OperationName ?? "Unknown", 
                "Database connection failed", 
                null, 
                exception, 
                context.CorrelationId);
        }

        return new ExternalServiceException(
            "Database", 
            context.OperationName ?? "Unknown", 
            exception.Message, 
            null, 
            exception, 
            context.CorrelationId);
    }
}

// Exception boundary pattern
public interface IExceptionBoundary
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, ExceptionContext context = null);
    Task ExecuteAsync(Func<Task> operation, ExceptionContext context = null);
    T Execute<T>(Func<T> operation, ExceptionContext context = null);
    void Execute(Action operation, ExceptionContext context = null);
}

public class ExceptionBoundary : IExceptionBoundary
{
    private readonly ExceptionTransformationEngine transformationEngine;
    private readonly IExceptionHandler exceptionHandler;
    private readonly ILogger<ExceptionBoundary> logger;

    public ExceptionBoundary(
        ExceptionTransformationEngine transformationEngine = null,
        IExceptionHandler exceptionHandler = null,
        ILogger<ExceptionBoundary> logger = null)
    {
        this.transformationEngine = transformationEngine ?? new ExceptionTransformationEngine();
        this.exceptionHandler = exceptionHandler ?? new DefaultExceptionHandler();
        this.logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, ExceptionContext context = null)
    {
        context ??= new ExceptionContext();
        
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var transformedException = transformationEngine.Transform(ex, context);
            await exceptionHandler.HandleAsync(transformedException, context).ConfigureAwait(false);
            
            // Preserve original stack trace if rethrowing
            if (ReferenceEquals(ex, transformedException))
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            
            throw transformedException;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, ExceptionContext context = null)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, context).ConfigureAwait(false);
    }

    public T Execute<T>(Func<T> operation, ExceptionContext context = null)
    {
        context ??= new ExceptionContext();
        
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            var transformedException = transformationEngine.Transform(ex, context);
            exceptionHandler.Handle(transformedException, context);
            
            if (ReferenceEquals(ex, transformedException))
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            
            throw transformedException;
        }
    }

    public void Execute(Action operation, ExceptionContext context = null)
    {
        Execute(() =>
        {
            operation();
            return Task.CompletedTask;
        }, context);
    }
}

// Exception handling strategies
public interface IExceptionHandler
{
    Task HandleAsync(Exception exception, ExceptionContext context);
    void Handle(Exception exception, ExceptionContext context);
}

public class DefaultExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DefaultExceptionHandler> logger;

    public DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger = null)
    {
        this.logger = logger;
    }

    public Task HandleAsync(Exception exception, ExceptionContext context)
    {
        Handle(exception, context);
        return Task.CompletedTask;
    }

    public void Handle(Exception exception, ExceptionContext context)
    {
        if (exception is DomainException domainEx)
        {
            logger?.LogWarning(exception, 
                "Domain exception in operation {OperationName}: {ErrorCode} - {Message}. CorrelationId: {CorrelationId}",
                context.OperationName, domainEx.ErrorCode, domainEx.Message, domainEx.CorrelationId);
        }
        else
        {
            logger?.LogError(exception, 
                "Unhandled exception in operation {OperationName}. CorrelationId: {CorrelationId}",
                context.OperationName, context.CorrelationId);
        }
    }
}

public class CompositeExceptionHandler : IExceptionHandler
{
    private readonly List<IExceptionHandler> handlers = new List<IExceptionHandler>();

    public void AddHandler(IExceptionHandler handler)
    {
        handlers.Add(handler ?? throw new ArgumentNullException(nameof(handler)));
    }

    public async Task HandleAsync(Exception exception, ExceptionContext context)
    {
        var tasks = handlers.Select(h => h.HandleAsync(exception, context));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void Handle(Exception exception, ExceptionContext context)
    {
        foreach (var handler in handlers)
        {
            try
            {
                handler.Handle(exception, context);
            }
            catch (Exception handlerEx)
            {
                // Log handler failures but don't let them affect the original exception
                Debug.WriteLine($"Exception handler {handler.GetType().Name} failed: {handlerEx}");
            }
        }
    }
}

// Exception metrics and monitoring
public class ExceptionMetrics
{
    private readonly ConcurrentDictionary<string, ExceptionTypeMetric> metrics = new();

    public void RecordException(Exception exception, ExceptionContext context)
    {
        var key = $"{exception.GetType().Name}:{context.OperationName}";
        
        metrics.AddOrUpdate(key, 
            new ExceptionTypeMetric(exception.GetType().Name, context.OperationName),
            (k, existing) => existing.IncrementCount());
    }

    public IEnumerable<ExceptionTypeMetric> GetMetrics()
    {
        return metrics.Values.ToList();
    }

    public ExceptionTypeMetric GetMetric(string exceptionType, string operationName = null)
    {
        var key = $"{exceptionType}:{operationName}";
        return metrics.TryGetValue(key, out var metric) ? metric : null;
    }

    public void Reset()
    {
        metrics.Clear();
    }
}

public class ExceptionTypeMetric
{
    private long count = 0;
    private DateTime firstOccurrence = DateTime.UtcNow;
    private DateTime lastOccurrence = DateTime.UtcNow;

    public string ExceptionType { get; }
    public string OperationName { get; }
    public long Count => count;
    public DateTime FirstOccurrence => firstOccurrence;
    public DateTime LastOccurrence => lastOccurrence;

    public ExceptionTypeMetric(string exceptionType, string operationName)
    {
        ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
        OperationName = operationName;
    }

    public ExceptionTypeMetric IncrementCount()
    {
        Interlocked.Increment(ref count);
        lastOccurrence = DateTime.UtcNow;
        return this;
    }
}

public class MetricsExceptionHandler : IExceptionHandler
{
    private readonly ExceptionMetrics metrics;

    public MetricsExceptionHandler(ExceptionMetrics metrics = null)
    {
        this.metrics = metrics ?? new ExceptionMetrics();
    }

    public Task HandleAsync(Exception exception, ExceptionContext context)
    {
        Handle(exception, context);
        return Task.CompletedTask;
    }

    public void Handle(Exception exception, ExceptionContext context)
    {
        metrics.RecordException(exception, context);
    }

    public ExceptionMetrics GetMetrics() => metrics;
}

// Result pattern for exception-free error handling
public readonly struct Result<T>
{
    private readonly T value;
    private readonly Exception exception;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess ? value : throw new InvalidOperationException("Cannot access value of failed result");
    public Exception Exception => IsFailure ? exception : null;

    private Result(T value)
    {
        this.value = value;
        this.exception = null;
        IsSuccess = true;
    }

    private Result(Exception exception)
    {
        this.value = default;
        this.exception = exception ?? throw new ArgumentNullException(nameof(exception));
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new Result<T>(value);
    public static Result<T> Failure(Exception exception) => new Result<T>(exception);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Exception exception) => Failure(exception);

    public Result<U> Map<U>(Func<T, U> mapper)
    {
        return IsSuccess 
            ? Result<U>.Success(mapper(value))
            : Result<U>.Failure(exception);
    }

    public async Task<Result<U>> MapAsync<U>(Func<T, Task<U>> mapper)
    {
        return IsSuccess 
            ? Result<U>.Success(await mapper(value).ConfigureAwait(false))
            : Result<U>.Failure(exception);
    }

    public Result<U> Bind<U>(Func<T, Result<U>> binder)
    {
        return IsSuccess ? binder(value) : Result<U>.Failure(exception);
    }

    public async Task<Result<U>> BindAsync<U>(Func<T, Task<Result<U>>> binder)
    {
        return IsSuccess 
            ? await binder(value).ConfigureAwait(false) 
            : Result<U>.Failure(exception);
    }

    public T ValueOr(T defaultValue) => IsSuccess ? value : defaultValue;
    public T ValueOr(Func<T> defaultValueFactory) => IsSuccess ? value : defaultValueFactory();

    public void Match(Action<T> onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
            onSuccess(value);
        else
            onFailure(exception);
    }

    public U Match<U>(Func<T, U> onSuccess, Func<Exception, U> onFailure)
    {
        return IsSuccess ? onSuccess(value) : onFailure(exception);
    }
}

public static class Result
{
    public static Result<T> Try<T>(Func<T> operation)
    {
        try
        {
            return Result<T>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    public static Result<T> From<T>(T value, Func<T, bool> predicate, Func<Exception> exceptionFactory)
    {
        return predicate(value) 
            ? Result<T>.Success(value) 
            : Result<T>.Failure(exceptionFactory());
    }
}

// Exception aggregation for batch operations
public class ExceptionAggregator
{
    private readonly List<Exception> exceptions = new List<Exception>();
    private readonly object lockObj = new object();

    public void Add(Exception exception)
    {
        if (exception == null) return;
        
        lock (lockObj)
        {
            exceptions.Add(exception);
        }
    }

    public void AddRange(IEnumerable<Exception> exceptions)
    {
        if (exceptions == null) return;
        
        lock (lockObj)
        {
            this.exceptions.AddRange(exceptions.Where(ex => ex != null));
        }
    }

    public bool HasExceptions => exceptions.Count > 0;
    public int Count => exceptions.Count;
    public IReadOnlyList<Exception> Exceptions => exceptions.AsReadOnly();

    public void ThrowIfAny()
    {
        if (HasExceptions)
        {
            throw CreateAggregateException();
        }
    }

    public AggregateException CreateAggregateException()
    {
        return new AggregateException(exceptions);
    }

    public void Clear()
    {
        lock (lockObj)
        {
            exceptions.Clear();
        }
    }
}

// Safe execution patterns
public static class SafeExecution
{
    public static Result<T> Try<T>(Func<T> operation, ILogger logger = null)
    {
        try
        {
            return Result<T>.Success(operation());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Safe execution failed");
            return Result<T>.Failure(ex);
        }
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation, ILogger logger = null)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Safe async execution failed");
            return Result<T>.Failure(ex);
        }
    }

    public static void Ignore(Action operation, ILogger logger = null)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Ignored exception during safe execution");
        }
    }

    public static async Task IgnoreAsync(Func<Task> operation, ILogger logger = null)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Ignored exception during safe async execution");
        }
    }

    public static T WithDefault<T>(Func<T> operation, T defaultValue, ILogger logger = null)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Exception occurred, returning default value");
            return defaultValue;
        }
    }

    public static async Task<T> WithDefaultAsync<T>(Func<Task<T>> operation, T defaultValue, ILogger logger = null)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Exception occurred, returning default value");
            return defaultValue;
        }
    }
}

// Exception context enrichment
public class ExceptionEnricher
{
    private readonly Dictionary<string, Func<ExceptionContext, object>> enrichers = new();

    public void AddEnricher(string key, Func<ExceptionContext, object> enricher)
    {
        enrichers[key] = enricher ?? throw new ArgumentNullException(nameof(enricher));
    }

    public ExceptionContext Enrich(ExceptionContext context)
    {
        foreach (var enricher in enrichers)
        {
            try
            {
                var value = enricher.Value(context);
                if (value != null)
                {
                    context.Properties[enricher.Key] = value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception enricher '{enricher.Key}' failed: {ex}");
            }
        }
        
        return context;
    }
}

// Global exception handler for unhandled exceptions
public static class GlobalExceptionHandler
{
    private static readonly ConcurrentQueue<Exception> unhandledExceptions = new();
    private static IExceptionHandler globalHandler;
    private static bool isInitialized = false;

    public static void Initialize(IExceptionHandler handler)
    {
        if (isInitialized) return;
        
        globalHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        isInitialized = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            unhandledExceptions.Enqueue(exception);
            
            try
            {
                var context = new ExceptionContext
                {
                    OperationName = "UnhandledException",
                    CorrelationId = Guid.NewGuid().ToString()
                };
                
                globalHandler?.Handle(exception, context);
            }
            catch
            {
                // Avoid throwing exceptions in the global handler
            }
        }
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        unhandledExceptions.Enqueue(e.Exception);
        
        try
        {
            var context = new ExceptionContext
            {
                OperationName = "UnobservedTaskException",
                CorrelationId = Guid.NewGuid().ToString()
            };
            
            globalHandler?.Handle(e.Exception, context);
            e.SetObserved(); // Prevent application termination
        }
        catch
        {
            // Avoid throwing exceptions in the global handler
        }
    }

    public static IEnumerable<Exception> GetUnhandledExceptions()
    {
        var exceptions = new List<Exception>();
        while (unhandledExceptions.TryDequeue(out var exception))
        {
            exceptions.Add(exception);
        }
        return exceptions;
    }
}

// Exception debugging and diagnostics
public class ExceptionDiagnostics
{
    public static string GetDetailedExceptionInfo(Exception exception)
    {
        if (exception == null) return "No exception";
        
        var details = new List<string>
        {
            $"Exception Type: {exception.GetType().FullName}",
            $"Message: {exception.Message}",
            $"Source: {exception.Source}",
            $"HResult: 0x{exception.HResult:X8}",
            $"Stack Trace: {exception.StackTrace}"
        };
        
        if (exception.Data.Count > 0)
        {
            details.Add("Data:");
            foreach (var key in exception.Data.Keys)
            {
                details.Add($"  {key}: {exception.Data[key]}");
            }
        }
        
        if (exception is DomainException domainEx)
        {
            details.AddRange(new[]
            {
                $"Error Code: {domainEx.ErrorCode}",
                $"Error Category: {domainEx.ErrorCategory}",
                $"Correlation ID: {domainEx.CorrelationId}",
                $"Timestamp: {domainEx.Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC"
            });
            
            if (domainEx.ErrorData.Any())
            {
                details.Add("Error Data:");
                foreach (var kvp in domainEx.ErrorData)
                {
                    details.Add($"  {kvp.Key}: {kvp.Value}");
                }
            }
        }
        
        if (exception.InnerException != null)
        {
            details.Add("");
            details.Add("--- Inner Exception ---");
            details.Add(GetDetailedExceptionInfo(exception.InnerException));
        }
        
        return string.Join(Environment.NewLine, details);
    }
    
    public static Dictionary<string, object> ExtractExceptionProperties(Exception exception)
    {
        var properties = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().FullName,
            ["Message"] = exception.Message,
            ["Source"] = exception.Source,
            ["HResult"] = exception.HResult,
            ["HasInnerException"] = exception.InnerException != null
        };
        
        if (exception is DomainException domainEx)
        {
            properties["ErrorCode"] = domainEx.ErrorCode;
            properties["ErrorCategory"] = domainEx.ErrorCategory;
            properties["CorrelationId"] = domainEx.CorrelationId;
            properties["Timestamp"] = domainEx.Timestamp;
        }
        
        return properties;
    }
}
```

**Usage**:

```csharp
// Example 1: Domain Exception Hierarchy
Console.WriteLine("Domain Exception Examples:");

try
{
    // Validation exception with multiple errors
    var validationErrors = new[]
    {
        new ValidationError("Email", "Email is required"),
        new ValidationError("Age", "Age must be positive", -5)
    };
    
    throw new ValidationException("Validation failed", validationErrors)
        .WithData("RequestId", "REQ-123")
        .WithData("UserId", "user-456");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation Error: {ex.ErrorCode}");
    Console.WriteLine($"Correlation ID: {ex.CorrelationId}");
    Console.WriteLine($"Validation Errors: {ex.ValidationErrors.Count}");
    
    foreach (var error in ex.ValidationErrors)
    {
        Console.WriteLine($"  {error.PropertyName}: {error.ErrorMessage}");
    }
    
    Console.WriteLine($"Request ID: {ex.GetData<string>("RequestId")}");
}

try
{
    // Business logic exception
    throw new BusinessLogicException("INSUFFICIENT_FUNDS", "Account balance is insufficient");
}
catch (BusinessLogicException ex)
{
    Console.WriteLine($"Business Logic Error: {ex.ErrorCode} - {ex.Message}");
}

try
{
    // External service exception
    throw new ExternalServiceException("PaymentGateway", "ProcessPayment", 
        "Payment processing failed", 503);
}
catch (ExternalServiceException ex)
{
    Console.WriteLine($"External Service Error: {ex.ServiceName}.{ex.OperationName}");
    Console.WriteLine($"HTTP Status: {ex.HttpStatusCode}");
}

// Example 2: Exception Transformation
Console.WriteLine("\nException Transformation Examples:");

var transformationEngine = new ExceptionTransformationEngine();
transformationEngine.AddTransformer(new HttpExceptionTransformer());
transformationEngine.AddTransformer(new DatabaseExceptionTransformer());

var testExceptions = new Exception[]
{
    new HttpRequestException("HTTP request failed"),
    new TaskCanceledException("Request timed out"),
    new InvalidOperationException("Database connection timeout occurred"),
    new ArgumentException("Invalid argument")
};

foreach (var ex in testExceptions)
{
    var context = new ExceptionContext 
    { 
        OperationName = "TestOperation",
        CorrelationId = Guid.NewGuid().ToString()
    };
    
    var transformed = transformationEngine.Transform(ex, context);
    
    Console.WriteLine($"Original: {ex.GetType().Name}");
    Console.WriteLine($"Transformed: {transformed.GetType().Name}");
    
    if (transformed is DomainException domainEx)
    {
        Console.WriteLine($"  Error Code: {domainEx.ErrorCode}");
        Console.WriteLine($"  Category: {domainEx.ErrorCategory}");
    }
    
    Console.WriteLine();
}

// Example 3: Exception Boundary Pattern
Console.WriteLine("Exception Boundary Examples:");

var boundary = new ExceptionBoundary(transformationEngine);

// Async operation with exception boundary
var result1 = await SafeExecution.TryAsync(async () =>
{
    return await boundary.ExecuteAsync(async () =>
    {
        // Simulate operation that might fail
        if (Random.Shared.NextDouble() < 0.5)
        {
            throw new HttpRequestException("Service unavailable");
        }
        
        await Task.Delay(100);
        return "Operation completed successfully";
    }, new ExceptionContext { OperationName = "AsyncOperation" });
});

result1.Match(
    onSuccess: value => Console.WriteLine($"✓ Success: {value}"),
    onFailure: ex => Console.WriteLine($"✗ Failed: {ex.GetType().Name} - {ex.Message}")
);

// Synchronous operation with exception boundary
var result2 = SafeExecution.Try(() =>
{
    return boundary.Execute(() =>
    {
        if (Random.Shared.NextDouble() < 0.3)
        {
            throw new ArgumentException("Invalid input parameter");
        }
        
        return 42;
    }, new ExceptionContext { OperationName = "SyncOperation" });
});

Console.WriteLine($"Sync result: {(result2.IsSuccess ? result2.Value.ToString() : "Failed")}");

// Example 4: Result Pattern for Exception-Free Programming
Console.WriteLine("\nResult Pattern Examples:");

// Chain operations with Result pattern
var chainResult = await Result.TryAsync(async () => "initial value")
    .BindAsync(async value => 
    {
        await Task.Delay(50);
        return Result<string>.Success($"{value} -> processed");
    })
    .BindAsync(async value =>
    {
        if (value.Length > 10)
        {
            return Result<int>.Success(value.Length);
        }
        return Result<int>.Failure(new ArgumentException("Value too short"));
    })
    .MapAsync(async length =>
    {
        await Task.Delay(25);
        return length * 2;
    });

chainResult.Match(
    onSuccess: value => Console.WriteLine($"✓ Chain result: {value}"),
    onFailure: ex => Console.WriteLine($"✗ Chain failed: {ex.Message}")
);

// Multiple operations with different outcomes
var operations = new[]
{
    () => Result<string>.Success("Operation 1 succeeded"),
    () => Result<string>.Failure(new InvalidOperationException("Operation 2 failed")),
    () => Result<string>.Success("Operation 3 succeeded")
};

foreach (var (op, index) in operations.Select((op, i) => (op, i)))
{
    var result = op();
    Console.WriteLine($"Operation {index + 1}: {(result.IsSuccess ? result.Value : $"Failed - {result.Exception.Message}")}");
}

// Example 5: Exception Metrics and Monitoring
Console.WriteLine("\nException Metrics Examples:");

var exceptionMetrics = new ExceptionMetrics();
var metricsHandler = new MetricsExceptionHandler(exceptionMetrics);

// Simulate various exceptions to collect metrics
var simulatedExceptions = new[]
{
    ("DatabaseOperation", new InvalidOperationException("DB Error")),
    ("WebRequest", new HttpRequestException("HTTP Error")),
    ("DatabaseOperation", new TimeoutException("DB Timeout")),
    ("ValidationCheck", new ValidationException("Invalid data")),
    ("WebRequest", new TaskCanceledException("Request cancelled"))
};

foreach (var (operation, exception) in simulatedExceptions)
{
    var context = new ExceptionContext { OperationName = operation };
    metricsHandler.Handle(exception, context);
}

// Display metrics
Console.WriteLine("Exception Metrics:");
foreach (var metric in exceptionMetrics.GetMetrics())
{
    Console.WriteLine($"  {metric.ExceptionType} in {metric.OperationName}: {metric.Count} occurrences");
    Console.WriteLine($"    First: {metric.FirstOccurrence:HH:mm:ss}, Last: {metric.LastOccurrence:HH:mm:ss}");
}

// Example 6: Exception Aggregation for Batch Operations
Console.WriteLine("\nException Aggregation Examples:");

var aggregator = new ExceptionAggregator();

// Simulate batch operation with some failures
var batchOperations = Enumerable.Range(1, 10).Select(i => new Func<string>(() =>
{
    if (i % 3 == 0) // Every 3rd operation fails
    {
        throw new InvalidOperationException($"Batch operation {i} failed");
    }
    return $"Batch operation {i} succeeded";
}));

var results = new List<string>();

foreach (var (operation, index) in batchOperations.Select((op, i) => (op, i + 1)))
{
    try
    {
        var result = operation();
        results.Add(result);
        Console.WriteLine($"✓ {result}");
    }
    catch (Exception ex)
    {
        aggregator.Add(ex);
        Console.WriteLine($"✗ Operation {index} failed: {ex.Message}");
    }
}

Console.WriteLine($"\nBatch Summary: {results.Count} succeeded, {aggregator.Count} failed");

if (aggregator.HasExceptions)
{
    Console.WriteLine("Failed operations:");
    foreach (var ex in aggregator.Exceptions)
    {
        Console.WriteLine($"  - {ex.Message}");
    }
}

// Example 7: Safe Execution Patterns
Console.WriteLine("\nSafe Execution Examples:");

// Safe execution with default values
var safeValue1 = SafeExecution.WithDefault(() =>
{
    if (Random.Shared.NextDouble() < 0.5)
        throw new InvalidOperationException("Random failure");
    return "Success value";
}, "Default value");

Console.WriteLine($"Safe execution result: {safeValue1}");

// Safe async execution with Result pattern
var safeResult = await SafeExecution.TryAsync(async () =>
{
    await Task.Delay(100);
    
    if (Random.Shared.NextDouble() < 0.6)
        throw new HttpRequestException("Service temporarily unavailable");
    
    return "Async operation completed";
});

safeResult.Match(
    onSuccess: value => Console.WriteLine($"✓ Async safe execution: {value}"),
    onFailure: ex => Console.WriteLine($"✗ Async safe execution failed: {ex.Message}")
);

// Ignore exceptions for cleanup operations
SafeExecution.Ignore(() =>
{
    // Simulate cleanup that might fail but shouldn't stop execution
    if (Random.Shared.NextDouble() < 0.7)
        throw new IOException("Cleanup file operation failed");
    
    Console.WriteLine("Cleanup completed successfully");
});

Console.WriteLine("Cleanup operation completed (exceptions ignored)");

// Example 8: Exception Context Enrichment
Console.WriteLine("\nException Context Enrichment Examples:");

var enricher = new ExceptionEnricher();
enricher.AddEnricher("MachineName", ctx => Environment.MachineName);
enricher.AddEnricher("ThreadId", ctx => Thread.CurrentThread.ManagedThreadId);
enricher.AddEnricher("Timestamp", ctx => DateTime.UtcNow);
enricher.AddEnricher("ProcessId", ctx => Process.GetCurrentProcess().Id);

var enrichedContext = new ExceptionContext
{
    OperationName = "EnrichedOperation",
    CorrelationId = Guid.NewGuid().ToString()
};

enricher.Enrich(enrichedContext);

Console.WriteLine("Enriched Context Properties:");
foreach (var prop in enrichedContext.Properties)
{
    Console.WriteLine($"  {prop.Key}: {prop.Value}");
}

// Example 9: Exception Diagnostics
Console.WriteLine("\nException Diagnostics Examples:");

try
{
    var innerException = new ArgumentException("Inner exception message");
    var outerException = new ValidationException("Outer validation error")
        .WithData("UserId", "12345")
        .WithData("RequestTime", DateTime.UtcNow);
    
    outerException.Data["CustomProperty"] = "CustomValue";
    
    throw new BusinessLogicException("COMPLEX_ERROR", "Complex error scenario", outerException);
}
catch (Exception ex)
{
    Console.WriteLine("Detailed Exception Information:");
    Console.WriteLine(ExceptionDiagnostics.GetDetailedExceptionInfo(ex));
    
    Console.WriteLine("\nExtracted Properties:");
    var properties = ExceptionDiagnostics.ExtractExceptionProperties(ex);
    foreach (var prop in properties)
    {
        Console.WriteLine($"  {prop.Key}: {prop.Value}");
    }
}

// Example 10: Global Exception Handler Setup
Console.WriteLine("\nGlobal Exception Handler Examples:");

var globalHandler = new CompositeExceptionHandler();
globalHandler.AddHandler(new DefaultExceptionHandler());
globalHandler.AddHandler(metricsHandler);

// Initialize global exception handling
GlobalExceptionHandler.Initialize(globalHandler);

// Simulate unhandled exception in background task
Task.Run(() =>
{
    throw new InvalidOperationException("Unhandled background task exception");
});

// Give time for unhandled exception to be processed
await Task.Delay(500);

var unhandledExceptions = GlobalExceptionHandler.GetUnhandledExceptions();
Console.WriteLine($"Captured {unhandledExceptions.Count()} unhandled exceptions");

foreach (var ex in unhandledExceptions)
{
    Console.WriteLine($"  Unhandled: {ex.GetType().Name} - {ex.Message}");
}

Console.WriteLine("\nException handling pattern examples completed!");
```

**Notes**:

- Use structured exception hierarchies to provide consistent error information across applications
- Implement exception transformation to convert low-level exceptions to domain-specific ones
- Apply exception boundaries to centralize exception handling and transformation logic
- Use the Result pattern to avoid exception-based control flow for expected error conditions
- Collect exception metrics for monitoring and operational insights
- Implement exception aggregation for batch operations to handle partial failures gracefully
- Use safe execution patterns to prevent exceptions from crashing critical operations
- Enrich exception context with additional diagnostic information
- Set up global exception handlers to capture and log unhandled exceptions
- Preserve original stack traces when rethrowing exceptions using ExceptionDispatchInfo
- Consider performance implications of exception handling in hot paths
- Use structured logging to correlate exceptions with business operations

**Prerequisites**:

- Understanding of .NET exception handling mechanisms and inheritance
- Knowledge of async/await patterns and Task-based programming
- Familiarity with serialization for exception persistence
- Understanding of thread safety for concurrent exception handling
- Knowledge of logging frameworks and structured logging
- Experience with monitoring and observability patterns

**Related Snippets**:

- [Circuit Breaker](circuit-breaker.md) - Fault tolerance patterns and resilience
- [Polly Patterns](polly-patterns.md) - Integration with Polly resilience library  
- [Logging Patterns](logging-patterns.md) - Comprehensive logging and observability
- [Async Patterns](async-enumerable.md) - Asynchronous programming patterns
