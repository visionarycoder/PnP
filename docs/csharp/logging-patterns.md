# Logging Patterns and Observability

**Description**: Comprehensive logging and observability patterns including structured logging, contextual logging, performance logging, log correlation, audit trails, metrics integration, and distributed tracing for building observable and maintainable applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

// Structured logging context and correlation
public class LogContext
{
    private static readonly AsyncLocal<Dictionary<string, object>> contextStorage = new();
    
    public static IReadOnlyDictionary<string, object> Current => 
        contextStorage.Value ?? new Dictionary<string, object>();
    
    public static IDisposable BeginScope(string key, object value)
    {
        return BeginScope(new Dictionary<string, object> { [key] = value });
    }
    
    public static IDisposable BeginScope(IEnumerable<KeyValuePair<string, object>> properties)
    {
        var currentContext = contextStorage.Value ?? new Dictionary<string, object>();
        var newContext = new Dictionary<string, object>(currentContext);
        
        foreach (var property in properties)
        {
            newContext[property.Key] = property.Value;
        }
        
        contextStorage.Value = newContext;
        
        return new LogContextScope(() => contextStorage.Value = currentContext);
    }
    
    public static void SetProperty(string key, object value)
    {
        var context = contextStorage.Value ?? new Dictionary<string, object>();
        context[key] = value;
        contextStorage.Value = context;
    }
    
    public static T GetProperty<T>(string key, T defaultValue = default)
    {
        var context = contextStorage.Value;
        if (context?.TryGetValue(key, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    public static void Clear()
    {
        contextStorage.Value = null;
    }
}

internal class LogContextScope : IDisposable
{
    private readonly Action restoreAction;
    private bool disposed = false;
    
    public LogContextScope(Action restoreAction)
    {
        this.restoreAction = restoreAction ?? throw new ArgumentNullException(nameof(restoreAction));
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            restoreAction();
            disposed = true;
        }
    }
}

// Enhanced structured logger with context awareness
public interface IContextualLogger : ILogger
{
    IContextualLogger WithContext(string key, object value);
    IContextualLogger WithContext(IEnumerable<KeyValuePair<string, object>> properties);
    IContextualLogger WithCorrelationId(string correlationId);
    IContextualLogger WithUserId(string userId);
    IContextualLogger WithOperationName(string operationName);
}

public class ContextualLogger : IContextualLogger
{
    private readonly ILogger innerLogger;
    private readonly Dictionary<string, object> contextProperties;
    
    public ContextualLogger(ILogger innerLogger, Dictionary<string, object> contextProperties = null)
    {
        this.innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        this.contextProperties = contextProperties ?? new Dictionary<string, object>();
    }
    
    public IContextualLogger WithContext(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(contextProperties)
        {
            [key] = value
        };
        return new ContextualLogger(innerLogger, newProperties);
    }
    
    public IContextualLogger WithContext(IEnumerable<KeyValuePair<string, object>> properties)
    {
        var newProperties = new Dictionary<string, object>(contextProperties);
        foreach (var prop in properties)
        {
            newProperties[prop.Key] = prop.Value;
        }
        return new ContextualLogger(innerLogger, newProperties);
    }
    
    public IContextualLogger WithCorrelationId(string correlationId) => 
        WithContext("CorrelationId", correlationId);
    
    public IContextualLogger WithUserId(string userId) => 
        WithContext("UserId", userId);
    
    public IContextualLogger WithOperationName(string operationName) => 
        WithContext("OperationName", operationName);
    
    public IDisposable BeginScope<TState>(TState state) => innerLogger.BeginScope(state);
    
    public bool IsEnabled(LogLevel logLevel) => innerLogger.IsEnabled(logLevel);
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        
        // Combine context properties with current log context and state
        var allProperties = new Dictionary<string, object>(contextProperties);
        
        // Add current log context
        foreach (var contextProp in LogContext.Current)
        {
            allProperties[contextProp.Key] = contextProp.Value;
        }
        
        // Add state properties if it's a structured log
        if (state is IEnumerable<KeyValuePair<string, object>> stateProps)
        {
            foreach (var prop in stateProps)
            {
                allProperties[prop.Key] = prop.Value;
            }
        }
        
        innerLogger.Log(logLevel, eventId, allProperties, exception, (props, ex) => 
            formatter(state, ex));
    }
}

// Performance and operation logging
public interface IOperationLogger
{
    IOperationTracker BeginOperation(string operationName, 
        Dictionary<string, object> properties = null);
    Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation, 
        Dictionary<string, object> properties = null);
    T LogOperation<T>(string operationName, Func<T> operation, 
        Dictionary<string, object> properties = null);
    Task LogOperationAsync(string operationName, Func<Task> operation, 
        Dictionary<string, object> properties = null);
    void LogOperation(string operationName, Action operation, 
        Dictionary<string, object> properties = null);
}

public interface IOperationTracker : IDisposable
{
    string OperationId { get; }
    string OperationName { get; }
    TimeSpan Elapsed { get; }
    void SetProperty(string key, object value);
    void SetResult(object result);
    void SetSuccess(bool success = true);
    void AddMetric(string name, double value, string unit = null);
    void LogCheckpoint(string checkpointName);
}

public class OperationLogger : IOperationLogger
{
    private readonly ILogger<OperationLogger> logger;
    private readonly IServiceProvider serviceProvider;
    
    public OperationLogger(ILogger<OperationLogger> logger, IServiceProvider serviceProvider = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider;
    }
    
    public IOperationTracker BeginOperation(string operationName, 
        Dictionary<string, object> properties = null)
    {
        return new OperationTracker(logger, operationName, properties);
    }
    
    public async Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation, 
        Dictionary<string, object> properties = null)
    {
        using var tracker = BeginOperation(operationName, properties);
        
        try
        {
            var result = await operation().ConfigureAwait(false);
            tracker.SetResult(result);
            tracker.SetSuccess(true);
            return result;
        }
        catch (Exception ex)
        {
            tracker.SetSuccess(false);
            tracker.SetProperty("Exception", ex.GetType().Name);
            tracker.SetProperty("ExceptionMessage", ex.Message);
            throw;
        }
    }
    
    public T LogOperation<T>(string operationName, Func<T> operation, 
        Dictionary<string, object> properties = null)
    {
        using var tracker = BeginOperation(operationName, properties);
        
        try
        {
            var result = operation();
            tracker.SetResult(result);
            tracker.SetSuccess(true);
            return result;
        }
        catch (Exception ex)
        {
            tracker.SetSuccess(false);
            tracker.SetProperty("Exception", ex.GetType().Name);
            tracker.SetProperty("ExceptionMessage", ex.Message);
            throw;
        }
    }
    
    public async Task LogOperationAsync(string operationName, Func<Task> operation, 
        Dictionary<string, object> properties = null)
    {
        await LogOperationAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, properties).ConfigureAwait(false);
    }
    
    public void LogOperation(string operationName, Action operation, 
        Dictionary<string, object> properties = null)
    {
        LogOperation(() =>
        {
            operation();
            return Task.CompletedTask;
        }, properties);
    }
}

public class OperationTracker : IOperationTracker
{
    private readonly ILogger logger;
    private readonly Stopwatch stopwatch;
    private readonly Dictionary<string, object> properties;
    private readonly List<OperationCheckpoint> checkpoints;
    private readonly List<OperationMetric> metrics;
    private bool disposed = false;
    
    public string OperationId { get; } = Guid.NewGuid().ToString("N")[..8];
    public string OperationName { get; }
    public TimeSpan Elapsed => stopwatch.Elapsed;
    
    public OperationTracker(ILogger logger, string operationName, 
        Dictionary<string, object> initialProperties = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        
        properties = new Dictionary<string, object>(initialProperties ?? new Dictionary<string, object>())
        {
            ["OperationId"] = OperationId,
            ["OperationName"] = OperationName,
            ["StartTime"] = DateTime.UtcNow
        };
        
        checkpoints = new();
        metrics = new();
        stopwatch = Stopwatch.StartNew();
        
        logger.LogInformation("Operation {OperationName} started with ID {OperationId}", 
            OperationName, OperationId);
    }
    
    public void SetProperty(string key, object value)
    {
        if (disposed) return;
        properties[key] = value;
    }
    
    public void SetResult(object result)
    {
        if (disposed) return;
        
        if (result != null)
        {
            properties["ResultType"] = result.GetType().Name;
            
            // Log result size for collections
            if (result is System.Collections.ICollection collection)
            {
                properties["ResultCount"] = collection.Count;
            }
        }
    }
    
    public void SetSuccess(bool success = true)
    {
        if (disposed) return;
        properties["Success"] = success;
    }
    
    public void AddMetric(string name, double value, string unit = null)
    {
        if (disposed) return;
        
        metrics.Add(new OperationMetric(name, value, unit, stopwatch.Elapsed));
    }
    
    public void LogCheckpoint(string checkpointName)
    {
        if (disposed) return;
        
        var checkpoint = new OperationCheckpoint(checkpointName, stopwatch.Elapsed);
        checkpoints.Add(checkpoint);
        
        logger.LogDebug("Operation {OperationName} checkpoint: {CheckpointName} at {Elapsed:F2}ms",
            OperationName, checkpointName, checkpoint.Elapsed.TotalMilliseconds);
    }
    
    public void Dispose()
    {
        if (disposed) return;
        
        stopwatch.Stop();
        
        properties["EndTime"] = DateTime.UtcNow;
        properties["Duration"] = stopwatch.Elapsed;
        properties["DurationMs"] = stopwatch.Elapsed.TotalMilliseconds;
        
        if (checkpoints.Any())
        {
            properties["Checkpoints"] = checkpoints.Select(c => new { c.Name, ElapsedMs = c.Elapsed.TotalMilliseconds });
        }
        
        if (metrics.Any())
        {
            properties["Metrics"] = metrics.Select(m => new { m.Name, m.Value, m.Unit, ElapsedMs = m.Timestamp.TotalMilliseconds });
        }
        
        var success = properties.TryGetValue("Success", out var successValue) && (bool)successValue;
        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        var message = success 
            ? "Operation {OperationName} completed successfully in {Duration:F2}ms"
            : "Operation {OperationName} failed after {Duration:F2}ms";
        
        logger.Log(logLevel, message, OperationName, stopwatch.Elapsed.TotalMilliseconds);
        
        disposed = true;
    }
}

public record OperationCheckpoint(string Name, TimeSpan Elapsed);
public record OperationMetric(string Name, double Value, string Unit, TimeSpan Timestamp);

// Audit logging for security and compliance
public interface IAuditLogger
{
    Task LogAuditEventAsync(AuditEvent auditEvent);
    void LogAuditEvent(AuditEvent auditEvent);
    Task LogUserActionAsync(string userId, string action, object details = null);
    Task LogDataAccessAsync(string userId, string resourceType, string resourceId, 
        string action, bool success = true);
    Task LogSecurityEventAsync(string eventType, string userId = null, object details = null);
}

public class AuditEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }
    public string ResourceType { get; set; }
    public string ResourceId { get; set; }
    public bool Success { get; set; } = true;
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string CorrelationId { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public string SessionId { get; set; }
    public string TenantId { get; set; }
}

public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> logger;
    private readonly AuditLoggerOptions options;
    
    public AuditLogger(ILogger<AuditLogger> logger, IOptions<AuditLoggerOptions> options = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options?.Value ?? new AuditLoggerOptions();
    }
    
    public Task LogAuditEventAsync(AuditEvent auditEvent)
    {
        LogAuditEvent(auditEvent);
        return Task.CompletedTask;
    }
    
    public void LogAuditEvent(AuditEvent auditEvent)
    {
        if (auditEvent == null) return;
        
        // Enrich audit event with context
        EnrichAuditEvent(auditEvent);
        
        var properties = new Dictionary<string, object>
        {
            ["AuditEventId"] = auditEvent.EventId,
            ["EventType"] = auditEvent.EventType,
            ["UserId"] = auditEvent.UserId,
            ["UserName"] = auditEvent.UserName,
            ["Action"] = auditEvent.Action,
            ["ResourceType"] = auditEvent.ResourceType,
            ["ResourceId"] = auditEvent.ResourceId,
            ["Success"] = auditEvent.Success,
            ["IpAddress"] = auditEvent.IpAddress,
            ["UserAgent"] = auditEvent.UserAgent,
            ["SessionId"] = auditEvent.SessionId,
            ["TenantId"] = auditEvent.TenantId,
            ["Timestamp"] = auditEvent.Timestamp
        };
        
        // Add correlation ID if available
        if (!string.IsNullOrEmpty(auditEvent.CorrelationId))
        {
            properties["CorrelationId"] = auditEvent.CorrelationId;
        }
        
        // Add details
        foreach (var detail in auditEvent.Details)
        {
            properties[$"Detail_{detail.Key}"] = detail.Value;
        }
        
        var logLevel = auditEvent.Success ? LogLevel.Information : LogLevel.Warning;
        var message = "Audit: {EventType} - {Action} on {ResourceType} by {UserId} - {Success}";
        
        using (logger.BeginScope(properties))
        {
            logger.Log(logLevel, message, auditEvent.EventType, auditEvent.Action, 
                auditEvent.ResourceType, auditEvent.UserId, auditEvent.Success ? "Success" : "Failed");
        }
    }
    
    public async Task LogUserActionAsync(string userId, string action, object details = null)
    {
        var auditEvent = new AuditEvent
        {
            EventType = "UserAction",
            UserId = userId,
            Action = action
        };
        
        if (details != null)
        {
            auditEvent.Details = ConvertToStringObjectDictionary(details);
        }
        
        await LogAuditEventAsync(auditEvent);
    }
    
    public async Task LogDataAccessAsync(string userId, string resourceType, string resourceId, 
        string action, bool success = true)
    {
        var auditEvent = new AuditEvent
        {
            EventType = "DataAccess",
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Success = success
        };
        
        await LogAuditEventAsync(auditEvent);
    }
    
    public async Task LogSecurityEventAsync(string eventType, string userId = null, object details = null)
    {
        var auditEvent = new AuditEvent
        {
            EventType = "Security",
            UserId = userId,
            Action = eventType
        };
        
        if (details != null)
        {
            auditEvent.Details = ConvertToStringObjectDictionary(details);
        }
        
        await LogAuditEventAsync(auditEvent);
    }
    
    private void EnrichAuditEvent(AuditEvent auditEvent)
    {
        // Add correlation ID from context if not set
        if (string.IsNullOrEmpty(auditEvent.CorrelationId))
        {
            auditEvent.CorrelationId = LogContext.GetProperty<string>("CorrelationId");
        }
        
        // Add tenant ID from context if not set
        if (string.IsNullOrEmpty(auditEvent.TenantId))
        {
            auditEvent.TenantId = LogContext.GetProperty<string>("TenantId");
        }
        
        // Add session ID from context if not set
        if (string.IsNullOrEmpty(auditEvent.SessionId))
        {
            auditEvent.SessionId = LogContext.GetProperty<string>("SessionId");
        }
    }
    
    private Dictionary<string, object> ConvertToStringObjectDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, object>();
        
        if (obj is Dictionary<string, object> dict) return dict;
        
        // Use reflection to convert object properties to dictionary
        var properties = obj.GetType().GetProperties();
        var result = new();
        
        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                result[prop.Name] = prop.GetValue(obj);
            }
        }
        
        return result;
    }
}

public class AuditLoggerOptions
{
    public bool IncludeUserAgent { get; set; } = true;
    public bool IncludeIpAddress { get; set; } = true;
    public List<string> SensitiveProperties { get; set; } = new List<string> 
    { 
        "Password", "Token", "Secret", "Key", "Authorization" 
    };
}

// Correlation and distributed tracing
public static class CorrelationContext
{
    private static readonly AsyncLocal<string> correlationId = new();
    
    public static string CorrelationId
    {
        get => correlationId.Value ?? GenerateCorrelationId();
        set => correlationId.Value = value;
    }
    
    public static IDisposable BeginCorrelationScope(string correlationId = null)
    {
        var previousId = CorrelationContext.CorrelationId;
        CorrelationContext.CorrelationId = correlationId ?? GenerateCorrelationId();
        
        return new CorrelationScope(() => CorrelationContext.CorrelationId = previousId);
    }
    
    private static string GenerateCorrelationId() => Guid.NewGuid().ToString("N")[..16];
}

internal class CorrelationScope : IDisposable
{
    private readonly Action restoreAction;
    private bool disposed = false;
    
    public CorrelationScope(Action restoreAction)
    {
        this.restoreAction = restoreAction ?? throw new ArgumentNullException(nameof(restoreAction));
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            restoreAction();
            disposed = true;
        }
    }
}

// Metrics integration for observability
public interface ILogMetrics
{
    void IncrementCounter(string name, Dictionary<string, string> tags = null);
    void RecordValue(string name, double value, Dictionary<string, string> tags = null);
    void RecordTiming(string name, TimeSpan duration, Dictionary<string, string> tags = null);
    IDisposable BeginTiming(string name, Dictionary<string, string> tags = null);
}

public class LogMetrics : ILogMetrics
{
    private readonly ConcurrentDictionary<string, MetricCollector> metrics = new();
    private readonly ILogger<LogMetrics> logger;
    
    public LogMetrics(ILogger<LogMetrics> logger = null)
    {
        this.logger = logger;
    }
    
    public void IncrementCounter(string name, Dictionary<string, string> tags = null)
    {
        var key = GetMetricKey(name, tags);
        var collector = metrics.GetOrAdd(key, k => new MetricCollector(name, MetricType.Counter, tags));
        collector.Increment();
        
        LogMetric("Counter", name, 1, tags);
    }
    
    public void RecordValue(string name, double value, Dictionary<string, string> tags = null)
    {
        var key = GetMetricKey(name, tags);
        var collector = metrics.GetOrAdd(key, k => new MetricCollector(name, MetricType.Gauge, tags));
        collector.RecordValue(value);
        
        LogMetric("Gauge", name, value, tags);
    }
    
    public void RecordTiming(string name, TimeSpan duration, Dictionary<string, string> tags = null)
    {
        var key = GetMetricKey(name, tags);
        var collector = metrics.GetOrAdd(key, k => new MetricCollector(name, MetricType.Timer, tags));
        collector.RecordTiming(duration);
        
        LogMetric("Timer", name, duration.TotalMilliseconds, tags);
    }
    
    public IDisposable BeginTiming(string name, Dictionary<string, string> tags = null)
    {
        return new TimingScope(duration => RecordTiming(name, duration, tags));
    }
    
    public IEnumerable<MetricCollector> GetMetrics() => metrics.Values;
    
    public MetricCollector GetMetric(string name, Dictionary<string, string> tags = null)
    {
        var key = GetMetricKey(name, tags);
        return metrics.TryGetValue(key, out var metric) ? metric : null;
    }
    
    private void LogMetric(string type, string name, double value, Dictionary<string, string> tags)
    {
        var properties = new Dictionary<string, object>
        {
            ["MetricType"] = type,
            ["MetricName"] = name,
            ["MetricValue"] = value,
            ["Timestamp"] = DateTime.UtcNow
        };
        
        if (tags?.Any() == true)
        {
            foreach (var tag in tags)
            {
                properties[$"Tag_{tag.Key}"] = tag.Value;
            }
        }
        
        using (logger?.BeginScope(properties))
        {
            logger?.LogDebug("Metric {MetricType}.{MetricName} = {MetricValue}", type, name, value);
        }
    }
    
    private static string GetMetricKey(string name, Dictionary<string, string> tags)
    {
        if (tags?.Any() != true) return name;
        
        var tagString = string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"));
        return $"{name}[{tagString}]";
    }
}

public class MetricCollector
{
    private long count = 0;
    private double sum = 0;
    private double min = double.MaxValue;
    private double max = double.MinValue;
    private readonly object lockObj = new();
    
    public string Name { get; }
    public MetricType Type { get; }
    public Dictionary<string, string> Tags { get; }
    public long Count => count;
    public double Sum => sum;
    public double Average => count > 0 ? sum / count : 0;
    public double Min => count > 0 ? min : 0;
    public double Max => count > 0 ? max : 0;
    public DateTime LastUpdated { get; private set; }
    
    public MetricCollector(string name, MetricType type, Dictionary<string, string> tags = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Tags = tags ?? new Dictionary<string, string>();
        LastUpdated = DateTime.UtcNow;
    }
    
    public void Increment()
    {
        RecordValue(1);
    }
    
    public void RecordValue(double value)
    {
        lock (lockObj)
        {
            Interlocked.Increment(ref count);
            sum += value;
            
            if (value < min) min = value;
            if (value > max) max = value;
            
            LastUpdated = DateTime.UtcNow;
        }
    }
    
    public void RecordTiming(TimeSpan duration)
    {
        RecordValue(duration.TotalMilliseconds);
    }
}

public enum MetricType
{
    Counter,
    Gauge,
    Timer
}

internal class TimingScope : IDisposable
{
    private readonly Action<TimeSpan> recordAction;
    private readonly Stopwatch stopwatch;
    private bool disposed = false;
    
    public TimingScope(Action<TimeSpan> recordAction)
    {
        this.recordAction = recordAction ?? throw new ArgumentNullException(nameof(recordAction));
        stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            stopwatch.Stop();
            recordAction(stopwatch.Elapsed);
            disposed = true;
        }
    }
}

// Log sanitization and security
public interface ILogSanitizer
{
    string Sanitize(string message);
    object SanitizeObject(object obj);
    Dictionary<string, object> SanitizeProperties(Dictionary<string, object> properties);
}

public class LogSanitizer : ILogSanitizer
{
    private readonly HashSet<string> sensitiveKeys;
    private readonly Dictionary<string, Func<object, object>> customSanitizers;
    
    public LogSanitizer(IEnumerable<string> sensitiveKeys = null)
    {
        this.sensitiveKeys = new HashSet<string>(
            sensitiveKeys ?? GetDefaultSensitiveKeys(),
            StringComparer.OrdinalIgnoreCase);
        
        customSanitizers = new Dictionary<string, Func<object, object>>(StringComparer.OrdinalIgnoreCase);
    }
    
    public void AddCustomSanitizer(string key, Func<object, object> sanitizer)
    {
        customSanitizers[key] = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }
    
    public string Sanitize(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        
        // Simple pattern-based sanitization for common sensitive data
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"password\s*[=:]\s*[^\s,}]+", "password=***", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"token\s*[=:]\s*[^\s,}]+", "token=***", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return message;
    }
    
    public object SanitizeObject(object obj)
    {
        if (obj == null) return null;
        
        return obj switch
        {
            string str => Sanitize(str),
            Dictionary<string, object> dict => SanitizeProperties(dict),
            IDictionary<string, object> idict => SanitizeProperties(
                new Dictionary<string, object>(idict)),
            _ when IsComplexType(obj) => SanitizeComplexObject(obj),
            _ => obj
        };
    }
    
    public Dictionary<string, object> SanitizeProperties(Dictionary<string, object> properties)
    {
        if (properties == null) return null;
        
        var sanitized = new();
        
        foreach (var prop in properties)
        {
            if (IsSensitiveKey(prop.Key))
            {
                sanitized[prop.Key] = "***";
            }
            else if (customSanitizers.TryGetValue(prop.Key, out var customSanitizer))
            {
                sanitized[prop.Key] = customSanitizer(prop.Value);
            }
            else
            {
                sanitized[prop.Key] = SanitizeObject(prop.Value);
            }
        }
        
        return sanitized;
    }
    
    private bool IsSensitiveKey(string key)
    {
        return sensitiveKeys.Contains(key) || 
               key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("token", StringComparison.OrdinalIgnoreCase);
    }
    
    private bool IsComplexType(object obj)
    {
        var type = obj.GetType();
        return !type.IsPrimitive && type != typeof(string) && type != typeof(DateTime);
    }
    
    private object SanitizeComplexObject(object obj)
    {
        try
        {
            // Convert to dictionary using reflection for sanitization
            var properties = obj.GetType().GetProperties();
            var dict = new();
            
            foreach (var prop in properties)
            {
                if (prop.CanRead)
                {
                    dict[prop.Name] = prop.GetValue(obj);
                }
            }
            
            return SanitizeProperties(dict);
        }
        catch
        {
            return "***";
        }
    }
    
    private static IEnumerable<string> GetDefaultSensitiveKeys()
    {
        return new[]
        {
            "password", "pwd", "secret", "token", "key", "authorization",
            "credentials", "auth", "bearer", "api_key", "apikey", "access_token",
            "refresh_token", "private_key", "certificate", "ssn", "social_security",
            "credit_card", "card_number", "cvv", "pin"
        };
    }
}

// High-performance logging with minimal allocations
public sealed class HighPerformanceLogger
{
    private readonly ILogger innerLogger;
    private readonly LogSanitizer sanitizer;
    
    public HighPerformanceLogger(ILogger logger, LogSanitizer sanitizer = null)
    {
        innerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.sanitizer = sanitizer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogFast(LogLevel level, string message)
    {
        if (!innerLogger.IsEnabled(level)) return;
        
        innerLogger.Log(level, message);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogFastWithException(LogLevel level, Exception exception, string message)
    {
        if (!innerLogger.IsEnabled(level)) return;
        
        innerLogger.Log(level, exception, message);
    }
    
    public void LogStructured<T1>(LogLevel level, string template, T1 arg1)
    {
        if (!innerLogger.IsEnabled(level)) return;
        
        innerLogger.Log(level, template, SanitizeArg(arg1));
    }
    
    public void LogStructured<T1, T2>(LogLevel level, string template, T1 arg1, T2 arg2)
    {
        if (!innerLogger.IsEnabled(level)) return;
        
        innerLogger.Log(level, template, SanitizeArg(arg1), SanitizeArg(arg2));
    }
    
    public void LogStructured<T1, T2, T3>(LogLevel level, string template, T1 arg1, T2 arg2, T3 arg3)
    {
        if (!innerLogger.IsEnabled(level)) return;
        
        innerLogger.Log(level, template, SanitizeArg(arg1), SanitizeArg(arg2), SanitizeArg(arg3));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object SanitizeArg<T>(T arg)
    {
        return sanitizer?.SanitizeObject(arg) ?? arg;
    }
}

// Dependency injection extensions
public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddAdvancedLogging(this IServiceCollection services, 
        Action<AdvancedLoggingOptions> configureOptions = null)
    {
        var options = new AdvancedLoggingOptions();
        configureOptions?.Invoke(options);
        
        services.Configure<AdvancedLoggingOptions>(opts =>
        {
            opts.EnableContextualLogging = options.EnableContextualLogging;
            opts.EnableOperationLogging = options.EnableOperationLogging;
            opts.EnableAuditLogging = options.EnableAuditLogging;
            opts.EnableMetrics = options.EnableMetrics;
            opts.SensitiveKeys = options.SensitiveKeys;
        });
        
        services.AddScoped<IContextualLogger>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ContextualLogger>>();
            return new ContextualLogger(logger);
        });
        
        services.AddScoped<IOperationLogger, OperationLogger>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddSingleton<ILogMetrics, LogMetrics>();
        services.AddSingleton<ILogSanitizer>(provider =>
        {
            var opts = provider.GetService<IOptions<AdvancedLoggingOptions>>()?.Value ?? new AdvancedLoggingOptions();
            return new LogSanitizer(opts.SensitiveKeys);
        });
        
        return services;
    }
}

public class AdvancedLoggingOptions
{
    public bool EnableContextualLogging { get; set; } = true;
    public bool EnableOperationLogging { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public List<string> SensitiveKeys { get; set; } = new();
}
```

**Usage**:

```csharp
// Example 1: Contextual Logging with Correlation
Console.WriteLine("Contextual Logging Examples:");

// Set up correlation context
using (CorrelationContext.BeginCorrelationScope("REQ-12345"))
{
    // Set log context properties
    using (LogContext.BeginScope("UserId", "user-789"))
    using (LogContext.BeginScope("OperationName", "ProcessOrder"))
    {
        var logger = new ContextualLogger(
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>());
        
        var contextualLogger = logger
            .WithCorrelationId(CorrelationContext.CorrelationId)
            .WithUserId("user-789")
            .WithOperationName("ProcessOrder")
            .WithContext("OrderId", "ORD-456");
        
        contextualLogger.LogInformation("Starting order processing for {OrderId}", "ORD-456");
        contextualLogger.LogWarning("Inventory level low for product {ProductId}", "PROD-123");
        contextualLogger.LogInformation("Order processing completed successfully");
    }
}

// Example 2: Operation Logging with Performance Tracking
Console.WriteLine("\nOperation Logging Examples:");

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var operationLogger = new OperationLogger(loggerFactory.CreateLogger<OperationLogger>());

// Async operation with automatic tracking
var result = await operationLogger.LogOperationAsync("DatabaseQuery", async () =>
{
    // Simulate database operation
    await Task.Delay(150);
    return "Query result data";
}, new Dictionary<string, object> { ["TableName"] = "Orders", ["QueryType"] = "SELECT" });

Console.WriteLine($"Operation result: {result}");

// Manual operation tracking with checkpoints and metrics
using (var tracker = operationLogger.BeginOperation("ComplexOperation"))
{
    tracker.SetProperty("BatchSize", 100);
    
    // First phase
    await Task.Delay(50);
    tracker.LogCheckpoint("DataLoaded");
    tracker.AddMetric("RecordsLoaded", 100, "records");
    
    // Second phase
    await Task.Delay(75);
    tracker.LogCheckpoint("DataProcessed");
    tracker.AddMetric("ProcessingTime", 75, "ms");
    
    // Third phase
    await Task.Delay(25);
    tracker.LogCheckpoint("DataSaved");
    tracker.AddMetric("RecordsSaved", 95, "records");
    
    tracker.SetSuccess(true);
    tracker.SetResult("95 records processed successfully");
}

// Example 3: Audit Logging for Compliance
Console.WriteLine("\nAudit Logging Examples:");

var auditLogger = new AuditLogger(loggerFactory.CreateLogger<AuditLogger>());

// User action audit
await auditLogger.LogUserActionAsync("user-123", "LOGIN", new
{
    IpAddress = "192.168.1.100",
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
    LoginMethod = "OAuth"
});

// Data access audit
await auditLogger.LogDataAccessAsync("user-123", "Customer", "CUST-456", "READ", true);
await auditLogger.LogDataAccessAsync("user-123", "Customer", "CUST-789", "UPDATE", true);

// Security event audit
await auditLogger.LogSecurityEventAsync("FAILED_LOGIN_ATTEMPT", "user-999", new
{
    AttemptCount = 3,
    IpAddress = "192.168.1.200",
    Reason = "Invalid password"
});

// Custom audit event
var customAuditEvent = new AuditEvent
{
    EventType = "BusinessProcess",
    UserId = "user-123",
    Action = "ORDER_APPROVAL",
    ResourceType = "Order",
    ResourceId = "ORD-789",
    Success = true,
    Details = new Dictionary<string, object>
    {
        ["ApprovalAmount"] = 1500.00,
        ["ApproverRole"] = "Manager",
        ["BusinessUnit"] = "Sales"
    }
};

await auditLogger.LogAuditEventAsync(customAuditEvent);

// Example 4: Metrics Integration
Console.WriteLine("\nMetrics Integration Examples:");

var logMetrics = new LogMetrics(loggerFactory.CreateLogger<LogMetrics>());

// Counter metrics
logMetrics.IncrementCounter("http_requests", new Dictionary<string, string>
{
    ["method"] = "GET",
    ["endpoint"] = "/api/orders"
});

logMetrics.IncrementCounter("database_queries", new Dictionary<string, string>
{
    ["table"] = "customers",
    ["operation"] = "select"
});

// Gauge metrics
logMetrics.RecordValue("active_connections", 45);
logMetrics.RecordValue("memory_usage_mb", 512.5);

// Timer metrics with automatic timing
using (logMetrics.BeginTiming("operation_duration", new Dictionary<string, string>
{
    ["operation_type"] = "data_processing"
}))
{
    // Simulate operation
    await Task.Delay(200);
}

// Manual timer recording
var sw = Stopwatch.StartNew();
await Task.Delay(100);
sw.Stop();
logMetrics.RecordTiming("custom_operation", sw.Elapsed, new Dictionary<string, string>
{
    ["component"] = "payment_processor"
});

// Display collected metrics
Console.WriteLine("\nCollected Metrics:");
foreach (var metric in logMetrics.GetMetrics())
{
    Console.WriteLine($"  {metric.Name} ({metric.Type}): Count={metric.Count}, Avg={metric.Average:F2}, Min={metric.Min:F2}, Max={metric.Max:F2}");
}

// Example 5: Log Sanitization and Security
Console.WriteLine("\nLog Sanitization Examples:");

var sanitizer = new LogSanitizer();

// Add custom sanitizer for credit card numbers
sanitizer.AddCustomSanitizer("CreditCard", value =>
{
    if (value is string ccNumber && ccNumber.Length >= 4)
    {
        return $"****-****-****-{ccNumber[^4..]}";
    }
    return "***";
});

// Test data with sensitive information
var testData = new Dictionary<string, object>
{
    ["Username"] = "john.doe@example.com",
    ["Password"] = "secret123",
    ["ApiToken"] = "abc123def456",
    ["CreditCard"] = "1234567890123456",
    ["Amount"] = 99.99,
    ["Description"] = "Payment for order containing password=test123 and token=xyz789"
};

Console.WriteLine("Original data:");
foreach (var item in testData)
{
    Console.WriteLine($"  {item.Key}: {item.Value}");
}

var sanitizedData = sanitizer.SanitizeProperties(testData);
Console.WriteLine("\nSanitized data:");
foreach (var item in sanitizedData)
{
    Console.WriteLine($"  {item.Key}: {item.Value}");
}

// Sanitize message content
var sensitiveMessage = "User login failed for password=secret123 with token=abc456def";
var sanitizedMessage = sanitizer.Sanitize(sensitiveMessage);
Console.WriteLine($"\nOriginal message: {sensitiveMessage}");
Console.WriteLine($"Sanitized message: {sanitizedMessage}");

// Example 6: High-Performance Logging
Console.WriteLine("\nHigh-Performance Logging Examples:");

var highPerfLogger = new HighPerformanceLogger(
    loggerFactory.CreateLogger<HighPerformanceLogger>(),
    sanitizer);

// Fast logging without allocations
highPerfLogger.LogFast(LogLevel.Information, "Fast log message");

// Structured logging with sanitization
highPerfLogger.LogStructured(LogLevel.Information, 
    "Processing order {OrderId} for user {UserId} with amount {Amount}",
    "ORD-123", "user-456", 99.99m);

// Exception logging
try
{
    throw new InvalidOperationException("Test exception");
}
catch (Exception ex)
{
    highPerfLogger.LogFastWithException(LogLevel.Error, ex, "Operation failed");
}

// Example 7: Batch Operation Logging with Correlation
Console.WriteLine("\nBatch Operation Logging Examples:");

var batchOperations = Enumerable.Range(1, 5).Select(i => new
{
    Id = $"ITEM-{i:D3}",
    Action = $"Process item {i}"
});

using (CorrelationContext.BeginCorrelationScope("BATCH-001"))
using (var batchTracker = operationLogger.BeginOperation("BatchProcessing"))
{
    batchTracker.SetProperty("BatchId", "BATCH-001");
    batchTracker.SetProperty("ItemCount", 5);
    
    var processed = 0;
    var failed = 0;
    
    foreach (var item in batchOperations)
    {
        using (LogContext.BeginScope("ItemId", item.Id))
        {
            try
            {
                // Simulate item processing
                await Task.Delay(Random.Shared.Next(50, 150));
                
                if (Random.Shared.NextDouble() < 0.8) // 80% success rate
                {
                    processed++;
                    
                    var itemLogger = new ContextualLogger(loggerFactory.CreateLogger("BatchProcessor"))
                        .WithCorrelationId(CorrelationContext.CorrelationId)
                        .WithContext("ItemId", item.Id);
                    
                    itemLogger.LogInformation("Item {ItemId} processed successfully", item.Id);
                }
                else
                {
                    failed++;
                    throw new InvalidOperationException($"Failed to process item {item.Id}");
                }
            }
            catch (Exception ex)
            {
                var itemLogger = new ContextualLogger(loggerFactory.CreateLogger("BatchProcessor"))
                    .WithCorrelationId(CorrelationContext.CorrelationId)
                    .WithContext("ItemId", item.Id);
                
                itemLogger.LogError(ex, "Item {ItemId} processing failed: {ErrorMessage}", 
                    item.Id, ex.Message);
            }
        }
    }
    
    batchTracker.AddMetric("ProcessedCount", processed, "items");
    batchTracker.AddMetric("FailedCount", failed, "items");
    batchTracker.AddMetric("SuccessRate", (double)processed / (processed + failed) * 100, "percent");
    
    batchTracker.SetSuccess(failed == 0);
    batchTracker.SetResult($"{processed} processed, {failed} failed");
}

// Example 8: Structured Logging with Complex Objects
Console.WriteLine("\nStructured Logging with Complex Objects:");

var order = new
{
    OrderId = "ORD-789",
    CustomerId = "CUST-123",
    Items = new[]
    {
        new { ProductId = "PROD-001", Quantity = 2, Price = 25.99m },
        new { ProductId = "PROD-002", Quantity = 1, Price = 15.50m }
    },
    PaymentInfo = new
    {
        Method = "CreditCard",
        Last4Digits = "1234",
        Token = "secret_payment_token_abc123" // This should be sanitized
    },
    Total = 67.48m
};

using (var orderTracker = operationLogger.BeginOperation("OrderProcessing"))
{
    orderTracker.SetProperty("Order", sanitizer.SanitizeObject(order));
    
    var contextualLogger = new ContextualLogger(loggerFactory.CreateLogger("OrderProcessor"))
        .WithCorrelationId("ORD-CORR-789")
        .WithContext("OrderId", order.OrderId)
        .WithContext("CustomerId", order.CustomerId);
    
    contextualLogger.LogInformation("Processing order {OrderId} for customer {CustomerId} with {ItemCount} items",
        order.OrderId, order.CustomerId, order.Items.Length);
    
    // Simulate processing steps
    await Task.Delay(100);
    orderTracker.LogCheckpoint("InventoryChecked");
    
    await Task.Delay(75);
    orderTracker.LogCheckpoint("PaymentProcessed");
    
    await Task.Delay(50);
    orderTracker.LogCheckpoint("OrderFulfilled");
    
    orderTracker.SetSuccess(true);
    contextualLogger.LogInformation("Order {OrderId} processed successfully", order.OrderId);
}

Console.WriteLine("\nLogging patterns examples completed!");
```

**Notes**:

- Use structured logging with consistent property names across your application
- Implement log context for automatic correlation and enrichment of log entries
- Track operation performance with automatic timing and checkpoint logging
- Maintain audit trails for compliance and security requirements with detailed audit events
- Correlate related log entries across distributed operations using correlation IDs
- Collect metrics alongside logs for comprehensive observability
- Sanitize sensitive data in logs to prevent security breaches and comply with privacy regulations
- Use high-performance logging patterns in hot paths to minimize overhead
- Implement contextual loggers for automatic property injection and correlation
- Set up proper log levels and enable conditional logging to control verbosity
- Consider log aggregation and centralized logging for distributed applications
- Use async logging where possible to avoid blocking application threads

**Prerequisites**:

- Understanding of Microsoft.Extensions.Logging framework and dependency injection
- Knowledge of structured logging concepts and log aggregation systems
- Familiarity with observability patterns and monitoring systems
- Experience with async/await patterns and thread-safe operations
- Understanding of security and privacy requirements for log data
- Knowledge of performance profiling and optimization techniques

**Related Snippets**:

- [Exception Handling](exception-handling.md) - Structured error management and diagnostics
- [Circuit Breaker](circuit-breaker.md) - Fault tolerance patterns with logging integration
- [Polly Patterns](polly-patterns.md) - Resilience library with comprehensive logging
- [Performance Optimization](micro-optimizations.md) - High-performance programming patterns
