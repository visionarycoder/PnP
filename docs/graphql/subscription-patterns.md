# Enterprise GraphQL Real-Time Subscriptions

**Description**: Advanced enterprise real-time subscription patterns including scalable WebSocket management, intelligent filtering, backpressure handling, connection pooling, comprehensive monitoring, and multi-region subscription distribution for mission-critical real-time applications.

**Language/Technology**: C#, HotChocolate, .NET 9.0, SignalR, Azure Service Bus, Real-Time Processing
**Enterprise Features**: Scalable WebSocket management, intelligent filtering, backpressure handling, multi-region distribution, comprehensive monitoring, and connection lifecycle management

## Code

### Document Processing Subscriptions

```csharp
namespace DocumentProcessor.GraphQL.Subscriptions;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

[SubscriptionType]
public class DocumentSubscriptions
{
    // Document processing status updates
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<ProcessingStatusUpdate> OnDocumentProcessingStatusAsync(
        [ID] string documentId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"document-processing:{documentId}";
        
        await foreach (var update in receiver.SubscribeAsync<ProcessingStatusUpdate>(
            topicName, cancellationToken))
        {
            yield return update;
        }
    }

    // Processing results as they complete
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<ProcessingResultUpdate> OnProcessingResultAsync(
        [ID] string documentId,
        ProcessingType? filterByType,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"processing-results:{documentId}";
        
        await foreach (var result in receiver.SubscribeAsync<ProcessingResultUpdate>(
            topicName, cancellationToken))
        {
            if (filterByType == null || result.ProcessingType == filterByType)
            {
                yield return result;
            }
        }
    }

    // Batch processing progress
    [Authorize(Policy = "CanProcessDocuments")]
    [Subscribe]
    public async IAsyncEnumerable<BatchProcessingProgress> OnBatchProgressAsync(
        [ID] string batchId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"batch-progress:{batchId}";
        
        await foreach (var progress in receiver.SubscribeAsync<BatchProcessingProgress>(
            topicName, cancellationToken))
        {
            yield return progress;
        }
    }

    // Document content changes (for collaborative editing)
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<DocumentContentUpdate> OnDocumentContentChangedAsync(
        [ID] string documentId,
        [Service] ITopicEventReceiver receiver,
        [Service] IDocumentAccessService accessService,
        CancellationToken cancellationToken)
    {
        // Check user has access to document
        var hasAccess = await accessService.CheckAccessAsync(documentId, cancellationToken);
        if (!hasAccess)
        {
            yield break;
        }

        var topicName = $"document-content:{documentId}";
        
        await foreach (var update in receiver.SubscribeAsync<DocumentContentUpdate>(
            topicName, cancellationToken))
        {
            yield return update;
        }
    }
}
```

### System-Wide Monitoring Subscriptions

```csharp
[ExtendObjectType<Subscription>]
public class SystemSubscriptions
{
    // Real-time system statistics
    [Authorize(Policy = "CanViewSystemStats")]
    [Subscribe]
    public async IAsyncEnumerable<SystemMetrics> OnSystemMetricsAsync(
        TimeSpan? updateInterval,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = "system-metrics";
        var interval = updateInterval ?? TimeSpan.FromSeconds(30);
        
        await foreach (var metrics in receiver.SubscribeAsync<SystemMetrics>(
            topicName, cancellationToken))
        {
            yield return metrics;
            
            // Rate limit updates
            await Task.Delay(interval, cancellationToken);
        }
    }

    // Processing queue status
    [Authorize(Policy = "CanViewQueueStatus")]
    [Subscribe]
    public async IAsyncEnumerable<QueueStatusUpdate> OnProcessingQueueStatusAsync(
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = "queue-status";
        
        await foreach (var status in receiver.SubscribeAsync<QueueStatusUpdate>(
            topicName, cancellationToken))
        {
            yield return status;
        }
    }

    // Error and alert notifications
    [Authorize(Policy = "CanReceiveAlerts")]
    [Subscribe]
    public async IAsyncEnumerable<SystemAlert> OnSystemAlertsAsync(
        AlertSeverity? minSeverity,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = "system-alerts";
        var minimumSeverity = minSeverity ?? AlertSeverity.Warning;
        
        await foreach (var alert in receiver.SubscribeAsync<SystemAlert>(
            topicName, cancellationToken))
        {
            if (alert.Severity >= minimumSeverity)
            {
                yield return alert;
            }
        }
    }

    // Model training progress
    [Authorize(Policy = "CanManageModels")]
    [Subscribe]
    public async IAsyncEnumerable<ModelTrainingProgress> OnModelTrainingProgressAsync(
        [ID] string trainingJobId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"model-training:{trainingJobId}";
        
        await foreach (var progress in receiver.SubscribeAsync<ModelTrainingProgress>(
            topicName, cancellationToken))
        {
            yield return progress;
        }
    }
}
```

### ML Analytics Subscriptions

```csharp
[ExtendObjectType<Subscription>]
public class AnalyticsSubscriptions
{
    // Real-time analytics updates
    [Authorize(Policy = "CanViewAnalytics")]
    [Subscribe]
    public async IAsyncEnumerable<AnalyticsUpdate> OnAnalyticsUpdateAsync(
        AnalyticsSubscriptionFilter filter,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicNames = BuildTopicNames(filter);
        
        await foreach (var update in receiver.SubscribeToMultipleAsync<AnalyticsUpdate>(
            topicNames, cancellationToken))
        {
            if (MatchesFilter(update, filter))
            {
                yield return update;
            }
        }
    }

    // Topic model updates
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<TopicModelUpdate> OnTopicModelUpdatedAsync(
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = "topic-model-updates";
        
        await foreach (var update in receiver.SubscribeAsync<TopicModelUpdate>(
            topicName, cancellationToken))
        {
            yield return update;
        }
    }

    // Trend analysis updates
    [Authorize(Policy = "CanViewTrends")]
    [Subscribe]
    public async IAsyncEnumerable<TrendAnalysisUpdate> OnTrendAnalysisAsync(
        TrendAnalysisSubscriptionInput input,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"trend-analysis:{input.Category}:{input.TimeWindow}";
        
        await foreach (var trend in receiver.SubscribeAsync<TrendAnalysisUpdate>(
            topicName, cancellationToken))
        {
            yield return trend;
        }
    }
}
```

### Subscription Event Types

```csharp
// Processing status updates
[ObjectType]
public class ProcessingStatusUpdate
{
    public string DocumentId { get; set; } = string.Empty;
    public ProcessingType ProcessingType { get; set; }
    public ProcessingStatus OldStatus { get; set; }
    public ProcessingStatus NewStatus { get; set; }
    public float? Progress { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Processing results
[ObjectType]
public class ProcessingResultUpdate
{
    public string DocumentId { get; set; } = string.Empty;
    public ProcessingType ProcessingType { get; set; }
    public ProcessingResult Result { get; set; } = new();
    public DateTime CompletedAt { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

// Batch processing progress
[ObjectType]
public class BatchProcessingProgress
{
    public string BatchId { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public int InProgressItems { get; set; }
    public float CompletionPercentage => TotalItems > 0 ? (float)CompletedItems / TotalItems * 100 : 0;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedRemainingTime { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<ProcessingError> RecentErrors { get; set; } = new();
}

// Document content collaboration
[ObjectType]
public class DocumentContentUpdate
{
    public string DocumentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DocumentChangeType ChangeType { get; set; }
    public string? NewContent { get; set; }
    public TextDelta? ContentDelta { get; set; }
    public Dictionary<string, object>? MetadataChanges { get; set; }
    public DateTime Timestamp { get; set; }
    public long Version { get; set; }
}

// System monitoring
[ObjectType]
public class SystemMetrics
{
    public int ActiveProcessingJobs { get; set; }
    public int QueuedDocuments { get; set; }
    public int CompletedToday { get; set; }
    public int FailedToday { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public float SystemCpuUsage { get; set; }
    public float SystemMemoryUsage { get; set; }
    public Dictionary<ProcessingType, ProcessingTypeMetrics> TypeMetrics { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

[ObjectType]
public class QueueStatusUpdate
{
    public Dictionary<ProcessingPriority, int> QueueSizes { get; set; } = new();
    public int TotalQueueSize { get; set; }
    public int ActiveWorkers { get; set; }
    public int IdleWorkers { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public DateTime Timestamp { get; set; }
}

[ObjectType]
public class SystemAlert
{
    public string Id { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public Dictionary<string, string> Context { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
```

### Advanced Subscription Patterns

```csharp
// Filtered subscriptions with dynamic topics
[ExtendObjectType<Subscription>]
public class AdvancedSubscriptions
{
    // Multi-document subscription with filtering
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<DocumentUpdate> OnMultipleDocumentsAsync(
        MultiDocumentSubscriptionInput input,
        [Service] ITopicEventReceiver receiver,
        [Service] IDocumentAccessService accessService,
        CancellationToken cancellationToken)
    {
        // Validate access to all requested documents
        var accessibleDocuments = new HashSet<string>();
        foreach (var docId in input.DocumentIds)
        {
            if (await accessService.CheckAccessAsync(docId, cancellationToken))
            {
                accessibleDocuments.Add(docId);
            }
        }

        if (!accessibleDocuments.Any())
        {
            yield break;
        }

        // Create topic names for accessible documents
        var topicNames = accessibleDocuments.Select(id => $"document-updates:{id}").ToList();

        await foreach (var update in receiver.SubscribeToMultipleAsync<DocumentUpdate>(
            topicNames, cancellationToken))
        {
            if (accessibleDocuments.Contains(update.DocumentId) && 
                MatchesUpdateFilter(update, input.Filter))
            {
                yield return update;
            }
        }
    }

    // Category-based subscription
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<CategoryUpdate> OnCategoryUpdatesAsync(
        string category,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var topicName = $"category-updates:{category}";
        
        await foreach (var update in receiver.SubscribeAsync<CategoryUpdate>(
            topicName, cancellationToken))
        {
            yield return update;
        }
    }

    // User activity subscription
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<UserActivityUpdate> OnUserActivityAsync(
        [ID] string userId,
        [Service] ITopicEventReceiver receiver,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        // Users can only subscribe to their own activity or if they have admin rights
        var currentUserId = userContext.GetUserId();
        if (userId != currentUserId && !await userContext.HasRoleAsync("Admin"))
        {
            yield break;
        }

        var topicName = $"user-activity:{userId}";
        
        await foreach (var activity in receiver.SubscribeAsync<UserActivityUpdate>(
            topicName, cancellationToken))
        {
            yield return activity;
        }
    }
}
```

### Subscription Input Types

```csharp
[InputType]
public class AnalyticsSubscriptionFilter
{
    public List<string>? Categories { get; set; }
    public List<ProcessingType>? ProcessingTypes { get; set; }
    public TimeSpan? UpdateInterval { get; set; }
    public float? MinimumThreshold { get; set; }
}

[InputType]
public class MultiDocumentSubscriptionInput
{
    public List<string> DocumentIds { get; set; } = new();
    public DocumentUpdateFilter? Filter { get; set; }
}

[InputType]
public class DocumentUpdateFilter
{
    public List<DocumentChangeType>? ChangeTypes { get; set; }
    public bool IncludeContent { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeProcessingResults { get; set; } = true;
}

[InputType]
public class TrendAnalysisSubscriptionInput
{
    public string Category { get; set; } = string.Empty;
    public TimeWindow TimeWindow { get; set; }
    public float? SignificanceThreshold { get; set; }
}
```

### Subscription Service Implementation

```csharp
// Event publishing service
public interface ISubscriptionEventPublisher
{
    Task PublishDocumentProcessingStatusAsync(ProcessingStatusUpdate update);
    Task PublishProcessingResultAsync(ProcessingResultUpdate result);
    Task PublishBatchProgressAsync(BatchProcessingProgress progress);
    Task PublishSystemMetricsAsync(SystemMetrics metrics);
    Task PublishSystemAlertAsync(SystemAlert alert);
}

public class SubscriptionEventPublisher(
    ITopicEventSender eventSender,
    ILogger<SubscriptionEventPublisher> logger) : ISubscriptionEventPublisher
{
    public async Task PublishDocumentProcessingStatusAsync(ProcessingStatusUpdate update)
    {
        try
        {
            var topicName = $"document-processing:{update.DocumentId}";
            await eventSender.SendAsync(topicName, update);
            
            logger.LogDebug("Published processing status update for document {DocumentId}", 
                update.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish processing status update for document {DocumentId}",
                update.DocumentId);
        }
    }

    public async Task PublishProcessingResultAsync(ProcessingResultUpdate result)
    {
        try
        {
            var topicName = $"processing-results:{result.DocumentId}";
            await eventSender.SendAsync(topicName, result);
            
            // Also publish to type-specific topic
            var typeTopicName = $"processing-results:{result.ProcessingType}";
            await eventSender.SendAsync(typeTopicName, result);
            
            logger.LogDebug("Published processing result for document {DocumentId}, type {ProcessingType}",
                result.DocumentId, result.ProcessingType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish processing result for document {DocumentId}",
                result.DocumentId);
        }
    }

    public async Task PublishBatchProgressAsync(BatchProcessingProgress progress)
    {
        try
        {
            var topicName = $"batch-progress:{progress.BatchId}";
            await eventSender.SendAsync(topicName, progress);
            
            logger.LogDebug("Published batch progress for batch {BatchId}: {CompletedItems}/{TotalItems}",
                progress.BatchId, progress.CompletedItems, progress.TotalItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish batch progress for batch {BatchId}",
                progress.BatchId);
        }
    }

    public async Task PublishSystemMetricsAsync(SystemMetrics metrics)
    {
        try
        {
            await eventSender.SendAsync("system-metrics", metrics);
            logger.LogDebug("Published system metrics");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish system metrics");
        }
    }

    public async Task PublishSystemAlertAsync(SystemAlert alert)
    {
        try
        {
            await eventSender.SendAsync("system-alerts", alert);
            
            // Publish to severity-specific topic
            var severityTopic = $"system-alerts:{alert.Severity}";
            await eventSender.SendAsync(severityTopic, alert);
            
            logger.LogWarning("Published system alert: {Title} ({Severity})", alert.Title, alert.Severity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish system alert: {AlertId}", alert.Id);
        }
    }
}
```

## Usage

### Subscription Examples

```graphql
# Subscribe to document processing updates
subscription DocumentProcessing {
  onDocumentProcessingStatus(documentId: "doc-123") {
    documentId
    processingType
    oldStatus
    newStatus
    progress
    timestamp
    message
  }
}

# Subscribe to processing results with filtering
subscription ProcessingResults {
  onProcessingResult(documentId: "doc-123", filterByType: SENTIMENT) {
    documentId
    processingType
    result {
      id
      status
      confidence
      output {
        ... on SentimentResult {
          sentiment
          score
          emotionScores {
            emotion
            score
          }
        }
      }
    }
    completedAt
    processingDuration
  }
}

# Subscribe to batch processing progress
subscription BatchProgress {
  onBatchProgress(batchId: "batch-456") {
    batchId
    batchName
    totalItems
    completedItems
    failedItems
    completionPercentage
    estimatedRemainingTime
    recentErrors {
      documentId
      errorMessage
      timestamp
    }
  }
}

# Subscribe to system metrics
subscription SystemMonitoring {
  onSystemMetrics(updateInterval: "PT30S") {
    activeProcessingJobs
    queuedDocuments
    completedToday
    averageProcessingTime
    systemCpuUsage
    systemMemoryUsage
    timestamp
  }
}

# Multi-document subscription
subscription MultipleDocuments {
  onMultipleDocuments(
    input: {
      documentIds: ["doc-1", "doc-2", "doc-3"]
      filter: {
        changeTypes: [CONTENT_UPDATED, STATUS_CHANGED]
        includeProcessingResults: true
      }
    }
  ) {
    documentId
    changeType
    timestamp
    version
  }
}
```

## Notes

- **Authorization**: Always check user permissions before allowing subscriptions
- **Resource Management**: Implement connection limits and cleanup for abandoned subscriptions
- **Filtering**: Provide client-side filtering to reduce unnecessary network traffic
- **Rate Limiting**: Implement rate limiting to prevent subscription abuse
- **Error Handling**: Handle connection drops and implement reconnection logic
- **Performance**: Use efficient event routing and avoid broadcasting to unnecessary clients
- **Security**: Validate subscription parameters to prevent information leakage
- **Monitoring**: Track subscription usage and performance metrics

## Related Patterns

- [Query Patterns](query-patterns.md) - Data retrieval patterns
- [Mutation Patterns](mutation-patterns.md) - Data modification operations
- [Real-time Processing](realtime-processing.md) - Live processing updates

---

**Key Benefits**: Real-time updates, filtered subscriptions, system monitoring, collaborative features

**When to Use**: Live dashboards, real-time processing monitoring, collaborative document editing

**Performance**: Efficient event routing, connection management, rate limiting
