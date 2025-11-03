# GraphQL Orleans Integration Patterns

**Description**: Comprehensive patterns for integrating HotChocolate GraphQL with Microsoft Orleans for scalable, distributed document processing systems using the actor model.

**Language/Technology**: C# / HotChocolate / Orleans

## Code

### Orleans Grain Interfaces

```csharp
namespace DocumentProcessor.Orleans.Grains;

using Orleans;

// Document processing grain interface
public interface IDocumentProcessingGrain : IGrainWithStringKey
{
    Task<ProcessingResult> StartProcessingAsync(ProcessingRequest request);
    Task<ProcessingStatus> GetStatusAsync();
    Task<ProcessingResult?> GetResultAsync();
    Task CancelProcessingAsync();
    Task<ProcessingMetrics> GetMetricsAsync();
}

public interface IDocumentGrain : IGrainWithStringKey
{
    Task<Document> GetDocumentAsync();
    Task<Document> UpdateDocumentAsync(UpdateDocumentRequest request);
    Task<bool> DeleteDocumentAsync();
    Task<IEnumerable<ProcessingResult>> GetProcessingResultsAsync();
    Task<DocumentStatistics> GetStatisticsAsync();
    Task AddCollaboratorAsync(string userId, CollaborationType type);
    Task RemoveCollaboratorAsync(string userId);
}

// Analytics grain for aggregated data
public interface IAnalyticsGrain : IGrainWithStringKey
{
    Task RecordEventAsync(AnalyticsEvent analyticsEvent);
    Task<AnalyticsReport> GenerateReportAsync(DateTime from, DateTime to);
    Task<UserActivityReport> GetUserActivityAsync(string userId);
    Task<ProcessingMetrics> GetProcessingMetricsAsync();
}

// User session management grain
public interface IUserSessionGrain : IGrainWithStringKey
{
    Task<UserSession> StartSessionAsync(string connectionId);
    Task EndSessionAsync();
    Task<UserSession> GetSessionAsync();
    Task UpdateActivityAsync(UserActivity activity);
    Task<IEnumerable<string>> GetActiveDocumentsAsync();
    Task JoinDocumentAsync(string documentId);
    Task LeaveDocumentAsync(string documentId);
}

// Distributed cache grain
public interface ICacheGrain : IGrainWithStringKey
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern);
}

// Processing pipeline coordinator
public interface IPipelineCoordinatorGrain : IGrainWithStringKey
{
    Task<PipelineExecution> ExecutePipelineAsync(PipelineExecutionRequest request);
    Task<PipelineStatus> GetExecutionStatusAsync(string executionId);
    Task<IEnumerable<PipelineExecution>> GetActiveExecutionsAsync();
    Task CancelExecutionAsync(string executionId);
    Task<PipelineMetrics> GetPipelineMetricsAsync();
}
```

### Orleans Grain Implementations

```csharp
// Document processing grain implementation
public class DocumentProcessingGrain : Grain, IDocumentProcessingGrain
{
    private readonly ILogger<DocumentProcessingGrain> logger;
    private readonly IProcessingService processingService;
    private readonly IPersistentState<ProcessingState> processingState;
    
    private IDisposable? processingTimer;

    public DocumentProcessingGrain(
        ILogger<DocumentProcessingGrain> logger,
        IProcessingService processingService,
        [PersistentState("processing", "documentStorage")] IPersistentState<ProcessingState> processingState)
    {
        logger = logger;
        processingService = processingService;
        processingState = processingState;
    }

    public async Task<ProcessingResult> StartProcessingAsync(ProcessingRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Starting processing for document {DocumentId}", documentId);

        // Check if already processing
        if (processingState.State.Status == ProcessingStatus.InProgress)
        {
            throw new InvalidOperationException("Document is already being processed");
        }

        // Initialize processing state
        processingState.State.Status = ProcessingStatus.InProgress;
        processingState.State.StartedAt = DateTime.UtcNow;
        processingState.State.Request = request;
        processingState.State.Progress = 0;
        
        await processingState.WriteStateAsync();

        // Start processing with progress tracking
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await processingService.ProcessDocumentAsync(
                    documentId, 
                    request, 
                    new Progress<ProcessingProgress>(OnProgressUpdated));

                processingState.State.Status = ProcessingStatus.Completed;
                processingState.State.CompletedAt = DateTime.UtcNow;
                processingState.State.Result = result;
                processingState.State.Progress = 100;

                await processingState.WriteStateAsync();
                
                // Notify subscribers about completion
                var streamProvider = this.GetStreamProvider("ProcessingEvents");
                var stream = streamProvider.GetStream<ProcessingEvent>(Guid.Parse(documentId));
                await stream.OnNextAsync(new ProcessingEvent
                {
                    DocumentId = documentId,
                    Type = ProcessingEventType.Completed,
                    Timestamp = DateTime.UtcNow,
                    Data = result
                });

                logger.LogInformation("Processing completed for document {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Processing failed for document {DocumentId}", documentId);
                
                processingState.State.Status = ProcessingStatus.Failed;
                processingState.State.CompletedAt = DateTime.UtcNow;
                processingState.State.Error = ex.Message;

                await processingState.WriteStateAsync();
                
                // Notify subscribers about failure
                var streamProvider = this.GetStreamProvider("ProcessingEvents");
                var stream = streamProvider.GetStream<ProcessingEvent>(Guid.Parse(documentId));
                await stream.OnNextAsync(new ProcessingEvent
                {
                    DocumentId = documentId,
                    Type = ProcessingEventType.Failed,
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                });
            }
        });

        return new ProcessingResult
        {
            Status = ProcessingStatus.InProgress,
            StartedAt = processingState.State.StartedAt,
            Progress = 0
        };
    }

    public Task<ProcessingStatus> GetStatusAsync()
    {
        return Task.FromResult(processingState.State.Status);
    }

    public Task<ProcessingResult?> GetResultAsync()
    {
        return Task.FromResult(processingState.State.Result);
    }

    public async Task CancelProcessingAsync()
    {
        if (processingState.State.Status == ProcessingStatus.InProgress)
        {
            processingState.State.Status = ProcessingStatus.Cancelled;
            processingState.State.CompletedAt = DateTime.UtcNow;
            
            await processingState.WriteStateAsync();
            
            logger.LogInformation("Processing cancelled for document {DocumentId}", this.GetPrimaryKeyString());
        }
    }

    public Task<ProcessingMetrics> GetMetricsAsync()
    {
        var metrics = new ProcessingMetrics
        {
            TotalProcessingTime = processingState.State.CompletedAt - processingState.State.StartedAt,
            Status = processingState.State.Status,
            Progress = processingState.State.Progress,
            ProcessingSteps = processingState.State.ProcessingSteps?.Count ?? 0
        };

        return Task.FromResult(metrics);
    }

    private async void OnProgressUpdated(ProcessingProgress progress)
    {
        processingState.State.Progress = progress.Percentage;
        processingState.State.CurrentStep = progress.CurrentStep;
        processingState.State.ProcessingSteps ??= new List<ProcessingStep>();
        
        if (progress.Step != null)
        {
            processingState.State.ProcessingSteps.Add(progress.Step);
        }

        await processingState.WriteStateAsync();

        // Notify real-time subscribers
        var streamProvider = this.GetStreamProvider("ProcessingEvents");
        var stream = streamProvider.GetStream<ProcessingEvent>(Guid.Parse(this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new ProcessingEvent
        {
            DocumentId = this.GetPrimaryKeyString(),
            Type = ProcessingEventType.ProgressUpdated,
            Timestamp = DateTime.UtcNow,
            Progress = progress.Percentage
        });
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("DocumentProcessingGrain activated for {DocumentId}", this.GetPrimaryKeyString());
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogDebug("DocumentProcessingGrain deactivated for {DocumentId}, reason: {Reason}", 
            this.GetPrimaryKeyString(), reason);
            
        processingTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}

// Document grain implementation with state management
public class DocumentGrain : Grain, IDocumentGrain
{
    private readonly ILogger<DocumentGrain> logger;
    private readonly IDocumentRepository documentRepository;
    private readonly IPersistentState<DocumentState> documentState;

    public DocumentGrain(
        ILogger<DocumentGrain> logger,
        IDocumentRepository documentRepository,
        [PersistentState("document", "documentStorage")] IPersistentState<DocumentState> documentState)
    {
        logger = logger;
        documentRepository = documentRepository;
        documentState = documentState;
    }

    public async Task<Document> GetDocumentAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        // Try grain state first
        if (documentState.State.Document != null)
        {
            return documentState.State.Document;
        }

        // Fallback to repository
        var document = await documentRepository.GetByIdAsync(documentId);
        if (document != null)
        {
            documentState.State.Document = document;
            documentState.State.LastAccessed = DateTime.UtcNow;
            await documentState.WriteStateAsync();
        }

        return document ?? throw new InvalidOperationException($"Document {documentId} not found");
    }

    public async Task<Document> UpdateDocumentAsync(UpdateDocumentRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        var document = await GetDocumentAsync();
        
        // Apply updates
        if (!string.IsNullOrEmpty(request.Title))
        {
            document.Title = request.Title;
        }
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            document.Content = request.Content;
        }
        
        if (request.Tags != null)
        {
            document.Metadata.Tags = request.Tags;
        }

        document.Metadata.UpdatedAt = DateTime.UtcNow;
        document.Metadata.UpdatedBy = request.UserId;

        // Update in repository
        await documentRepository.UpdateAsync(document);

        // Update grain state
        documentState.State.Document = document;
        documentState.State.LastModified = DateTime.UtcNow;
        await documentState.WriteStateAsync();

        // Notify collaborators
        await NotifyCollaboratorsAsync(document, "updated");

        logger.LogInformation("Document {DocumentId} updated by {UserId}", documentId, request.UserId);

        return document;
    }

    public async Task<bool> DeleteDocumentAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        // Delete from repository
        var deleted = await documentRepository.DeleteAsync(documentId);
        
        if (deleted)
        {
            // Clear grain state
            documentState.State = new DocumentState();
            await documentState.ClearStateAsync();
            
            logger.LogInformation("Document {DocumentId} deleted", documentId);
        }

        return deleted;
    }

    public async Task<IEnumerable<ProcessingResult>> GetProcessingResultsAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        // Get processing grain and check for results
        var processingGrain = GrainFactory.GetGrain<IDocumentProcessingGrain>(documentId);
        var result = await processingGrain.GetResultAsync();
        
        return result != null ? new[] { result } : Array.Empty<ProcessingResult>();
    }

    public async Task<DocumentStatistics> GetStatisticsAsync()
    {
        var document = await GetDocumentAsync();
        
        return new DocumentStatistics
        {
            WordCount = CountWords(document.Content),
            CharacterCount = document.Content.Length,
            ReadingTime = CalculateReadingTime(document.Content),
            LastAccessed = documentState.State.LastAccessed,
            AccessCount = documentState.State.AccessCount,
            CollaboratorCount = documentState.State.Collaborators?.Count ?? 0
        };
    }

    public async Task AddCollaboratorAsync(string userId, CollaborationType type)
    {
        documentState.State.Collaborators ??= new Dictionary<string, CollaborationType>();
        documentState.State.Collaborators[userId] = type;
        
        await documentState.WriteStateAsync();
        
        logger.LogInformation("Added collaborator {UserId} to document {DocumentId} with type {Type}", 
            userId, this.GetPrimaryKeyString(), type);
    }

    public async Task RemoveCollaboratorAsync(string userId)
    {
        if (documentState.State.Collaborators?.Remove(userId) == true)
        {
            await documentState.WriteStateAsync();
            
            logger.LogInformation("Removed collaborator {UserId} from document {DocumentId}", 
                userId, this.GetPrimaryKeyString());
        }
    }

    private async Task NotifyCollaboratorsAsync(Document document, string action)
    {
        if (documentState.State.Collaborators?.Any() == true)
        {
            var streamProvider = this.GetStreamProvider("CollaborationEvents");
            var stream = streamProvider.GetStream<CollaborationEvent>(Guid.Parse(document.Id));
            
            await stream.OnNextAsync(new CollaborationEvent
            {
                DocumentId = document.Id,
                Action = action,
                Timestamp = DateTime.UtcNow,
                Document = document
            });
        }
    }

    private int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private TimeSpan CalculateReadingTime(string text)
    {
        var wordCount = CountWords(text);
        var wordsPerMinute = 200; // Average reading speed
        var minutes = Math.Max(1, wordCount / wordsPerMinute);
        return TimeSpan.FromMinutes(minutes);
    }
}

// Analytics grain for aggregated metrics
public class AnalyticsGrain : Grain, IAnalyticsGrain
{
    private readonly ILogger<AnalyticsGrain> logger;
    private readonly IPersistentState<AnalyticsState> analyticsState;
    private readonly IAnalyticsRepository analyticsRepository;

    public AnalyticsGrain(
        ILogger<AnalyticsGrain> logger,
        [PersistentState("analytics", "analyticsStorage")] IPersistentState<AnalyticsState> analyticsState,
        IAnalyticsRepository analyticsRepository)
    {
        logger = logger;
        analyticsState = analyticsState;
        analyticsRepository = analyticsRepository;
    }

    public async Task RecordEventAsync(AnalyticsEvent analyticsEvent)
    {
        analyticsState.State.Events ??= new List<AnalyticsEvent>();
        analyticsState.State.Events.Add(analyticsEvent);
        
        // Keep only recent events in memory (last 1000)
        if (analyticsState.State.Events.Count > 1000)
        {
            var eventsToRemove = analyticsState.State.Events.Take(100).ToList();
            foreach (var eventToRemove in eventsToRemove)
            {
                analyticsState.State.Events.Remove(eventToRemove);
            }
        }

        await analyticsState.WriteStateAsync();

        // Persist to long-term storage
        await analyticsRepository.SaveEventAsync(analyticsEvent);

        logger.LogDebug("Recorded analytics event: {EventType} for {EntityId}", 
            analyticsEvent.EventType, analyticsEvent.EntityId);
    }

    public async Task<AnalyticsReport> GenerateReportAsync(DateTime from, DateTime to)
    {
        var events = await analyticsRepository.GetEventsAsync(from, to);
        
        var report = new AnalyticsReport
        {
            Period = new DateRange { From = from, To = to },
            TotalEvents = events.Count,
            EventsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
            DocumentViews = events.Count(e => e.EventType == "DocumentViewed"),
            DocumentCreations = events.Count(e => e.EventType == "DocumentCreated"),
            ProcessingJobs = events.Count(e => e.EventType == "ProcessingStarted"),
            ActiveUsers = events.Select(e => e.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count(),
            TopDocuments = events.Where(e => e.EventType == "DocumentViewed")
                .GroupBy(e => e.EntityId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return report;
    }

    public async Task<UserActivityReport> GetUserActivityAsync(string userId)
    {
        var events = await analyticsRepository.GetUserEventsAsync(userId);
        
        return new UserActivityReport
        {
            UserId = userId,
            TotalActions = events.Count,
            ActionsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
            LastActivity = events.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp,
            DocumentsAccessed = events.Where(e => e.EventType.Contains("Document"))
                .Select(e => e.EntityId)
                .Distinct()
                .Count(),
            ProcessingJobsStarted = events.Count(e => e.EventType == "ProcessingStarted")
        };
    }

    public async Task<ProcessingMetrics> GetProcessingMetricsAsync()
    {
        var events = await analyticsRepository.GetProcessingEventsAsync();
        
        var completedJobs = events.Where(e => e.EventType == "ProcessingCompleted").ToList();
        var failedJobs = events.Where(e => e.EventType == "ProcessingFailed").ToList();
        
        return new ProcessingMetrics
        {
            TotalJobs = events.Count(e => e.EventType == "ProcessingStarted"),
            CompletedJobs = completedJobs.Count,
            FailedJobs = failedJobs.Count,
            AverageProcessingTime = completedJobs.Any() 
                ? TimeSpan.FromSeconds(completedJobs.Average(e => 
                    e.Properties?.GetValueOrDefault("processingTimeSeconds", 0) as double? ?? 0))
                : TimeSpan.Zero,
            SuccessRate = completedJobs.Any() 
                ? (double)completedJobs.Count / (completedJobs.Count + failedJobs.Count) * 100 
                : 0
        };
    }
}
```

### GraphQL Resolvers with Orleans Integration

```csharp
// Document resolvers using Orleans grains
[QueryType]
public class DocumentQueriesWithOrleans
{
    public async Task<Document?> GetDocumentAsync(
        string id,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(id);
        
        try
        {
            return await documentGrain.GetDocumentAsync();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetUserDocumentsAsync(
        string userId,
        [Service] IDocumentRepository documentRepository,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        // Get document IDs from repository
        var documentIds = await documentRepository.GetDocumentIdsByUserAsync(userId, cancellationToken);
        
        // Load documents using grains for caching and state management
        var documents = new List<Document>();
        
        foreach (var documentId in documentIds)
        {
            try
            {
                var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
                var document = await documentGrain.GetDocumentAsync();
                documents.Add(document);
            }
            catch (InvalidOperationException)
            {
                // Document not found, skip
                continue;
            }
        }

        return documents;
    }

    public async Task<ProcessingStatus> GetProcessingStatusAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        var processingGrain = grainFactory.GetGrain<IDocumentProcessingGrain>(documentId);
        return await processingGrain.GetStatusAsync();
    }

    public async Task<ProcessingResult?> GetProcessingResultAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        var processingGrain = grainFactory.GetGrain<IDocumentProcessingGrain>(documentId);
        return await processingGrain.GetResultAsync();
    }

    public async Task<AnalyticsReport> GetAnalyticsReportAsync(
        DateTime from,
        DateTime to,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        return await analyticsGrain.GenerateReportAsync(from, to);
    }

    public async Task<UserActivityReport> GetUserActivityAsync(
        string userId,
        [Service] IGrainFactory grainFactory,
        CancellationToken cancellationToken)
    {
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        return await analyticsGrain.GetUserActivityAsync(userId);
    }
}

[MutationType]
public class DocumentMutationsWithOrleans
{
    public async Task<Document> CreateDocumentAsync(
        CreateDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IGrainFactory grainFactory,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // Create document using service
        var document = await documentService.CreateAsync(input, userId, cancellationToken);

        // Initialize document grain
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(document.Id);
        await documentGrain.GetDocumentAsync(); // This will cache the document in the grain

        // Record analytics event
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        await analyticsGrain.RecordEventAsync(new AnalyticsEvent
        {
            EventType = "DocumentCreated",
            EntityId = document.Id,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["title"] = document.Title,
                ["contentLength"] = document.Content.Length
            }
        });

        return document;
    }

    public async Task<Document> UpdateDocumentAsync(
        string id,
        UpdateDocumentInput input,
        [Service] IGrainFactory grainFactory,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(id);
        
        var request = new UpdateDocumentRequest
        {
            Title = input.Title,
            Content = input.Content,
            Tags = input.Tags,
            UserId = userId
        };

        var updatedDocument = await documentGrain.UpdateDocumentAsync(request);

        // Record analytics event
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        await analyticsGrain.RecordEventAsync(new AnalyticsEvent
        {
            EventType = "DocumentUpdated",
            EntityId = id,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });

        return updatedDocument;
    }

    public async Task<ProcessingResult> StartProcessingAsync(
        string documentId,
        ProcessingRequest request,
        [Service] IGrainFactory grainFactory,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // Ensure user has access to the document
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        await documentGrain.GetDocumentAsync(); // This will throw if not found or no access

        var processingGrain = grainFactory.GetGrain<IDocumentProcessingGrain>(documentId);
        var result = await processingGrain.StartProcessingAsync(request);

        // Record analytics event
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        await analyticsGrain.RecordEventAsync(new AnalyticsEvent
        {
            EventType = "ProcessingStarted",
            EntityId = documentId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["pipelineType"] = request.PipelineType,
                ["priority"] = request.Priority.ToString()
            }
        });

        return result;
    }

    public async Task<bool> CancelProcessingAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var processingGrain = grainFactory.GetGrain<IDocumentProcessingGrain>(documentId);
        await processingGrain.CancelProcessingAsync();

        // Record analytics event
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        await analyticsGrain.RecordEventAsync(new AnalyticsEvent
        {
            EventType = "ProcessingCancelled",
            EntityId = documentId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> AddCollaboratorAsync(
        string documentId,
        string collaboratorUserId,
        CollaborationType type,
        [Service] IGrainFactory grainFactory,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        await documentGrain.AddCollaboratorAsync(collaboratorUserId, type);

        // Record analytics event
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("global");
        await analyticsGrain.RecordEventAsync(new AnalyticsEvent
        {
            EventType = "CollaboratorAdded",
            EntityId = documentId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["collaboratorUserId"] = collaboratorUserId,
                ["collaborationType"] = type.ToString()
            }
        });

        return true;
    }
}
```

### Orleans Streams Integration for Real-time Updates

```csharp
// GraphQL subscription with Orleans streams
[SubscriptionType]
public class DocumentSubscriptionsWithOrleans
{
    [Subscribe]
    public async IAsyncEnumerable<ProcessingEvent> ProcessingUpdatesAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamProvider = grainFactory.GetStreamProvider("ProcessingEvents");
        var stream = streamProvider.GetStream<ProcessingEvent>(Guid.Parse(documentId));

        await foreach (var processingEvent in stream.AsAsyncEnumerable(cancellationToken))
        {
            yield return processingEvent;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<CollaborationEvent> CollaborationUpdatesAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamProvider = grainFactory.GetStreamProvider("CollaborationEvents");
        var stream = streamProvider.GetStream<CollaborationEvent>(Guid.Parse(documentId));

        await foreach (var collaborationEvent in stream.AsAsyncEnumerable(cancellationToken))
        {
            yield return collaborationEvent;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<DocumentStatistics> DocumentStatisticsUpdatesAsync(
        string documentId,
        [Service] IGrainFactory grainFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        
        // Poll for statistics updates every 30 seconds
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var statistics = await documentGrain.GetStatisticsAsync();
            yield return statistics;
        }
    }
}

// Stream observer for processing events
public class ProcessingEventObserver : IAsyncObserver<ProcessingEvent>
{
    private readonly ILogger<ProcessingEventObserver> logger;
    private readonly IHubContext<ProcessingHub> hubContext;

    public ProcessingEventObserver(
        ILogger<ProcessingEventObserver> logger,
        IHubContext<ProcessingHub> hubContext)
    {
        logger = logger;
        hubContext = hubContext;
    }

    public async Task OnNextAsync(ProcessingEvent item, StreamSequenceToken? token = null)
    {
        logger.LogDebug("Processing event received: {EventType} for {DocumentId}", 
            item.Type, item.DocumentId);

        // Forward to SignalR clients
        await hubContext.Clients.Group($"document:{item.DocumentId}")
            .SendAsync("ProcessingUpdate", item);
    }

    public Task OnCompletedAsync()
    {
        logger.LogDebug("Processing event stream completed");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        logger.LogError(ex, "Error in processing event stream");
        return Task.CompletedTask;
    }
}
```

### Orleans Configuration and Setup

```csharp
// Orleans configuration
public static class OrleansConfiguration
{
    public static IServiceCollection AddOrleansServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOrleans(builder =>
        {
            builder
                .UseLocalhostClustering()
                .ConfigureLogging(logging => logging.AddConsole())
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("documentStorage")
                .AddMemoryGrainStorage("analyticsStorage")
                .AddSimpleMessageStreamProvider("ProcessingEvents")
                .AddSimpleMessageStreamProvider("CollaborationEvents")
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("ProcessingEvents")
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("CollaborationEvents")
                .UseDashboard(options => { });

            // Production configuration would use persistent storage
            if (!string.IsNullOrEmpty(configuration.GetConnectionString("Orleans")))
            {
                builder.UseAdoNetClustering(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("Orleans");
                    options.Invariant = "System.Data.SqlClient";
                });

                builder.AddAdoNetGrainStorage("documentStorage", options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("Orleans");
                    options.Invariant = "System.Data.SqlClient";
                });

                builder.AddAdoNetGrainStorage("analyticsStorage", options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("Orleans");
                    options.Invariant = "System.Data.SqlClient";
                });
            }
        });

        return services;
    }

    public static IApplicationBuilder UseOrleansServices(this IApplicationBuilder app)
    {
        // Orleans dashboard
        app.UseOrleansDashboard(new DashboardOptions
        {
            Host = "*",
            Port = 8080,
            HostSelf = true,
            CounterUpdateIntervalMs = 1000
        });

        return app;
    }
}

// GraphQL configuration with Orleans
services
    .AddGraphQLServer()
    .AddQueryType<DocumentQueriesWithOrleans>()
    .AddMutationType<DocumentMutationsWithOrleans>()
    .AddSubscriptionType<DocumentSubscriptionsWithOrleans>()
    .AddOrleansServices(configuration)
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = true;
    });
```

## Usage

### GraphQL Operations with Orleans

```graphql
# Query using Orleans grain
query GetDocumentWithProcessing($id: ID!) {
  document(id: $id) {
    id
    title
    content
    statistics {
      wordCount
      readingTime
      accessCount
    }
  }
  
  processingStatus(documentId: $id)
  
  processingResult(documentId: $id) {
    status
    startedAt
    completedAt
    progress
  }
}

# Mutation with Orleans coordination
mutation ProcessDocument($documentId: ID!, $request: ProcessingRequest!) {
  startProcessing(documentId: $documentId, request: $request) {
    status
    startedAt
    progress
  }
}

# Real-time subscription using Orleans streams
subscription ProcessingUpdates($documentId: ID!) {
  processingUpdates(documentId: $documentId) {
    documentId
    type
    timestamp
    progress
    data
    error
  }
}

# Analytics query using Orleans analytics grain
query GetAnalytics($from: DateTime!, $to: DateTime!) {
  analyticsReport(from: $from, to: $to) {
    totalEvents
    eventsByType
    documentViews
    activeUsers
    topDocuments
  }
}
```

## Notes

- **Actor Model**: Leverage Orleans grains for stateful, distributed processing
- **Stream Integration**: Use Orleans streams for real-time GraphQL subscriptions
- **State Management**: Implement persistent state for reliable grain operations
- **Scalability**: Design grains for horizontal scaling and load distribution
- **Error Handling**: Implement proper error handling in grain operations
- **Monitoring**: Use Orleans dashboard and metrics for system monitoring
- **Performance**: Consider grain lifecycle and memory management
- **Clustering**: Configure proper clustering for production deployments

## Related Patterns

- [Subscription Patterns](subscription-patterns.md) - Real-time updates with Orleans streams
- [Performance Optimization](performance-optimization.md) - Orleans-specific optimizations
- [Error Handling](error-handling.md) - Error handling in distributed grain operations

---

**Key Benefits**: Distributed state management, scalable processing, real-time coordination, actor model patterns

**When to Use**: Distributed systems, real-time collaboration, stateful processing, scalable architectures

**Performance**: Distributed processing, in-memory state, stream-based updates, horizontal scaling