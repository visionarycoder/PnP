# Enterprise GraphQL Performance Optimization

**Description**: Advanced enterprise-grade performance optimization strategies for HotChocolate GraphQL including intelligent query analysis, multi-layer distributed caching, advanced batching patterns, comprehensive monitoring, and automated performance tuning for high-scale production environments.

**Language/Technology**: C#, HotChocolate, .NET 9.0, Redis, Application Insights, Performance Monitoring
**Enterprise Features**: Intelligent query optimization, distributed caching, performance monitoring, automated tuning, SLA management, and comprehensive analytics

## Code

### Query Complexity Analysis

```csharp
namespace DocumentProcessor.GraphQL.Performance;

using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

// Custom complexity analyzer
public class DocumentComplexityAnalyzer : IComplexityAnalyzer
{
    private readonly ILogger<DocumentComplexityAnalyzer> logger;
    private readonly ComplexityAnalyzerSettings settings;

    public DocumentComplexityAnalyzer(
        ILogger<DocumentComplexityAnalyzer> logger,
        ComplexityAnalyzerSettings settings)
    {
logger = logger;
settings = settings;
    }

    public ComplexityAnalyzerResult Analyze(
        IRequestContext context,
        int maximumAllowed)
    {
        var complexity = 0;
        var multipliers = new Dictionary<string, int>();
        
        // Analyze query structure
        var visitor = new ComplexityVisitor(settings);
        visitor.Visit(context.Document, context.Variables);
        
        complexity = visitor.Complexity;
        multipliers = visitor.Multipliers;
logger.LogDebug("Query complexity: {Complexity}, Max allowed: {MaxAllowed}", 
            complexity, maximumAllowed);

        if (complexity > maximumAllowed)
        {
            var errorMessage = $"Query complexity {complexity} exceeds maximum allowed {maximumAllowed}";
logger.LogWarning(errorMessage);
            
            return ComplexityAnalyzerResult.TooComplex(
                complexity, maximumAllowed, errorMessage);
        }

        return ComplexityAnalyzerResult.Ok(complexity, multipliers);
    }
}

public class ComplexityAnalyzerSettings
{
    public int DefaultFieldComplexity { get; set; } = 1;
    public int DefaultIntrospectionComplexity { get; set; } = 1000;
    public Dictionary<string, int> FieldComplexities { get; set; } = new();
    public Dictionary<string, int> TypeComplexities { get; set; } = new();
    
    public ComplexityAnalyzerSettings()
    {
        // Configure field-specific complexities
        FieldComplexities["documents"] = 10;
        FieldComplexities["searchDocuments"] = 50;
        FieldComplexities["similarDocuments"] = 100;
        FieldComplexities["processingResults"] = 20;
        FieldComplexities["analyticsData"] = 200;
        
        // Configure type complexities
        TypeComplexities["Document"] = 2;
        TypeComplexities["ProcessingResult"] = 5;
        TypeComplexities["AnalyticsData"] = 10;
    }
}

// Query depth limiter
public class QueryDepthLimiter
{
    private readonly int maxDepth;
    private readonly ILogger<QueryDepthLimiter> logger;

    public QueryDepthLimiter(int maxDepth, ILogger<QueryDepthLimiter> logger)
    {
maxDepth = maxDepth;
logger = logger;
    }

    public ValidationResult ValidateDepth(IDocument document)
    {
        var visitor = new DepthAnalysisVisitor();
        visitor.Visit(document, null);
        
        if (visitor.MaxDepth > maxDepth)
        {
            var error = $"Query depth {visitor.MaxDepth} exceeds maximum allowed {maxDepth}";
logger.LogWarning(error);
            return ValidationResult.Error(error);
        }
logger.LogDebug("Query depth: {Depth}, Max allowed: {MaxDepth}", 
            visitor.MaxDepth, maxDepth);
            
        return ValidationResult.Success(visitor.MaxDepth);
    }
}
```

### Advanced Caching Strategies

```csharp
// Multi-level caching service
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
}

public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache memoryCache;
    private readonly IDistributedCache distributedCache;
    private readonly ILogger<HybridCacheService> logger;
    private readonly JsonSerializerOptions jsonOptions;

    public HybridCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<HybridCacheService> logger)
    {
memoryCache = memoryCache;
distributedCache = distributedCache;
logger = logger;
jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Try memory cache first (fastest)
        if (memoryCache.TryGetValue(key, out T? cachedValue))
        {
logger.LogDebug("Cache hit (memory): {Key}", key);
            return cachedValue;
        }

        // Try distributed cache (slower but shared)
        var distributedValue = await distributedCache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(distributedValue))
        {
logger.LogDebug("Cache hit (distributed): {Key}", key);
            
            var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, jsonOptions);
            
            // Store in memory cache for faster future access
memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
            
            return deserializedValue;
        }
logger.LogDebug("Cache miss: {Key}", key);
        return default(T);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var defaultExpiry = expiry ?? TimeSpan.FromMinutes(30);
        
        // Set in memory cache
memoryCache.Set(key, value, TimeSpan.FromMinutes(Math.Min(5, defaultExpiry.Minutes)));
        
        // Set in distributed cache
        var serializedValue = JsonSerializer.Serialize(value, jsonOptions);
        await distributedCache.SetStringAsync(key, serializedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = defaultExpiry
        }, cancellationToken);
logger.LogDebug("Cache set: {Key} (expiry: {Expiry})", key, defaultExpiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
memoryCache.Remove(key);
        await distributedCache.RemoveAsync(key, cancellationToken);
logger.LogDebug("Cache removed: {Key}", key);
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Implementation would depend on your distributed cache provider
        // Redis example would use SCAN with pattern matching
logger.LogDebug("Cache pattern removed: {Pattern}", pattern);
        await Task.CompletedTask;
    }
}

// Query result caching middleware
public class QueryResultCacheMiddleware
{
    private readonly RequestDelegate next;
    private readonly ICacheService cacheService;
    private readonly ILogger<QueryResultCacheMiddleware> logger;

    public QueryResultCacheMiddleware(
        RequestDelegate next,
        ICacheService cacheService,
        ILogger<QueryResultCacheMiddleware> logger)
    {
next = next;
cacheService = cacheService;
logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldCache(context))
        {
            await next(context);
            return;
        }

        var cacheKey = GenerateCacheKey(context);
        
        // Try to get cached response
        var cachedResponse = await cacheService.GetAsync<CachedGraphQLResponse>(cacheKey);
        if (cachedResponse != null)
        {
logger.LogDebug("Returning cached GraphQL response for key: {CacheKey}", cacheKey);
            
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse.Json);
            return;
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await next(context);

        // Cache successful responses
        if (context.Response.StatusCode == 200 && responseBody.Length > 0)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            
            await cacheService.SetAsync(cacheKey, new CachedGraphQLResponse
            {
                Json = responseText,
                StatusCode = context.Response.StatusCode,
                CachedAt = DateTime.UtcNow
            }, TimeSpan.FromMinutes(10));
logger.LogDebug("Cached GraphQL response for key: {CacheKey}", cacheKey);
        }

        // Copy response back to original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    private bool ShouldCache(HttpContext context)
    {
        return context.Request.Method == "POST" && 
               context.Request.Path.StartsWithSegments("/graphql") &&
               !context.Request.Headers.ContainsKey("Cache-Control");
    }

    private string GenerateCacheKey(HttpContext context)
    {
        // Generate cache key based on query, variables, and user context
        var queryHash = GenerateQueryHash(context);
        var userContext = GetUserContext(context);
        
        return $"gql:{queryHash}:{userContext}";
    }

    private string GenerateQueryHash(HttpContext context)
    {
        // Implementation to hash the GraphQL query and variables
        return "query-hash"; // Simplified
    }

    private string GetUserContext(HttpContext context)
    {
        // Include user-specific context that affects the response
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var roles = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));
        
        return $"{userId}:{roles}";
    }
}

public class CachedGraphQLResponse
{
    public string Json { get; set; } = "";
    public int StatusCode { get; set; }
    public DateTime CachedAt { get; set; }
}
```

### Connection Pooling and Database Optimization

```csharp
// Optimized repository with connection pooling
public class OptimizedDocumentRepository : IDocumentRepository
{
    private readonly IDbContextFactory<DocumentContext> contextFactory;
    private readonly ILogger<OptimizedDocumentRepository> logger;
    private readonly IMemoryCache cache;

    public OptimizedDocumentRepository(
        IDbContextFactory<DocumentContext> contextFactory,
        ILogger<OptimizedDocumentRepository> logger,
        IMemoryCache cache)
    {
contextFactory = contextFactory;
logger = logger;
cache = cache;
    }

    public async Task<IQueryable<Document>> GetDocumentsAsync(
        DocumentFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.Documents.AsNoTracking();

        if (filter != null)
        {
            query = ApplyFilter(query, filter);
        }

        // Use compiled queries for frequently executed queries
        return query;
    }

    public async Task<Document?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cacheKey = $"document:{id}";
        if (cache.TryGetValue(cacheKey, out Document? cachedDocument))
        {
            return cachedDocument;
        }

        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var document = await context.Documents
            .AsNoTracking()
            .Include(d => d.Metadata)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        // Cache for 15 minutes
        if (document != null)
        {
cache.Set(cacheKey, document, TimeSpan.FromMinutes(15));
        }

        return document;
    }

    public async Task<IEnumerable<Document>> GetByIdsAsync(
        IEnumerable<string> ids, 
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var documents = new List<Document>();
        var uncachedIds = new List<string>();

        // Check cache for each ID
        foreach (var id in idList)
        {
            var cacheKey = $"document:{id}";
            if (cache.TryGetValue(cacheKey, out Document? cachedDocument) && cachedDocument != null)
            {
                documents.Add(cachedDocument);
            }
            else
            {
                uncachedIds.Add(id);
            }
        }

        // Load uncached documents
        if (uncachedIds.Any())
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            
            var uncachedDocuments = await context.Documents
                .AsNoTracking()
                .Include(d => d.Metadata)
                .Where(d => uncachedIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            // Cache newly loaded documents
            foreach (var document in uncachedDocuments)
            {
                var cacheKey = $"document:{document.Id}";
cache.Set(cacheKey, document, TimeSpan.FromMinutes(15));
                documents.Add(document);
            }
        }

        return documents;
    }

    private IQueryable<Document> ApplyFilter(IQueryable<Document> query, DocumentFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.Title))
        {
            query = query.Where(d => EF.Functions.Like(d.Title, $"%{filter.Title}%"));
        }

        if (filter.AuthorIds?.Any() == true)
        {
            query = query.Where(d => filter.AuthorIds.Contains(d.Metadata.AuthorId));
        }

        if (filter.CreatedAfter.HasValue)
        {
            query = query.Where(d => d.Metadata.CreatedAt >= filter.CreatedAfter.Value);
        }

        if (filter.Tags?.Any() == true)
        {
            query = query.Where(d => d.Metadata.Tags.Any(t => filter.Tags.Contains(t)));
        }

        return query;
    }
}

// Compiled queries for performance
public static class CompiledQueries
{
    public static readonly Func<DocumentContext, string, Task<Document?>> GetDocumentById =
        EF.CompileAsyncQuery((DocumentContext context, string id) =>
            context.Documents
                .Include(d => d.Metadata)
                .FirstOrDefault(d => d.Id == id));

    public static readonly Func<DocumentContext, string, IAsyncEnumerable<Document>> GetDocumentsByAuthor =
        EF.CompileAsyncQuery((DocumentContext context, string authorId) =>
            context.Documents
                .Where(d => d.Metadata.AuthorId == authorId)
                .OrderByDescending(d => d.Metadata.CreatedAt));

    public static readonly Func<DocumentContext, DateTime, int, IAsyncEnumerable<Document>> GetRecentDocuments =
        EF.CompileAsyncQuery((DocumentContext context, DateTime since, int limit) =>
            context.Documents
                .Where(d => d.Metadata.CreatedAt >= since)
                .OrderByDescending(d => d.Metadata.CreatedAt)
                .Take(limit));
}
```

### Performance Monitoring and Metrics

```csharp
// GraphQL performance monitoring
public class GraphQLPerformanceMonitor : IRequestInterceptor
{
    private readonly ILogger<GraphQLPerformanceMonitor> logger;
    private readonly IMetrics metrics;
    private readonly DiagnosticSource diagnosticSource;

    public GraphQLPerformanceMonitor(
        ILogger<GraphQLPerformanceMonitor> logger,
        IMetrics metrics,
        DiagnosticSource diagnosticSource)
    {
logger = logger;
metrics = metrics;
diagnosticSource = diagnosticSource;
    }

    public async ValueTask OnCreateAsync(CreateRequestContext context, RequestDelegate next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = GetOperationName(context.Request);
        
        try
        {
            await next(context);
            
            stopwatch.Stop();
            
            // Log performance metrics
logger.LogInformation(
                "GraphQL operation {OperationName} completed in {Duration}ms",
                operationName, stopwatch.ElapsedMilliseconds);

            // Record metrics
metrics.Measure.Timer.Time(
                "graphql.operation.duration",
                stopwatch.Elapsed,
                new MetricTags("operation", operationName));

            if (context.Result is IQueryResult queryResult)
            {
metrics.Measure.Counter.Increment(
                    "graphql.operation.count",
                    new MetricTags("operation", operationName, "status", "success"));

                if (queryResult.Errors?.Count > 0)
                {
metrics.Measure.Counter.Increment(
                        "graphql.errors.count",
                        queryResult.Errors.Count,
                        new MetricTags("operation", operationName));
                }
            }

            // Emit diagnostic events
diagnosticSource.Write("GraphQL.OperationCompleted", new
            {
                OperationName = operationName,
                Duration = stopwatch.Elapsed,
                Success = context.Result?.Errors?.Count == 0
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
logger.LogError(ex,
                "GraphQL operation {OperationName} failed after {Duration}ms",
                operationName, stopwatch.ElapsedMilliseconds);
metrics.Measure.Counter.Increment(
                "graphql.operation.count",
                new MetricTags("operation", operationName, "status", "error"));

            throw;
        }
    }

    private string GetOperationName(IRequestContext request)
    {
        return request.Request.OperationName ?? "unknown";
    }
}

// DataLoader performance monitoring
public class MonitoredDataLoader<TKey, TValue> : BatchDataLoader<TKey, TValue> where TKey : notnull
{
    private readonly ILogger<MonitoredDataLoader<TKey, TValue>> logger;
    private readonly IMetrics metrics;
    private readonly string dataLoaderName;

    public MonitoredDataLoader(
        IBatchScheduler batchScheduler,
        ILogger<MonitoredDataLoader<TKey, TValue>> logger,
        IMetrics metrics,
        string dataLoaderName) : base(batchScheduler)
    {
logger = logger;
metrics = metrics;
dataLoaderName = dataLoaderName;
    }

    protected override async Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await LoadBatchInternalAsync(keys, cancellationToken);
            
            stopwatch.Stop();
logger.LogDebug(
                "DataLoader {DataLoader} loaded {Count} items in {Duration}ms",
dataLoaderName, keys.Count, stopwatch.ElapsedMilliseconds);
metrics.Measure.Timer.Time(
                "dataloader.batch.duration",
                stopwatch.Elapsed,
                new MetricTags("dataloader", dataLoaderName));
metrics.Measure.Histogram.Update(
                "dataloader.batch.size",
                keys.Count,
                new MetricTags("dataloader", dataLoaderName));

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
logger.LogError(ex,
                "DataLoader {DataLoader} failed to load {Count} items after {Duration}ms",
dataLoaderName, keys.Count, stopwatch.ElapsedMilliseconds);
metrics.Measure.Counter.Increment(
                "dataloader.errors.count",
                new MetricTags("dataloader", dataLoaderName));

            throw;
        }
    }

    protected virtual async Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchInternalAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        // Override in derived classes
        throw new NotImplementedException("Override LoadBatchInternalAsync in derived class");
    }
}
```

### Memory and Resource Optimization

```csharp
// Memory-efficient document streaming
public class StreamingDocumentRepository : IDocumentRepository
{
    private readonly IDbContextFactory<DocumentContext> contextFactory;
    private readonly ILogger<StreamingDocumentRepository> logger;

    public StreamingDocumentRepository(
        IDbContextFactory<DocumentContext> contextFactory,
        ILogger<StreamingDocumentRepository> logger)
    {
contextFactory = contextFactory;
logger = logger;
    }

    public async IAsyncEnumerable<Document> StreamDocumentsAsync(
        DocumentFilter? filter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.Documents.AsNoTracking().AsAsyncEnumerable();

        if (filter != null)
        {
            query = query.Where(d => MatchesFilter(d, filter));
        }

        var count = 0;
        await foreach (var document in query.WithCancellation(cancellationToken))
        {
            // Periodic garbage collection for large result sets
            if (++count % 1000 == 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
logger.LogDebug("Processed {Count} documents, triggered GC", count);
            }

            yield return document;
        }
logger.LogDebug("Streamed {TotalCount} documents", count);
    }

    public async Task<PagedResult<Document>> GetPagedDocumentsAsync(
        int offset,
        int limit,
        DocumentFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.Documents.AsNoTracking();

        if (filter != null)
        {
            query = ApplyFilter(query, filter);
        }

        // Get total count efficiently
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paged results
        var documents = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<Document>
        {
            Items = documents,
            TotalCount = totalCount,
            Offset = offset,
            Limit = limit
        };
    }

    private bool MatchesFilter(Document document, DocumentFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.Title) && 
            !document.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.AuthorIds?.Any() == true && 
            !filter.AuthorIds.Contains(document.Metadata.AuthorId))
        {
            return false;
        }

        if (filter.CreatedAfter.HasValue && 
            document.Metadata.CreatedAt < filter.CreatedAfter.Value)
        {
            return false;
        }

        return true;
    }
}

// Object pooling for frequently allocated objects
public class ObjectPooling
{
    private readonly ObjectPool<StringBuilder> stringBuilderPool;
    private readonly ObjectPool<List<string>> stringListPool;
    private readonly ObjectPool<Dictionary<string, object>> dictionaryPool;

    public ObjectPooling(ObjectPoolProvider objectPoolProvider)
    {
stringBuilderPool = objectPoolProvider.CreateStringBuilderPool();
stringListPool = objectPoolProvider.Create<List<string>>(new StringListPoolPolicy());
dictionaryPool = objectPoolProvider.Create<Dictionary<string, object>>(new DictionaryPoolPolicy());
    }

    public string BuildCacheKey(string prefix, params string[] parts)
    {
        var sb =stringBuilderPool.Get();
        try
        {
            sb.Append(prefix);
            foreach (var part in parts)
            {
                sb.Append(':');
                sb.Append(part);
            }
            return sb.ToString();
        }
        finally
        {
stringBuilderPool.Return(sb);
        }
    }

    public List<string> GetStringList()
    {
        return stringListPool.Get();
    }

    public void ReturnStringList(List<string> list)
    {
stringListPool.Return(list);
    }

    public Dictionary<string, object> GetDictionary()
    {
        return dictionaryPool.Get();
    }

    public void ReturnDictionary(Dictionary<string, object> dictionary)
    {
dictionaryPool.Return(dictionary);
    }
}

public class StringListPoolPolicy : IPooledObjectPolicy<List<string>>
{
    public List<string> Create() => new();

    public bool Return(List<string> obj)
    {
        obj.Clear();
        return obj.Capacity <= 100; // Don't pool oversized lists
    }
}

public class DictionaryPoolPolicy : IPooledObjectPolicy<Dictionary<string, object>>
{
    public Dictionary<string, object> Create() => new();

    public bool Return(Dictionary<string, object> obj)
    {
        obj.Clear();
        return obj.Count <= 50; // Don't pool oversized dictionaries
    }
}
```

### Performance Configuration

```csharp
// Performance-optimized GraphQL configuration
public static class PerformanceConfiguration
{
    public static IServiceCollection AddOptimizedGraphQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Entity Framework for performance
        services.AddDbContextFactory<DocumentContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(3);
            });
            
            // Optimize for read-heavy workloads
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        });

        // Configure memory cache
        services.Configure<MemoryCacheOptions>(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.25;
        });

        // Configure Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "DocumentProcessor";
        });

        // Add object pooling
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.AddSingleton<ObjectPooling>();

        // Add caching services
        services.AddSingleton<ICacheService, HybridCacheService>();

        // Configure GraphQL server
        services
            .AddGraphQLServer()
            .AddQueryType<DocumentQueries>()
            .AddMutationType<DocumentMutations>()
            .AddSubscriptionType<DocumentSubscriptions>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .ModifyRequestOptions(opt =>
            {
                opt.MaxExecutionDepth = 15;
                opt.MaxOperationComplexity = 1000;
                opt.UseComplexityAnalyzer = true;
                opt.ExecutionTimeout = TimeSpan.FromSeconds(30);
                opt.IncludeExceptionDetails = true;
            })
            .ModifyValidationOptions(opt =>
            {
                opt.MaxAllowedRules = 100;
                opt.MaxAllowedExecutionDepth = 15;
            })
            .AddInstrumentation(opt =>
            {
                opt.RenameRootActivity = true;
                opt.RequestDetails = RequestInstrumentationScope.All;
            })
            .AddDiagnosticEventListener<GraphQLPerformanceMonitor>()
            .UseRequest<GraphQLPerformanceMonitor>()
            .UseComplexityAnalysis()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();

        return services;
    }

    public static IApplicationBuilder UseOptimizedGraphQL(this IApplicationBuilder app)
    {
        // Add performance monitoring middleware
        app.UseMiddleware<QueryResultCacheMiddleware>();
        
        return app;
    }
}
```

## Usage

### Performance Monitoring Query

```graphql
# Monitor query performance with custom directives
query GetDocumentsWithPerformanceTracking {
  documents(first: 100) @cached(ttl: 300) {
    nodes {
      id
      title
      
      # This field uses DataLoader for efficient loading
      author @defer {
        id
        name
      }
      
      # Complex field with caching
      processingResults @cached(ttl: 600) {
        type
        confidence
      }
      
      # Expensive computation with timeout
      analytics @timeout(seconds: 10) {
        readingTime
        complexityScore
      }
    }
  }
}

# Performance-optimized search query
query OptimizedDocumentSearch($term: String!, $limit: Int = 20) {
  searchDocuments(term: $term, limit: $limit) {
    # Use projections to limit data transfer
    id
    title
    snippet
    
    # Defer expensive fields
    fullContent @defer
    
    # Batch load related data
    author {
      id
      name
    }
  }
}
```

## Notes

- **Query Analysis**: Implement complexity and depth analysis to prevent expensive queries
- **Caching Layers**: Use multi-level caching (memory, distributed, CDN) for optimal performance
- **Connection Pooling**: Configure database connection pooling for concurrent requests
- **Batching**: Use DataLoaders to batch database queries and reduce N+1 problems
- **Memory Management**: Implement object pooling and streaming for large datasets
- **Monitoring**: Add comprehensive performance monitoring and alerting
- **Resource Limits**: Set appropriate timeouts and resource limits
- **Database Optimization**: Use compiled queries, proper indexing, and read replicas

## Related Patterns

- [DataLoader Patterns](dataloader-patterns.md) - Efficient data loading strategies
- [Query Patterns](query-patterns.md) - Optimized query implementations
- [Schema Design](schema-design.md) - Performance-conscious schema design

---

**Key Benefits**: Query optimization, efficient caching, resource management, performance monitoring

**When to Use**: High-traffic applications, complex data relationships, performance-critical systems

**Performance**: Complexity analysis, multi-level caching, connection pooling, batch optimization