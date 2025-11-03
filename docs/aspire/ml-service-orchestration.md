# ML Service Orchestration with .NET Aspire

**Description**: Patterns for orchestrating ML.NET, Azure AI Services, and custom ML models within .NET Aspire framework for document processing workflows.

**Technology**: .NET Aspire + ML.NET + Azure AI Services + OpenAI

## Overview

ML service orchestration with Aspire enables coordinated deployment and management of multiple machine learning services, models, and pipelines. This pattern provides centralized configuration, service discovery, health monitoring, and performance tracking for complex document processing workflows.

## Core ML Service Architecture

### ML Service Registration

```csharp
namespace DocumentProcessor.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Add ML service dependencies
var textAnalytics = builder.AddAzureTextAnalytics("text-analytics");
var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(new("gpt-4", "gpt-4", "2024-turbo-preview"));

var redis = builder.AddRedis("ml-cache")
    .WithRedisInsight();

// Add ML processing services
var mlService = builder.AddProject<Projects.MLService>("ml-service")
    .WithReference(textAnalytics)
    .WithReference(openai)
    .WithReference(redis)
    .WithEnvironment("mlModel_PATH", "/app/models")
    .WithBindMount("./models", "/app/models");

var documentProcessor = builder.AddProject<Projects.DocumentProcessor>("document-processor")
    .WithReference(mlService)
    .WithHttpsEndpoint(port: 7002, name: "https");

// Add model management service
var modelManager = builder.AddProject<Projects.ModelManager>("model-manager")
    .WithReference(mlService)
    .WithReference(redis)
    .WithHttpsEndpoint(port: 7003, name: "https");

builder.Build().Run();
```

### ML Service Implementation

```csharp
namespace MLService;

using Microsoft.ML;
using Azure.AI.TextAnalytics;
using Azure.AI.OpenAI;

public interface IMLOrchestrator
{
    Task<TextAnalysisResult> AnalyzeDocumentAsync(string documentId, string content);
    Task<TopicModelingResult> ExtractTopicsAsync(string content, int topicCount = 5);
    Task<SummarizationResult> GenerateSummariesAsync(string content, SummarizationOptions options);
    Task<ClassificationResult> ClassifyDocumentAsync(string content, string[] categories);
}

public class MLOrchestrator : IMLOrchestrator
{
    private readonly MLContext mlContext;
    private readonly TextAnalyticsClient textAnalytics;
    private readonly OpenAIClient openAiClient;
    private readonly IDistributedCache cache;
    private readonly ILogger<MLOrchestrator> logger;
    private readonly Dictionary<string, ITransformer> models;

    public MLOrchestrator(
        MLContext mlContext,
        TextAnalyticsClient textAnalytics,
        OpenAIClient openAiClient,
        IDistributedCache cache,
        ILogger<MLOrchestrator> logger)
    {
        mlContext = mlContext;
        textAnalytics = textAnalytics;
        openAiClient = openAiClient;
        cache = cache;
        logger = logger;
        models = new Dictionary<string, ITransformer>();
        
        LoadPretrainedModels();
    }

    public async Task<TextAnalysisResult> AnalyzeDocumentAsync(string documentId, string content)
    {
        var cacheKey = $"text-analysis:{documentId}:{content.GetHashCode()}";
        
        // Check cache first
        var cachedResult = await cache.GetStringAsync(cacheKey);
        if (cachedResult != null)
        {
            return JsonSerializer.Deserialize<TextAnalysisResult>(cachedResult)!;
        }

        logger.LogInformation("Analyzing document {DocumentId}", documentId);

        // Parallel analysis with multiple services
        var tasks = new[]
        {
            AnalyzeSentimentAsync(content),
            ExtractKeyPhrasesAsync(content),
            RecognizeEntitiesAsync(content),
            DetectLanguageAsync(content)
        };

        var results = await Task.WhenAll(tasks);

        var analysisResult = new TextAnalysisResult(
            DocumentId: documentId,
            Sentiment: results[0],
            KeyPhrases: results[1],
            Entities: results[2],
            Language: results[3],
            AnalyzedAt: DateTime.UtcNow);

        // Cache result
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(analysisResult),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

        return analysisResult;
    }

    public async Task<TopicModelingResult> ExtractTopicsAsync(string content, int topicCount = 5)
    {
        logger.LogInformation("Extracting {TopicCount} topics from content", topicCount);

        // Use Azure OpenAI for topic extraction
        var chatCompletions = openAiClient.GetChatCompletionsClient();
        
        var response = await chatCompletions.CompleteAsync(new ChatCompletionsOptions
        {
            Messages =
            {
                new ChatRequestSystemMessage($"Extract {topicCount} main topics from the following text. Return as JSON with topic names and confidence scores."),
                new ChatRequestUserMessage(content)
            },
            MaxTokens = 500,
            Temperature = 0.3f
        });

        var topicsJson = response.Value.Choices[0].Message.Content;
        var topics = JsonSerializer.Deserialize<Dictionary<string, double>>(topicsJson!)!;

        return new TopicModelingResult(
            Topics: topics,
            TopicCount: topicCount,
            ExtractedAt: DateTime.UtcNow);
    }

    public async Task<SummarizationResult> GenerateSummariesAsync(string content, SummarizationOptions options)
    {
        logger.LogInformation("Generating summaries with types: {SummaryTypes}", 
            string.Join(", ", options.SummaryTypes));

        var summaries = new Dictionary<string, string>();
        var chatCompletions = openAiClient.GetChatCompletionsClient();

        foreach (var summaryType in options.SummaryTypes)
        {
            var prompt = summaryType switch
            {
                "executive" => "Provide a 2-3 sentence executive summary",
                "detailed" => "Provide a comprehensive 1-2 paragraph summary",
                "bullet-points" => "Provide key points as bulleted list",
                "abstract" => "Provide an academic-style abstract",
                _ => "Provide a brief summary"
            };

            var response = await chatCompletions.CompleteAsync(new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatRequestSystemMessage($"{prompt} of the following text:"),
                    new ChatRequestUserMessage(content)
                },
                MaxTokens = summaryType == "detailed" ? 800 : 300,
                Temperature = 0.2f
            });

            summaries[summaryType] = response.Value.Choices[0].Message.Content!;
        }

        return new SummarizationResult(
            Summaries: summaries,
            GeneratedAt: DateTime.UtcNow);
    }

    public async Task<ClassificationResult> ClassifyDocumentAsync(string content, string[] categories)
    {
        logger.LogInformation("Classifying document into categories: {Categories}", 
            string.Join(", ", categories));

        // Use pre-trained ML.NET model for classification
        if (models.TryGetValue("document-classifier", out var classifier))
        {
            var prediction = await PredictWithMLNetAsync(classifier, content, categories);
            return prediction;
        }

        // Fallback to Azure OpenAI classification
        return await ClassifyWithOpenAIAsync(content, categories);
    }

    private async Task<string> AnalyzeSentimentAsync(string content)
    {
        var response = await textAnalytics.AnalyzeSentimentAsync(content);
        return response.Value.Sentiment.ToString();
    }

    private async Task<string[]> ExtractKeyPhrasesAsync(string content)
    {
        var response = await textAnalytics.ExtractKeyPhrasesAsync(content);
        return response.Value.KeyPhrases.ToArray();
    }

    private async Task<string[]> RecognizeEntitiesAsync(string content)
    {
        var response = await textAnalytics.RecognizeEntitiesAsync(content);
        return response.Value.Entities.Select(e => $"{e.Text}:{e.Category}").ToArray();
    }

    private async Task<string> DetectLanguageAsync(string content)
    {
        var response = await textAnalytics.DetectLanguageAsync(content);
        return response.Value.Iso6391Name;
    }
}
```

## ML Pipeline Orchestration

### Pipeline Configuration

```csharp
namespace MLService.Pipelines;

public interface IMLPipelineOrchestrator
{
    Task<PipelineResult> ExecutePipelineAsync(string pipelineId, PipelineInput input);
    Task<PipelineStatus> GetPipelineStatusAsync(string pipelineId);
    Task RegisterPipelineAsync(MLPipelineDefinition definition);
}

public class MLPipelineOrchestrator : IMLPipelineOrchestrator
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MLPipelineOrchestrator> logger;
    private readonly Dictionary<string, MLPipelineDefinition> pipelines;
    private readonly Dictionary<string, PipelineExecution> executions;

    public MLPipelineOrchestrator(IServiceProvider serviceProvider, ILogger<MLPipelineOrchestrator> logger)
    {
        serviceProvider = serviceProvider;
        logger = logger;
        pipelines = new Dictionary<string, MLPipelineDefinition>();
        executions = new Dictionary<string, PipelineExecution>();
        
        RegisterDefaultPipelines();
    }

    public async Task<PipelineResult> ExecutePipelineAsync(string pipelineId, PipelineInput input)
    {
        if (!pipelines.TryGetValue(pipelineId, out var pipeline))
        {
            throw new ArgumentException($"Pipeline {pipelineId} not found");
        }

        var executionId = Guid.NewGuid().ToString();
        var execution = new PipelineExecution(executionId, pipelineId, DateTime.UtcNow);
        _executions[executionId] = execution;

        logger.LogInformation("Executing pipeline {PipelineId} with execution {ExecutionId}", 
            pipelineId, executionId);

        try
        {
            var context = new PipelineExecutionContext(input, serviceProvider, logger);
            var result = await ExecutePipelineStepsAsync(pipeline, context);
            
            execution.Complete(result);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline {PipelineId} execution {ExecutionId} failed", 
                pipelineId, executionId);
            
            execution.Fail(ex);
            throw;
        }
    }

    private async Task<PipelineResult> ExecutePipelineStepsAsync(
        MLPipelineDefinition pipeline, 
        PipelineExecutionContext context)
    {
        var stepResults = new Dictionary<string, object>();
        
        foreach (var step in pipeline.Steps.OrderBy(s => s.Order))
        {
            logger.LogDebug("Executing step {StepName} in pipeline {PipelineName}", 
                step.Name, pipeline.Name);

            var stepResult = await ExecuteStepAsync(step, context, stepResults);
            stepResults[step.Name] = stepResult;
            
            // Update context with step result for next steps
            context.AddStepResult(step.Name, stepResult);
        }

        return new PipelineResult(
            PipelineId: pipeline.Id,
            ExecutionId: context.ExecutionId,
            StepResults: stepResults,
            CompletedAt: DateTime.UtcNow);
    }

    private void RegisterDefaultPipelines()
    {
        // Document Analysis Pipeline
        var documentAnalysis = new MLPipelineDefinition(
            Id: "document-analysis",
            Name: "Document Analysis Pipeline",
            Description: "Complete document analysis with sentiment, entities, and topics",
            Steps: new[]
            {
                new PipelineStep("preprocess", 1, typeof(TextPreprocessingStep)),
                new PipelineStep("sentiment", 2, typeof(SentimentAnalysisStep)),
                new PipelineStep("entities", 3, typeof(EntityExtractionStep)),
                new PipelineStep("topics", 4, typeof(TopicExtractionStep)),
                new PipelineStep("summary", 5, typeof(SummarizationStep))
            });
        
        _pipelines[documentAnalysis.Id] = documentAnalysis;

        // Classification Pipeline
        var classification = new MLPipelineDefinition(
            Id: "document-classification",
            Name: "Document Classification Pipeline",
            Description: "Classify documents into predefined categories",
            Steps: new[]
            {
                new PipelineStep("preprocess", 1, typeof(TextPreprocessingStep)),
                new PipelineStep("vectorize", 2, typeof(TextVectorizationStep)),
                new PipelineStep("classify", 3, typeof(ClassificationStep)),
                new PipelineStep("confidence", 4, typeof(ConfidenceCalculationStep))
            });
        
        _pipelines[classification.Id] = classification;
    }
}
```

### Pipeline Steps Implementation

```csharp
namespace MLService.Pipelines.Steps;

public interface IPipelineStep
{
    Task<object> ExecuteAsync(PipelineExecutionContext context, Dictionary<string, object> previousResults);
}

public class SentimentAnalysisStep : IPipelineStep
{
    private readonly IMLOrchestrator mlOrchestrator;

    public SentimentAnalysisStep(IMLOrchestrator mlOrchestrator)
    {
        mlOrchestrator = mlOrchestrator;
    }

    public async Task<object> ExecuteAsync(PipelineExecutionContext context, Dictionary<string, object> previousResults)
    {
        var content = context.Input.Content;
        
        // Get preprocessed content if available
        if (previousResults.TryGetValue("preprocess", out var preprocessedObj) && 
            preprocessedObj is PreprocessingResult preprocessing)
        {
            content = preprocessing.ProcessedText;
        }

        var analysis = await mlOrchestrator.AnalyzeDocumentAsync(context.Input.DocumentId, content);
        return new SentimentResult(analysis.Sentiment, DateTime.UtcNow);
    }
}

public class TopicExtractionStep : IPipelineStep
{
    private readonly IMLOrchestrator mlOrchestrator;

    public TopicExtractionStep(IMLOrchestrator mlOrchestrator)
    {
        mlOrchestrator = mlOrchestrator;
    }

    public async Task<object> ExecuteAsync(PipelineExecutionContext context, Dictionary<string, object> previousResults)
    {
        var content = context.Input.Content;
        var topicCount = context.Input.Options?.GetValueOrDefault("topicCount", 5) ?? 5;
        
        return await mlOrchestrator.ExtractTopicsAsync(content, topicCount);
    }
}

public class SummarizationStep : IPipelineStep
{
    private readonly IMLOrchestrator mlOrchestrator;

    public SummarizationStep(IMLOrchestrator mlOrchestrator)
    {
        mlOrchestrator = mlOrchestrator;
    }

    public async Task<object> ExecuteAsync(PipelineExecutionContext context, Dictionary<string, object> previousResults)
    {
        var content = context.Input.Content;
        var summaryTypes = context.Input.Options?.GetValueOrDefault("summaryTypes", new[] { "executive", "detailed" });
        
        var options = new SummarizationOptions { SummaryTypes = summaryTypes };
        return await mlOrchestrator.GenerateSummariesAsync(content, options);
    }
}
```

## Service Configuration and DI

### Service Registration

```csharp
namespace MLService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMLServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register ML.NET context
        services.AddSingleton(_ => new MLContext(seed: 42));
        
        // Register Azure AI Services
        services.AddSingleton<TextAnalyticsClient>(provider =>
        {
            var endpoint = configuration.GetConnectionString("text-analytics");
            return new TextAnalyticsClient(new Uri(endpoint), new DefaultAzureCredential());
        });
        
        services.AddSingleton<OpenAIClient>(provider =>
        {
            var endpoint = configuration.GetConnectionString("openai");
            return new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        });

        // Register Redis cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("ml-cache");
        });

        // Register ML services
        services.AddScoped<IMLOrchestrator, MLOrchestrator>();
        services.AddScoped<IMLPipelineOrchestrator, MLPipelineOrchestrator>();
        services.AddScoped<IModelManager, ModelManager>();
        
        // Register pipeline steps
        services.AddScoped<TextPreprocessingStep>();
        services.AddScoped<SentimentAnalysisStep>();
        services.AddScoped<EntityExtractionStep>();
        services.AddScoped<TopicExtractionStep>();
        services.AddScoped<SummarizationStep>();
        services.AddScoped<ClassificationStep>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<MLServiceHealthCheck>("ml-service")
            .AddCheck<ModelHealthCheck>("ml-models")
            .AddAzureTextAnalytics(options =>
            {
                options.Endpoint = configuration.GetConnectionString("text-analytics");
            });

        return services;
    }
}
```

### Configuration Classes

```csharp
namespace MLService.Configuration;

public class MLServiceOptions
{
    public const string SectionName = "MLService";
    
    public string ModelPath { get; set; } = "./models";
    public int MaxConcurrentRequests { get; set; } = 10;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
    public PipelineConfiguration Pipelines { get; set; } = new();
}

public class ModelConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool AutoLoad { get; set; } = true;
}

public class PipelineConfiguration
{
    public int DefaultTimeoutMinutes { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
    public Dictionary<string, object> DefaultOptions { get; set; } = new();
}
```

## Performance Monitoring and Health Checks

### ML Performance Metrics

```csharp
namespace MLService.Monitoring;

public interface IMLMetricsCollector
{
    void RecordPipelineExecution(string pipelineId, TimeSpan duration, bool success);
    void RecordModelPrediction(string modelName, TimeSpan duration, double confidence);
    void RecordCacheHit(string cacheKey);
    Task<MLPerformanceReport> GenerateReportAsync(TimeSpan period);
}

public class MLMetricsCollector : IMLMetricsCollector
{
    private readonly IMetrics metrics;
    private readonly Counter<long> pipelineExecutions;
    private readonly Histogram<double> pipelineDuration;
    private readonly Counter<long> cacheHits;
    private readonly Histogram<double> modelConfidence;

    public MLMetricsCollector(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MLService");
        
        pipelineExecutions = meter.CreateCounter<long>("ml.pipeline.executions.total",
            description: "Total number of pipeline executions");
        
        pipelineDuration = meter.CreateHistogram<double>("ml.pipeline.duration.seconds",
            description: "Pipeline execution duration in seconds");
        
        cacheHits = meter.CreateCounter<long>("ml.cache.hits.total",
            description: "Total number of cache hits");
        
        modelConfidence = meter.CreateHistogram<double>("ml.model.confidence",
            description: "Model prediction confidence scores");
    }

    public void RecordPipelineExecution(string pipelineId, TimeSpan duration, bool success)
    {
        pipelineExecutions.Add(1, 
            new KeyValuePair<string, object?>("pipeline_id", pipelineId),
            new KeyValuePair<string, object?>("success", success));
        
        pipelineDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("pipeline_id", pipelineId));
    }

    public void RecordModelPrediction(string modelName, TimeSpan duration, double confidence)
    {
        modelConfidence.Record(confidence,
            new KeyValuePair<string, object?>("model_name", modelName));
    }

    public void RecordCacheHit(string cacheKey)
    {
        cacheHits.Add(1,
            new KeyValuePair<string, object?>("cache_key_prefix", cacheKey.Split(':')[0]));
    }
}

public class MLServiceHealthCheck : IHealthCheck
{
    private readonly IMLOrchestrator orchestrator;
    private readonly IDistributedCache cache;

    public MLServiceHealthCheck(IMLOrchestrator orchestrator, IDistributedCache cache)
    {
        orchestrator = orchestrator;
        cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic ML functionality
            var testResult = await orchestrator.AnalyzeDocumentAsync("health-check", "test content");
            
            // Test cache connectivity
            await cache.SetStringAsync("health-check", "ok", cancellationToken);
            var cacheResult = await cache.GetStringAsync("health-check", cancellationToken);
            
            if (cacheResult == "ok")
            {
                return HealthCheckResult.Healthy("ML Service is functioning correctly");
            }
            
            return HealthCheckResult.Degraded("Cache connectivity issues");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ML Service health check failed", ex);
        }
    }
}
```

## Best Practices

### Resource Management

- **Connection pooling** - Reuse HTTP clients for Azure AI Services
- **Model caching** - Keep frequently used ML.NET models in memory
- **Request throttling** - Implement rate limiting for expensive operations
- **Batch processing** - Group similar requests for efficiency

### Error Handling

- **Retry policies** - Implement exponential backoff for transient failures
- **Circuit breakers** - Protect against cascading failures in ML services
- **Graceful degradation** - Provide fallback responses when services are unavailable
- **Timeout management** - Set appropriate timeouts for ML operations

### Performance Optimization

- **Asynchronous processing** - Use async/await throughout ML pipelines
- **Parallel execution** - Process independent ML tasks concurrently
- **Result caching** - Cache expensive ML predictions with appropriate TTL
- **Model optimization** - Use ONNX runtime for faster inference

## Related Patterns

- [Orleans Integration](orleans-integration.md) - Integrating with Orleans actors
- [Service Orchestration](service-orchestration.md) - General service coordination
- [Document Pipeline Architecture](document-pipeline-architecture.md) - End-to-end workflows
- [ML.NET Patterns](../mlnet/readme.md) - Detailed ML.NET implementation patterns

---

**Key Benefits**: Centralized ML orchestration, scalable service architecture, integrated monitoring, flexible pipeline configuration

**When to Use**: Coordinating multiple ML models, building complex document processing workflows, managing ML service dependencies

**Performance**: Parallel processing, intelligent caching, resource optimization, monitoring and alerting