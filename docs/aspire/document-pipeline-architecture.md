# .NET Aspire Document Pipeline Architecture

**Description**: End-to-end document processing flow architecture using .NET Aspire, including pipeline design, data flow orchestration, error handling, and scalability patterns for document classification and analysis.

**Language/Technology**: C#, .NET Aspire, .NET 9.0

**Code**:

## Pipeline Architecture Overview

```csharp
namespace DocumentProcessor.Aspire.Pipeline;

// Core pipeline abstraction
public interface IDocumentPipeline
{
    Task<PipelineResult> ProcessAsync(DocumentInput input, CancellationToken cancellationToken = default);
    Task<BatchPipelineResult> ProcessBatchAsync(IEnumerable<DocumentInput> inputs, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PipelineStageResult> ProcessWithStagesAsync(DocumentInput input, CancellationToken cancellationToken = default);
}

// Pipeline orchestrator with Aspire integration
public class DocumentPipeline : IDocumentPipeline
{
    private readonly IDocumentIngestionService ingestionService;
    private readonly IDocumentValidationService validationService;
    private readonly IContentExtractionService extractionService;
    private readonly IMLProcessingService mlService;
    private readonly IDocumentStorageService storageService;
    private readonly INotificationService notificationService;
    private readonly IPipelineStateManager stateManager;
    private readonly ILogger<DocumentPipeline> logger;
    private readonly DocumentPipelineOptions options;

    public DocumentPipeline(
        IDocumentIngestionService ingestionService,
        IDocumentValidationService validationService,
        IContentExtractionService extractionService,
        IMLProcessingService mlService,
        IDocumentStorageService storageService,
        INotificationService notificationService,
        IPipelineStateManager stateManager,
        ILogger<DocumentPipeline> logger,
        IOptions<DocumentPipelineOptions> options)
    {
        ingestionService = ingestionService;
        validationService = validationService;
        extractionService = extractionService;
        mlService = mlService;
        storageService = storageService;
        notificationService = notificationService;
        stateManager = stateManager;
        logger = logger;
        options = options.Value;
    }

    public async Task<PipelineResult> ProcessAsync(DocumentInput input, CancellationToken cancellationToken = default)
    {
        var pipelineId = Guid.NewGuid().ToString();
        var context = new PipelineContext(pipelineId, input, DateTime.UtcNow);

        using var activity = PipelineActivitySource.StartActivity("DocumentPipeline.Process");
        activity?.SetTag("pipeline.id", pipelineId);
        activity?.SetTag("document.type", input.ContentType);
        activity?.SetTag("document.size", input.Content?.Length ?? 0);

        try
        {
            await stateManager.InitializePipelineAsync(context);

            // Stage 1: Ingestion
            var ingestionResult = await ExecuteStageAsync(
                "Ingestion",
                () => ingestionService.IngestAsync(input, cancellationToken),
                context);

            if (!ingestionResult.IsSuccess)
            {
                return CreateFailureResult(context, "Ingestion failed", ingestionResult.Error);
            }

            var document = ingestionResult.Data!;
            context.UpdateDocument(document);

            // Stage 2: Validation
            var validationResult = await ExecuteStageAsync(
                "Validation",
                () => validationService.ValidateAsync(document, cancellationToken),
                context);

            if (!validationResult.IsSuccess)
            {
                return CreateFailureResult(context, "Validation failed", validationResult.Error);
            }

            // Stage 3: Content Extraction
            var extractionResult = await ExecuteStageAsync(
                "ContentExtraction",
                () => extractionService.ExtractContentAsync(document, cancellationToken),
                context);

            if (!extractionResult.IsSuccess)
            {
                return CreateFailureResult(context, "Content extraction failed", extractionResult.Error);
            }

            var extractedContent = extractionResult.Data!;
            context.UpdateExtractedContent(extractedContent);

            // Stage 4: ML Processing (parallel operations)
            var mlResult = await ExecuteStageAsync(
                "MLProcessing",
                () => ProcessMLAnalysisAsync(extractedContent, cancellationToken),
                context);

            if (!mlResult.IsSuccess)
            {
                return CreateFailureResult(context, "ML processing failed", mlResult.Error);
            }

            var analysisResults = mlResult.Data!;
            context.UpdateAnalysisResults(analysisResults);

            // Stage 5: Storage
            var storageResult = await ExecuteStageAsync(
                "Storage",
                () => storageService.StoreAsync(document, extractedContent, analysisResults, cancellationToken),
                context);

            if (!storageResult.IsSuccess)
            {
                return CreateFailureResult(context, "Storage failed", storageResult.Error);
            }

            // Stage 6: Notification
            var notificationResult = await ExecuteStageAsync(
                "Notification",
                () => notificationService.NotifyProcessingCompleteAsync(context, cancellationToken),
                context);

            // Notification failure doesn't fail the entire pipeline
            if (!notificationResult.IsSuccess)
            {
                logger.LogWarning("Notification failed for pipeline {PipelineId}: {Error}",
                    pipelineId, notificationResult.Error);
            }

            await stateManager.CompletePipelineAsync(context);

            return new PipelineResult
            {
                PipelineId = pipelineId,
                IsSuccess = true,
                Document = document,
                ExtractedContent = extractedContent,
                AnalysisResults = analysisResults,
                ProcessingTime = DateTime.UtcNow - context.StartTime,
                StageResults = context.StageResults
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Pipeline {PipelineId} was cancelled", pipelineId);
            await stateManager.CancelPipelineAsync(context);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in pipeline {PipelineId}", pipelineId);
            await stateManager.FailPipelineAsync(context, ex.Message);
            return CreateFailureResult(context, "Unexpected error", ex.Message);
        }
    }

    public async Task<BatchPipelineResult> ProcessBatchAsync(
        IEnumerable<DocumentInput> inputs, 
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid().ToString();
        var inputList = inputs.ToList();
        var semaphore = new SemaphoreSlim(options.MaxConcurrentProcessing, options.MaxConcurrentProcessing);
        var results = new ConcurrentBag<PipelineResult>();

        using var activity = PipelineActivitySource.StartActivity("DocumentPipeline.ProcessBatch");
        activity?.SetTag("batch.id", batchId);
        activity?.SetTag("batch.size", inputList.Count);

        logger.LogInformation("Starting batch processing {BatchId} with {DocumentCount} documents",
            batchId, inputList.Count);

        try
        {
            var tasks = inputList.Select(async (input, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using var itemActivity = PipelineActivitySource.StartActivity("DocumentPipeline.ProcessBatchItem");
                    itemActivity?.SetTag("batch.id", batchId);
                    itemActivity?.SetTag("batch.index", index);

                    var result = await ProcessAsync(input, cancellationToken);
                    results.Add(result);

                    logger.LogDebug("Completed batch item {Index} in batch {BatchId}: {Success}",
                        index, batchId, result.IsSuccess);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var resultList = results.ToList();
            var successCount = resultList.Count(r => r.IsSuccess);
            var failureCount = resultList.Count - successCount;

            logger.LogInformation("Completed batch processing {BatchId}: {SuccessCount} successful, {FailureCount} failed",
                batchId, successCount, failureCount);

            return new BatchPipelineResult
            {
                BatchId = batchId,
                TotalDocuments = inputList.Count,
                SuccessfulDocuments = successCount,
                FailedDocuments = failureCount,
                Results = resultList,
                ProcessingTime = DateTime.UtcNow - activity?.StartTimeUtc ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch processing failed for batch {BatchId}", batchId);
            throw;
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    public async IAsyncEnumerable<PipelineStageResult> ProcessWithStagesAsync(
        DocumentInput input, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pipelineId = Guid.NewGuid().ToString();
        var context = new PipelineContext(pipelineId, input, DateTime.UtcNow);

        try
        {
            await stateManager.InitializePipelineAsync(context);

            // Yield each stage result as it completes
            var ingestionResult = await ExecuteStageAsync(
                "Ingestion",
                () => ingestionService.IngestAsync(input, cancellationToken),
                context);
            yield return ingestionResult;

            if (!ingestionResult.IsSuccess) yield break;

            var document = ingestionResult.Data!;
            context.UpdateDocument(document);

            var validationResult = await ExecuteStageAsync(
                "Validation",
                () => validationService.ValidateAsync(document, cancellationToken),
                context);
            yield return validationResult;

            if (!validationResult.IsSuccess) yield break;

            var extractionResult = await ExecuteStageAsync(
                "ContentExtraction",
                () => extractionService.ExtractContentAsync(document, cancellationToken),
                context);
            yield return extractionResult;

            if (!extractionResult.IsSuccess) yield break;

            var extractedContent = extractionResult.Data!;
            context.UpdateExtractedContent(extractedContent);

            var mlResult = await ExecuteStageAsync(
                "MLProcessing",
                () => ProcessMLAnalysisAsync(extractedContent, cancellationToken),
                context);
            yield return mlResult;

            if (!mlResult.IsSuccess) yield break;

            var analysisResults = mlResult.Data!;
            context.UpdateAnalysisResults(analysisResults);

            var storageResult = await ExecuteStageAsync(
                "Storage",
                () => storageService.StoreAsync(document, extractedContent, analysisResults, cancellationToken),
                context);
            yield return storageResult;

            if (storageResult.IsSuccess)
            {
                await stateManager.CompletePipelineAsync(context);
            }
        }
        finally
        {
            if (!context.IsCompleted)
            {
                await stateManager.CancelPipelineAsync(context);
            }
        }
    }

    private async Task<PipelineStageResult<T>> ExecuteStageAsync<T>(
        string stageName,
        Func<Task<T>> stageOperation,
        PipelineContext context)
    {
        using var activity = PipelineActivitySource.StartActivity($"DocumentPipeline.Stage.{stageName}");
        activity?.SetTag("pipeline.id", context.PipelineId);
        activity?.SetTag("stage.name", stageName);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("Starting stage {StageName} for pipeline {PipelineId}",
                stageName, context.PipelineId);

            await stateManager.StartStageAsync(context, stageName);

            var result = await stageOperation();
            
            stopwatch.Stop();

            var stageResult = new PipelineStageResult<T>
            {
                StageName = stageName,
                IsSuccess = true,
                Data = result,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow
            };

            context.AddStageResult(stageResult);
            await stateManager.CompleteStageAsync(context, stageName, stageResult);

            logger.LogDebug("Completed stage {StageName} for pipeline {PipelineId} in {ElapsedMs}ms",
                stageName, context.PipelineId, stopwatch.ElapsedMilliseconds);

            activity?.SetTag("stage.success", true);
            activity?.SetTag("stage.duration_ms", stopwatch.ElapsedMilliseconds);

            return stageResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var stageResult = new PipelineStageResult<T>
            {
                StageName = stageName,
                IsSuccess = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow
            };

            context.AddStageResult(stageResult);
            await stateManager.FailStageAsync(context, stageName, ex.Message);

            logger.LogError(ex, "Stage {StageName} failed for pipeline {PipelineId} after {ElapsedMs}ms",
                stageName, context.PipelineId, stopwatch.ElapsedMilliseconds);

            activity?.SetTag("stage.success", false);
            activity?.SetTag("stage.error", ex.Message);
            activity?.SetTag("stage.duration_ms", stopwatch.ElapsedMilliseconds);

            return stageResult;
        }
    }

    private async Task<MLAnalysisResults> ProcessMLAnalysisAsync(
        ExtractedContent content, 
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var results = new MLAnalysisResults();

        // Parallel ML processing
        if (options.EnableTextClassification)
        {
            tasks.Add(Task.Run(async () =>
            {
                results.Classification = await mlService.ClassifyTextAsync(content.Text, cancellationToken);
            }, cancellationToken));
        }

        if (options.EnableSentimentAnalysis)
        {
            tasks.Add(Task.Run(async () =>
            {
                results.Sentiment = await mlService.AnalyzeSentimentAsync(content.Text, cancellationToken);
            }, cancellationToken));
        }

        if (options.EnableTopicModeling)
        {
            tasks.Add(Task.Run(async () =>
            {
                results.Topics = await mlService.ExtractTopicsAsync(content.Text, cancellationToken);
            }, cancellationToken));
        }

        if (options.EnableEntityExtraction)
        {
            tasks.Add(Task.Run(async () =>
            {
                results.Entities = await mlService.ExtractEntitiesAsync(content.Text, cancellationToken);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
        return results;
    }

    private PipelineResult CreateFailureResult(PipelineContext context, string reason, string? error)
    {
        return new PipelineResult
        {
            PipelineId = context.PipelineId,
            IsSuccess = false,
            Error = $"{reason}: {error}",
            ProcessingTime = DateTime.UtcNow - context.StartTime,
            StageResults = context.StageResults
        };
    }

    private static readonly ActivitySource PipelineActivitySource = new("DocumentProcessor.Pipeline");
}
```

## Pipeline Context and State Management

```csharp
namespace DocumentProcessor.Aspire.Pipeline;

// Pipeline execution context
public class PipelineContext
{
    public string PipelineId { get; }
    public DocumentInput Input { get; }
    public DateTime StartTime { get; }
    public Document? Document { get; private set; }
    public ExtractedContent? ExtractedContent { get; private set; }
    public MLAnalysisResults? AnalysisResults { get; private set; }
    public List<PipelineStageResult> StageResults { get; } = new();
    public Dictionary<string, object> Properties { get; } = new();
    public bool IsCompleted { get; private set; }

    public PipelineContext(string pipelineId, DocumentInput input, DateTime startTime)
    {
        PipelineId = pipelineId;
        Input = input;
        StartTime = startTime;
    }

    public void UpdateDocument(Document document) => Document = document;
    public void UpdateExtractedContent(ExtractedContent content) => ExtractedContent = content;
    public void UpdateAnalysisResults(MLAnalysisResults results) => AnalysisResults = results;
    public void AddStageResult(PipelineStageResult result) => StageResults.Add(result);
    public void SetProperty(string key, object value) => Properties[key] = value;
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default;
    public void Complete() => IsCompleted = true;
}

// Pipeline state management interface
public interface IPipelineStateManager
{
    Task InitializePipelineAsync(PipelineContext context);
    Task StartStageAsync(PipelineContext context, string stageName);
    Task CompleteStageAsync(PipelineContext context, string stageName, PipelineStageResult result);
    Task FailStageAsync(PipelineContext context, string stageName, string error);
    Task CompletePipelineAsync(PipelineContext context);
    Task FailPipelineAsync(PipelineContext context, string error);
    Task CancelPipelineAsync(PipelineContext context);
    Task<PipelineState?> GetPipelineStateAsync(string pipelineId);
    Task<List<PipelineState>> GetActivePipelinesAsync();
}

// Orleans-based pipeline state manager
public class OrleansPipelineStateManager : IPipelineStateManager
{
    private readonly IClusterClient clusterClient;
    private readonly ILogger<OrleansPipelineStateManager> logger;

    public OrleansPipelineStateManager(IClusterClient clusterClient, ILogger<OrleansPipelineStateManager> logger)
    {
        clusterClient = clusterClient;
        logger = logger;
    }

    public async Task InitializePipelineAsync(PipelineContext context)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        
        var state = new PipelineState
        {
            PipelineId = context.PipelineId,
            Status = PipelineStatus.Running,
            StartTime = context.StartTime,
            InputDocument = context.Input,
            Stages = new Dictionary<string, StageState>()
        };

        await grain.InitializeAsync(state);
        
        logger.LogDebug("Initialized pipeline state for {PipelineId}", context.PipelineId);
    }

    public async Task StartStageAsync(PipelineContext context, string stageName)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.StartStageAsync(stageName);
        
        logger.LogDebug("Started stage {StageName} for pipeline {PipelineId}", stageName, context.PipelineId);
    }

    public async Task CompleteStageAsync(PipelineContext context, string stageName, PipelineStageResult result)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.CompleteStageAsync(stageName, result);
        
        logger.LogDebug("Completed stage {StageName} for pipeline {PipelineId}", stageName, context.PipelineId);
    }

    public async Task FailStageAsync(PipelineContext context, string stageName, string error)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.FailStageAsync(stageName, error);
        
        logger.LogWarning("Failed stage {StageName} for pipeline {PipelineId}: {Error}", 
            stageName, context.PipelineId, error);
    }

    public async Task CompletePipelineAsync(PipelineContext context)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.CompleteAsync();
        
        context.Complete();
        
        logger.LogInformation("Completed pipeline {PipelineId} in {Duration}ms", 
            context.PipelineId, (DateTime.UtcNow - context.StartTime).TotalMilliseconds);
    }

    public async Task FailPipelineAsync(PipelineContext context, string error)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.FailAsync(error);
        
        logger.LogError("Failed pipeline {PipelineId}: {Error}", context.PipelineId, error);
    }

    public async Task CancelPipelineAsync(PipelineContext context)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(context.PipelineId);
        await grain.CancelAsync();
        
        logger.LogInformation("Cancelled pipeline {PipelineId}", context.PipelineId);
    }

    public async Task<PipelineState?> GetPipelineStateAsync(string pipelineId)
    {
        var grain = clusterClient.GetGrain<IPipelineStateGrain>(pipelineId);
        return await grain.GetStateAsync();
    }

    public async Task<List<PipelineState>> GetActivePipelinesAsync()
    {
        // This would require a registry grain or database query
        // For now, return empty list - implementation depends on requirements
        await Task.CompletedTask;
        return new List<PipelineState>();
    }
}

// Pipeline state grain interface
public interface IPipelineStateGrain : IGrainWithStringKey
{
    Task InitializeAsync(PipelineState state);
    Task StartStageAsync(string stageName);
    Task CompleteStageAsync(string stageName, PipelineStageResult result);
    Task FailStageAsync(string stageName, string error);
    Task CompleteAsync();
    Task FailAsync(string error);
    Task CancelAsync();
    Task<PipelineState> GetStateAsync();
}

// Pipeline state grain implementation
public class PipelineStateGrain : Grain, IPipelineStateGrain
{
    private readonly IPersistentState<PipelineState> state;

    public PipelineStateGrain([PersistentState("pipelineState", "pipelineStorage")] IPersistentState<PipelineState> state)
    {
        state = state;
    }

    public async Task InitializeAsync(PipelineState state)
    {
        state.State = state;
        await state.WriteStateAsync();
    }

    public async Task StartStageAsync(string stageName)
    {
        state.State.Stages[stageName] = new StageState
        {
            Status = StageStatus.Running,
            StartTime = DateTime.UtcNow
        };
        
        await state.WriteStateAsync();
    }

    public async Task CompleteStageAsync(string stageName, PipelineStageResult result)
    {
        if (state.State.Stages.TryGetValue(stageName, out var stage))
        {
            stage.Status = StageStatus.Completed;
            stage.EndTime = DateTime.UtcNow;
            stage.Duration = result.Duration;
            stage.Result = result;
        }
        
        await state.WriteStateAsync();
    }

    public async Task FailStageAsync(string stageName, string error)
    {
        if (state.State.Stages.TryGetValue(stageName, out var stage))
        {
            stage.Status = StageStatus.Failed;
            stage.EndTime = DateTime.UtcNow;
            stage.Error = error;
        }
        
        state.State.Status = PipelineStatus.Failed;
        state.State.Error = $"Stage {stageName} failed: {error}";
        state.State.EndTime = DateTime.UtcNow;
        
        await state.WriteStateAsync();
    }

    public async Task CompleteAsync()
    {
        state.State.Status = PipelineStatus.Completed;
        state.State.EndTime = DateTime.UtcNow;
        
        await state.WriteStateAsync();
    }

    public async Task FailAsync(string error)
    {
        state.State.Status = PipelineStatus.Failed;
        state.State.Error = error;
        state.State.EndTime = DateTime.UtcNow;
        
        await state.WriteStateAsync();
    }

    public async Task CancelAsync()
    {
        state.State.Status = PipelineStatus.Cancelled;
        state.State.EndTime = DateTime.UtcNow;
        
        await state.WriteStateAsync();
    }

    public Task<PipelineState> GetStateAsync()
    {
        return Task.FromResult(state.State);
    }
}
```

## Service Implementations

```csharp
namespace DocumentProcessor.Aspire.Services;

// Document ingestion service
public interface IDocumentIngestionService
{
    Task<Document> IngestAsync(DocumentInput input, CancellationToken cancellationToken = default);
}

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly ILogger<DocumentIngestionService> logger;

    public DocumentIngestionService(ILogger<DocumentIngestionService> logger)
    {
        logger = logger;
    }

    public async Task<Document> IngestAsync(DocumentInput input, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("DocumentIngestion.Ingest");
        
        // Simulate document ingestion processing
        await Task.Delay(100, cancellationToken);

        var document = new Document
        {
            Id = Guid.NewGuid().ToString(),
            Name = input.Name,
            ContentType = input.ContentType,
            Content = input.Content,
            Size = input.Content?.Length ?? 0,
            UploadedAt = DateTime.UtcNow,
            Metadata = input.Metadata ?? new Dictionary<string, string>()
        };

        logger.LogDebug("Ingested document {DocumentId} with size {Size} bytes", 
            document.Id, document.Size);

        return document;
    }
}

// Document validation service
public interface IDocumentValidationService
{
    Task<ValidationResult> ValidateAsync(Document document, CancellationToken cancellationToken = default);
}

public class DocumentValidationService : IDocumentValidationService
{
    private readonly DocumentPipelineOptions options;
    private readonly ILogger<DocumentValidationService> logger;

    public DocumentValidationService(IOptions<DocumentPipelineOptions> options, ILogger<DocumentValidationService> logger)
    {
        options = options.Value;
        logger = logger;
    }

    public async Task<ValidationResult> ValidateAsync(Document document, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("DocumentValidation.Validate");
        
        await Task.Delay(50, cancellationToken);

        var errors = new List<string>();

        // Size validation
        if (document.Size > options.MaxDocumentSize)
        {
            errors.Add($"Document size {document.Size} exceeds maximum allowed size {options.MaxDocumentSize}");
        }

        // Content type validation
        if (!options.AllowedContentTypes.Contains(document.ContentType))
        {
            errors.Add($"Content type {document.ContentType} is not allowed");
        }

        // Content validation
        if (string.IsNullOrEmpty(document.Content))
        {
            errors.Add("Document content is empty");
        }

        var isValid = errors.Count == 0;
        
        logger.LogDebug("Validated document {DocumentId}: {IsValid} (errors: {ErrorCount})", 
            document.Id, isValid, errors.Count);

        return new ValidationResult
        {
            IsValid = isValid,
            Errors = errors
        };
    }
}

// Content extraction service  
public interface IContentExtractionService
{
    Task<ExtractedContent> ExtractContentAsync(Document document, CancellationToken cancellationToken = default);
}

public class ContentExtractionService : IContentExtractionService
{
    private readonly ILogger<ContentExtractionService> logger;

    public ContentExtractionService(ILogger<ContentExtractionService> logger)
    {
        logger = logger;
    }

    public async Task<ExtractedContent> ExtractContentAsync(Document document, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ContentExtraction.Extract");
        
        // Simulate content extraction processing
        await Task.Delay(200, cancellationToken);

        var extractedContent = new ExtractedContent
        {
            DocumentId = document.Id,
            Text = document.Content ?? string.Empty,
            Language = DetectLanguage(document.Content ?? string.Empty),
            WordCount = CountWords(document.Content ?? string.Empty),
            ExtractedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["originalContentType"] = document.ContentType,
                ["extractionMethod"] = "simple-text"
            }
        };

        logger.LogDebug("Extracted content from document {DocumentId}: {WordCount} words, language: {Language}", 
            document.Id, extractedContent.WordCount, extractedContent.Language);

        return extractedContent;
    }

    private static string DetectLanguage(string text)
    {
        // Simplified language detection - in real implementation use proper NLP library
        return text.Length > 0 ? "en" : "unknown";
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

// ML processing service
public interface IMLProcessingService
{
    Task<ClassificationResult> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);
    Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);
    Task<TopicResult[]> ExtractTopicsAsync(string text, CancellationToken cancellationToken = default);
    Task<EntityResult[]> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default);
}

public class MLProcessingService : IMLProcessingService
{
    private readonly ILogger<MLProcessingService> logger;

    public MLProcessingService(ILogger<MLProcessingService> logger)
    {
        logger = logger;
    }

    public async Task<ClassificationResult> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("MLProcessing.ClassifyText");
        
        await Task.Delay(300, cancellationToken);

        // Simulate ML classification
        var result = new ClassificationResult
        {
            Category = "Technical Documentation",
            Confidence = 0.85f,
            Timestamp = DateTime.UtcNow
        };

        logger.LogDebug("Classified text as {Category} with confidence {Confidence}", 
            result.Category, result.Confidence);

        return result;
    }

    public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("MLProcessing.AnalyzeSentiment");
        
        await Task.Delay(200, cancellationToken);

        var result = new SentimentResult
        {
            Sentiment = "Neutral",
            Score = 0.12f,
            Timestamp = DateTime.UtcNow
        };

        logger.LogDebug("Analyzed sentiment as {Sentiment} with score {Score}", 
            result.Sentiment, result.Score);

        return result;
    }

    public async Task<TopicResult[]> ExtractTopicsAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("MLProcessing.ExtractTopics");
        
        await Task.Delay(400, cancellationToken);

        var results = new[]
        {
            new TopicResult { Topic = "Software Development", Weight = 0.7f },
            new TopicResult { Topic = "Documentation", Weight = 0.5f },
            new TopicResult { Topic = "Architecture", Weight = 0.3f }
        };

        logger.LogDebug("Extracted {TopicCount} topics from text", results.Length);

        return results;
    }

    public async Task<EntityResult[]> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("MLProcessing.ExtractEntities");
        
        await Task.Delay(250, cancellationToken);

        var results = new[]
        {
            new EntityResult { Entity = "C#", Type = "Programming Language", Confidence = 0.9f },
            new EntityResult { Entity = ".NET", Type = "Framework", Confidence = 0.95f },
            new EntityResult { Entity = "Aspire", Type = "Technology", Confidence = 0.8f }
        };

        logger.LogDebug("Extracted {EntityCount} entities from text", results.Length);

        return results;
    }
}
```

## Data Models

```csharp
namespace DocumentProcessor.Aspire.Models;

// Pipeline configuration
public class DocumentPipelineOptions
{
    public int MaxConcurrentProcessing { get; set; } = Environment.ProcessorCount;
    public long MaxDocumentSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public List<string> AllowedContentTypes { get; set; } = new() { "text/plain", "application/pdf", "text/html" };
    public bool EnableTextClassification { get; set; } = true;
    public bool EnableSentimentAnalysis { get; set; } = true;
    public bool EnableTopicModeling { get; set; } = true;
    public bool EnableEntityExtraction { get; set; } = true;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

// Pipeline input
public record DocumentInput
{
    public string Name { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? Content { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

// Document model
public record Document
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? Content { get; init; }
    public long Size { get; init; }
    public DateTime UploadedAt { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

// Extracted content
public record ExtractedContent
{
    public string DocumentId { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public int WordCount { get; init; }
    public DateTime ExtractedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

// ML analysis results
public record MLAnalysisResults
{
    public ClassificationResult? Classification { get; set; }
    public SentimentResult? Sentiment { get; set; }
    public TopicResult[]? Topics { get; set; }
    public EntityResult[]? Entities { get; set; }
}

public record ClassificationResult
{
    public string Category { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public DateTime Timestamp { get; init; }
}

public record SentimentResult
{
    public string Sentiment { get; init; } = string.Empty;
    public float Score { get; init; }
    public DateTime Timestamp { get; init; }
}

public record TopicResult
{
    public string Topic { get; init; } = string.Empty;
    public float Weight { get; init; }
}

public record EntityResult
{
    public string Entity { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public float Confidence { get; init; }
}

// Pipeline results
public record PipelineResult
{
    public string PipelineId { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public Document? Document { get; init; }
    public ExtractedContent? ExtractedContent { get; init; }
    public MLAnalysisResults? AnalysisResults { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public List<PipelineStageResult> StageResults { get; init; } = new();
}

public record BatchPipelineResult
{
    public string BatchId { get; init; } = string.Empty;
    public int TotalDocuments { get; init; }
    public int SuccessfulDocuments { get; init; }
    public int FailedDocuments { get; init; }
    public List<PipelineResult> Results { get; init; } = new();
    public TimeSpan ProcessingTime { get; init; }
}

public record PipelineStageResult
{
    public string StageName { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PipelineStageResult<T> : PipelineStageResult
{
    public T? Data { get; init; }
}

// Pipeline state models
public enum PipelineStatus { Running, Completed, Failed, Cancelled }
public enum StageStatus { Pending, Running, Completed, Failed }

public class PipelineState
{
    public string PipelineId { get; set; } = string.Empty;
    public PipelineStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Error { get; set; }
    public DocumentInput? InputDocument { get; set; }
    public Dictionary<string, StageState> Stages { get; set; } = new();
}

public class StageState
{
    public StageStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Error { get; set; }
    public PipelineStageResult? Result { get; set; }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
}
```

**Usage**:

### Basic Pipeline Processing

```csharp
// Single document processing
var pipeline = serviceProvider.GetRequiredService<IDocumentPipeline>();

var input = new DocumentInput
{
    Name = "technical-specification.txt",
    ContentType = "text/plain",
    Content = "This is a technical specification document...",
    Metadata = new Dictionary<string, string>
    {
        ["source"] = "upload",
        ["userId"] = "user123"
    }
};

var result = await pipeline.ProcessAsync(input);

if (result.IsSuccess)
{
    Console.WriteLine($"Document processed successfully in {result.ProcessingTime.TotalSeconds:F2} seconds");
    Console.WriteLine($"Classification: {result.AnalysisResults?.Classification?.Category}");
    Console.WriteLine($"Sentiment: {result.AnalysisResults?.Sentiment?.Sentiment}");
}
else
{
    Console.WriteLine($"Processing failed: {result.Error}");
}
```

### Batch Processing

```csharp
// Batch document processing
var documents = new[]
{
    new DocumentInput { Name = "doc1.txt", ContentType = "text/plain", Content = "Content 1" },
    new DocumentInput { Name = "doc2.txt", ContentType = "text/plain", Content = "Content 2" },
    new DocumentInput { Name = "doc3.txt", ContentType = "text/plain", Content = "Content 3" }
};

var batchResult = await pipeline.ProcessBatchAsync(documents);

Console.WriteLine($"Batch processing completed: {batchResult.SuccessfulDocuments}/{batchResult.TotalDocuments} successful");
```

### Streaming Pipeline Processing

```csharp
// Stream pipeline stages
await foreach (var stageResult in pipeline.ProcessWithStagesAsync(input))
{
    Console.WriteLine($"Stage {stageResult.StageName} completed in {stageResult.Duration.TotalMilliseconds}ms");
    
    if (!stageResult.IsSuccess)
    {
        Console.WriteLine($"Stage failed: {stageResult.Error}");
        break;
    }
}
```

**Notes**:

- **End-to-End Architecture**: Complete document processing pipeline with all stages
- **Orleans Integration**: State management using Orleans grains for scalability
- **Parallel Processing**: ML analysis stages run in parallel for performance
- **Error Handling**: Comprehensive error handling with stage-level failure isolation
- **Observability**: Built-in distributed tracing and logging throughout pipeline
- **Flexible Processing**: Support for single document, batch, and streaming processing modes

**Related Patterns**:

- [Service Orchestration](service-orchestration.md) - Pipeline service coordination
- [Orleans Integration](orleans-integration.md) - Orleans grain state management
- [Health Monitoring](health-monitoring.md) - Pipeline health tracking
- [Scaling Strategies](scaling-strategies.md) - Pipeline scaling patterns
