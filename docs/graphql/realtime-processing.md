# GraphQL Real-time Processing Patterns

**Description**: Comprehensive patterns for integrating HotChocolate GraphQL with real-time document processing, streaming data, event-driven architectures, and live collaboration features.

**Language/Technology**: C# / HotChocolate / SignalR / Event Streaming

## Code

### Real-time Processing Infrastructure

```csharp
namespace DocumentProcessor.RealTime;

using Microsoft.AspNetCore.SignalR;

// SignalR Hub for real-time communication
public class DocumentProcessingHub : Hub
{
    private readonly ILogger<DocumentProcessingHub> logger;
    private readonly IDocumentService documentService;
    private readonly IUserSessionService userSessionService;

    public DocumentProcessingHub(
        ILogger<DocumentProcessingHub> logger,
        IDocumentService documentService,
        IUserSessionService userSessionService)
    {
        logger = logger;
        documentService = documentService;
        userSessionService = userSessionService;
    }

    public async Task JoinDocumentGroup(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"document:{documentId}");
        
        // Track user session
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await userSessionService.JoinDocumentAsync(userId, documentId, Context.ConnectionId);
        }

        logger.LogInformation("User {UserId} joined document group {DocumentId}", userId, documentId);
    }

    public async Task LeaveDocumentGroup(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"document:{documentId}");
        
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await userSessionService.LeaveDocumentAsync(userId, documentId, Context.ConnectionId);
        }

        logger.LogInformation("User {UserId} left document group {DocumentId}", userId, documentId);
    }

    public async Task SendDocumentChange(string documentId, DocumentChange change)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Validate user has access to the document
        var hasAccess = await documentService.HasAccessAsync(documentId, userId);
        if (!hasAccess)
        {
            throw new HubException("Access denied to document");
        }

        // Broadcast change to other users in the document group
        await Clients.GroupExcept($"document:{documentId}", Context.ConnectionId)
            .SendAsync("DocumentChanged", new
            {
                DocumentId = documentId,
                Change = change,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

        logger.LogDebug("Document change sent for document {DocumentId} by user {UserId}", documentId, userId);
    }

    public async Task SendCursorPosition(string documentId, CursorPosition position)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Clients.GroupExcept($"document:{documentId}", Context.ConnectionId)
            .SendAsync("CursorMoved", new
            {
                DocumentId = documentId,
                Position = position,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
    }

    public async Task StartCollaborativeSession(string documentId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await userSessionService.StartCollaborativeSessionAsync(userId, documentId, Context.ConnectionId);
        
        await Clients.Group($"document:{documentId}")
            .SendAsync("CollaborativeSessionStarted", new
            {
                DocumentId = documentId,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} connected to DocumentProcessingHub", userId);
        
        if (!string.IsNullOrEmpty(userId))
        {
            await userSessionService.UserConnectedAsync(userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} disconnected from DocumentProcessingHub", userId);

        if (!string.IsNullOrEmpty(userId))
        {
            await userSessionService.UserDisconnectedAsync(userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

// Real-time processing service
public interface IRealTimeProcessingService
{
    Task StartProcessingStreamAsync(string documentId, ProcessingRequest request);
    Task<IAsyncEnumerable<ProcessingUpdate>> GetProcessingUpdatesAsync(string documentId);
    Task NotifyProcessingCompleteAsync(string documentId, ProcessingResult result);
    Task BroadcastSystemStatusAsync(SystemStatus status);
}

public class RealTimeProcessingService : IRealTimeProcessingService
{
    private readonly IHubContext<DocumentProcessingHub> hubContext;
    private readonly IProcessingService processingService;
    private readonly ILogger<RealTimeProcessingService> logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> activeProcessing;

    public RealTimeProcessingService(
        IHubContext<DocumentProcessingHub> hubContext,
        IProcessingService processingService,
        ILogger<RealTimeProcessingService> logger)
    {
        hubContext = hubContext;
        processingService = processingService;
        logger = logger;
        activeProcessing = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public async Task StartProcessingStreamAsync(string documentId, ProcessingRequest request)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        activeProcessing.TryAdd(documentId, cancellationTokenSource);

        try
        {
            // Notify processing started
            await hubContext.Clients.Group($"document:{documentId}")
                .SendAsync("ProcessingStarted", new ProcessingUpdate
                {
                    DocumentId = documentId,
                    Status = ProcessingStatus.InProgress,
                    Progress = 0,
                    Message = "Processing started",
                    Timestamp = DateTime.UtcNow
                }, cancellationTokenSource.Token);

            // Process with real-time updates
            var progress = new Progress<ProcessingProgress>(async p =>
            {
                await hubContext.Clients.Group($"document:{documentId}")
                    .SendAsync("ProcessingProgress", new ProcessingUpdate
                    {
                        DocumentId = documentId,
                        Status = ProcessingStatus.InProgress,
                        Progress = p.Percentage,
                        Message = p.CurrentStep,
                        Step = p.Step,
                        Timestamp = DateTime.UtcNow
                    }, cancellationTokenSource.Token);
            });

            var result = await processingService.ProcessDocumentAsync(
                documentId, 
                request, 
                progress, 
                cancellationTokenSource.Token);

            await NotifyProcessingCompleteAsync(documentId, result);
        }
        catch (OperationCanceledException)
        {
            await hubContext.Clients.Group($"document:{documentId}")
                .SendAsync("ProcessingCancelled", new ProcessingUpdate
                {
                    DocumentId = documentId,
                    Status = ProcessingStatus.Cancelled,
                    Message = "Processing cancelled",
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during real-time processing for document {DocumentId}", documentId);
            
            await hubContext.Clients.Group($"document:{documentId}")
                .SendAsync("ProcessingError", new ProcessingUpdate
                {
                    DocumentId = documentId,
                    Status = ProcessingStatus.Failed,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
        }
        finally
        {
            activeProcessing.TryRemove(documentId, out _);
            cancellationTokenSource.Dispose();
        }
    }

    public async Task<IAsyncEnumerable<ProcessingUpdate>> GetProcessingUpdatesAsync(string documentId)
    {
        return ProcessingUpdatesAsyncEnumerable(documentId);
    }

    private async IAsyncEnumerable<ProcessingUpdate> ProcessingUpdatesAsyncEnumerable(string documentId)
    {
        var channel = Channel.CreateUnbounded<ProcessingUpdate>();
        var writer = channel.Writer;

        // Subscribe to processing updates for this document
        // In a real implementation, this would connect to a message queue or event stream
        
        // Simulate real-time updates
        _ = Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i <= 100; i += 10)
                {
                    await Task.Delay(1000);
                    
                    await writer.WriteAsync(new ProcessingUpdate
                    {
                        DocumentId = documentId,
                        Status = ProcessingStatus.InProgress,
                        Progress = i,
                        Message = $"Processing step {i}%",
                        Timestamp = DateTime.UtcNow
                    });
                }

                await writer.WriteAsync(new ProcessingUpdate
                {
                    DocumentId = documentId,
                    Status = ProcessingStatus.Completed,
                    Progress = 100,
                    Message = "Processing completed",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await writer.WriteAsync(new ProcessingUpdate
                {
                    DocumentId = documentId,
                    Status = ProcessingStatus.Failed,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            finally
            {
                writer.Complete();
            }
        });

        await foreach (var update in channel.Reader.ReadAllAsync())
        {
            yield return update;
        }
    }

    public async Task NotifyProcessingCompleteAsync(string documentId, ProcessingResult result)
    {
        await hubContext.Clients.Group($"document:{documentId}")
            .SendAsync("ProcessingCompleted", new ProcessingUpdate
            {
                DocumentId = documentId,
                Status = ProcessingStatus.Completed,
                Progress = 100,
                Message = "Processing completed successfully",
                Result = result,
                Timestamp = DateTime.UtcNow
            });

        logger.LogInformation("Processing completed notification sent for document {DocumentId}", documentId);
    }

    public async Task BroadcastSystemStatusAsync(SystemStatus status)
    {
        await hubContext.Clients.All.SendAsync("SystemStatusUpdate", status);
        logger.LogDebug("System status update broadcasted: {Status}", status.Status);
    }
}
```

### Real-time Data Models

```csharp
// Real-time processing models
public class ProcessingUpdate
{
    public string DocumentId { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public int Progress { get; set; }
    public string? Message { get; set; }
    public ProcessingStep? Step { get; set; }
    public ProcessingResult? Result { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DocumentChange
{
    public string ChangeType { get; set; } = string.Empty; // "insert", "delete", "replace"
    public int Position { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Length { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class CursorPosition
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Position { get; set; }
    public string Selection { get; set; } = string.Empty;
}

public class CollaborativeSession
{
    public string DocumentId { get; set; } = string.Empty;
    public string[] ActiveUsers { get; set; } = Array.Empty<string>();
    public DateTime StartedAt { get; set; }
    public Dictionary<string, CursorPosition> UserCursors { get; set; } = new();
    public DocumentChange[] RecentChanges { get; set; } = Array.Empty<DocumentChange>();
}

public class SystemStatus
{
    public string Status { get; set; } = string.Empty; // "healthy", "degraded", "offline"
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string[] ActiveServices { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; }
}

public class RealTimeMetrics
{
    public int ActiveConnections { get; set; }
    public int ActiveDocuments { get; set; }
    public int ProcessingJobs { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### GraphQL Subscriptions for Real-time Features

```csharp
[SubscriptionType]
public class RealTimeSubscriptions
{
    [Subscribe]
    public async IAsyncEnumerable<ProcessingUpdate> ProcessingUpdatesAsync(
        string documentId,
        [Service] IRealTimeProcessingService processingService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in processingService.GetProcessingUpdatesAsync(documentId))
        {
            yield return update;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<DocumentChange> DocumentChangesAsync(
        string documentId,
        [Service] IDocumentChangeService documentChangeService,
        ClaimsPrincipal currentUser,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        // Verify user has access to the document
        var hasAccess = await documentChangeService.HasAccessAsync(documentId, userId);
        if (!hasAccess)
        {
            throw new GraphQLException("Access denied to document");
        }

        await foreach (var change in documentChangeService.GetChangesAsync(documentId, cancellationToken))
        {
            // Filter out changes made by the current user to prevent echo
            if (change.UserId != userId)
            {
                yield return change;
            }
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<CollaborativeSession> CollaborationUpdatesAsync(
        string documentId,
        [Service] ICollaborationService collaborationService,
        ClaimsPrincipal currentUser,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        await foreach (var session in collaborationService.GetSessionUpdatesAsync(documentId, cancellationToken))
        {
            yield return session;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<SystemStatus> SystemStatusUpdatesAsync(
        [Service] ISystemMonitoringService monitoringService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var status in monitoringService.GetStatusUpdatesAsync(cancellationToken))
        {
            yield return status;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<RealTimeMetrics> MetricsUpdatesAsync(
        [Service] IMetricsService metricsService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var metrics = await metricsService.GetRealTimeMetricsAsync();
            yield return metrics;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<BatchProcessingStatus> BatchProcessingUpdatesAsync(
        string batchId,
        [Service] IBatchProcessingService batchService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var status in batchService.GetBatchStatusUpdatesAsync(batchId, cancellationToken))
        {
            yield return status;
        }
    }
}

// Real-time mutations for collaborative editing
[MutationType]
public class RealTimeMutations
{
    public async Task<bool> ApplyDocumentChangeAsync(
        string documentId,
        DocumentChange change,
        [Service] IDocumentChangeService documentChangeService,
        [Service] IHubContext<DocumentProcessingHub> hubContext,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        change.UserId = userId;
        change.Timestamp = DateTime.UtcNow;

        // Apply the change to the document
        var success = await documentChangeService.ApplyChangeAsync(documentId, change, cancellationToken);
        
        if (success)
        {
            // Broadcast the change to other connected clients
            await hubContext.Clients.GroupExcept($"document:{documentId}", Context.ConnectionId)
                .SendAsync("DocumentChanged", change, cancellationToken);
        }

        return success;
    }

    public async Task<bool> UpdateCursorPositionAsync(
        string documentId,
        CursorPosition position,
        [Service] ICollaborationService collaborationService,
        [Service] IHubContext<DocumentProcessingHub> hubContext,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        // Update cursor position in collaboration service
        await collaborationService.UpdateCursorPositionAsync(documentId, userId, position, cancellationToken);

        // Broadcast cursor position to other users
        await hubContext.Clients.GroupExcept($"document:{documentId}", Context.ConnectionId)
            .SendAsync("CursorMoved", new { UserId = userId, Position = position }, cancellationToken);

        return true;
    }

    public async Task<bool> StartRealTimeProcessingAsync(
        string documentId,
        ProcessingRequest request,
        [Service] IRealTimeProcessingService processingService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        // Start processing with real-time updates
        _ = Task.Run(async () =>
        {
            await processingService.StartProcessingStreamAsync(documentId, request);
        }, cancellationToken);

        return true;
    }

    public async Task<bool> CancelRealTimeProcessingAsync(
        string documentId,
        [Service] IRealTimeProcessingService processingService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User not authenticated");
        }

        // Cancel active processing
        // Implementation would need to track and cancel active processing tasks
        
        return true;
    }
}
```

### Event Streaming Service

```csharp
// Event streaming service for real-time updates
public interface IEventStreamingService
{
    Task PublishEventAsync<T>(string eventType, T eventData, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> SubscribeToEventsAsync<T>(string eventType, CancellationToken cancellationToken = default);
    Task PublishToTopicAsync<T>(string topic, T eventData, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> SubscribeToTopicAsync<T>(string topic, CancellationToken cancellationToken = default);
}

public class EventStreamingService : IEventStreamingService
{
    private readonly ILogger<EventStreamingService> logger;
    private readonly ConcurrentDictionary<string, Channel<object>> eventChannels;
    private readonly ConcurrentDictionary<string, Channel<object>> topicChannels;

    public EventStreamingService(ILogger<EventStreamingService> logger)
    {
        logger = logger;
        eventChannels = new ConcurrentDictionary<string, Channel<object>>();
        topicChannels = new ConcurrentDictionary<string, Channel<object>>();
    }

    public async Task PublishEventAsync<T>(string eventType, T eventData, CancellationToken cancellationToken = default)
    {
        var channel = eventChannels.GetOrAdd(eventType, _ => Channel.CreateUnbounded<object>());
        
        try
        {
            await channel.Writer.WriteAsync(eventData!, cancellationToken);
            logger.LogDebug("Event published to {EventType}", eventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing event to {EventType}", eventType);
        }
    }

    public async IAsyncEnumerable<T> SubscribeToEventsAsync<T>(
        string eventType, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = eventChannels.GetOrAdd(eventType, _ => Channel.CreateUnbounded<object>());
        
        await foreach (var eventData in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (eventData is T typedData)
            {
                yield return typedData;
            }
        }
    }

    public async Task PublishToTopicAsync<T>(string topic, T eventData, CancellationToken cancellationToken = default)
    {
        var channel = topicChannels.GetOrAdd(topic, _ => Channel.CreateUnbounded<object>());
        
        try
        {
            await channel.Writer.WriteAsync(eventData!, cancellationToken);
            logger.LogDebug("Event published to topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing event to topic {Topic}", topic);
        }
    }

    public async IAsyncEnumerable<T> SubscribeToTopicAsync<T>(
        string topic, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = topicChannels.GetOrAdd(topic, _ => Channel.CreateUnbounded<object>());
        
        await foreach (var eventData in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (eventData is T typedData)
            {
                yield return typedData;
            }
        }
    }
}

// Background service for processing real-time events
public class RealTimeEventProcessor : BackgroundService
{
    private readonly ILogger<RealTimeEventProcessor> logger;
    private readonly IEventStreamingService eventStreamingService;
    private readonly IHubContext<DocumentProcessingHub> hubContext;

    public RealTimeEventProcessor(
        ILogger<RealTimeEventProcessor> logger,
        IEventStreamingService eventStreamingService,
        IHubContext<DocumentProcessingHub> hubContext)
    {
        logger = logger;
        eventStreamingService = eventStreamingService;
        hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Real-time event processor started");

        // Process document changes
        _ = ProcessDocumentChangesAsync(stoppingToken);
        
        // Process processing updates
        _ = ProcessProcessingUpdatesAsync(stoppingToken);
        
        // Process system status updates
        _ = ProcessSystemStatusAsync(stoppingToken);

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        logger.LogInformation("Real-time event processor stopped");
    }

    private async Task ProcessDocumentChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var change in eventStreamingService.SubscribeToEventsAsync<DocumentChange>("DocumentChanged", cancellationToken))
            {
                await hubContext.Clients.Group($"document:{change.DocumentId}")
                    .SendAsync("DocumentChanged", change, cancellationToken);
                
                logger.LogDebug("Document change processed for document {DocumentId}", change.DocumentId);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing document changes");
        }
    }

    private async Task ProcessProcessingUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var update in eventStreamingService.SubscribeToEventsAsync<ProcessingUpdate>("ProcessingUpdate", cancellationToken))
            {
                await hubContext.Clients.Group($"document:{update.DocumentId}")
                    .SendAsync("ProcessingUpdate", update, cancellationToken);
                
                logger.LogDebug("Processing update sent for document {DocumentId}", update.DocumentId);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing updates");
        }
    }

    private async Task ProcessSystemStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var status in eventStreamingService.SubscribeToEventsAsync<SystemStatus>("SystemStatus", cancellationToken))
            {
                await hubContext.Clients.All.SendAsync("SystemStatusUpdate", status, cancellationToken);
                
                logger.LogDebug("System status update broadcasted: {Status}", status.Status);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing system status updates");
        }
    }
}
```

### Configuration and Setup

```csharp
// Real-time services configuration
public static class RealTimeConfiguration
{
    public static IServiceCollection AddRealTimeServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SignalR configuration
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
        }).AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // Real-time services
        services.AddScoped<IRealTimeProcessingService, RealTimeProcessingService>();
        services.AddSingleton<IEventStreamingService, EventStreamingService>();
        services.AddHostedService<RealTimeEventProcessor>();

        // CORS for SignalR
        services.AddCors(options =>
        {
            options.AddPolicy("SignalRPolicy", builder =>
            {
                builder.WithOrigins("https://localhost:5001", "http://localhost:5000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseRealTimeServices(this IApplicationBuilder app)
    {
        app.UseCors("SignalRPolicy");
        
        // Map SignalR hub
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<DocumentProcessingHub>("/hubs/processing");
        });

        return app;
    }
}

// GraphQL with real-time subscriptions
services
    .AddGraphQLServer()
    .AddQueryType<DocumentQueries>()
    .AddMutationType<DocumentMutations>()
    .AddSubscriptionType<RealTimeSubscriptions>()
    .AddInMemorySubscriptions() // For development
    // .AddRedisSubscriptions() // For production
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = true;
    });
```

## Usage

### GraphQL Subscriptions for Real-time Updates

```graphql
# Real-time processing updates
subscription ProcessingUpdates($documentId: ID!) {
  processingUpdates(documentId: $documentId) {
    documentId
    status
    progress
    message
    step {
      name
      description
      duration
    }
    error
    timestamp
  }
}

# Collaborative editing updates
subscription DocumentChanges($documentId: ID!) {
  documentChanges(documentId: $documentId) {
    changeType
    position
    content
    length
    userId
    timestamp
  }
}

# Real-time collaboration
subscription CollaborationUpdates($documentId: ID!) {
  collaborationUpdates(documentId: $documentId) {
    documentId
    activeUsers
    userCursors
    recentChanges {
      changeType
      position
      content
      userId
      timestamp
    }
  }
}

# System monitoring
subscription SystemStatus {
  systemStatusUpdates {
    status
    metrics
    activeServices
    timestamp
  }
}

# Real-time metrics
subscription Metrics {
  metricsUpdates {
    activeConnections
    activeDocuments
    processingJobs
    averageResponseTime
    lastUpdated
  }
}

# Batch processing updates
subscription BatchProcessing($batchId: ID!) {
  batchProcessingUpdates(batchId: $batchId) {
    batchId
    totalItems
    processedItems
    failedItems
    currentItem
    estimatedCompletion
    status
  }
}
```

### JavaScript Client Integration

```javascript
// SignalR client setup
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
    .withUrl('/hubs/processing')
    .withAutomaticReconnect()
    .build();

// Document collaboration
await connection.start();
await connection.invoke('JoinDocumentGroup', documentId);

connection.on('DocumentChanged', (change) => {
    applyDocumentChange(change);
});

connection.on('ProcessingUpdate', (update) => {
    updateProcessingStatus(update);
});

connection.on('CursorMoved', (cursorUpdate) => {
    updateUserCursor(cursorUpdate);
});

// GraphQL subscription client
import { createClient } from 'graphql-ws';

const wsClient = createClient({
    url: 'wss://localhost:5001/graphql',
});

// Subscribe to processing updates
const processingSubscription = wsClient.subscribe({
    query: `
        subscription ProcessingUpdates($documentId: ID!) {
            processingUpdates(documentId: $documentId) {
                status
                progress
                message
                timestamp
            }
        }
    `,
    variables: { documentId: 'doc-123' }
}, {
    next: (data) => {
        updateProcessingUI(data.processingUpdates);
    },
    error: (err) => {
        console.error('Subscription error:', err);
    },
    complete: () => {
        console.log('Subscription completed');
    }
});
```

## Notes

- **SignalR Integration**: Use SignalR for bidirectional real-time communication
- **Subscription Management**: Implement proper subscription lifecycle management
- **Error Handling**: Handle connection failures and automatic reconnection
- **Security**: Implement proper authentication and authorization for real-time features
- **Scalability**: Consider using Redis backplane for multi-server scenarios
- **Performance**: Monitor connection counts and message throughput
- **Resource Management**: Implement proper cleanup of resources and subscriptions
- **Rate Limiting**: Prevent abuse of real-time features with rate limiting

## Related Patterns

- [Subscription Patterns](subscription-patterns.md) - Advanced GraphQL subscription patterns
- [Performance Optimization](performance-optimization.md) - Optimizing real-time performance
- [Orleans Integration](orleans-integration.md) - Distributed real-time processing

---

**Key Benefits**: Real-time updates, collaborative editing, live monitoring, instant feedback, enhanced user experience

**When to Use**: Collaborative applications, live monitoring, real-time processing, instant notifications, interactive features

**Performance**: Connection pooling, message batching, efficient serialization, resource cleanup, rate limiting