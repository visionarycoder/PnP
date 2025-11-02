# GraphQL Query Patterns

**Description**: Advanced GraphQL query patterns for document processing, filtering, pagination, and complex data retrieval using HotChocolate.

**Language/Technology**: C# / HotChocolate

## Code

### Complex Document Queries

```csharp
namespace DocumentProcessor.GraphQL.Queries;

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public class DocumentQueries
{
    // Basic document retrieval with projection
    [UseProjection]
    public IQueryable<Document> GetDocuments([Service] IDocumentRepository repository) =>
        repository.GetQueryable();

    // Advanced filtering with custom predicates
    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Document> SearchDocuments(
        [Service] IDocumentRepository repository,
        DocumentSearchInput? search = null)
    {
        var query = repository.GetQueryable();

        if (search != null)
        {
            if (!string.IsNullOrEmpty(search.FullTextQuery))
            {
                query = query.Where(d => 
                    EF.Functions.Contains(d.Content, search.FullTextQuery) ||
                    EF.Functions.Contains(d.Title, search.FullTextQuery));
            }

            if (search.MinConfidence.HasValue)
            {
                query = query.Where(d => d.ProcessingResults
                    .Any(r => r.Confidence >= search.MinConfidence));
            }

            if (search.ProcessingTypes?.Any() == true)
            {
                query = query.Where(d => d.ProcessingResults
                    .Any(r => search.ProcessingTypes.Contains(r.ProcessingType)));
            }
        }

        return query;
    }

    // Semantic search with vector similarity
    public async Task<IEnumerable<DocumentSimilarity>> FindSimilarDocumentsAsync(
        string documentId,
        float threshold = 0.7f,
        int maxResults = 20,
        [Service] IVectorSearchService vectorSearch,
        CancellationToken cancellationToken)
    {
        return await vectorSearch.FindSimilarAsync(
            documentId, threshold, maxResults, cancellationToken);
    }

    // Aggregated analytics queries
    public async Task<DocumentAnalytics> GetDocumentAnalyticsAsync(
        AnalyticsTimeRange timeRange,
        AnalyticsFilters? filters,
        [Service] IAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        return await analyticsService.GetDocumentAnalyticsAsync(
            timeRange, filters ?? new AnalyticsFilters(), cancellationToken);
    }

    // Topic modeling queries
    [UsePaging]
    public async Task<Connection<TopicCluster>> GetTopicClustersAsync(
        TopicAnalysisOptions options,
        [Service] ITopicAnalysisService topicService,
        CancellationToken cancellationToken)
    {
        var clusters = await topicService.GetTopicClustersAsync(options, cancellationToken);
        return clusters.ToConnection();
    }

    // Real-time processing queue status
    public async Task<ProcessingQueueStatus> GetProcessingQueueStatusAsync(
        [Service] IProcessingQueueService queueService,
        CancellationToken cancellationToken)
    {
        return await queueService.GetQueueStatusAsync(cancellationToken);
    }
}
```

### Advanced Filtering Implementation

```csharp
// Custom filter input types
[InputType]
public class DocumentSearchInput
{
    public string? FullTextQuery { get; set; }
    
    public float? MinConfidence { get; set; }
    
    public List<ProcessingType>? ProcessingTypes { get; set; }
    
    public DateRange? CreatedDateRange { get; set; }
    
    public List<string>? Authors { get; set; }
    
    public List<string>? Categories { get; set; }
    
    public SentimentRange? SentimentRange { get; set; }
    
    public Dictionary<string, string>? MetadataFilters { get; set; }
}

[InputType]
public class DateRange
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

[InputType]
public class SentimentRange
{
    public SentimentClass? MinSentiment { get; set; }
    public SentimentClass? MaxSentiment { get; set; }
    public float? MinScore { get; set; }
    public float? MaxScore { get; set; }
}

// Custom filter conventions
public class DocumentFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        
        descriptor
            .Operation(DefaultFilterOperations.Equals)
            .Name("eq");
            
        descriptor
            .Operation(DefaultFilterOperations.Contains)
            .Name("contains");
            
        descriptor
            .Operation(DefaultFilterOperations.GreaterThan)
            .Name("gt");
            
        descriptor
            .Operation(DefaultFilterOperations.LowerThan)
            .Name("lt");
            
        // Custom operations
        descriptor
            .Operation(CustomFilterOperations.VectorSimilarity)
            .Name("similar");
            
        descriptor
            .Operation(CustomFilterOperations.FullTextSearch)
            .Name("search");
    }
}

// Custom filter operations
public static class CustomFilterOperations
{
    public const int VectorSimilarity = 1000;
    public const int FullTextSearch = 1001;
}
```

### Pagination Patterns

```csharp
// Cursor-based pagination with custom implementation
public class DocumentConnection : Connection<Document>
{
    public DocumentConnection(
        IReadOnlyList<Edge<Document>> edges,
        ConnectionPageInfo pageInfo,
        Func<CancellationToken, ValueTask<int>>? getTotalCount = null)
        : base(edges, pageInfo, getTotalCount)
    {
    }
    
    // Additional connection metadata
    public DocumentConnectionMetadata Metadata { get; set; } = new();
}

public class DocumentConnectionMetadata
{
    public Dictionary<ProcessingStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
    public TimeSpan AverageProcessingTime { get; set; }
    public DateTime QueryExecutedAt { get; set; } = DateTime.UtcNow;
}

// Custom pagination resolver
public class DocumentPaginationResolver
{
    public async Task<DocumentConnection> ResolveAsync(
        IResolverContext context,
        IQueryable<Document> source,
        int? first,
        string? after,
        int? last,
        string? before,
        CancellationToken cancellationToken)
    {
        var connection = await source.ApplyCursorPaginationAsync(
            context, first, after, last, before, cancellationToken);

        // Add metadata
        var metadata = new DocumentConnectionMetadata
        {
            StatusDistribution = await source
                .GroupBy(d => d.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken),
            AverageProcessingTime = TimeSpan.FromSeconds(
                await source.AverageAsync(d => d.ProcessingResults
                    .Where(r => r.CompletedAt.HasValue)
                    .Average(r => (r.CompletedAt!.Value - r.StartedAt).TotalSeconds), 
                    cancellationToken))
        };

        return new DocumentConnection(connection.Edges, connection.PageInfo)
        {
            Metadata = metadata
        };
    }
}
```

### Complex Query Examples

```csharp
// Multi-level filtering and aggregation
[ExtendObjectType<Query>]
public class DocumentAnalyticsQueries
{
    // Sentiment trend analysis
    public async Task<List<SentimentTrend>> GetSentimentTrendsAsync(
        AnalyticsTimeRange timeRange,
        string? category,
        [Service] IAnalyticsService analytics,
        CancellationToken cancellationToken)
    {
        return await analytics.GetSentimentTrendsAsync(
            timeRange, category, cancellationToken);
    }

    // Processing performance metrics
    public async Task<ProcessingMetrics> GetProcessingMetricsAsync(
        AnalyticsTimeRange timeRange,
        List<ProcessingType>? processingTypes,
        [Service] IAnalyticsService analytics,
        CancellationToken cancellationToken)
    {
        return await analytics.GetProcessingMetricsAsync(
            timeRange, processingTypes, cancellationToken);
    }

    // Content analysis by categories
    public async Task<List<CategoryAnalysis>> AnalyzeCategoriesAsync(
        AnalyticsTimeRange timeRange,
        int topN = 10,
        [Service] IContentAnalysisService contentAnalysis,
        CancellationToken cancellationToken)
    {
        return await contentAnalysis.GetTopCategoriesAsync(
            timeRange, topN, cancellationToken);
    }

    // Document clustering analysis
    public async Task<ClusteringResult> GetDocumentClustersAsync(
        ClusteringOptions options,
        [Service] IClusteringService clusteringService,
        CancellationToken cancellationToken)
    {
        return await clusteringService.PerformClusteringAsync(
            options, cancellationToken);
    }
}

// Supporting types for complex queries
[ObjectType]
public class SentimentTrend
{
    public DateTime Date { get; set; }
    public Dictionary<SentimentClass, int> SentimentCounts { get; set; } = new();
    public float AverageScore { get; set; }
    public int TotalDocuments { get; set; }
}

[ObjectType]
public class ProcessingMetrics
{
    public TimeSpan AverageProcessingTime { get; set; }
    public Dictionary<ProcessingType, ProcessingTypeMetrics> TypeMetrics { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public float SuccessRate => TotalProcessed > 0 ? (float)SuccessCount / TotalProcessed : 0;
}

[ObjectType]
public class ProcessingTypeMetrics
{
    public ProcessingType Type { get; set; }
    public TimeSpan AverageTime { get; set; }
    public float AverageConfidence { get; set; }
    public int Count { get; set; }
    public Dictionary<string, float> ModelPerformance { get; set; } = new();
}

[ObjectType]
public class CategoryAnalysis
{
    public string Category { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public float AverageConfidence { get; set; }
    public Dictionary<SentimentClass, int> SentimentDistribution { get; set; } = new();
    public List<string> TopKeywords { get; set; } = new();
    public float GrowthRate { get; set; }
}

[ObjectType]
public class ClusteringResult
{
    public List<DocumentCluster> Clusters { get; set; } = new();
    public float SilhouetteScore { get; set; }
    public int OptimalClusterCount { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

[ObjectType]
public class DocumentCluster
{
    public int ClusterId { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public List<string> DocumentIds { get; set; } = new();
    public List<string> KeyTerms { get; set; } = new();
    public float Coherence { get; set; }
    public Dictionary<string, float> CentroidVector { get; set; } = new();
}
```

### Query Optimization Techniques

```csharp
// Optimized field resolvers with caching
[ExtendObjectType<Document>]
public class DocumentResolvers
{
    // Cached expensive computations
    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, WordCloudData>> GetWordCloudsAsync(
        IReadOnlyList<string> documentIds,
        [Service] IWordCloudService wordCloudService,
        CancellationToken cancellationToken)
    {
        return await wordCloudService.GenerateBatchAsync(documentIds, cancellationToken);
    }

    // Selective field resolution
    public async Task<ProcessingResult?> GetLatestProcessingResultAsync(
        [Parent] Document document,
        ProcessingType? type,
        [Service] IProcessingResultRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository.GetLatestByDocumentAsync(
            document.Id, type, cancellationToken);
    }

    // Conditional field loading
    public async Task<List<RelatedDocument>> GetRelatedDocumentsAsync(
        [Parent] Document document,
        int maxResults = 5,
        float minSimilarity = 0.7f,
        [Service] IVectorSearchService vectorSearch,
        CancellationToken cancellationToken)
    {
        if (document.Status != ProcessingStatus.Completed)
            return new List<RelatedDocument>();

        var similar = await vectorSearch.FindSimilarAsync(
            document.Id, minSimilarity, maxResults, cancellationToken);

        return similar.Select(s => new RelatedDocument
        {
            Document = s.Document,
            SimilarityScore = s.SimilarityScore,
            Reason = s.SimilarityReason
        }).ToList();
    }
}
```

## Usage

### Basic Query Examples

```graphql
# Simple document query with projection
query GetDocuments {
  documents(first: 10) {
    nodes {
      id
      title
      status
      createdAt
    }
    pageInfo {
      hasNextPage
      endCursor
    }
    totalCount
  }
}

# Complex filtering
query SearchDocuments {
  searchDocuments(
    search: {
      fullTextQuery: "machine learning"
      minConfidence: 0.8
      processingTypes: [CLASSIFICATION, SENTIMENT]
      createdDateRange: {
        from: "2024-01-01"
        to: "2024-12-31"
      }
    }
    first: 20
  ) {
    nodes {
      id
      title
      processingResults {
        type
        confidence
        output {
          ... on ClassificationResult {
            predictedCategory
            categoryScores {
              category
              score
            }
          }
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
    }
    metadata {
      statusDistribution
      averageProcessingTime
    }
  }
}

# Analytics query
query GetAnalytics {
  documentAnalytics(
    timeRange: LAST_30_DAYS
    filters: {
      categories: ["research", "news"]
    }
  ) {
    totalDocuments
    processingMetrics {
      averageProcessingTime
      successRate
      typeMetrics {
        type
        averageTime
        averageConfidence
      }
    }
    sentimentDistribution
    topCategories {
      category
      documentCount
      growthRate
    }
  }
}
```

## Notes

- **Performance**: Use DataLoaders and projections to optimize query performance
- **Complexity**: Implement query complexity analysis to prevent expensive operations
- **Caching**: Cache expensive field resolvers and aggregated data
- **Pagination**: Always use cursor-based pagination for large datasets
- **Filtering**: Provide comprehensive filtering options while maintaining performance
- **Analytics**: Pre-compute expensive analytics queries where possible
- **Security**: Apply authorization checks at the field level for sensitive data

## Related Patterns

- [Schema Design](schema-design.md) - Type definitions and schema structure
- [DataLoader Patterns](dataloader-patterns.md) - Efficient data loading strategies
- [Performance Optimization](performance-optimization.md) - Query optimization techniques

---

**Key Benefits**: Flexible querying, efficient data retrieval, comprehensive filtering, analytics capabilities

**When to Use**: Complex document search, analytics dashboards, data exploration interfaces

**Performance**: DataLoader optimization, query caching, selective field loading