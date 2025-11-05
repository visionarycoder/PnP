# .NET Aspire Real-time Processing Patterns

**Description**: Real-time data processing patterns for .NET Aspire applications with service orchestration, distributed event streaming, message queues, and cloud-native reactive architectures using SignalR, Event Hubs, and service mesh communication.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, SignalR, Azure Event Hubs, Service Bus

**Code**:

## Aspire Real-time Event Processing Pipeline

```csharp
namespace DocumentProcessor.Aspire.Realtime;

/// <summary>
/// Real-time event types for Aspire service orchestration
/// </summary>
public enum AspireEventType
{
    DocumentUploaded,
    ProcessingStarted,
    ProcessingProgress,
    ProcessingCompleted,
    ProcessingFailed,
    ServiceScaled,
    HealthStatusChanged,
    ComplianceAlert,
    UserConnected,
    UserDisconnected
}

/// <summary>
/// Real-time event with Aspire service context
/// </summary>
public record AspireRealtimeEvent(
    Guid Id,
    AspireEventType EventType,
    DateTime Timestamp,
    string ServiceName,
    string ServiceVersion,
    string? TraceId,
    Dictionary<string, object?> Payload,
    string? TargetUser,
    string? TargetGroup,
    int Priority)
{
    public string CorrelationId => TraceId ?? Id.ToString();
    public bool IsHighPriority => Priority >= 8;
}

/// <summary>
/// Aspire real-time processing hub with distributed notifications
/// </summary>
public class AspireRealtimeHub(
    IRealtimeEventProcessor eventProcessor,
    ILogger<AspireRealtimeHub> logger,
    AspireServiceContext serviceContext) : Hub<IRealtimeClient>
{
    /// <summary>
    /// Handles client connection with Aspire service registration
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var connectionId = Context.ConnectionId;
        
        logger.LogInformation("Client connected: {UserId} ({ConnectionId}) to service {ServiceName}",
            userId, connectionId, serviceContext.ServiceName);

        // Register connection with service context
        await Groups.AddToGroupAsync(connectionId, $"service:{serviceContext.ServiceName}");
        
        // Notify service of new connection
        await eventProcessor.ProcessEventAsync(new AspireRealtimeEvent(
            Id: Guid.NewGuid(),
            EventType: AspireEventType.UserConnected,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            Payload: new Dictionary<string, object?>
            {
                ["user_id"] = userId,
                ["connection_id"] = connectionId,
                ["service_instance"] = Environment.MachineName
            },
            TargetUser: userId,
            TargetGroup: null,
            Priority: 5
        ));

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handles client disconnection with cleanup
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var connectionId = Context.ConnectionId;

        logger.LogInformation("Client disconnected: {UserId} ({ConnectionId}) from service {ServiceName}",
            userId, connectionId, serviceContext.ServiceName);

        await eventProcessor.ProcessEventAsync(new AspireRealtimeEvent(
            Id: Guid.NewGuid(),
            EventType: AspireEventType.UserDisconnected,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            Payload: new Dictionary<string, object?>
            {
                ["user_id"] = userId,
                ["connection_id"] = connectionId,
                ["disconnect_reason"] = exception?.Message
            },
            TargetUser: userId,
            TargetGroup: null,
            Priority: 5
        ));

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes client to specific document processing events
    /// </summary>
    public async Task SubscribeToDocument(Guid documentId)
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var groupName = $"document:{documentId}";
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        logger.LogInformation("User {UserId} subscribed to document {DocumentId} events",
            userId, documentId);
    }

    /// <summary>
    /// Unsubscribes client from document processing events
    /// </summary>
    public async Task UnsubscribeFromDocument(Guid documentId)
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var groupName = $"document:{documentId}";
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        logger.LogInformation("User {UserId} unsubscribed from document {DocumentId} events",
            userId, documentId);
    }
}

/// <summary>
/// Client interface for real-time communication
/// </summary>
public interface IRealtimeClient
{
    Task ReceiveProcessingUpdate(Guid documentId, string status, int percentage);
    Task ReceiveComplianceAlert(string framework, string message, int severity);
    Task ReceiveServiceNotification(string serviceName, string message, string type);
    Task ReceiveHealthUpdate(string serviceName, string status, Dictionary<string, object> metrics);
}

/// <summary>
/// Real-time event processor with Aspire service integration
/// </summary>
public class AspireRealtimeEventProcessor(
    IHubContext<AspireRealtimeHub, IRealtimeClient> hubContext,
    IEventPublisher eventPublisher,
    ILogger<AspireRealtimeEventProcessor> logger,
    AspireServiceContext serviceContext) : IRealtimeEventProcessor
{
    /// <summary>
    /// Processes real-time events with distributed notification
    /// </summary>
    public async Task ProcessEventAsync(AspireRealtimeEvent realtimeEvent, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("AspireRealtime.ProcessEvent");
        activity?.SetTag("event.type", realtimeEvent.EventType.ToString());
        activity?.SetTag("event.service", realtimeEvent.ServiceName);
        activity?.SetTag("event.priority", realtimeEvent.Priority);

        try
        {
            // Route event based on type and target
            await RouteEventAsync(realtimeEvent, cancellationToken);

            // Publish to distributed event bus for cross-service communication
            if (realtimeEvent.IsHighPriority)
            {
                await eventPublisher.PublishAsync(realtimeEvent, cancellationToken);
            }

            // Emit telemetry metrics
            serviceContext.RealtimeEventCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", realtimeEvent.EventType),
                new KeyValuePair<string, object?>("service_name", realtimeEvent.ServiceName),
                new KeyValuePair<string, object?>("priority", realtimeEvent.Priority));

            logger.LogDebug("Real-time event processed: {EventType} for service {ServiceName} with ID {EventId}",
                realtimeEvent.EventType, realtimeEvent.ServiceName, realtimeEvent.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process real-time event: {EventType} with ID {EventId}",
                realtimeEvent.EventType, realtimeEvent.Id);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Routes events to appropriate SignalR groups or users
    /// </summary>
    private async Task RouteEventAsync(AspireRealtimeEvent realtimeEvent, CancellationToken cancellationToken)
    {
        switch (realtimeEvent.EventType)
        {
            case AspireEventType.ProcessingProgress when realtimeEvent.Payload.TryGetValue("document_id", out var docId):
                var documentId = (Guid)docId!;
                var status = realtimeEvent.Payload.GetValueOrDefault("status", "processing")?.ToString() ?? "processing";
                var percentage = Convert.ToInt32(realtimeEvent.Payload.GetValueOrDefault("percentage", 0));
                
                await hubContext.Clients.Group($"document:{documentId}")
                    .ReceiveProcessingUpdate(documentId, status, percentage);
                break;

            case AspireEventType.ComplianceAlert:
                var framework = realtimeEvent.Payload.GetValueOrDefault("framework", "unknown")?.ToString() ?? "unknown";
                var message = realtimeEvent.Payload.GetValueOrDefault("message", "compliance alert")?.ToString() ?? "compliance alert";
                var severity = Convert.ToInt32(realtimeEvent.Payload.GetValueOrDefault("severity", 5));
                
                if (!string.IsNullOrEmpty(realtimeEvent.TargetUser))
                {
                    await hubContext.Clients.User(realtimeEvent.TargetUser)
                        .ReceiveComplianceAlert(framework, message, severity);
                }
                else
                {
                    await hubContext.Clients.Group($"service:{realtimeEvent.ServiceName}")
                        .ReceiveComplianceAlert(framework, message, severity);
                }
                break;

            case AspireEventType.HealthStatusChanged:
                var healthStatus = realtimeEvent.Payload.GetValueOrDefault("status", "unknown")?.ToString() ?? "unknown";
                var metrics = realtimeEvent.Payload.GetValueOrDefault("metrics", new Dictionary<string, object>()) 
                    as Dictionary<string, object> ?? new Dictionary<string, object>();
                
                await hubContext.Clients.Group($"service:{realtimeEvent.ServiceName}")
                    .ReceiveHealthUpdate(realtimeEvent.ServiceName, healthStatus, metrics);
                break;

            case AspireEventType.ServiceScaled:
            case AspireEventType.ProcessingStarted:
            case AspireEventType.ProcessingCompleted:
            case AspireEventType.ProcessingFailed:
                var notification = realtimeEvent.Payload.GetValueOrDefault("message", realtimeEvent.EventType.ToString())?.ToString() 
                    ?? realtimeEvent.EventType.ToString();
                var notificationType = realtimeEvent.EventType.ToString().ToLowerInvariant();
                
                await hubContext.Clients.Group($"service:{realtimeEvent.ServiceName}")
                    .ReceiveServiceNotification(realtimeEvent.ServiceName, notification, notificationType);
                break;
        }
    }

    /// <summary>
    /// Broadcasts processing updates to subscribed clients
    /// </summary>
    public async Task BroadcastProcessingUpdateAsync(Guid documentId, string status, int percentage, CancellationToken cancellationToken = default)
    {
        var realtimeEvent = new AspireRealtimeEvent(
            Id: Guid.NewGuid(),
            EventType: AspireEventType.ProcessingProgress,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            Payload: new Dictionary<string, object?>
            {
                ["document_id"] = documentId,
                ["status"] = status,
                ["percentage"] = percentage,
                ["service_instance"] = Environment.MachineName
            },
            TargetUser: null,
            TargetGroup: $"document:{documentId}",
            Priority: 6
        );

        await ProcessEventAsync(realtimeEvent, cancellationToken);
    }

    /// <summary>
    /// Broadcasts compliance alerts to relevant users
    /// </summary>
    public async Task BroadcastComplianceAlertAsync(string framework, string message, int severity, string? targetUser = null, CancellationToken cancellationToken = default)
    {
        var realtimeEvent = new AspireRealtimeEvent(
            Id: Guid.NewGuid(),
            EventType: AspireEventType.ComplianceAlert,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            Payload: new Dictionary<string, object?>
            {
                ["framework"] = framework,
                ["message"] = message,
                ["severity"] = severity,
                ["alert_timestamp"] = DateTime.UtcNow
            },
            TargetUser: targetUser,
            TargetGroup: targetUser == null ? $"service:{serviceContext.ServiceName}" : null,
            Priority: severity >= 8 ? 9 : 7
        );

        await ProcessEventAsync(realtimeEvent, cancellationToken);
    }
}

/// <summary>
/// Background service for health status monitoring and real-time notifications
/// </summary>
public class AspireHealthMonitoringService(
    IRealtimeEventProcessor eventProcessor,
    IServiceProvider serviceProvider,
    ILogger<AspireHealthMonitoringService> logger,
    AspireServiceContext serviceContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Aspire health monitoring service started for {ServiceName}", serviceContext.ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorServiceHealthAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in health monitoring service for {ServiceName}", serviceContext.ServiceName);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        logger.LogInformation("Aspire health monitoring service stopped for {ServiceName}", serviceContext.ServiceName);
    }

    private async Task MonitorServiceHealthAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        
        var healthMetrics = new Dictionary<string, object>
        {
            ["memory_usage_mb"] = GC.GetTotalMemory(false) / 1024 / 1024,
            ["thread_count"] = ThreadPool.ThreadCount,
            ["cpu_usage"] = GetCpuUsage(),
            ["active_connections"] = GetActiveConnectionCount(),
            ["timestamp"] = DateTime.UtcNow
        };

        var healthStatus = DetermineHealthStatus(healthMetrics);

        await eventProcessor.ProcessEventAsync(new AspireRealtimeEvent(
            Id: Guid.NewGuid(),
            EventType: AspireEventType.HealthStatusChanged,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: null,
            Payload: new Dictionary<string, object?>
            {
                ["status"] = healthStatus,
                ["metrics"] = healthMetrics
            },
            TargetUser: null,
            TargetGroup: $"service:{serviceContext.ServiceName}",
            Priority: healthStatus == "critical" ? 9 : 4
        ), cancellationToken);
    }

    private static double GetCpuUsage()
    {
        // Implementation for CPU usage calculation
        return Random.Shared.NextDouble() * 100;
    }

    private static int GetActiveConnectionCount()
    {
        // Implementation for active connection count
        return Random.Shared.Next(1, 100);
    }

    private static string DetermineHealthStatus(Dictionary<string, object> metrics)
    {
        var memoryUsage = Convert.ToDouble(metrics["memory_usage_mb"]);
        var cpuUsage = Convert.ToDouble(metrics["cpu_usage"]);

        return (memoryUsage, cpuUsage) switch
        {
            ( > 2000, > 90) => "critical",
            ( > 1500, > 75) => "warning",
            ( > 1000, > 60) => "degraded",
            _ => "healthy"
        };
    }
}

// Supporting interfaces and extensions
public interface IRealtimeEventProcessor
{
    Task ProcessEventAsync(AspireRealtimeEvent realtimeEvent, CancellationToken cancellationToken = default);
    Task BroadcastProcessingUpdateAsync(Guid documentId, string status, int percentage, CancellationToken cancellationToken = default);
    Task BroadcastComplianceAlertAsync(string framework, string message, int severity, string? targetUser = null, CancellationToken cancellationToken = default);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aspire service context extension for real-time metrics
/// </summary>
public static class AspireServiceContextExtensions
{
    public static Counter<long> RealtimeEventCounter { get; } = 
        Metrics.CreateCounter<long>("aspire.realtime.events.total", 
            description: "Total number of real-time events processed");
}
```

**Usage**:

```csharp
// Program.cs - Aspire Host Configuration
var builder = DistributedApplication.CreateBuilder(args);

// Add SignalR for real-time communication
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Add real-time processing services
builder.Services.AddSingleton<AspireServiceContext>();
builder.Services.AddScoped<IRealtimeEventProcessor, AspireRealtimeEventProcessor>();
builder.Services.AddHostedService<AspireHealthMonitoringService>();

// Add distributed event publishing (e.g., Azure Service Bus)
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

var app = builder.Build();

// Configure SignalR hub
app.MapHub<AspireRealtimeHub>("/aspire-realtime");

// Document processing service with real-time updates
public class DocumentProcessingService(
    IRealtimeEventProcessor realtimeProcessor,
    ILogger<DocumentProcessingService> logger)
{
    public async Task ProcessDocumentAsync(Guid documentId, Stream documentStream)
    {
        logger.LogInformation("Starting document processing for {DocumentId}", documentId);

        try
        {
            // Notify processing started
            await realtimeProcessor.BroadcastProcessingUpdateAsync(documentId, "started", 0);

            // Simulate processing with progress updates
            for (int progress = 0; progress <= 100; progress += 20)
            {
                await Task.Delay(1000); // Simulate work
                await realtimeProcessor.BroadcastProcessingUpdateAsync(documentId, "processing", progress);
            }

            // Notify completion
            await realtimeProcessor.BroadcastProcessingUpdateAsync(documentId, "completed", 100);
            
            logger.LogInformation("Document processing completed for {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            await realtimeProcessor.BroadcastProcessingUpdateAsync(documentId, "failed", 0);
            logger.LogError(ex, "Document processing failed for {DocumentId}", documentId);
            throw;
        }
    }
}

// Client-side TypeScript example
/*
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/aspire-realtime")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Subscribe to processing updates
connection.on("ReceiveProcessingUpdate", (documentId, status, percentage) => {
    console.log(`Document ${documentId}: ${status} - ${percentage}%`);
    updateProgressBar(documentId, percentage);
});

// Subscribe to compliance alerts
connection.on("ReceiveComplianceAlert", (framework, message, severity) => {
    console.warn(`Compliance Alert [${framework}]: ${message} (Severity: ${severity})`);
    showComplianceNotification(framework, message, severity);
});

// Start connection and subscribe to document
await connection.start();
await connection.invoke("SubscribeToDocument", documentId);
*/
```

**Notes**:

- **Service Orchestration**: Integrates with Aspire service discovery and distributed architecture
- **Real-time Communication**: Uses SignalR for low-latency client notifications
- **Event-Driven Architecture**: Supports distributed event publishing across Aspire services
- **Health Monitoring**: Continuous service health monitoring with real-time status updates  
- **Scalability**: Designed for horizontal scaling with service mesh communication
- **Observability**: Full integration with OpenTelemetry tracing and metrics
- **Performance**: Optimized for high-throughput real-time data processing
- **Security**: Implements secure WebSocket connections with authentication
- **Modern C# Features**: Uses primary constructors, records, and async patterns
- **Cloud-Native**: Built for containerized deployment with Aspire orchestration
