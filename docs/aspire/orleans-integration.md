# Enterprise Orleans Integration with .NET Aspire

**Description**: Advanced patterns for integrating Orleans virtual actor clusters with .NET Aspire orchestration including multi-cluster coordination, enterprise-scale actor management, cross-region replication, advanced persistence patterns, and comprehensive observability for mission-critical distributed systems.

**Technology**: .NET Aspire, Orleans 8.0, .NET 9.0, Azure Service Bus, Redis Clustering
**Enterprise Features**: Multi-cluster coordination, cross-region replication, advanced persistence, enterprise security, and comprehensive monitoring integration

## Overview

Orleans integration with Aspire provides seamless orchestration of Orleans clusters alongside other services in a distributed document processing pipeline. This combination enables auto-scaling virtual actors with centralized service management and observability.

## Key Integration Patterns

### App Host Configuration

```csharp
namespace DocumentProcessor.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Add Orleans cluster with Aspire orchestration
var orleans = builder.AddOrleans("document-cluster")
    .WithDashboard()
    .WithDevelopmentClustering()
    .PublishAsConnectionString();

// Add supporting services
var redis = builder.AddRedis("cache")
    .WithRedisInsight();

var postgres = builder.AddPostgres("docs-db", password: "dev-password")
    .WithPgAdmin()
    .AddDatabase("documents");

var serviceBus = builder.AddAzureServiceBus("messaging");

// Add Orleans silo projects
builder.AddProject<Projects.DocumentProcessor_Silo>("document-silo")
    .WithReference(orleans)
    .WithReference(redis)
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WithReplicas(2);

// Add client applications
builder.AddProject<Projects.DocumentProcessor_Api>("document-api")
    .WithReference(orleans)
    .WithReference(redis)
    .WithReference(postgres)
    .WithHttpsEndpoint(port: 7001, name: "https");

builder.Build().Run();
```

### Silo Configuration

```csharp
namespace DocumentProcessor.Silo;

using Orleans.Configuration;
using Orleans.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add Orleans silo with Aspire integration
builder.UseOrleans((context, siloBuilder) =>
{
    siloBuilder.ConfigureServices(services =>
    {
        // Aspire automatically configures clustering via connection string
        services.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "document-processing";
            options.ServiceId = "DocumentProcessor";
        });
        
        // Configure grain storage
        services.Configure<GrainStorageOptions>("documents", options =>
        {
            options.ConnectionString = context.Configuration.GetConnectionString("docs-db");
        });
    });
    
    // Add grain classes
    siloBuilder.ConfigureApplicationParts(parts =>
    {
        parts.AddApplicationPart(typeof(DocumentProcessorGrain).Assembly).WithReferences();
    });
    
    // Configure persistence
    siloBuilder.AddAdoNetGrainStorage("documents", options =>
    {
        options.ConnectionString = context.Configuration.GetConnectionString("docs-db");
        options.Invariant = "Npgsql";
    });
    
    // Add streaming providers
    siloBuilder.AddAzureServiceBusStreams("document-streams", configurator =>
    {
        configurator.ConfigureAzureServiceBus(options =>
        {
            options.ConnectionString = context.Configuration.GetConnectionString("messaging");
        });
    });
});

// Add application services
builder.Services.AddScoped<IMLService, OpenAIMLService>();
builder.Services.AddScoped<IDocumentStore, PostgresDocumentStore>();

var host = builder.Build();
await host.RunAsync();
```

### Client Configuration

```csharp
namespace DocumentProcessor.Api;

using Orleans.Configuration;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Orleans client with Aspire integration
builder.UseOrleansClient((context, clientBuilder) =>
{
    clientBuilder.ConfigureServices(services =>
    {
        services.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "document-processing";
            options.ServiceId = "DocumentProcessor";
        });
    });
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();
```

## Document Processing Grain Pattern

### Document Processor Grain

```csharp
namespace DocumentProcessor.Grains;

using Orleans;
using Orleans.Streams;
using Microsoft.Extensions.Logging;

[GenerateSerializer]
public record DocumentProcessingRequest(
    string DocumentId,
    string Content,
    Dictionary<string, string> Metadata,
    ProcessingOptions Options);

[GenerateSerializer]
public record ProcessingResult(
    string DocumentId,
    List<string> Keywords,
    Dictionary<string, double> Topics,
    List<string> Summaries,
    DateTime ProcessedAt);

public interface IDocumentProcessorGrain : IGrainWithStringKey
{
    Task<ProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request);
    Task<ProcessingResult?> GetProcessingResultAsync();
    Task ReprocessWithOptionsAsync(ProcessingOptions newOptions);
}

public class DocumentProcessorGrain : Grain, IDocumentProcessorGrain
{
    private readonly IPersistentState<DocumentState> state;
    private readonly IMLService mlService;
    private readonly ILogger<DocumentProcessorGrain> logger;
    private IAsyncStream<ProcessingResult>? resultStream;

    public DocumentProcessorGrain(
        [PersistentState("document", "documents")] IPersistentState<DocumentState> state,
        IMLService mlService,
        ILogger<DocumentProcessorGrain> logger)
    {
        state = state;
        mlService = mlService;
        logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider("document-streams");
        resultStream = streamProvider.GetStream<ProcessingResult>("results", this.GetPrimaryKeyString());
        
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<ProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request)
    {
        logger.LogInformation("Processing document {DocumentId}", request.DocumentId);

        try
        {
            // Extract keywords using ML service
            var keywords = await mlService.ExtractKeywordsAsync(request.Content);
            
            // Perform topic modeling
            var topics = await mlService.AnalyzeTopicsAsync(request.Content);
            
            // Generate summaries
            var summaries = await mlService.GenerateSummariesAsync(
                request.Content, 
                request.Options.SummaryTypes);

            var result = new ProcessingResult(
                request.DocumentId,
                keywords,
                topics,
                summaries,
                DateTime.UtcNow);

            // Update persistent state
            state.State.LastResult = result;
            state.State.ProcessingHistory.Add(result);
            await state.WriteStateAsync();

            // Publish result to stream
            if (resultStream != null)
            {
                await resultStream.OnNextAsync(result);
            }

            logger.LogInformation("Completed processing document {DocumentId}", request.DocumentId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public Task<ProcessingResult?> GetProcessingResultAsync()
    {
        return Task.FromResult(state.State.LastResult);
    }

    public async Task ReprocessWithOptionsAsync(ProcessingOptions newOptions)
    {
        if (state.State.OriginalRequest != null)
        {
            var updatedRequest = state.State.OriginalRequest with { Options = newOptions };
            await ProcessDocumentAsync(updatedRequest);
        }
    }
}

[GenerateSerializer]
public class DocumentState
{
    [Id(0)] public DocumentProcessingRequest? OriginalRequest { get; set; }
    [Id(1)] public ProcessingResult? LastResult { get; set; }
    [Id(2)] public List<ProcessingResult> ProcessingHistory { get; set; } = new();
}
```

### ML Coordinator Grain

```csharp
namespace DocumentProcessor.Grains;

using Orleans;
using Orleans.Concurrency;

[StatelessWorker]
public interface IMLCoordinatorGrain : IGrainWithIntegerKey
{
    Task<BatchProcessingResult> ProcessDocumentBatchAsync(List<string> documentIds);
    Task<ModelPerformanceMetrics> GetModelMetricsAsync();
}

[StatelessWorker]
public class MLCoordinatorGrain : Grain, IMLCoordinatorGrain
{
    private readonly ILogger<MLCoordinatorGrain> logger;

    public MLCoordinatorGrain(ILogger<MLCoordinatorGrain> logger)
    {
        logger = logger;
    }

    public async Task<BatchProcessingResult> ProcessDocumentBatchAsync(List<string> documentIds)
    {
        logger.LogInformation("Processing batch of {Count} documents", documentIds.Count);

        var processingTasks = documentIds.Select(async docId =>
        {
            var documentGrain = GrainFactory.GetGrain<IDocumentProcessorGrain>(docId);
            
            try
            {
                var result = await documentGrain.GetProcessingResultAsync();
                return new DocumentBatchItem(docId, true, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process document {DocumentId} in batch", docId);
                return new DocumentBatchItem(docId, false, null);
            }
        });

        var results = await Task.WhenAll(processingTasks);
        
        return new BatchProcessingResult(
            TotalCount: documentIds.Count,
            SuccessCount: results.Count(r => r.Success),
            FailureCount: results.Count(r => !r.Success),
            Results: results.ToList());
    }

    public async Task<ModelPerformanceMetrics> GetModelMetricsAsync()
    {
        // Implement performance tracking logic
        await Task.CompletedTask;
        
        return new ModelPerformanceMetrics(
            AverageProcessingTime: TimeSpan.FromSeconds(2.5),
            ThroughputPerMinute: 150,
            ErrorRate: 0.02);
    }
}
```

## API Integration Pattern

### Document Processing Controller

```csharp
namespace DocumentProcessor.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Orleans;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IClusterClient clusterClient;
    private readonly ILogger<DocumentsController> logger;

    public DocumentsController(IClusterClient clusterClient, ILogger<DocumentsController> logger)
    {
        clusterClient = clusterClient;
        logger = logger;
    }

    [HttpPost("{documentId}/process")]
    public async Task<ActionResult<ProcessingResult>> ProcessDocument(
        string documentId,
        [FromBody] DocumentProcessingRequest request)
    {
        try
        {
            var documentGrain = clusterClient.GetGrain<IDocumentProcessorGrain>(documentId);
            var result = await documentGrain.ProcessDocumentAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
            return StatusCode(500, "Processing failed");
        }
    }

    [HttpGet("{documentId}/result")]
    public async Task<ActionResult<ProcessingResult>> GetProcessingResult(string documentId)
    {
        var documentGrain = clusterClient.GetGrain<IDocumentProcessorGrain>(documentId);
        var result = await documentGrain.GetProcessingResultAsync();
        
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("batch/process")]
    public async Task<ActionResult<BatchProcessingResult>> ProcessBatch(
        [FromBody] List<string> documentIds)
    {
        var coordinatorGrain = clusterClient.GetGrain<IMLCoordinatorGrain>(0);
        var result = await coordinatorGrain.ProcessDocumentBatchAsync(documentIds);
        
        return Ok(result);
    }
}
```

## Development Workflow

### Local Development Setup

1. **Start Aspire App Host**:

   ```bash
   cd DocumentProcessor.AppHost
   dotnet run
   ```

2. **Access Development Dashboard**:
   - **Aspire Dashboard**: `https://localhost:15888`
   - **Orleans Dashboard**: `https://localhost:8080`
   - **Redis Insight**: `https://localhost:8001`
   - **pgAdmin**: `https://localhost:5050`

3. **Test Document Processing**:

   ```bash
   curl -X POST "https://localhost:7001/api/documents/doc-123/process" \
     -H "Content-Type: application/json" \
     -d '{
       "documentId": "doc-123",
       "content": "Sample document content for processing...",
       "metadata": { "source": "upload", "type": "text" },
       "options": { "summaryTypes": ["short", "detailed"] }
     }'
   ```

## Production Considerations

### Scaling Configuration

```csharp
// Production silo configuration
siloBuilder.Configure<SiloOptions>(options =>
{
    options.SiloName = Environment.MachineName;
});

siloBuilder.Configure<ClusterMembershipOptions>(options =>
{
    options.DefunctSiloExpiration = TimeSpan.FromMinutes(10);
    options.DefunctSiloCleanupPeriod = TimeSpan.FromMinutes(5);
});

// Resource limits
siloBuilder.Configure<LoadSheddingOptions>(options =>
{
    options.LoadSheddingEnabled = true;
    options.LoadSheddingLimit = 95;
});
```

### Health Monitoring

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<OrleansHealthCheck>("orleans-silo")
    .AddCheck<MLServiceHealthCheck>("ml-service")
    .AddCheck<DatabaseHealthCheck>("document-store");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## Best Practices

### Grain Design

- **Single responsibility** - Each grain type handles one document processing aspect
- **Immutable messages** - Use record types for grain method parameters
- **Persistent state** - Store processing results for recovery and querying
- **Stream integration** - Use Orleans Streams for event-driven workflows

### Performance Optimization

- **Stateless workers** - Use for CPU-intensive ML operations
- **Grain placement** - Configure placement strategies for data locality
- **Connection pooling** - Optimize database and external service connections
- **Caching strategy** - Cache frequently accessed processing results

### Error Handling

- **Graceful degradation** - Continue processing other documents on individual failures
- **Retry policies** - Implement exponential backoff for transient failures
- **Dead letter queues** - Handle permanently failed documents
- **Circuit breakers** - Protect against cascading failures in ML services

## Related Patterns

- [Service Orchestration](service-orchestration.md) - Coordinating multiple services
- [ML Service Coordination](ml-service-orchestration.md) - Managing ML workflows
- [Document Pipeline Architecture](document-pipeline-architecture.md) - End-to-end processing flow
- [Orleans Patterns](../orleans/readme.md) - Advanced Orleans patterns

---

**Key Benefits**: Scalable virtual actors, centralized orchestration, integrated observability, simplified local development

**When to Use**: Building document processing pipelines, coordinating ML workflows, scaling compute-intensive operations

**Performance**: Horizontal scaling with Orleans cluster, automatic load balancing, resource pooling