# Logging Utilities

**Description**: Enterprise structured logging and observability utilities with performance optimization and comprehensive telemetry
**Language/Technology**: C# .NET 9.0 / Logging & Observability
**Prerequisites**: Microsoft.Extensions.Logging, System.Diagnostics, Microsoft.Extensions.Telemetry

## Structured Logging Manager

**Code**:

```csharp
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// High-performance structured logging manager with telemetry integration
/// </summary>
public interface IStructuredLogger<T>
{
    void LogOperation<TResult>(string operationName, Func<TResult> operation, LogLevel logLevel = LogLevel.Information);
    Task LogOperationAsync<TResult>(string operationName, Func<Task<TResult>> operation, LogLevel logLevel = LogLevel.Information, CancellationToken cancellationToken = default);
    void LogWithContext(LogLevel logLevel, string message, object? context = null, Exception? exception = null);
    IDisposable BeginScope(string scopeName, object? properties = null);
    void LogPerformanceMetric(string metricName, TimeSpan duration, object? metadata = null);
}

public class StructuredLogger<T>(ILogger<T> logger, IServiceProvider serviceProvider) : IStructuredLogger<T>
{
    private static readonly ActivitySource activitySource = new(typeof(T).FullName ?? typeof(T).Name);

    public void LogOperation<TResult>(string operationName, Func<TResult> operation, LogLevel logLevel = LogLevel.Information)
    {
        using var activity = activitySource.StartActivity(operationName);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Log(logLevel, "Starting operation: {OperationName}", operationName);
            
            var result = operation();
            stopwatch.Stop();
            
            activity?.SetTag("operation.success", true);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            logger.Log(logLevel, "Operation completed successfully: {OperationName} in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
                
            LogPerformanceMetric(operationName, stopwatch.Elapsed, new { Success = true });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            activity?.SetTag("operation.success", false);
            activity?.SetTag("operation.error", ex.Message);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogError(ex, "Operation failed: {OperationName} after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
                
            LogPerformanceMetric(operationName, stopwatch.Elapsed, new { Success = false, Error = ex.GetType().Name });
            
            throw;
        }
    }

    public async Task LogOperationAsync<TResult>(string operationName, Func<Task<TResult>> operation, 
        LogLevel logLevel = LogLevel.Information, CancellationToken cancellationToken = default)
    {
        using var activity = activitySource.StartActivity(operationName);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Log(logLevel, "Starting async operation: {OperationName}", operationName);
            
            var result = await operation().ConfigureAwait(false);
            stopwatch.Stop();
            
            activity?.SetTag("operation.success", true);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            logger.Log(logLevel, "Async operation completed successfully: {OperationName} in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
                
            LogPerformanceMetric(operationName, stopwatch.Elapsed, new { Success = true, IsAsync = true });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            activity?.SetTag("operation.success", false);
            activity?.SetTag("operation.error", ex.Message);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogError(ex, "Async operation failed: {OperationName} after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
                
            LogPerformanceMetric(operationName, stopwatch.Elapsed, 
                new { Success = false, Error = ex.GetType().Name, IsAsync = true });
            
            throw;
        }
    }

    public void LogWithContext(LogLevel logLevel, string message, object? context = null, Exception? exception = null)
    {
        using var scope = context != null ? logger.BeginScope(context) : null;
        
        if (exception != null)
        {
            logger.Log(logLevel, exception, message);
        }
        else
        {
            logger.Log(logLevel, message);
        }
    }

    public IDisposable BeginScope(string scopeName, object? properties = null)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["ScopeName"] = scopeName,
            ["ScopeId"] = Guid.NewGuid().ToString("N")[..8],
            ["Timestamp"] = DateTimeOffset.UtcNow
        };

        if (properties != null)
        {
            var propertiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(properties)) ?? new Dictionary<string, object>();
            
            foreach (var (key, value) in propertiesDict)
            {
                scopeData[key] = value;
            }
        }

        return logger.BeginScope(scopeData);
    }

    public void LogPerformanceMetric(string metricName, TimeSpan duration, object? metadata = null)
    {
        var metricData = new
        {
            MetricName = metricName,
            DurationMs = duration.TotalMilliseconds,
            DurationTicks = duration.Ticks,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = metadata
        };

        logger.LogInformation("Performance metric: {MetricName} took {Duration}ms", 
            metricName, duration.TotalMilliseconds);

        // Send to telemetry system if available
        var telemetryCollector = serviceProvider.GetService<ITelemetryCollector>();
        telemetryCollector?.RecordMetric(metricName, duration, metadata);
    }
}
```

## Telemetry and Metrics Collection

**Code**:

```csharp
/// <summary>
/// Enterprise telemetry collection with multiple backends
/// </summary>
public interface ITelemetryCollector
{
    void RecordMetric(string name, TimeSpan duration, object? metadata = null);
    void RecordCounter(string name, long value = 1, IReadOnlyDictionary<string, string>? tags = null);
    void RecordGauge(string name, double value, IReadOnlyDictionary<string, string>? tags = null);
    Task FlushAsync(CancellationToken cancellationToken = default);
}

public class TelemetryCollector(ILogger<TelemetryCollector> logger) : ITelemetryCollector, IDisposable
{
    private readonly ConcurrentQueue<MetricEntry> metricQueue = new();
    private readonly Timer flushTimer = new Timer(FlushMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    private readonly SemaphoreSlim flushSemaphore = new(1, 1);

    public void RecordMetric(string name, TimeSpan duration, object? metadata = null)
    {
        var entry = new MetricEntry
        {
            Name = name,
            Type = MetricType.Duration,
            Value = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            Tags = ExtractTags(metadata),
            Metadata = metadata
        };

        metricQueue.Enqueue(entry);
    }

    public void RecordCounter(string name, long value = 1, IReadOnlyDictionary<string, string>? tags = null)
    {
        var entry = new MetricEntry
        {
            Name = name,
            Type = MetricType.Counter,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Tags = tags ?? ImmutableDictionary<string, string>.Empty
        };

        metricQueue.Enqueue(entry);
    }

    public void RecordGauge(string name, double value, IReadOnlyDictionary<string, string>? tags = null)
    {
        var entry = new MetricEntry
        {
            Name = name,
            Type = MetricType.Gauge,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Tags = tags ?? ImmutableDictionary<string, string>.Empty
        };

        metricQueue.Enqueue(entry);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await flushSemaphore.WaitAsync(cancellationToken);
        try
        {
            await FlushMetricsInternal(cancellationToken);
        }
        finally
        {
            flushSemaphore.Release();
        }
    }

    private static void FlushMetrics(object? state)
    {
        if (state is TelemetryCollector collector)
        {
            _ = collector.FlushAsync();
        }
    }

    private async Task FlushMetricsInternal(CancellationToken cancellationToken)
    {
        var metrics = new List<MetricEntry>();
        
        while (metricQueue.TryDequeue(out var metric))
        {
            metrics.Add(metric);
        }

        if (metrics.Count == 0)
        {
            return;
        }

        try
        {
            // Send to multiple backends (Application Insights, Prometheus, etc.)
            await SendToApplicationInsights(metrics, cancellationToken);
            await SendToPrometheus(metrics, cancellationToken);
            
            logger.LogDebug("Flushed {MetricCount} metrics to telemetry backends", metrics.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to flush metrics to telemetry backends");
            
            // Re-queue metrics for retry
            foreach (var metric in metrics)
            {
                metricQueue.Enqueue(metric);
            }
        }
    }

    private IReadOnlyDictionary<string, string> ExtractTags(object? metadata)
    {
        if (metadata == null)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        try
        {
            var json = JsonSerializer.Serialize(metadata);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            return dict?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty
            ).ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;
        }
        catch
        {
            return ImmutableDictionary<string, string>.Empty;
        }
    }

    private async Task SendToApplicationInsights(List<MetricEntry> metrics, CancellationToken cancellationToken)
    {
        // Implementation for Application Insights telemetry
        await Task.Delay(1, cancellationToken);
    }

    private async Task SendToPrometheus(List<MetricEntry> metrics, CancellationToken cancellationToken)
    {
        // Implementation for Prometheus metrics
        await Task.Delay(1, cancellationToken);
    }

    public void Dispose()
    {
        flushTimer?.Dispose();
        _ = FlushAsync();
        flushSemaphore?.Dispose();
    }
}

public record MetricEntry
{
    public required string Name { get; init; }
    public required MetricType Type { get; init; }
    public required double Value { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyDictionary<string, string> Tags { get; init; }
    public object? Metadata { get; init; }
}

public enum MetricType
{
    Counter,
    Gauge,
    Duration,
    Histogram
}
```

## Diagnostic and Health Monitoring

**Code**:

```csharp
/// <summary>
/// Enterprise diagnostic utilities with health monitoring
/// </summary>
public interface IDiagnosticCollector
{
    Task<HealthCheckResult> CheckHealthAsync(string componentName, CancellationToken cancellationToken = default);
    void RecordDiagnosticEvent(string eventName, object? data = null, DiagnosticSeverity severity = DiagnosticSeverity.Info);
    Task<DiagnosticReport> GenerateReportAsync(CancellationToken cancellationToken = default);
}

public class DiagnosticCollector(
    ILogger<DiagnosticCollector> logger,
    ITelemetryCollector telemetryCollector) : IDiagnosticCollector
{
    private readonly ConcurrentQueue<DiagnosticEvent> diagnosticEvents = new();
    
    public async Task<HealthCheckResult> CheckHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var health = await PerformHealthCheck(componentName, cancellationToken);
            stopwatch.Stop();
            
            telemetryCollector.RecordMetric($"health_check.{componentName}", stopwatch.Elapsed);
            telemetryCollector.RecordGauge($"health_status.{componentName}", health.IsHealthy ? 1.0 : 0.0);
            
            logger.LogInformation("Health check for {Component}: {Status} in {Duration}ms", 
                componentName, health.Status, stopwatch.ElapsedMilliseconds);
            
            return health;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogError(ex, "Health check failed for {Component} after {Duration}ms", 
                componentName, stopwatch.ElapsedMilliseconds);
            
            return new HealthCheckResult
            {
                ComponentName = componentName,
                IsHealthy = false,
                Status = "Unhealthy",
                Message = ex.Message,
                CheckedAt = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed,
                Details = new Dictionary<string, object>
                {
                    ["Exception"] = ex.GetType().Name,
                    ["ErrorMessage"] = ex.Message
                }
            };
        }
    }

    public void RecordDiagnosticEvent(string eventName, object? data = null, DiagnosticSeverity severity = DiagnosticSeverity.Info)
    {
        var diagnosticEvent = new DiagnosticEvent
        {
            EventName = eventName,
            Severity = severity,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data,
            ThreadId = Environment.CurrentManagedThreadId,
            ActivityId = Activity.Current?.Id
        };

        diagnosticEvents.Enqueue(diagnosticEvent);
        
        var logLevel = severity switch
        {
            DiagnosticSeverity.Error => LogLevel.Error,
            DiagnosticSeverity.Warning => LogLevel.Warning,
            DiagnosticSeverity.Info => LogLevel.Information,
            DiagnosticSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, "Diagnostic event: {EventName} [{Severity}]", eventName, severity);
        
        // Record metrics based on severity
        telemetryCollector.RecordCounter($"diagnostic_events.{severity.ToString().ToLowerInvariant()}");
    }

    public async Task<DiagnosticReport> GenerateReportAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<DiagnosticEvent>();
        while (diagnosticEvents.TryDequeue(out var evt))
        {
            events.Add(evt);
        }

        var report = new DiagnosticReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            EventCount = events.Count,
            Events = events.ToImmutableList(),
            Summary = GenerateEventSummary(events),
            SystemInfo = await CollectSystemInfoAsync(cancellationToken)
        };

        logger.LogInformation("Generated diagnostic report with {EventCount} events", events.Count);
        
        return report;
    }

    private async Task<HealthCheckResult> PerformHealthCheck(string componentName, CancellationToken cancellationToken)
    {
        // Component-specific health checks
        return componentName.ToLowerInvariant() switch
        {
            "database" => await CheckDatabaseHealth(cancellationToken),
            "cache" => await CheckCacheHealth(cancellationToken),
            "external_api" => await CheckExternalApiHealth(cancellationToken),
            "disk_space" => CheckDiskSpaceHealth(),
            "memory" => CheckMemoryHealth(),
            _ => throw new ArgumentException($"Unknown component: {componentName}")
        };
    }

    private async Task<HealthCheckResult> CheckDatabaseHealth(CancellationToken cancellationToken)
    {
        // Database connectivity and performance check
        await Task.Delay(10, cancellationToken); // Simulate check
        
        return new HealthCheckResult
        {
            ComponentName = "database",
            IsHealthy = true,
            Status = "Healthy",
            Message = "Database connection successful",
            CheckedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(10),
            Details = new Dictionary<string, object>
            {
                ["ConnectionPoolSize"] = 50,
                ["ActiveConnections"] = 12,
                ["ResponseTimeMs"] = 8.5
            }
        };
    }

    private async Task<HealthCheckResult> CheckCacheHealth(CancellationToken cancellationToken)
    {
        // Cache connectivity and performance check
        await Task.Delay(5, cancellationToken);
        
        return new HealthCheckResult
        {
            ComponentName = "cache",
            IsHealthy = true,
            Status = "Healthy",
            Message = "Cache is responsive",
            CheckedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(5),
            Details = new Dictionary<string, object>
            {
                ["HitRatio"] = 0.95,
                ["MemoryUsage"] = "250MB",
                ["KeyCount"] = 15420
            }
        };
    }

    private async Task<HealthCheckResult> CheckExternalApiHealth(CancellationToken cancellationToken)
    {
        // External API connectivity check
        await Task.Delay(50, cancellationToken);
        
        return new HealthCheckResult
        {
            ComponentName = "external_api",
            IsHealthy = true,
            Status = "Healthy",
            Message = "External API is accessible",
            CheckedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(50),
            Details = new Dictionary<string, object>
            {
                ["Endpoint"] = "https://api.example.com/health",
                ["StatusCode"] = 200,
                ["ResponseTimeMs"] = 45
            }
        };
    }

    private HealthCheckResult CheckDiskSpaceHealth()
    {
        // Disk space check
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
        var isHealthy = drives.All(d => d.AvailableFreeSpace > d.TotalSize * 0.1); // 10% free space minimum
        
        return new HealthCheckResult
        {
            ComponentName = "disk_space",
            IsHealthy = isHealthy,
            Status = isHealthy ? "Healthy" : "Warning",
            Message = isHealthy ? "Sufficient disk space available" : "Low disk space detected",
            CheckedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(1),
            Details = drives.ToDictionary(
                d => d.Name,
                d => (object)new
                {
                    TotalSize = d.TotalSize,
                    AvailableSpace = d.AvailableFreeSpace,
                    UsagePercentage = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100
                }
            )
        };
    }

    private HealthCheckResult CheckMemoryHealth()
    {
        // Memory usage check
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;
        var gcMemory = GC.GetTotalMemory(false);
        
        var isHealthy = workingSet < 1_000_000_000; // 1GB threshold
        
        return new HealthCheckResult
        {
            ComponentName = "memory",
            IsHealthy = isHealthy,
            Status = isHealthy ? "Healthy" : "Warning",
            Message = isHealthy ? "Memory usage within normal limits" : "High memory usage detected",
            CheckedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(1),
            Details = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = workingSet / 1024 / 1024,
                ["GCMemoryMB"] = gcMemory / 1024 / 1024,
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            }
        };
    }

    private Dictionary<string, int> GenerateEventSummary(List<DiagnosticEvent> events)
    {
        return events
            .GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
    }

    private async Task<Dictionary<string, object>> CollectSystemInfoAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        
        return new Dictionary<string, object>
        {
            ["MachineName"] = Environment.MachineName,
            ["OSVersion"] = Environment.OSVersion.ToString(),
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["CLRVersion"] = Environment.Version.ToString(),
            ["WorkingSet"] = Environment.WorkingSet,
            ["TickCount"] = Environment.TickCount64,
            ["ApplicationUptime"] = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
        };
    }
}

public record HealthCheckResult
{
    public required string ComponentName { get; init; }
    public required bool IsHealthy { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset CheckedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public Dictionary<string, object>? Details { get; init; }
}

public record DiagnosticEvent
{
    public required string EventName { get; init; }
    public required DiagnosticSeverity Severity { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public object? Data { get; init; }
    public int ThreadId { get; init; }
    public string? ActivityId { get; init; }
}

public record DiagnosticReport
{
    public required DateTimeOffset GeneratedAt { get; init; }
    public required int EventCount { get; init; }
    public required IReadOnlyList<DiagnosticEvent> Events { get; init; }
    public required Dictionary<string, int> Summary { get; init; }
    public required Dictionary<string, object> SystemInfo { get; init; }
}

public enum DiagnosticSeverity
{
    Debug,
    Info,
    Warning,
    Error
}

log_debug() {
    if [[ $LogLevel -ge $LogLevelDebug ]]; then
        log "DEBUG" "$1"
    fi
}

# Log rotation utility
rotate_log() {
    local logFile="$1"
    local max_size="${2:-10M}"
    local keep_files="${3:-5}"
    
    if [[ -f "$LogFile" ]]; then
        local size=$(stat -c%s "$LogFile")
        local max_bytes=$(numfmt --from=iec "$max_size")
        
        if [[ $size -gt $max_bytes ]]; then
            # Rotate existing logs
            for ((i=keep_files-1; i>0; i--)); do
                if [[ -f "${logFile}.$i" ]]; then
                    mv "${logFile}.$i" "${logFile}.$((i+1))"
                fi
            done
            
            # Move current log to .1
            mv "$LogFile" "${logFile}.1"
            echo "Log rotated: $LogFile"
        fi
    fi
}
```

```python
# Python logging utilities
import logging
import logging.handlers
import json
import sys
from datetime import datetime
```

**Usage**:

```csharp
// Program.cs - DI registration
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(typeof(IStructuredLogger<>), typeof(StructuredLogger<>));
builder.Services.AddSingleton<ITelemetryCollector, TelemetryCollector>();
builder.Services.AddSingleton<IDiagnosticCollector, DiagnosticCollector>();

var app = builder.Build();

// Service usage examples
public class UserService(IStructuredLogger<UserService> logger)
{
    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        return await logger.LogOperationAsync(
            "CreateUser", 
            async () =>
            {
                using var scope = logger.BeginScope("UserCreation", new { UserId = request.Email, RequestId = Guid.NewGuid() });
                
                logger.LogWithContext(LogLevel.Information, "Validating user creation request", new { Email = request.Email });
                
                // Business logic here
                var user = await ProcessUserCreation(request, cancellationToken);
                
                logger.LogPerformanceMetric("user_creation_db_insert", TimeSpan.FromMilliseconds(45));
                
                return Result<User>.Success(user);
            },
            LogLevel.Information,
            cancellationToken
        );
    }

    public void HandleUserEvent(UserEvent userEvent)
    {
        logger.LogOperation("HandleUserEvent", () =>
        {
            logger.LogWithContext(LogLevel.Information, "Processing user event", new 
            {
                EventType = userEvent.Type,
                UserId = userEvent.UserId,
                EventId = userEvent.Id
            });

            // Process event
            ProcessEvent(userEvent);
            
            return Unit.Value;
        });
    }
}

// Health monitoring service
public class HealthMonitoringService(IDiagnosticCollector diagnostics) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform health checks
                var databaseHealth = await diagnostics.CheckHealthAsync("database", stoppingToken);
                var cacheHealth = await diagnostics.CheckHealthAsync("cache", stoppingToken);
                var memoryHealth = await diagnostics.CheckHealthAsync("memory", stoppingToken);
                var diskHealth = await diagnostics.CheckHealthAsync("disk_space", stoppingToken);

                // Record diagnostic events based on health status
                if (!databaseHealth.IsHealthy)
                {
                    diagnostics.RecordDiagnosticEvent("database_unhealthy", databaseHealth, DiagnosticSeverity.Error);
                }

                if (!cacheHealth.IsHealthy)
                {
                    diagnostics.RecordDiagnosticEvent("cache_unhealthy", cacheHealth, DiagnosticSeverity.Warning);
                }

                // Generate periodic diagnostic reports
                if (DateTime.UtcNow.Minute == 0) // Every hour
                {
                    var report = await diagnostics.GenerateReportAsync(stoppingToken);
                    await ProcessDiagnosticReport(report, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                diagnostics.RecordDiagnosticEvent("health_check_failure", new { Exception = ex.Message }, DiagnosticSeverity.Error);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

// Telemetry usage in controllers
[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    IStructuredLogger<OrdersController> logger,
    ITelemetryCollector telemetry) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        return await logger.LogOperationAsync("CreateOrder", async () =>
        {
            using var scope = logger.BeginScope("OrderCreation", new 
            { 
                OrderId = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                ItemCount = request.Items.Count
            });

            // Record business metrics
            telemetry.RecordCounter("orders.created");
            telemetry.RecordGauge("order.item_count", request.Items.Count);

            var result = await ProcessOrder(request);
            
            telemetry.RecordCounter("orders.processed", tags: new Dictionary<string, string>
            {
                ["status"] = "success",
                ["customer_type"] = request.CustomerType
            });

            return Ok(result);
        });
    }
}

// Custom middleware for request logging
public class RequestLoggingMiddleware(RequestDelegate next, IStructuredLogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await logger.LogOperationAsync("ProcessRequest", async () =>
        {
            using var scope = logger.BeginScope("HttpRequest", new
            {
                RequestId = context.TraceIdentifier,
                Method = context.Request.Method,
                Path = context.Request.Path,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIp = context.Connection.RemoteIpAddress?.ToString()
            });

            logger.LogWithContext(LogLevel.Information, "Processing HTTP request");

            await next(context);

            logger.LogWithContext(LogLevel.Information, "Completed HTTP request", new
            {
                StatusCode = context.Response.StatusCode,
                ResponseTime = "calculated_by_structured_logger"
            });

            return Unit.Value;
        });
    }
}
// Node.js logging utilities
const fs = require('fs');
const path = require('path');
const util = require('util');

class Logger {
    constructor(name, options = {}) {
        this.name = name;
        this.level = options.level || 'INFO';
        this.outputs = options.outputs || ['console'];
        this.logFile = options.logFile;
        this.jsonFormat = options.jsonFormat || false;
        this.colors = options.colors !== false;
        
        this.levels = {
            DEBUG: 0,
            INFO: 1,
            WARN: 2,
            ERROR: 3,
            FATAL: 4
        };
        
        this.colors_map = {
            DEBUG: '\x1b[36m',   // Cyan
            INFO: '\x1b[32m',    // Green
            WARN: '\x1b[33m',    // Yellow
            ERROR: '\x1b[31m',   // Red
            FATAL: '\x1b[35m',   // Magenta
            RESET: '\x1b[0m'     // Reset
        };
        
        // Create log directory if using file output
        if (this.logFile) {
            const logDir = path.dirname(this.logFile);
            if (!fs.existsSync(logDir)) {
                fs.mkdirSync(logDir, { recursive: true });
            }
        }
    }
    
    shouldLog(level) {
        return this.levels[level] >= this.levels[this.level];
    }
    
    formatMessage(level, message, extra = {}) {
        const timestamp = new Date().toISOString();
        
        if (this.jsonFormat) {
            return JSON.stringify({
                timestamp,
                level,
                logger: this.name,
                message,
                ...extra
            });
        } else {
            const color = this.colors ? this.colors_map[level] : '';
            const reset = this.colors ? this.colors_map.RESET : '';
            return `${timestamp} [${color}${level}${reset}] ${this.name}: ${message}`;
        }
    }
    
    log(level, message, extra = {}) {
        if (!this.shouldLog(level)) return;
        
        const formattedMessage = this.formatMessage(level, message, extra);
        
        // Console output
        if (this.outputs.includes('console')) {
            console.log(formattedMessage);
        }
        
        // File output
        if (this.outputs.includes('file') && this.logFile) {
            fs.appendFileSync(this.logFile, formattedMessage + '\n');
        }
    }
    
    debug(message, extra = {}) {
        this.log('DEBUG', message, extra);
    }
    
    info(message, extra = {}) {
        this.log('INFO', message, extra);
    }
    
    warn(message, extra = {}) {
        this.log('WARN', message, extra);
    }
    
    error(message, extra = {}) {
        this.log('ERROR', message, extra);
    }
    
    fatal(message, extra = {}) {
        this.log('FATAL', message, extra);
    }
    
    // Log rotation
    rotateLog(maxSize = 10 * 1024 * 1024, keepFiles = 5) {
        if (!this.logFile || !fs.existsSync(this.logFile)) return;
        
        const stats = fs.statSync(this.logFile);
        if (stats.size > maxSize) {
            // Rotate existing logs
            for (let i = keepFiles - 1; i > 0; i--) {
                const oldFile = `${this.logFile}.${i}`;
                const newFile = `${this.logFile}.${i + 1}`;
                
                if (fs.existsSync(oldFile)) {
                    fs.renameSync(oldFile, newFile);
                }
            }
            
            // Move current log to .1
            fs.renameSync(this.logFile, `${this.logFile}.1`);
            console.log(`Log rotated: ${this.logFile}`);
        }
    }
}

// Request logging middleware (Express.js)
function requestLoggingMiddleware(logger) {
    return (req, res, next) => {
        const startTime = Date.now();
        const originalEnd = res.end;
        
        res.end = function(...args) {
            const duration = Date.now() - startTime;
            
            logger.info('HTTP Request', {
                method: req.method,
                url: req.url,
                status: res.statusCode,
                duration: `${duration}ms`,
                userAgent: req.get('User-Agent'),
                ip: req.ip || req.connection.remoteAddress
            });
            
            originalEnd.apply(this, args);
        };
        
        next();
    };
}

// Performance logging decorator
function logPerformance(logger, operation) {
    return function(target, propertyKey, descriptor) {
        const originalMethod = descriptor.value;
        
        descriptor.value = async function(...args) {
            const startTime = Date.now();
            const opName = operation || `${target.constructor.name}.${propertyKey}`;
            
            try {
                logger.info(`Starting ${opName}`);
                const result = await originalMethod.apply(this, args);
                const duration = Date.now() - startTime;
                logger.info(`Completed ${opName}`, { duration: `${duration}ms` });
                return result;
            } catch (error) {
                const duration = Date.now() - startTime;
                logger.error(`Failed ${opName}`, { 
                    duration: `${duration}ms`,
                    error: error.message
                });
                throw error;
            }
        };
        
        return descriptor;
    };
}
```

## Log Analysis Utilities

**Code**:

```bash
# Log analysis functions
analyze_logs() {
    local logFile="$1"
    local time_range="${2:-1h}"
    
    echo "Log Analysis Report for: $LogFile"
    echo "Time range: Last $time_range"
    echo "=================================="
    
    # Total log lines
    echo "Total entries: $(wc -l < "$LogFile")"
    
    # Log level distribution
    echo -e "\nLog level distribution:"
    grep -oE '\[(DEBUG|INFO|WARN|ERROR|FATAL)\]' "$LogFile" | sort | uniq -c | sort -nr
    
    # Error analysis
    echo -e "\nRecent errors:"
    grep -E '\[(ERROR|FATAL)\]' "$LogFile" | tail -10
    
    # Top error messages
    echo -e "\nTop error patterns:"
    grep -E '\[(ERROR|FATAL)\]' "$LogFile" | \
        sed 's/.*\] //' | sort | uniq -c | sort -nr | head -5
}

extract_slow_operations() {
    local logFile="$1"
    local threshold_ms="${2:-1000}"
    
    echo "Operations slower than ${threshold_ms}ms:"
    grep -E "duration.*[0-9]+ms" "$LogFile" | \
        awk -v threshold="$threshold_ms" '
        match($0, /duration[^0-9]*([0-9]+)ms/, arr) {
            if (arr[1] > threshold) print $0
        }' | sort -k1,1
}

monitor_log_growth() {
    local logFile="$1"
    local interval="${2:-60}"
    
    while true; do
        if [[ -f "$LogFile" ]]; then
            local size=$(stat -c%s "$LogFile")
            local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
            echo "[$timestamp] Log size: $(numfmt --to=iec "$size")"
        fi
        sleep "$interval"
    done
}
```

**Usage**:

```bash
# Bash logging usage
logLevel=3 logFile="app.log" 

log_info "Application started"
log_error "Database connection failed"
log_debug "Processing user request" 

# Log rotation
rotate_log "app.log" "10M" 5

# Log analysis
analyze_logs "app.log" "24h"
extract_slow_operations "app.log" 500
```

```python
# Python logging usage
# Basic logger setup
logger = Logger('myapp', level='DEBUG')
logger.add_console_handler(colored=True)
logger.add_file_handler('logs/app.log', json_format=True)

logger.info("Application started", version="1.0.0")
logger.error("Database error", error_code=500, table="users")

# Context logging
with RequestLogger(logger, "user_registration", user_id="12345"):
    # Registration logic here
    pass

# Performance logging decorator
@log_performance(logger, "data_processing")
def process_data(data):
    # Processing logic
    return processed_data
```

```javascript
// Node.js logging usage
const logger = new Logger('myapp', {
    level: 'DEBUG',
    outputs: ['console', 'file'],
    logFile: 'logs/app.log',
    jsonFormat: true
});

logger.info('Application started', { version: '1.0.0' });
logger.error('Database error', { errorCode: 500, table: 'users' });

// Express middleware
app.use(requestLoggingMiddleware(logger));

// Log rotation (call periodically)
setInterval(() => logger.rotateLog(), 24 * 60 * 60 * 1000); // Daily
```

**Notes**:

- **Performance**: Asynchronous logging for high-throughput applications
- **Security**: Sanitize log messages to prevent log injection
- **Storage**: Consider log aggregation services for production
- **Format**: Use structured logging (JSON) for better parsing
- **Rotation**: Implement log rotation to manage disk space
- **Levels**: Use appropriate log levels to control verbosity
- **Context**: Include relevant context information in logs
- **Monitoring**: Set up alerts for error patterns and log volume

## Related Snippets

- [Error Handling](../csharp/exception-handling.md) - Exception logging patterns
- [Configuration Helpers](configuration-helpers.md) - Logger configuration
- [System Administration](../bash/system-admin.md) - Log monitoring
