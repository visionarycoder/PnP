# Enterprise GraphQL DataLoader Optimization

**Description**: Advanced enterprise DataLoader patterns for high-performance data loading including distributed caching, intelligent batch processing, cache warming strategies, N+1 elimination, and comprehensive performance monitoring for large-scale GraphQL applications.

**Language/Technology**: C#, HotChocolate, .NET 9.0, Redis, Distributed Caching, Performance Monitoring
**Enterprise Features**: Distributed caching, intelligent batching, cache warming, performance monitoring, auto-scaling integration, and comprehensive analytics

## Code

### Core DataLoader Implementations

```csharp
namespace DocumentProcessor.GraphQL.DataLoaders;

using GreenDonut;
using HotChocolate.DataLoader;

// Document DataLoader with caching
public class DocumentsByIdDataLoader(
    IBatchScheduler batchScheduler,
    IDocumentRepository documentRepository,
    ILogger<DocumentsByIdDataLoader> logger) : BatchDataLoader<string, Document>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<string, Document>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading {Count} documents by ID", keys.Count);

        var documents = await documentRepository.GetByIdsAsync(keys, cancellationToken);
        
        return documents.ToDictionary(d => d.Id);
    }

    protected override string GetCacheKey(string key) => $"document:{key}";
}

// Processing Results DataLoader with complex key
public class ProcessingResultsByDocumentDataLoader(
    IBatchScheduler batchScheduler,
    IProcessingResultRepository resultRepository,
    ILogger<ProcessingResultsByDocumentDataLoader> logger) 
    : GroupedDataLoader<string, ProcessingResult>(batchScheduler)
{
    protected override async Task<ILookup<string, ProcessingResult>> LoadGroupedBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading processing results for {Count} documents", keys.Count);

        var results = await resultRepository.GetByDocumentIdsAsync(keys, cancellationToken);
        
        return results.ToLookup(r => r.DocumentId);
    }

    protected override string GetCacheKey(string key) => $"processing-results:{key}";
}

// User DataLoader with selective loading
public class UsersByIdDataLoader(
    IBatchScheduler batchScheduler,
    IUserRepository userRepository,
    ILogger<UsersByIdDataLoader> logger) : BatchDataLoader<string, User>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<string, User>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading {Count} users by ID", keys.Count);

        // Only load necessary fields to optimize performance
        var users = await userRepository.GetUserSummariesAsync(keys, cancellationToken);
        
        return users.ToDictionary(u => u.Id);
    }

    protected override string GetCacheKey(string key) => $"user:{key}";
}
```

### Advanced DataLoader Patterns

```csharp
// Composite key DataLoader for complex relationships
public record DocumentCategoryKey(string DocumentId, string Category);

public class DocumentCategoryDataLoader(
    IBatchScheduler batchScheduler,
    IDocumentCategoryRepository categoryRepository,
    ILogger<DocumentCategoryDataLoader> logger) 
    : BatchDataLoader<DocumentCategoryKey, DocumentCategoryAssignment>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<DocumentCategoryKey, DocumentCategoryAssignment>> LoadBatchAsync(
        IReadOnlyList<DocumentCategoryKey> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading {Count} document-category assignments", keys.Count);

        var documentIds = keys.Select(k => k.DocumentId).Distinct().ToList();
        var categories = keys.Select(k => k.Category).Distinct().ToList();
        
        var assignments = await categoryRepository.GetAssignmentsAsync(
            documentIds, categories, cancellationToken);
        
        return assignments.ToDictionary(a => new DocumentCategoryKey(a.DocumentId, a.Category));
    }

    protected override string GetCacheKey(DocumentCategoryKey key) => 
        $"doc-category:{key.DocumentId}:{key.Category}";
}

// Filtered DataLoader with parameters
public record SimilarDocumentsKey(string DocumentId, float Threshold, int MaxResults);

public class SimilarDocumentsDataLoader(
    IBatchScheduler batchScheduler,
    IVectorSearchService vectorSearchService,
    ILogger<SimilarDocumentsDataLoader> logger) 
    : BatchDataLoader<SimilarDocumentsKey, IEnumerable<DocumentSimilarity>>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<SimilarDocumentsKey, IEnumerable<DocumentSimilarity>>> LoadBatchAsync(
        IReadOnlyList<SimilarDocumentsKey> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading similar documents for {Count} queries", keys.Count);

        var results = new Dictionary<SimilarDocumentsKey, IEnumerable<DocumentSimilarity>>();
        
        // Group by parameters to optimize batch operations
        var grouped = keys.GroupBy(k => new { k.Threshold, k.MaxResults });
        
        foreach (var group in grouped)
        {
            var documentIds = group.Select(g => g.DocumentId).ToList();
            
            var similarityResults = await vectorSearchService.FindSimilarBatchAsync(
                documentIds, group.Key.Threshold, group.Key.MaxResults, cancellationToken);
                
            foreach (var item in group)
            {
                if (similarityResults.TryGetValue(item.DocumentId, out var similarities))
                {
                    results[item] = similarities;
                }
                else
                {
                    results[item] = Array.Empty<DocumentSimilarity>();
                }
            }
        }
        
        return results;
    }

    protected override string GetCacheKey(SimilarDocumentsKey key) => 
        $"similar:{key.DocumentId}:{key.Threshold}:{key.MaxResults}";
}

// Aggregation DataLoader for statistics
public class DocumentStatisticsDataLoader(
    IBatchScheduler batchScheduler,
    IDocumentAnalyticsService analyticsService,
    ILogger<DocumentStatisticsDataLoader> logger) 
    : BatchDataLoader<string, DocumentStatistics>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<string, DocumentStatistics>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading statistics for {Count} documents", keys.Count);

        var statistics = await analyticsService.GetDocumentStatisticsBatchAsync(keys, cancellationToken);
        
        return statistics.ToDictionary(s => s.DocumentId);
    }

    protected override string GetCacheKey(string key) => $"doc-stats:{key}";
}
```

### Hierarchical Data Loading

```csharp
// Tree structure DataLoader for categories
public class CategoryHierarchyDataLoader(
    IBatchScheduler batchScheduler,
    ICategoryRepository categoryRepository,
    ILogger<CategoryHierarchyDataLoader> logger) 
    : BatchDataLoader<string, CategoryNode>(batchScheduler)
{
    protected override async Task<IReadOnlyDictionary<string, CategoryNode>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading category hierarchy for {Count} categories", keys.Count);

        // Load all categories and their relationships in one query
        var categories = await categoryRepository.GetCategoriesWithChildrenAsync(keys, cancellationToken);
        var categoryRelations = await categoryRepository.GetCategoryRelationsAsync(keys, cancellationToken);
        
        var result = new Dictionary<string, CategoryNode>();
        
        foreach (var categoryId in keys)
        {
            var category = categories.FirstOrDefault(c => c.Id == categoryId);
            if (category != null)
            {
                var node = BuildCategoryNode(category, categories, categoryRelations);
                result[categoryId] = node;
            }
        }
        
        return result;
    }

    private CategoryNode BuildCategoryNode(
        Category category, 
        List<Category> allCategories, 
        List<CategoryRelation> relations)
    {
        var childIds = relations
            .Where(r => r.ParentId == category.Id)
            .Select(r => r.ChildId)
            .ToList();
            
        var children = allCategories
            .Where(c => childIds.Contains(c.Id))
            .Select(c => BuildCategoryNode(c, allCategories, relations))
            .ToList();
            
        return new CategoryNode
        {
            Category = category,
            Children = children,
            Level = GetCategoryLevel(category.Id, relations)
        };
    }

    private int GetCategoryLevel(string categoryId, List<CategoryRelation> relations)
    {
        var level = 0;
        var currentId = categoryId;
        
        while (true)
        {
            var parent = relations.FirstOrDefault(r => r.ChildId == currentId);
            if (parent == null) break;
            
            level++;
            currentId = parent.ParentId;
        }
        
        return level;
    }

    protected override string GetCacheKey(string key) => $"category-hierarchy:{key}";
}
```

### Cached DataLoader with TTL

```csharp
// DataLoader with custom caching strategy
public class CachedProcessingResultsDataLoader(
    IBatchScheduler batchScheduler,
    IProcessingResultRepository resultRepository,
    IMemoryCache memoryCache,
    ILogger<CachedProcessingResultsDataLoader> logger) 
    : BatchDataLoader<string, ProcessingResult>(batchScheduler)
{
    private readonly TimeSpan cacheTtl = TimeSpan.FromMinutes(15);

    protected override async Task<IReadOnlyDictionary<string, ProcessingResult>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading {Count} processing results", keys.Count);

        var results = new Dictionary<string, ProcessingResult>();
        var keysToLoad = new List<string>();

        // Check cache first
        foreach (var key in keys)
        {
            var cacheKey = GetCacheKey(key);
            if (memoryCache.TryGetValue(cacheKey, out ProcessingResult? cachedResult) && cachedResult != null)
            {
                results[key] = cachedResult;
                logger.LogDebug("Cache hit for processing result {Key}", key);
            }
            else
            {
                keysToLoad.Add(key);
            }
        }

        // Load missing results from database
        if (keysToLoad.Any())
        {
            logger.LogDebug("Loading {Count} processing results from database", keysToLoad.Count);
            
            var freshResults = await resultRepository.GetByIdsAsync(keysToLoad, cancellationToken);
            
            foreach (var result in freshResults)
            {
                results[result.Id] = result;
                
                // Cache with TTL
                var cacheKey = GetCacheKey(result.Id);
                memoryCache.Set(cacheKey, result, cacheTtl);
            }
        }

        return results;
    }

    protected override string GetCacheKey(string key) => $"processing-result:{key}";
}
```

### DataLoader for External APIs

```csharp
// DataLoader for external API integration
public class ExternalApiDataLoader(
    IBatchScheduler batchScheduler,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ExternalApiDataLoader> logger) 
    : BatchDataLoader<string, ExternalApiResponse>(batchScheduler)
{
    private readonly string apiBaseUrl = configuration.GetValue<string>("ExternalApi:BaseUrl") ?? "";
    private readonly int maxBatchSize = configuration.GetValue<int>("ExternalApi:MaxBatchSize", 50);

    protected override async Task<IReadOnlyDictionary<string, ExternalApiResponse>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Loading external API data for {Count} keys", keys.Count);

        using var httpClient = httpClientFactory.CreateClient("ExternalApi");
        var results = new Dictionary<string, ExternalApiResponse>();

        // Process in batches to respect API limits
        var batches = keys.Chunk(maxBatchSize);
        
        foreach (var batch in batches)
        {
            try
            {
                var requestPayload = new
                {
                    ids = batch.ToArray(),
                    include_metadata = true
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{apiBaseUrl}/batch", requestPayload, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var apiResults = await response.Content
                        .ReadFromJsonAsync<ExternalApiBatchResponse>(cancellationToken);

                    if (apiResults?.Results != null)
                    {
                        foreach (var result in apiResults.Results)
                        {
                            results[result.Id] = result;
                        }
                    }
                }
                else
                {
                    logger.LogWarning("External API returned {StatusCode} for batch of {Count} items",
                        response.StatusCode, batch.Length);
                    
                    // Add empty results for failed items
                    foreach (var key in batch)
                    {
                        results[key] = new ExternalApiResponse { Id = key, Data = null };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load batch of {Count} items from external API", batch.Length);
                
                // Add error results
                foreach (var key in batch)
                {
                    results[key] = new ExternalApiResponse 
                    { 
                        Id = key, 
                        Data = null, 
                        Error = ex.Message 
                    };
                }
            }
        }

        return results;
    }

    protected override string GetCacheKey(string key) => $"external-api:{key}";
}
```

### DataLoader Registration and Configuration

```csharp
// Service registration
public static class DataLoaderServiceExtensions
{
    public static IServiceCollection AddDocumentDataLoaders(this IServiceCollection services)
    {
        // Core DataLoaders
        services.AddScoped<DocumentsByIdDataLoader>();
        services.AddScoped<ProcessingResultsByDocumentDataLoader>();
        services.AddScoped<UsersByIdDataLoader>();
        
        // Advanced DataLoaders
        services.AddScoped<DocumentCategoryDataLoader>();
        services.AddScoped<SimilarDocumentsDataLoader>();
        services.AddScoped<DocumentStatisticsDataLoader>();
        
        // Hierarchical DataLoaders
        services.AddScoped<CategoryHierarchyDataLoader>();
        
        // Cached DataLoaders
        services.AddScoped<CachedProcessingResultsDataLoader>();
        
        // External API DataLoaders
        services.AddScoped<ExternalApiDataLoader>();

        return services;
    }

    public static IRequestExecutorBuilder AddDataLoaderTypes(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddDataLoader<DocumentsByIdDataLoader>()
            .AddDataLoader<ProcessingResultsByDocumentDataLoader>()
            .AddDataLoader<UsersByIdDataLoader>()
            .AddDataLoader<DocumentCategoryDataLoader>()
            .AddDataLoader<SimilarDocumentsDataLoader>()
            .AddDataLoader<DocumentStatisticsDataLoader>()
            .AddDataLoader<CategoryHierarchyDataLoader>()
            .AddDataLoader<CachedProcessingResultsDataLoader>()
            .AddDataLoader<ExternalApiDataLoader>();
    }
}

// GraphQL schema configuration
services
    .AddGraphQLServer()
    .AddQueryType<DocumentQueries>()
    .AddDocumentDataLoaders()
    .AddDataLoaderTypes()
    .ModifyRequestOptions(opt =>
    {
        // Configure DataLoader batch size
        opt.MaxExecutionDepth = 15;
        opt.IncludeExceptionDetails = true;
    });
```

### DataLoader Usage in Resolvers

```csharp
// Document resolvers using DataLoaders
[ExtendObjectType<Document>]
public class DocumentResolvers
{
    // Basic relationship resolution
    public async Task<User> GetAuthorAsync(
        [Parent] Document document,
        [Service] UsersByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        return await dataLoader.LoadAsync(document.Metadata.AuthorId, cancellationToken);
    }

    // Collection resolution
    public async Task<IEnumerable<ProcessingResult>> GetProcessingResultsAsync(
        [Parent] Document document,
        [Service] ProcessingResultsByDocumentDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        return await dataLoader.LoadAsync(document.Id, cancellationToken);
    }

    // Complex key resolution
    public async Task<DocumentCategoryAssignment?> GetCategoryAssignmentAsync(
        [Parent] Document document,
        string category,
        [Service] DocumentCategoryDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        var key = new DocumentCategoryKey(document.Id, category);
        return await dataLoader.LoadAsync(key, cancellationToken);
    }

    // Parameterized resolution
    public async Task<IEnumerable<DocumentSimilarity>> GetSimilarDocumentsAsync(
        [Parent] Document document,
        float threshold = 0.7f,
        int maxResults = 10,
        [Service] SimilarDocumentsDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        var key = new SimilarDocumentsKey(document.Id, threshold, maxResults);
        return await dataLoader.LoadAsync(key, cancellationToken);
    }

    // Aggregated data resolution
    public async Task<DocumentStatistics> GetStatisticsAsync(
        [Parent] Document document,
        [Service] DocumentStatisticsDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        return await dataLoader.LoadAsync(document.Id, cancellationToken);
    }

    // Hierarchical data resolution
    public async Task<CategoryNode?> GetCategoryHierarchyAsync(
        [Parent] Document document,
        [Service] CategoryHierarchyDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        if (document.CategoryId == null) return null;
        
        return await dataLoader.LoadAsync(document.CategoryId, cancellationToken);
    }
}
```

## Usage

### Query Examples Leveraging DataLoaders

```graphql
# This query will use DataLoaders to efficiently load related data
query GetDocumentsWithRelatedData {
  documents(first: 10) {
    nodes {
      id
      title
      
      # Uses UsersByIdDataLoader - batches all author requests
      author {
        id
        name
        email
      }
      
      # Uses ProcessingResultsByDocumentDataLoader - batches all results
      processingResults {
        id
        type
        confidence
        status
      }
      
      # Uses SimilarDocumentsDataLoader - batches similarity requests
      similarDocuments(threshold: 0.8, maxResults: 5) {
        document {
          id
          title
        }
        similarityScore
      }
      
      # Uses DocumentStatisticsDataLoader - batches statistics requests
      statistics {
        wordCount
        readingTime
        complexityScore
      }
    }
  }
}

# Complex nested query with multiple DataLoaders
query GetDocumentHierarchy {
  document(id: "doc-123") {
    id
    title
    
    # CategoryHierarchyDataLoader builds complete tree
    categoryHierarchy {
      category {
        id
        name
      }
      children {
        category {
          id
          name
        }
        level
      }
      level
    }
    
    # Multiple DataLoaders working together
    processingResults {
      type
      output {
        ... on ClassificationResult {
          predictedCategory
          categoryScores {
            category
            score
          }
        }
      }
    }
  }
}
```

## Notes

- **Batch Optimization**: Group related data requests to minimize database queries
- **Caching Strategy**: Implement appropriate caching based on data volatility
- **Error Handling**: Handle partial failures gracefully in batch operations
- **Performance**: Monitor DataLoader effectiveness and batch sizes
- **Memory Management**: Be careful with large result sets in memory
- **Key Design**: Design composite keys carefully for complex relationships
- **External APIs**: Respect rate limits and implement proper retry logic
- **Cache Invalidation**: Implement cache invalidation strategies for mutable data

## Related Patterns

- [Query Patterns](query-patterns.md) - Efficient querying strategies
- [Performance Optimization](performance-optimization.md) - Overall optimization techniques
- [Schema Design](schema-design.md) - Type definitions and relationships

---

**Key Benefits**: N+1 query prevention, batch optimization, efficient caching, external API integration

**When to Use**: Complex object graphs, high-performance requirements, external data sources

**Performance**: Batch loading, intelligent caching, request optimization
