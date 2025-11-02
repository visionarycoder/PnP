# .NET Aspire Service Orchestration

**Description**: Patterns for coordinating ML pipelines and document services using .NET Aspire's service orchestration capabilities, including service discovery, dependency management, and workflow coordination.

**Language/Technology**: C#, .NET Aspire, .NET 9.0

**Code**:

## Service Orchestration Architecture

```csharp
namespace DocumentProcessor.Aspire.Orchestration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Infrastructure Dependencies
        var postgres = builder.AddPostgres("document-db")
            .WithDataVolume()
            .WithPgAdmin();

        var redis = builder.AddRedis("cache")
            .WithRedisCommander();

        var azureStorage = builder.AddAzureStorage("storage")
            .RunAsEmulator();

        // Orleans Cluster
        var orleansCluster = builder.AddOrleans("orleans-cluster")
            .WithDashboard()
            .WithReference(postgres)
            .WithReference(redis);

        // ML Services
        var textAnalysis = builder.AddProject<Projects.TextAnalysisService>("text-analysis")
            .WithReference(postgres)
            .WithReference(redis)
            .WithEnvironment("ML_MODEL_PATH", "/app/models");

        var topicExtraction = builder.AddProject<Projects.TopicExtractionService>("topic-extraction")
            .WithReference(postgres)
            .WithReference(redis)
            .WithEnvironment("TOPIC_MODEL_COUNT", "10");

        var summaryGeneration = builder.AddProject<Projects.SummaryService>("summary-generation")
            .WithReference(azureStorage)
            .WithReference(redis);

        // Document Processing API
        var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
            .WithReference(orleansCluster)
            .WithReference(textAnalysis)
            .WithReference(topicExtraction)
            .WithReference(summaryGeneration)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(azureStorage);

        // Web Frontend
        var webApp = builder.AddProject<Projects.DocumentProcessorWeb>("web-app")
            .WithReference(documentApi)
            .WithEnvironment("API_BASE_URL", documentApi.GetEndpoint("https"));

        builder.Build().Run();
    }
}
```

## Service Coordination Patterns

### Pipeline Orchestrator Service

```csharp
namespace DocumentProcessor.Aspire.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IPipelineOrchestrator
{
    Task<ProcessingResult> ProcessDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    Task<BatchProcessingResult> ProcessDocumentBatchAsync(IEnumerable<DocumentRequest> requests, CancellationToken cancellationToken = default);
    Task<PipelineStatus> GetPipelineStatusAsync(string pipelineId);
}

public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PipelineOrchestrator> _logger;
    private readonly PipelineOptions _options;
    private readonly IDistributedCache _cache;

    public PipelineOrchestrator(
        IServiceProvider serviceProvider,
        ILogger<PipelineOrchestrator> logger,
        IOptions<PipelineOptions> options,
        IDistributedCache cache)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<ProcessingResult> ProcessDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default)
    {
        var pipelineId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting document processing pipeline {PipelineId} for document {DocumentId}", 
            pipelineId, request.DocumentId);

        try
        {
            // Update pipeline status
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.Started, "Pipeline initiated");

            // Step 1: Text Analysis
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.TextAnalysis, "Analyzing document text");
            var textAnalysisResult = await ProcessTextAnalysisAsync(request, cancellationToken);

            // Step 2: Topic Extraction (can run in parallel with sentiment)
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.TopicExtraction, "Extracting topics");
            var topicTask = ProcessTopicExtractionAsync(request, cancellationToken);
            
            // Step 3: Sentiment Analysis (parallel with topic extraction)
            var sentimentTask = ProcessSentimentAnalysisAsync(request, cancellationToken);
            
            await Task.WhenAll(topicTask, sentimentTask);
            var topicResult = await topicTask;
            var sentimentResult = await sentimentTask;

            // Step 4: Summary Generation (depends on all previous steps)
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.SummaryGeneration, "Generating summary");
            var summaryResult = await ProcessSummaryGenerationAsync(request, textAnalysisResult, topicResult, sentimentResult, cancellationToken);

            // Step 5: Final aggregation and storage
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.Aggregation, "Aggregating results");
            var result = new ProcessingResult(
                PipelineId: pipelineId,
                DocumentId: request.DocumentId,
                TextAnalysis: textAnalysisResult,
                TopicExtraction: topicResult,
                SentimentAnalysis: sentimentResult,
                Summary: summaryResult,
                ProcessedAt: DateTime.UtcNow,
                Success: true,
                Errors: new List<ProcessingError>());

            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.Completed, "Pipeline completed successfully");
            
            _logger.LogInformation("Document processing pipeline {PipelineId} completed successfully", pipelineId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document processing pipeline {PipelineId} failed", pipelineId);
            await UpdatePipelineStatusAsync(pipelineId, PipelineStage.Failed, $"Pipeline failed: {ex.Message}");
            
            return new ProcessingResult(
                PipelineId: pipelineId,
                DocumentId: request.DocumentId,
                TextAnalysis: null,
                TopicExtraction: null,
                SentimentAnalysis: null,
                Summary: null,
                ProcessedAt: DateTime.UtcNow,
                Success: false,
                Errors: new List<ProcessingError> { new(ex.Message, ex.GetType().Name) });
        }
    }

    public async Task<BatchProcessingResult> ProcessDocumentBatchAsync(
        IEnumerable<DocumentRequest> requests, 
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid().ToString();
        var requestList = requests.ToList();
        
        _logger.LogInformation("Starting batch processing {BatchId} for {Count} documents", 
            batchId, requestList.Count);

        var semaphore = new SemaphoreSlim(_options.MaxConcurrentPipelines, _options.MaxConcurrentPipelines);
        var results = new ConcurrentBag<ProcessingResult>();
        var errors = new ConcurrentBag<ProcessingError>();

        var tasks = requestList.Select(async request =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await ProcessDocumentAsync(request, cancellationToken);
                results.Add(result);
                
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        errors.Add(error);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ProcessingError($"Document {request.DocumentId}: {ex.Message}", ex.GetType().Name));
                _logger.LogError(ex, "Failed to process document {DocumentId} in batch {BatchId}", request.DocumentId, batchId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var finalResults = results.ToList();
        var finalErrors = errors.ToList();

        var batchResult = new BatchProcessingResult(
            BatchId: batchId,
            Results: finalResults,
            TotalDocuments: requestList.Count,
            SuccessfulDocuments: finalResults.Count(r => r.Success),
            FailedDocuments: finalResults.Count(r => !r.Success),
            Errors: finalErrors,
            ProcessedAt: DateTime.UtcNow);

        _logger.LogInformation("Batch processing {BatchId} completed: {Success}/{Total} successful", 
            batchId, batchResult.SuccessfulDocuments, batchResult.TotalDocuments);

        return batchResult;
    }

    public async Task<PipelineStatus> GetPipelineStatusAsync(string pipelineId)
    {
        var cacheKey = $"pipeline:status:{pipelineId}";
        var statusJson = await _cache.GetStringAsync(cacheKey);
        
        if (statusJson != null)
        {
            return JsonSerializer.Deserialize<PipelineStatus>(statusJson) ?? 
                   new PipelineStatus(pipelineId, PipelineStage.Unknown, "Status not found", DateTime.UtcNow);
        }

        return new PipelineStatus(pipelineId, PipelineStage.NotFound, "Pipeline not found", DateTime.UtcNow);
    }

    private async Task<TextAnalysisResult> ProcessTextAnalysisAsync(DocumentRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var textAnalysisService = scope.ServiceProvider.GetRequiredService<ITextAnalysisService>();
        return await textAnalysisService.AnalyzeAsync(request.Content, cancellationToken);
    }

    private async Task<TopicExtractionResult> ProcessTopicExtractionAsync(DocumentRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var topicService = scope.ServiceProvider.GetRequiredService<ITopicExtractionService>();
        return await topicService.ExtractTopicsAsync(request.Content, cancellationToken);
    }

    private async Task<SentimentAnalysisResult> ProcessSentimentAnalysisAsync(DocumentRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sentimentService = scope.ServiceProvider.GetRequiredService<ISentimentAnalysisService>();
        return await sentimentService.AnalyzeAsync(request.Content, cancellationToken);
    }

    private async Task<SummaryResult> ProcessSummaryGenerationAsync(
        DocumentRequest request,
        TextAnalysisResult textAnalysis,
        TopicExtractionResult topicExtraction,
        SentimentAnalysisResult sentimentAnalysis,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryGenerationService>();
        
        var summaryRequest = new SummaryRequest(
            Content: request.Content,
            TextAnalysis: textAnalysis,
            TopicExtraction: topicExtraction,
            SentimentAnalysis: sentimentAnalysis);

        return await summaryService.GenerateSummaryAsync(summaryRequest, cancellationToken);
    }

    private async Task UpdatePipelineStatusAsync(string pipelineId, PipelineStage stage, string message)
    {
        var status = new PipelineStatus(pipelineId, stage, message, DateTime.UtcNow);
        var cacheKey = $"pipeline:status:{pipelineId}";
        var statusJson = JsonSerializer.Serialize(status);
        
        await _cache.SetStringAsync(cacheKey, statusJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        _logger.LogDebug("Pipeline {PipelineId} status updated: {Stage} - {Message}", pipelineId, stage, message);
    }
}

// Data Models
public record DocumentRequest(
    string DocumentId,
    string Content,
    string ContentType,
    Dictionary<string, object> Metadata);

public record ProcessingResult(
    string PipelineId,
    string DocumentId,
    TextAnalysisResult? TextAnalysis,
    TopicExtractionResult? TopicExtraction,
    SentimentAnalysisResult? SentimentAnalysis,
    SummaryResult? Summary,
    DateTime ProcessedAt,
    bool Success,
    List<ProcessingError> Errors);

public record BatchProcessingResult(
    string BatchId,
    List<ProcessingResult> Results,
    int TotalDocuments,
    int SuccessfulDocuments,
    int FailedDocuments,
    List<ProcessingError> Errors,
    DateTime ProcessedAt);

public record ProcessingError(string Message, string ErrorType);

public record PipelineStatus(
    string PipelineId,
    PipelineStage Stage,
    string Message,
    DateTime Timestamp);

public enum PipelineStage
{
    NotFound,
    Unknown,
    Started,
    TextAnalysis,
    TopicExtraction,
    SentimentAnalysis,
    SummaryGeneration,
    Aggregation,
    Completed,
    Failed
}

public class PipelineOptions
{
    public const string SectionName = "Pipeline";
    
    public int MaxConcurrentPipelines { get; set; } = Environment.ProcessorCount;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableRetries { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}
```

## Service Discovery Integration

```csharp
namespace DocumentProcessor.Aspire.Discovery;

public static class ServiceDiscoveryExtensions
{
    public static IServiceCollection AddDocumentProcessingServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register pipeline orchestrator
        services.AddScoped<IPipelineOrchestrator, PipelineOrchestrator>();
        
        // Register HTTP clients with service discovery
        services.AddHttpClient<ITextAnalysisService, TextAnalysisServiceClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://text-analysis");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ITopicExtractionService, TopicExtractionServiceClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://topic-extraction");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ISummaryGenerationService, SummaryGenerationServiceClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://summary-generation");
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        // Configure options
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<PipelineOrchestratorHealthCheck>("pipeline-orchestrator")
            .AddCheck<ServiceDependencyHealthCheck>("service-dependencies");

        return services;
    }
}

// HTTP Client Implementations
public interface ITextAnalysisService
{
    Task<TextAnalysisResult> AnalyzeAsync(string content, CancellationToken cancellationToken = default);
}

public class TextAnalysisServiceClient : ITextAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TextAnalysisServiceClient> _logger;

    public TextAnalysisServiceClient(HttpClient httpClient, ILogger<TextAnalysisServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TextAnalysisResult> AnalyzeAsync(string content, CancellationToken cancellationToken = default)
    {
        var request = new { content };
        var response = await _httpClient.PostAsJsonAsync("/analyze", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<TextAnalysisResult>(cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize text analysis result");
    }
}

// Similar implementations for other services...
public record TextAnalysisResult(
    string Language,
    int WordCount,
    int SentenceCount,
    double ReadabilityScore,
    List<string> KeyPhrases);

public record TopicExtractionResult(
    List<Topic> Topics,
    int DominantTopicId,
    double TopicConfidence);

public record Topic(int Id, string Label, List<string> Keywords, double Score);

public record SentimentAnalysisResult(
    bool IsPositive,
    double Score,
    double Confidence,
    string SentimentClass);

public record SummaryResult(
    string Summary,
    int SummaryLength,
    double CompressionRatio,
    List<string> KeySentences);

public record SummaryRequest(
    string Content,
    TextAnalysisResult TextAnalysis,
    TopicExtractionResult TopicExtraction,
    SentimentAnalysisResult SentimentAnalysis);
```

## Health Check Implementation

```csharp
namespace DocumentProcessor.Aspire.HealthChecks;

public class PipelineOrchestratorHealthCheck : IHealthCheck
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<PipelineOrchestratorHealthCheck> _logger;

    public PipelineOrchestratorHealthCheck(
        IPipelineOrchestrator orchestrator,
        ILogger<PipelineOrchestratorHealthCheck> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test with a simple document request
            var testRequest = new DocumentRequest(
                DocumentId: "health-check-test",
                Content: "This is a health check test document.",
                ContentType: "text/plain",
                Metadata: new Dictionary<string, object>());

            var result = await _orchestrator.ProcessDocumentAsync(testRequest, cancellationToken);
            
            if (result.Success)
            {
                return HealthCheckResult.Healthy("Pipeline orchestrator is functioning correctly");
            }
            else
            {
                return HealthCheckResult.Degraded($"Pipeline orchestrator completed with errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline orchestrator health check failed");
            return HealthCheckResult.Unhealthy("Pipeline orchestrator health check failed", ex);
        }
    }
}

public class ServiceDependencyHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceDependencyHealthCheck> _logger;

    public ServiceDependencyHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<ServiceDependencyHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var healthyServices = new List<string>();
        var unhealthyServices = new List<string>();

        // Check text analysis service
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var textAnalysisService = scope.ServiceProvider.GetRequiredService<ITextAnalysisService>();
            await textAnalysisService.AnalyzeAsync("test", cancellationToken);
            healthyServices.Add("text-analysis");
        }
        catch (Exception ex)
        {
            unhealthyServices.Add($"text-analysis: {ex.Message}");
        }

        // Check other services similarly...
        
        if (unhealthyServices.Count == 0)
        {
            return HealthCheckResult.Healthy($"All services healthy: {string.Join(", ", healthyServices)}");
        }
        else if (healthyServices.Count > 0)
        {
            return HealthCheckResult.Degraded($"Some services unhealthy: {string.Join(", ", unhealthyServices)}");
        }
        else
        {
            return HealthCheckResult.Unhealthy($"All services unhealthy: {string.Join(", ", unhealthyServices)}");
        }
    }
}
```

**Usage**:

### Basic Service Orchestration

```csharp
// Program.cs (App Host)
var builder = DistributedApplication.CreateBuilder(args);

// Add all services with proper dependencies
var documentProcessingApp = builder.AddDocumentProcessingPipeline();

builder.Build().Run();

// API Controller
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IPipelineOrchestrator _orchestrator;

    public DocumentsController(IPipelineOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("process")]
    public async Task<ActionResult<ProcessingResult>> ProcessDocument(
        [FromBody] DocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ProcessDocumentAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("process-batch")]
    public async Task<ActionResult<BatchProcessingResult>> ProcessDocumentBatch(
        [FromBody] IEnumerable<DocumentRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ProcessDocumentBatchAsync(requests, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pipeline/{pipelineId}/status")]
    public async Task<ActionResult<PipelineStatus>> GetPipelineStatus(string pipelineId)
    {
        var status = await _orchestrator.GetPipelineStatusAsync(pipelineId);
        return Ok(status);
    }
}
```

### Configuration

```json
{
  "Pipeline": {
    "MaxConcurrentPipelines": 10,
    "DefaultTimeout": "00:05:00",
    "EnableRetries": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:02"
  }
}
```

**Notes**:

- **Service Discovery**: Automatic service resolution using Aspire's built-in discovery
- **Parallel Processing**: Topic extraction and sentiment analysis run concurrently
- **Error Handling**: Comprehensive error tracking and pipeline status monitoring
- **Health Checks**: Built-in health monitoring for pipeline and service dependencies
- **Scalability**: Configurable concurrency limits and timeout settings
- **Observability**: Detailed logging and status tracking throughout the pipeline

**Related Patterns**:

- [Orleans Integration](orleans-integration.md) - Using Orleans grains within the pipeline
- [ML Service Coordination](ml-service-orchestration.md) - Detailed ML service patterns
- [Health Monitoring](health-monitoring.md) - Advanced health check strategies
- [Configuration Management](configuration-management.md) - Environment-specific settings
