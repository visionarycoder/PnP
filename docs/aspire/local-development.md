# Enterprise Local Development with .NET Aspire

**Description**: Advanced enterprise development workflows with .NET Aspire including container orchestration, development environment standardization, automated testing integration, performance profiling, and team collaboration patterns with enterprise tooling and security compliance.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, Docker Desktop, Testcontainers, Dev Containers
**Enterprise Features**: Standardized dev environments, automated testing, performance profiling, security scanning, team collaboration tools, and enterprise authentication integration

**Code**:

## Development Environment Setup

```csharp
namespace DocumentProcessor.Aspire.Development;

// Development-specific App Host configuration
public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Development flags
        var isDevelopment = builder.Environment.IsDevelopment();
        
        if (!isDevelopment)
        {
            throw new InvalidOperationException("This configuration is for development only");
        }

        // Local databases with management tools
        var postgres = builder.AddPostgres("document-db", password: "dev123")
            .WithDataVolume("postgres-data")
            .WithPgAdmin(c => c.WithHostPort(5050)) // Access at http://localhost:5050
            .WithInitBindMount("./database/init"); // Initialize with seed data

        var redis = builder.AddRedis("cache", password: "dev123")
            .WithDataVolume("redis-data")
            .WithRedisCommander(c => c.WithHostPort(8081)); // Access at http://localhost:8081

        // Local storage emulator
        var storage = builder.AddAzureStorage("storage")
            .RunAsEmulator()
            .WithDataVolume("storage-data");

        // ML Model serving (local)
        var mlModel = builder.AddContainer("ml-model-server", "onnxruntime/onnxruntime-server")
            .WithBindMount("./models", "/models")
            .WithHttpEndpoint(port: 8001, targetPort: 8001, name: "inference")
            .WithEnvironment("MODEL_PATH", "/models/document-classifier.onnx");

        // Document processing API
        var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(storage)
            .WithReference(mlModel, "ml-endpoint")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("DocumentProcessing__EnableDebugLogging", "true")
            .WithEnvironment("DocumentProcessing__Storage__UseLocalStorage", "false") // Use Azurite
            .WithEnvironment("DocumentProcessing__ML__UseRemoteModels", "true")
            .WithEnvironment("Orleans__Dashboard__Enabled", "true")
            .WithEnvironment("Orleans__Dashboard__Port", "8080")
            .WithHttpsEndpoint(port: 7001, name: "https")
            .WithHttpEndpoint(port: 5001, name: "http");

        // Orleans Silo (separate for easier debugging)
        var orleansSilo = builder.AddProject<Projects.DocumentProcessorSilo>("orleans-silo")
            .WithReference(postgres)
            .WithReference(redis)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Orleans__Dashboard__Enabled", "true")
            .WithEnvironment("Orleans__Dashboard__Port", "8082")
            .WithHttpEndpoint(port: 8082, name: "dashboard");

        // Background worker
        var backgroundWorker = builder.AddProject<Projects.DocumentProcessorWorker>("background-worker")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(storage)
            .WithReference(orleansSilo)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Worker__EnableDebugMode", "true")
            .WithEnvironment("Worker__ProcessingInterval", "5"); // 5 seconds for development

        // Development tools
        var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
            .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
            .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui") // Jaeger UI
            .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "collector")
            .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
            .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http");

        var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
            .WithBindMount("./monitoring/prometheus.yml", "/etc/prometheus/prometheus.yml")
            .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "ui");

        var grafana = builder.AddContainer("grafana", "grafana/grafana")
            .WithBindMount("./monitoring/grafana", "/etc/grafana/provisioning")
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
            .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "ui");

        // Configure distributed tracing for all services
        ConfigureTracing(documentApi, jaeger);
        ConfigureTracing(orleansSilo, jaeger);
        ConfigureTracing(backgroundWorker, jaeger);

        // Configure metrics for all services  
        ConfigureMetrics(documentApi, prometheus);
        ConfigureMetrics(orleansSilo, prometheus);
        ConfigureMetrics(backgroundWorker, prometheus);

        var app = builder.Build();
        app.Run();
    }

    private static void ConfigureTracing(IResourceBuilder<ProjectResource> project, IResourceBuilder<ContainerResource> jaeger)
    {
        project
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-http"))
            .WithEnvironment("OTEL_serviceName", project.Resource.Name)
            .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", $"service.name={project.Resource.Name},service.version=1.0.0");
    }

    private static void ConfigureMetrics(IResourceBuilder<ProjectResource> project, IResourceBuilder<ContainerResource> prometheus)
    {
        project
            .WithEnvironment("METRICS_ENABLED", "true")
            .WithEnvironment("METRICS_PORT", "9464");
    }
}
```

## Development Service Configuration

```csharp
namespace DocumentProcessor.Api;

public static class DevelopmentExtensions
{
    public static IServiceCollection AddDevelopmentServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        // Enhanced logging for development
        services.AddLogging(builder =>
        {
            builder.AddConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
            
            builder.AddDebug();
            
            // Add structured logging with Serilog in development
            builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug()
                .WriteTo.File("logs/development-.txt", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger());
        });

        // Development-specific HTTP client configuration
        services.AddHttpClient(Options.DefaultName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10); // Longer timeout for debugging
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Trust all certificates
        });

        // Memory diagnostics
        services.AddSingleton<IMemoryDiagnostics, MemoryDiagnostics>();
        services.AddHostedService<MemoryDiagnosticsService>();

        // Development middleware
        services.AddTransient<DevelopmentLoggingMiddleware>();
        services.AddTransient<RequestResponseLoggingMiddleware>();
        services.AddTransient<SlowRequestDetectionMiddleware>();

        // Hot reload support for configuration
        services.AddSingleton<IConfigurationReloader, ConfigurationReloader>();

        // Development APIs
        services.AddScoped<IDevelopmentController, DevelopmentController>();

        return services;
    }

    public static WebApplication UseDevelopmentMiddleware(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseMiddleware<DevelopmentLoggingMiddleware>();
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        app.UseMiddleware<SlowRequestDetectionMiddleware>();

        // Development exception page with enhanced details
        app.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions
        {
            IncludeExceptionDetails = true,
            FileProvider = app.Environment.ContentRootFileProvider,
            SourceCodeLineCount = 20
        });

        return app;
    }
}

// Development-specific middleware
public class DevelopmentLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<DevelopmentLoggingMiddleware> logger;

    public DevelopmentLoggingMiddleware(RequestDelegate next, ILogger<DevelopmentLoggingMiddleware> logger)
    {
        next = next;
        logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestPath"] = requestPath,
            ["RequestMethod"] = requestMethod,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        });

        logger.LogInformation("Processing request {Method} {Path}", requestMethod, requestPath);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next(context);
            
            stopwatch.Stop();
            
            logger.LogInformation("Request completed {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
                requestMethod, requestPath, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogError(ex, "Request failed {Method} {Path} after {ElapsedMs}ms",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestResponseLoggingMiddleware> logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        next = next;
        logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log request details
        if (logger.IsEnabled(LogLevel.Debug))
        {
            var request = await FormatRequestAsync(context.Request);
            logger.LogDebug("Request Details: {RequestDetails}", request);
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await next(context);

        // Log response details
        if (logger.IsEnabled(LogLevel.Debug))
        {
            var response = await FormatResponseAsync(context.Response, responseBodyStream);
            logger.LogDebug("Response Details: {ResponseDetails}", response);
        }

        // Copy response back to original stream
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
    }

    private async Task<string> FormatRequestAsync(HttpRequest request)
    {
        var bodyAsText = "";
        if (request.ContentLength > 0)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            bodyAsText = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        return $"Method: {request.Method}, Path: {request.Path}, Query: {request.QueryString}, Body: {bodyAsText}";
    }

    private async Task<string> FormatResponseAsync(HttpResponse response, MemoryStream responseBodyStream)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
        var bodyAsText = await reader.ReadToEndAsync();
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        return $"StatusCode: {response.StatusCode}, Body: {bodyAsText}";
    }
}

public class SlowRequestDetectionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<SlowRequestDetectionMiddleware> logger;
    private readonly TimeSpan slowRequestThreshold = TimeSpan.FromSeconds(5);

    public SlowRequestDetectionMiddleware(RequestDelegate next, ILogger<SlowRequestDetectionMiddleware> logger)
    {
        next = next;
        logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();
        
        if (stopwatch.Elapsed > slowRequestThreshold)
        {
            logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## Development Dashboard Integration

```csharp
namespace DocumentProcessor.Api.Controllers;

[ApiController]
[Route("api/dev")]
[Conditional("DEBUG")] // Only available in debug builds
public class DevelopmentController : ControllerBase
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly IMemoryDiagnostics memoryDiagnostics;
    private readonly ILogger<DevelopmentController> logger;

    public DevelopmentController(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IMemoryDiagnostics memoryDiagnostics,
        ILogger<DevelopmentController> logger)
    {
        serviceProvider = serviceProvider;
        configuration = configuration;
        memoryDiagnostics = memoryDiagnostics;
        logger = logger;
    }

    [HttpGet("info")]
    public ActionResult<DevelopmentInfo> GetDevelopmentInfo()
    {
        var info = new DevelopmentInfo
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingDirectory = Environment.CurrentDirectory,
            RuntimeVersion = Environment.Version.ToString(),
            StartTime = Process.GetCurrentProcess().StartTime,
            Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
            MemoryUsage = memoryDiagnostics.GetCurrentUsage(),
            ConfigurationSources = GetConfigurationSources(),
            RegisteredServices = GetRegisteredServices()
        };

        return Ok(info);
    }

    [HttpGet("config")]
    public ActionResult<Dictionary<string, object>> GetConfiguration()
    {
        var config = new Dictionary<string, object>();
        
        foreach (var kvp in configuration.AsEnumerable())
        {
            if (!string.IsNullOrEmpty(kvp.Value) && !IsSensitiveKey(kvp.Key))
            {
                config[kvp.Key] = kvp.Value;
            }
        }

        return Ok(config);
    }

    [HttpPost("config/reload")]
    public async Task<ActionResult> ReloadConfiguration()
    {
        try
        {
            var reloader = serviceProvider.GetService<IConfigurationReloader>();
            if (reloader != null)
            {
                await reloader.ReloadAsync();
                return Ok(new { Message = "Configuration reloaded successfully" });
            }
            
            return BadRequest(new { Message = "Configuration reloader not available" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload configuration");
            return StatusCode(500, new { Message = "Failed to reload configuration", Error = ex.Message });
        }
    }

    [HttpGet("memory")]
    public ActionResult<MemoryDiagnosticsReport> GetMemoryDiagnostics()
    {
        var report = memoryDiagnostics.GenerateReport();
        return Ok(report);
    }

    [HttpPost("gc")]
    public ActionResult ForceGarbageCollection()
    {
        var beforeMemory = GC.GetTotalMemory(false);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false);
        var freedMemory = beforeMemory - afterMemory;

        return Ok(new
        {
            BeforeMemoryBytes = beforeMemory,
            AfterMemoryBytes = afterMemory,
            FreedMemoryBytes = freedMemory,
            FreedMemoryMB = freedMemory / (1024.0 * 1024.0)
        });
    }

    [HttpGet("health")]
    public async Task<ActionResult<HealthReport>> GetHealthReport()
    {
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        if (healthCheckService == null)
        {
            return BadRequest(new { Message = "Health check service not configured" });
        }

        var report = await healthCheckService.CheckHealthAsync();
        return Ok(new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Status = kvp.Value.Status.ToString(),
                    Duration = kvp.Value.Duration,
                    Description = kvp.Value.Description,
                    Data = kvp.Value.Data
                })
        });
    }

    [HttpPost("simulate-error")]
    public ActionResult SimulateError([FromQuery] string type = "exception")
    {
        return type.ToLowerInvariant() switch
        {
            "exception" => throw new InvalidOperationException("Simulated exception for testing"),
            "timeout" => SimulateTimeout(),
            "memory" => SimulateMemoryPressure(),
            "deadlock" => SimulateDeadlock(),
            _ => BadRequest(new { Message = "Unknown error type" })
        };
    }

    private ActionResult SimulateTimeout()
    {
        Thread.Sleep(TimeSpan.FromSeconds(30));
        return Ok(new { Message = "Timeout simulation completed" });
    }

    private ActionResult SimulateMemoryPressure()
    {
        var memoryHog = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            memoryHog.Add(new byte[10 * 1024 * 1024]); // 10MB chunks
        }
        
        GC.KeepAlive(memoryHog);
        return Ok(new { Message = "Memory pressure simulation completed" });
    }

    private ActionResult SimulateDeadlock()
    {
        var lock1 = new object();
        var lock2 = new object();
        
        var task1 = Task.Run(() =>
        {
            lock (lock1)
            {
                Thread.Sleep(100);
                lock (lock2) { }
            }
        });
        
        var task2 = Task.Run(() =>
        {
            lock (lock2)
            {
                Thread.Sleep(100);
                lock (lock1) { }
            }
        });

        try
        {
            Task.WaitAll([task1, task2], TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Expected timeout
        }

        return Ok(new { Message = "Deadlock simulation completed" });
    }

    private List<string> GetConfigurationSources()
    {
        if (configuration is ConfigurationRoot root)
        {
            return root.Providers
                .Select(p => p.GetType().Name)
                .ToList();
        }
        
        return new List<string>();
    }

    private List<string> GetRegisteredServices()
    {
        if (serviceProvider is IServiceCollection services)
        {
            return services
                .Select(s => $"{s.ServiceType.Name} -> {s.ImplementationType?.Name ?? s.ImplementationFactory?.Method.Name ?? "Unknown"}")
                .Take(50) // Limit for readability
                .ToList();
        }
        
        return new List<string> { "Service collection not accessible" };
    }

    private static bool IsSensitiveKey(string key)
    {
        var sensitivePatterns = new[]
        {
            "password", "secret", "key", "token", "connectionstring"
        };
        
        return sensitivePatterns.Any(pattern => 
            key.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

public record DevelopmentInfo
{
    public string Environment { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public string WorkingDirectory { get; init; } = string.Empty;
    public string RuntimeVersion { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public TimeSpan Uptime { get; init; }
    public MemoryUsage MemoryUsage { get; init; } = new();
    public List<string> ConfigurationSources { get; init; } = new();
    public List<string> RegisteredServices { get; init; } = new();
}
```

## Memory Diagnostics

```csharp
namespace DocumentProcessor.Aspire.Diagnostics;

public interface IMemoryDiagnostics
{
    MemoryUsage GetCurrentUsage();
    MemoryDiagnosticsReport GenerateReport();
    Task<MemoryTrendAnalysis> AnalyzeTrendsAsync(TimeSpan period);
}

public class MemoryDiagnostics : IMemoryDiagnostics
{
    private readonly ILogger<MemoryDiagnostics> logger;
    private readonly ConcurrentQueue<MemorySnapshot> snapshots = new();
    private readonly Timer collectionTimer;

    public MemoryDiagnostics(ILogger<MemoryDiagnostics> logger)
    {
        logger = logger;
        collectionTimer = new Timer(CollectSnapshot, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public MemoryUsage GetCurrentUsage()
    {
        var process = Process.GetCurrentProcess();
        
        return new MemoryUsage
        {
            WorkingSetBytes = process.WorkingSet64,
            PrivateMemoryBytes = process.PrivateMemorySize64,
            VirtualMemoryBytes = process.VirtualMemorySize64,
            ManagedMemoryBytes = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            Timestamp = DateTime.UtcNow
        };
    }

    public MemoryDiagnosticsReport GenerateReport()
    {
        var currentUsage = GetCurrentUsage();
        var recentSnapshots = GetRecentSnapshots(TimeSpan.FromHours(1));
        
        var report = new MemoryDiagnosticsReport
        {
            CurrentUsage = currentUsage,
            PeakWorkingSet = recentSnapshots.Any() ? recentSnapshots.Max(s => s.Usage.WorkingSetBytes) : currentUsage.WorkingSetBytes,
            AverageWorkingSet = recentSnapshots.Any() ? (long)recentSnapshots.Average(s => s.Usage.WorkingSetBytes) : currentUsage.WorkingSetBytes,
            RecentGCActivity = AnalyzeGCActivity(recentSnapshots),
            MemoryTrend = AnalyzeMemoryTrend(recentSnapshots),
            Recommendations = GenerateRecommendations(currentUsage, recentSnapshots)
        };

        return report;
    }

    public async Task<MemoryTrendAnalysis> AnalyzeTrendsAsync(TimeSpan period)
    {
        await Task.CompletedTask; // Placeholder for async analysis
        
        var snapshots = GetRecentSnapshots(period);
        
        if (snapshots.Count < 2)
        {
            return new MemoryTrendAnalysis
            {
                Period = period,
                DataPoints = snapshots.Count,
                Trend = "Insufficient data",
                GrowthRate = 0,
                Recommendations = new List<string> { "Collect more data points for trend analysis" }
            };
        }

        var workingSetValues = snapshots.Select(s => (double)s.Usage.WorkingSetBytes).ToArray();
        var trend = CalculateLinearTrend(workingSetValues);
        
        return new MemoryTrendAnalysis
        {
            Period = period,
            DataPoints = snapshots.Count,
            Trend = trend > 0 ? "Increasing" : trend < 0 ? "Decreasing" : "Stable",
            GrowthRate = trend,
            Recommendations = GenerateTrendRecommendations(trend, snapshots)
        };
    }

    private void CollectSnapshot(object? state)
    {
        try
        {
            var usage = GetCurrentUsage();
            var snapshot = new MemorySnapshot(usage, DateTime.UtcNow);
            
            snapshots.Enqueue(snapshot);
            
            // Keep only recent snapshots (last 24 hours)
            while (snapshots.Count > 1440 && snapshots.TryDequeue(out _)) { }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect memory snapshot");
        }
    }

    private List<MemorySnapshot> GetRecentSnapshots(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        return snapshots
            .Where(s => s.Timestamp >= cutoff)
            .OrderBy(s => s.Timestamp)
            .ToList();
    }

    private GCActivity AnalyzeGCActivity(List<MemorySnapshot> snapshots)
    {
        if (snapshots.Count < 2)
        {
            return new GCActivity();
        }

        var first = snapshots.First();
        var last = snapshots.Last();
        
        return new GCActivity
        {
            Gen0CollectionsDelta = last.Usage.Gen0Collections - first.Usage.Gen0Collections,
            Gen1CollectionsDelta = last.Usage.Gen1Collections - first.Usage.Gen1Collections,
            Gen2CollectionsDelta = last.Usage.Gen2Collections - first.Usage.Gen2Collections,
            AverageCollectionsPerMinute = snapshots.Count > 1 
                ? (last.Usage.Gen0Collections - first.Usage.Gen0Collections) / (double)(last.Timestamp - first.Timestamp).TotalMinutes
                : 0
        };
    }

    private string AnalyzeMemoryTrend(List<MemorySnapshot> snapshots)
    {
        if (snapshots.Count < 3)
        {
            return "Insufficient data";
        }

        var workingSetValues = snapshots.Select(s => (double)s.Usage.WorkingSetBytes).ToArray();
        var trend = CalculateLinearTrend(workingSetValues);
        
        return trend switch
        {
            > 1024 * 1024 => "Increasing (potential memory leak)",
            > 0 => "Slightly increasing",
            < -1024 * 1024 => "Decreasing significantly", 
            < 0 => "Slightly decreasing",
            _ => "Stable"
        };
    }

    private double CalculateLinearTrend(double[] values)
    {
        if (values.Length < 2) return 0;
        
        var n = values.Length;
        var sumX = n * (n - 1) / 2.0;
        var sumY = values.Sum();
        var sumXY = values.Select((y, x) => x * y).Sum();
        var sumXX = Enumerable.Range(0, n).Select(x => x * x).Sum();
        
        return (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
    }

    private List<string> GenerateRecommendations(MemoryUsage currentUsage, List<MemorySnapshot> recentSnapshots)
    {
        var recommendations = new List<string>();
        
        // Check for high memory usage
        if (currentUsage.WorkingSetBytes > 1024 * 1024 * 1024) // 1GB
        {
            recommendations.Add("High memory usage detected. Consider profiling for memory leaks.");
        }
        
        // Check for frequent GC
        var gcActivity = AnalyzeGCActivity(recentSnapshots);
        if (gcActivity.AverageCollectionsPerMinute > 10)
        {
            recommendations.Add("Frequent garbage collection detected. Consider reducing object allocations.");
        }
        
        // Check for Gen2 collections
        if (gcActivity.Gen2CollectionsDelta > 0)
        {
            recommendations.Add("Gen2 garbage collections occurred. Consider reviewing large object usage.");
        }
        
        return recommendations;
    }

    private List<string> GenerateTrendRecommendations(double trend, List<MemorySnapshot> snapshots)
    {
        var recommendations = new List<string>();
        
        if (trend > 1024 * 1024) // Growing by more than 1MB per snapshot
        {
            recommendations.Add("Memory usage is increasing rapidly. Investigate for memory leaks.");
            recommendations.Add("Consider implementing memory profiling and monitoring.");
        }
        else if (trend > 0)
        {
            recommendations.Add("Memory usage is gradually increasing. Monitor for potential issues.");
        }
        
        return recommendations;
    }

    public void Dispose()
    {
        collectionTimer?.Dispose();
    }
}

// Data models
public record MemoryUsage
{
    public long WorkingSetBytes { get; init; }
    public long PrivateMemoryBytes { get; init; }
    public long VirtualMemoryBytes { get; init; }
    public long ManagedMemoryBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTime Timestamp { get; init; }
}

public record MemorySnapshot(MemoryUsage Usage, DateTime Timestamp);

public record MemoryDiagnosticsReport
{
    public MemoryUsage CurrentUsage { get; init; } = new();
    public long PeakWorkingSet { get; init; }
    public long AverageWorkingSet { get; init; }
    public GCActivity RecentGCActivity { get; init; } = new();
    public string MemoryTrend { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();
}

public record GCActivity
{
    public int Gen0CollectionsDelta { get; init; }
    public int Gen1CollectionsDelta { get; init; }
    public int Gen2CollectionsDelta { get; init; }
    public double AverageCollectionsPerMinute { get; init; }
}

public record MemoryTrendAnalysis
{
    public TimeSpan Period { get; init; }
    public int DataPoints { get; init; }
    public string Trend { get; init; } = string.Empty;
    public double GrowthRate { get; init; }
    public List<string> Recommendations { get; init; } = new();
}

// Background service for memory monitoring
public class MemoryDiagnosticsService : BackgroundService
{
    private readonly IMemoryDiagnostics memoryDiagnostics;
    private readonly ILogger<MemoryDiagnosticsService> logger;
    private readonly TimeSpan monitoringInterval = TimeSpan.FromMinutes(5);

    public MemoryDiagnosticsService(
        IMemoryDiagnostics memoryDiagnostics,
        ILogger<MemoryDiagnosticsService> logger)
    {
        memoryDiagnostics = memoryDiagnostics;
        logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(monitoringInterval);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var report = memoryDiagnostics.GenerateReport();
                
                if (report.Recommendations.Any())
                {
                    logger.LogWarning("Memory diagnostics recommendations: {Recommendations}",
                        string.Join("; ", report.Recommendations));
                }
                
                logger.LogInformation("Memory usage: Working Set {WorkingSetMB}MB, Managed {ManagedMB}MB",
                    report.CurrentUsage.WorkingSetBytes / (1024.0 * 1024.0),
                    report.CurrentUsage.ManagedMemoryBytes / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during memory diagnostics monitoring");
            }
        }
    }
}
```

**Usage**:

### Development Dashboard Access

```bash
# Start the Aspire application
dotnet run --project src/DocumentProcessor.AppHost

# Access development resources:
# - Aspire Dashboard: https://localhost:15000 (auto-opens)
# - Document API: https://localhost:7001
# - Orleans Dashboard: http://localhost:8080  
# - PostgreSQL Admin: http://localhost:5050
# - Redis Commander: http://localhost:8081
# - Jaeger Tracing: http://localhost:16686
# - Prometheus: http://localhost:9090
# - Grafana: http://localhost:3000

# Development API endpoints
curl https://localhost:7001/api/dev/info
curl https://localhost:7001/api/dev/config
curl https://localhost:7001/api/dev/memory
curl https://localhost:7001/api/dev/health
```

### Debugging Workflow

```csharp
// 1. Set breakpoints in Visual Studio/VS Code
// 2. Launch with debugging (F5)
// 3. Use development endpoints for testing
// 4. Monitor logs and metrics in real-time
// 5. Use hot reload for rapid iteration

// Example: Test document processing with debugging
var client = new HttpClient { BaseAddress = new Uri("https://localhost:7001") };

// Upload test document
var content = new MultipartFormDataContent();
content.Add(new StringContent("Test document content"), "content");
content.Add(new StringContent("text/plain"), "contentType");

var response = await client.PostAsync("/api/documents/process", content);
// Breakpoint will hit in DocumentController.ProcessDocument method
```

### Configuration Hot Reload

```json
// Save changes to appsettings.Development.json during development
{
  "DocumentProcessing": {
    "MaxConcurrentDocuments": 16, // Changed from 8
    "EnableDebugLogging": true
  }
}

// Configuration automatically reloads without application restart
// Use /api/dev/config/reload to force reload if needed
```

**Notes**:

- **Rich Development Experience**: Full dashboard integration with multiple monitoring tools
- **Hot Reload Support**: Configuration and code changes apply without restart
- **Comprehensive Logging**: Structured logging with request/response details
- **Memory Monitoring**: Real-time memory diagnostics and leak detection
- **Error Simulation**: Built-in tools for testing error handling scenarios
- **Service Discovery**: Easy access to all development resources through Aspire dashboard

**Related Patterns**:

- [Service Orchestration](service-orchestration.md) - Orchestrating services in development
- [Configuration Management](configuration-management.md) - Development configuration patterns
- [Health Monitoring](health-monitoring.md) - Development health checks
- [Orleans Integration](orleans-integration.md) - Orleans dashboard integration
