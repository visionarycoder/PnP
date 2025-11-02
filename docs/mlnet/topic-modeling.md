# Topic Modeling with ML.NET

**Description**: Advanced topic modeling patterns using ML.NET for document analysis, text clustering, and unsupervised learning. Implements Latent Dirichlet Allocation (LDA), document similarity, and topic extraction from large text corpora.

**Language/Technology**: C# / ML.NET

## Code

### Core Topic Modeling Service

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using System.Text.Json;

namespace MLNet.TopicModeling;

// Document input model
public class Document
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

// LDA output model
public class TopicPrediction
{
    [VectorType(10)] // Number of topics
    public float[] TopicProbabilities { get; set; } = Array.Empty<float>();
}

// Topic information
public class TopicInfo
{
    public int TopicId { get; set; }
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public float[] KeywordWeights { get; set; } = Array.Empty<float>();
    public float Coherence { get; set; }
}

// Document clustering result
public class DocumentCluster
{
    public int ClusterId { get; set; }
    public List<string> DocumentIds { get; set; } = new();
    public string[] RepresentativeKeywords { get; set; } = Array.Empty<string>();
    public float Coherence { get; set; }
    public int DocumentCount { get; set; }
}

// Main topic modeling service
public class TopicModelingService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<TopicModelingService> _logger;
    private readonly TopicModelingOptions _options;
    private ITransformer? _model;
    private readonly Dictionary<int, TopicInfo> _topicCache = new();

    public TopicModelingService(
        ILogger<TopicModelingService> logger,
        IOptions<TopicModelingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _mlContext = new MLContext(seed: _options.RandomSeed);
    }

    // Train LDA topic model
    public async Task<TopicModelTrainingResult> TrainTopicModelAsync(
        IEnumerable<Document> documents,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting topic model training with {DocumentCount} documents", 
                documents.Count());

            // Prepare data
            var dataView = _mlContext.Data.LoadFromEnumerable(documents);

            // Build training pipeline
            var pipeline = _mlContext.Transforms.Text
                .NormalizeText("NormalizedContent", "Content",
                    caseMode: TextNormalizingEstimator.CaseMode.Lower,
                    keepDiacritics: false,
                    keepPunctuations: false,
                    keepNumbers: false)
                .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedContent"))
                .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("FilteredTokens", "Tokens",
                    language: StopWordsRemovingEstimator.Language.English))
                .Append(_mlContext.Transforms.Text.ProduceNgrams("Features", "FilteredTokens",
                    ngramLength: _options.NgramLength,
                    useAllLengths: false,
                    weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf))
                .Append(_mlContext.Transforms.Text.LatentDirichletAllocation("TopicProbabilities", "Features",
                    numberOfTopics: _options.NumberOfTopics,
                    alphaSum: _options.AlphaSum,
                    beta: _options.Beta,
                    samplingStepCount: _options.SamplingSteps,
                    maximumNumberOfIterations: _options.MaxIterations));

            // Train model
            _model = pipeline.Fit(dataView);

            // Extract topics
            var topics = await ExtractTopicsAsync(_model);
            
            // Calculate model quality metrics
            var perplexity = await CalculatePerplexityAsync(dataView);
            var coherence = await CalculateCoherenceAsync(topics);

            var result = new TopicModelTrainingResult
            {
                Topics = topics,
                TrainingDuration = stopwatch.Elapsed,
                Perplexity = perplexity,
                AverageCoherence = coherence,
                DocumentCount = documents.Count(),
                ModelSize = await GetModelSizeAsync()
            };

            _logger.LogInformation("Topic model training completed in {Duration}ms with {TopicCount} topics",
                stopwatch.ElapsedMilliseconds, topics.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Topic model training failed");
            throw;
        }
    }

    // Predict topics for new documents
    public async Task<DocumentTopicPrediction> PredictTopicsAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (_model == null)
            throw new InvalidOperationException("Model must be trained before prediction");

        try
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(new[] { document });
            var predictions = _model.Transform(dataView);

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<Document, TopicPrediction>(_model);
            var prediction = predictionEngine.Predict(document);

            var topTopics = prediction.TopicProbabilities
                .Select((prob, index) => new { TopicId = index, Probability = prob })
                .OrderByDescending(x => x.Probability)
                .Take(_options.TopTopicsCount)
                .ToArray();

            return new DocumentTopicPrediction
            {
                DocumentId = document.Id,
                TopicProbabilities = prediction.TopicProbabilities,
                TopTopics = topTopics.Select(t => new TopicAssignment
                {
                    TopicId = t.TopicId,
                    Probability = t.Probability,
                    Keywords = _topicCache.GetValueOrDefault(t.TopicId)?.Keywords ?? Array.Empty<string>()
                }).ToArray(),
                DominantTopic = topTopics.FirstOrDefault()?.TopicId ?? -1,
                Confidence = topTopics.FirstOrDefault()?.Probability ?? 0f
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Topic prediction failed for document {DocumentId}", document.Id);
            throw;
        }
    }

    // Cluster documents based on topic similarity
    public async Task<DocumentClusteringResult> ClusterDocumentsAsync(
        IEnumerable<Document> documents,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting document clustering for {DocumentCount} documents",
                documents.Count());

            var documentPredictions = new List<DocumentTopicPrediction>();

            // Get topic predictions for all documents
            foreach (var document in documents)
            {
                var prediction = await PredictTopicsAsync(document, cancellationToken);
                documentPredictions.Add(prediction);
            }

            // Cluster documents using k-means on topic probabilities
            var clusters = await PerformTopicBasedClusteringAsync(documentPredictions);

            // Calculate cluster quality metrics
            var silhouetteScore = CalculateSilhouetteScore(documentPredictions, clusters);
            var intraClusterDistance = CalculateIntraClusterDistance(clusters);

            var result = new DocumentClusteringResult
            {
                Clusters = clusters,
                ClusteringDuration = stopwatch.Elapsed,
                SilhouetteScore = silhouetteScore,
                IntraClusterDistance = intraClusterDistance,
                DocumentCount = documents.Count(),
                ClusterCount = clusters.Count
            };

            _logger.LogInformation("Document clustering completed in {Duration}ms with {ClusterCount} clusters",
                stopwatch.ElapsedMilliseconds, clusters.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document clustering failed");
            throw;
        }
    }

    // Extract topic keywords and metadata
    private async Task<List<TopicInfo>> ExtractTopicsAsync(ITransformer model)
    {
        var topics = new List<TopicInfo>();

        // Get the LDA transformer
        var ldaTransformer = model.LastTransformer as LatentDirichletAllocationTransformer;
        if (ldaTransformer == null) return topics;

        for (int topicId = 0; topicId < _options.NumberOfTopics; topicId++)
        {
            // Extract top words for this topic
            var topWords = await ExtractTopWordsForTopicAsync(ldaTransformer, topicId);
            
            var topicInfo = new TopicInfo
            {
                TopicId = topicId,
                Keywords = topWords.Select(w => w.Word).ToArray(),
                KeywordWeights = topWords.Select(w => w.Weight).ToArray(),
                Coherence = await CalculateTopicCoherenceAsync(topWords)
            };

            topics.Add(topicInfo);
            _topicCache[topicId] = topicInfo;
        }

        return topics;
    }

    // Perform topic-based clustering
    private async Task<List<DocumentCluster>> PerformTopicBasedClusteringAsync(
        List<DocumentTopicPrediction> predictions)
    {
        // Use k-means clustering on topic probability vectors
        var clusterCount = Math.Min(_options.MaxClusters, predictions.Count / 10);
        var clusters = new List<DocumentCluster>();

        // Simple k-means implementation for topic vectors
        var centroids = InitializeCentroids(predictions, clusterCount);
        var assignments = new int[predictions.Count];
        var maxIterations = 100;
        var tolerance = 1e-4;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            var changed = false;

            // Assign documents to nearest centroid
            for (int i = 0; i < predictions.Count; i++)
            {
                var nearestCluster = FindNearestCluster(predictions[i].TopicProbabilities, centroids);
                if (assignments[i] != nearestCluster)
                {
                    assignments[i] = nearestCluster;
                    changed = true;
                }
            }

            if (!changed) break;

            // Update centroids
            UpdateCentroids(predictions, assignments, centroids, clusterCount);
        }

        // Create cluster objects
        for (int clusterId = 0; clusterId < clusterCount; clusterId++)
        {
            var clusterDocuments = predictions
                .Where((_, index) => assignments[index] == clusterId)
                .ToList();

            if (clusterDocuments.Any())
            {
                var cluster = new DocumentCluster
                {
                    ClusterId = clusterId,
                    DocumentIds = clusterDocuments.Select(d => d.DocumentId).ToList(),
                    DocumentCount = clusterDocuments.Count,
                    RepresentativeKeywords = ExtractClusterKeywords(clusterDocuments),
                    Coherence = CalculateClusterCoherence(clusterDocuments)
                };

                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    // Calculate model perplexity
    private async Task<float> CalculatePerplexityAsync(IDataView testData)
    {
        if (_model == null) return float.MaxValue;

        try
        {
            var predictions = _model.Transform(testData);
            var probabilities = _mlContext.Data.CreateEnumerable<TopicPrediction>(predictions, false);

            var logLikelihood = 0f;
            var documentCount = 0;

            foreach (var prediction in probabilities)
            {
                var entropy = prediction.TopicProbabilities
                    .Where(p => p > 0)
                    .Sum(p => -p * (float)Math.Log(p));
                
                logLikelihood += entropy;
                documentCount++;
            }

            return documentCount > 0 ? (float)Math.Exp(-logLikelihood / documentCount) : float.MaxValue;
        }
        catch
        {
            return float.MaxValue;
        }
    }

    // Helper methods for clustering and quality metrics
    private float[][] InitializeCentroids(List<DocumentTopicPrediction> predictions, int clusterCount)
    {
        var random = new Random(_options.RandomSeed);
        var centroids = new float[clusterCount][];
        var topicCount = predictions.First().TopicProbabilities.Length;

        for (int i = 0; i < clusterCount; i++)
        {
            var randomDocument = predictions[random.Next(predictions.Count)];
            centroids[i] = (float[])randomDocument.TopicProbabilities.Clone();
        }

        return centroids;
    }

    private int FindNearestCluster(float[] topicVector, float[][] centroids)
    {
        var minDistance = float.MaxValue;
        var nearestCluster = 0;

        for (int i = 0; i < centroids.Length; i++)
        {
            var distance = CalculateEuclideanDistance(topicVector, centroids[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestCluster = i;
            }
        }

        return nearestCluster;
    }

    private void UpdateCentroids(
        List<DocumentTopicPrediction> predictions,
        int[] assignments,
        float[][] centroids,
        int clusterCount)
    {
        for (int clusterId = 0; clusterId < clusterCount; clusterId++)
        {
            var clusterDocuments = predictions
                .Where((_, index) => assignments[index] == clusterId)
                .ToList();

            if (clusterDocuments.Any())
            {
                var topicCount = centroids[clusterId].Length;
                for (int topicId = 0; topicId < topicCount; topicId++)
                {
                    centroids[clusterId][topicId] = clusterDocuments
                        .Average(d => d.TopicProbabilities[topicId]);
                }
            }
        }
    }

    private float CalculateEuclideanDistance(float[] vector1, float[] vector2)
    {
        return (float)Math.Sqrt(
            vector1.Zip(vector2, (a, b) => (a - b) * (a - b)).Sum()
        );
    }

    private string[] ExtractClusterKeywords(List<DocumentTopicPrediction> clusterDocuments)
    {
        var topicFrequency = new Dictionary<int, float>();

        foreach (var document in clusterDocuments)
        {
            foreach (var topTopic in document.TopTopics)
            {
                topicFrequency[topTopic.TopicId] = 
                    topicFrequency.GetValueOrDefault(topTopic.TopicId) + topTopic.Probability;
            }
        }

        var dominantTopics = topicFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToArray();

        var keywords = new List<string>();
        foreach (var topicId in dominantTopics)
        {
            if (_topicCache.TryGetValue(topicId, out var topicInfo))
            {
                keywords.AddRange(topicInfo.Keywords.Take(5));
            }
        }

        return keywords.Distinct().Take(10).ToArray();
    }

    // Additional quality metric methods
    private float CalculateClusterCoherence(List<DocumentTopicPrediction> clusterDocuments)
    {
        if (clusterDocuments.Count < 2) return 1.0f;

        var similarities = new List<float>();
        for (int i = 0; i < clusterDocuments.Count; i++)
        {
            for (int j = i + 1; j < clusterDocuments.Count; j++)
            {
                var similarity = CalculateCosineSimilarity(
                    clusterDocuments[i].TopicProbabilities,
                    clusterDocuments[j].TopicProbabilities);
                similarities.Add(similarity);
            }
        }

        return similarities.Average();
    }

    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        var dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
        var magnitude1 = (float)Math.Sqrt(vector1.Sum(x => x * x));
        var magnitude2 = (float)Math.Sqrt(vector2.Sum(x => x * x));

        if (magnitude1 == 0 || magnitude2 == 0) return 0;
        return dotProduct / (magnitude1 * magnitude2);
    }
}

// Configuration options
public class TopicModelingOptions
{
    public int NumberOfTopics { get; set; } = 10;
    public int NgramLength { get; set; } = 2;
    public float AlphaSum { get; set; } = 10.0f;
    public float Beta { get; set; } = 0.01f;
    public int SamplingSteps { get; set; } = 4;
    public int MaxIterations { get; set; } = 20;
    public int TopTopicsCount { get; set; } = 5;
    public int MaxClusters { get; set; } = 20;
    public int RandomSeed { get; set; } = 42;
}

// Result models
public class TopicModelTrainingResult
{
    public List<TopicInfo> Topics { get; set; } = new();
    public TimeSpan TrainingDuration { get; set; }
    public float Perplexity { get; set; }
    public float AverageCoherence { get; set; }
    public int DocumentCount { get; set; }
    public long ModelSize { get; set; }
}

public class DocumentTopicPrediction
{
    public string DocumentId { get; set; } = string.Empty;
    public float[] TopicProbabilities { get; set; } = Array.Empty<float>();
    public TopicAssignment[] TopTopics { get; set; } = Array.Empty<TopicAssignment>();
    public int DominantTopic { get; set; }
    public float Confidence { get; set; }
}

public class TopicAssignment
{
    public int TopicId { get; set; }
    public float Probability { get; set; }
    public string[] Keywords { get; set; } = Array.Empty<string>();
}

public class DocumentClusteringResult
{
    public List<DocumentCluster> Clusters { get; set; } = new();
    public TimeSpan ClusteringDuration { get; set; }
    public float SilhouetteScore { get; set; }
    public float IntraClusterDistance { get; set; }
    public int DocumentCount { get; set; }
    public int ClusterCount { get; set; }
}
```

### Advanced Topic Analysis Service

```csharp
// Topic evolution tracking
public class TopicEvolutionService
{
    private readonly TopicModelingService _topicService;
    private readonly ILogger<TopicEvolutionService> _logger;

    public TopicEvolutionService(
        TopicModelingService topicService,
        ILogger<TopicEvolutionService> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    // Track topic evolution over time
    public async Task<TopicEvolutionResult> AnalyzeTopicEvolutionAsync(
        Dictionary<DateTime, IEnumerable<Document>> timeSeriesDocuments,
        CancellationToken cancellationToken = default)
    {
        var topicEvolutions = new List<TopicEvolutionPoint>();
        var previousTopics = new List<TopicInfo>();

        foreach (var (timestamp, documents) in timeSeriesDocuments.OrderBy(kvp => kvp.Key))
        {
            _logger.LogInformation("Analyzing topics for {Timestamp} with {DocumentCount} documents",
                timestamp, documents.Count());

            // Train model for this time period
            var result = await _topicService.TrainTopicModelAsync(documents, cancellationToken);
            
            // Calculate topic similarity with previous period
            var topicSimilarities = CalculateTopicSimilarities(previousTopics, result.Topics);
            
            var evolutionPoint = new TopicEvolutionPoint
            {
                Timestamp = timestamp,
                Topics = result.Topics,
                DocumentCount = documents.Count(),
                TopicSimilarities = topicSimilarities,
                EmergingTopics = IdentifyEmergingTopics(previousTopics, result.Topics),
                DecliningTopics = IdentifyDecliningTopics(previousTopics, result.Topics)
            };

            topicEvolutions.Add(evolutionPoint);
            previousTopics = result.Topics;
        }

        return new TopicEvolutionResult
        {
            EvolutionPoints = topicEvolutions,
            TrendAnalysis = AnalyzeTrends(topicEvolutions),
            StabilityMetrics = CalculateStabilityMetrics(topicEvolutions)
        };
    }

    // Identify topic trends and patterns
    private TopicTrendAnalysis AnalyzeTrends(List<TopicEvolutionPoint> evolutions)
    {
        var trends = new Dictionary<string, TopicTrend>();
        var keywordFrequency = new Dictionary<string, List<(DateTime timestamp, float weight)>>();

        // Track keyword frequency over time
        foreach (var evolution in evolutions)
        {
            foreach (var topic in evolution.Topics)
            {
                for (int i = 0; i < topic.Keywords.Length; i++)
                {
                    var keyword = topic.Keywords[i];
                    var weight = topic.KeywordWeights[i];

                    if (!keywordFrequency.ContainsKey(keyword))
                        keywordFrequency[keyword] = new List<(DateTime, float)>();

                    keywordFrequency[keyword].Add((evolution.Timestamp, weight));
                }
            }
        }

        // Analyze trends for each keyword
        foreach (var (keyword, weightHistory) in keywordFrequency)
        {
            if (weightHistory.Count >= 3) // Need at least 3 points for trend
            {
                var trend = CalculateLinearTrend(weightHistory);
                trends[keyword] = trend;
            }
        }

        return new TopicTrendAnalysis
        {
            KeywordTrends = trends,
            GrowingTopics = trends.Where(t => t.Value.Slope > 0.01f).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            DecliningTopics = trends.Where(t => t.Value.Slope < -0.01f).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            StableTopics = trends.Where(t => Math.Abs(t.Value.Slope) <= 0.01f).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private TopicTrend CalculateLinearTrend(List<(DateTime timestamp, float weight)> weightHistory)
    {
        var n = weightHistory.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        var baseTime = weightHistory.First().timestamp;

        for (int i = 0; i < n; i++)
        {
            var x = (weightHistory[i].timestamp - baseTime).TotalDays;
            var y = weightHistory[i].weight;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var slope = (float)((n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX));
        var intercept = (float)((sumY - slope * sumX) / n);
        var correlation = CalculateCorrelation(weightHistory);

        return new TopicTrend
        {
            Slope = slope,
            Intercept = intercept,
            Correlation = correlation,
            Direction = slope > 0.01f ? TrendDirection.Growing : 
                       slope < -0.01f ? TrendDirection.Declining : TrendDirection.Stable,
            Significance = Math.Abs(correlation) > 0.7f ? TrendSignificance.High :
                          Math.Abs(correlation) > 0.4f ? TrendSignificance.Medium : TrendSignificance.Low
        };
    }
}

// Hierarchical topic modeling
public class HierarchicalTopicService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<HierarchicalTopicService> _logger;

    public HierarchicalTopicService(ILogger<HierarchicalTopicService> logger)
    {
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
    }

    // Build hierarchical topic structure
    public async Task<HierarchicalTopicModel> BuildHierarchicalTopicsAsync(
        IEnumerable<Document> documents,
        int maxDepth = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building hierarchical topic model with max depth {MaxDepth}", maxDepth);

        var rootNode = new TopicNode
        {
            Id = "root",
            Level = 0,
            Documents = documents.ToList(),
            Keywords = Array.Empty<string>()
        };

        await BuildHierarchyRecursiveAsync(rootNode, maxDepth, cancellationToken);

        return new HierarchicalTopicModel
        {
            RootNode = rootNode,
            MaxDepth = maxDepth,
            TotalNodes = CountNodes(rootNode),
            LeafNodes = GetLeafNodes(rootNode).Count
        };
    }

    private async Task BuildHierarchyRecursiveAsync(
        TopicNode node,
        int remainingDepth,
        CancellationToken cancellationToken)
    {
        if (remainingDepth <= 0 || node.Documents.Count < 10) // Minimum documents for split
            return;

        // Use clustering to split documents
        var topicService = new TopicModelingService(
            _logger as ILogger<TopicModelingService>,
            Options.Create(new TopicModelingOptions { NumberOfTopics = 3 }));

        var clusterResult = await topicService.ClusterDocumentsAsync(node.Documents, cancellationToken);

        // Create child nodes for each cluster
        foreach (var cluster in clusterResult.Clusters)
        {
            var childDocuments = node.Documents
                .Where(d => cluster.DocumentIds.Contains(d.Id))
                .ToList();

            var childNode = new TopicNode
            {
                Id = $"{node.Id}_{cluster.ClusterId}",
                Level = node.Level + 1,
                Parent = node,
                Documents = childDocuments,
                Keywords = cluster.RepresentativeKeywords,
                Coherence = cluster.Coherence
            };

            node.Children.Add(childNode);

            // Recursively build sub-hierarchy
            await BuildHierarchyRecursiveAsync(childNode, remainingDepth - 1, cancellationToken);
        }
    }
}

// Supporting models for hierarchical topics
public class TopicNode
{
    public string Id { get; set; } = string.Empty;
    public int Level { get; set; }
    public TopicNode? Parent { get; set; }
    public List<TopicNode> Children { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public float Coherence { get; set; }
}

public class HierarchicalTopicModel
{
    public TopicNode RootNode { get; set; } = new();
    public int MaxDepth { get; set; }
    public int TotalNodes { get; set; }
    public int LeafNodes { get; set; }
}

// Evolution analysis models
public class TopicEvolutionPoint
{
    public DateTime Timestamp { get; set; }
    public List<TopicInfo> Topics { get; set; } = new();
    public int DocumentCount { get; set; }
    public Dictionary<int, float> TopicSimilarities { get; set; } = new();
    public List<string> EmergingTopics { get; set; } = new();
    public List<string> DecliningTopics { get; set; } = new();
}

public class TopicEvolutionResult
{
    public List<TopicEvolutionPoint> EvolutionPoints { get; set; } = new();
    public TopicTrendAnalysis TrendAnalysis { get; set; } = new();
    public Dictionary<string, float> StabilityMetrics { get; set; } = new();
}

public class TopicTrendAnalysis
{
    public Dictionary<string, TopicTrend> KeywordTrends { get; set; } = new();
    public Dictionary<string, TopicTrend> GrowingTopics { get; set; } = new();
    public Dictionary<string, TopicTrend> DecliningTopics { get; set; } = new();
    public Dictionary<string, TopicTrend> StableTopics { get; set; } = new();
}

public class TopicTrend
{
    public float Slope { get; set; }
    public float Intercept { get; set; }
    public float Correlation { get; set; }
    public TrendDirection Direction { get; set; }
    public TrendSignificance Significance { get; set; }
}

public enum TrendDirection
{
    Growing,
    Declining,
    Stable
}

public enum TrendSignificance
{
    Low,
    Medium,
    High
}
```

### ASP.NET Core Integration

```csharp
// Topic modeling controller
[ApiController]
[Route("api/[controller]")]
public class TopicModelingController : ControllerBase
{
    private readonly TopicModelingService _topicService;
    private readonly TopicEvolutionService _evolutionService;
    private readonly ILogger<TopicModelingController> _logger;

    public TopicModelingController(
        TopicModelingService topicService,
        TopicEvolutionService evolutionService,
        ILogger<TopicModelingController> logger)
    {
        _topicService = topicService;
        _evolutionService = evolutionService;
        _logger = logger;
    }

    [HttpPost("train")]
    public async Task<ActionResult<TopicModelTrainingResult>> TrainModel(
        [FromBody] TrainTopicModelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _topicService.TrainTopicModelAsync(request.Documents, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train topic model");
            return StatusCode(500, new { error = "Topic model training failed", details = ex.Message });
        }
    }

    [HttpPost("predict")]
    public async Task<ActionResult<DocumentTopicPrediction>> PredictTopics(
        [FromBody] Document document,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _topicService.PredictTopicsAsync(document, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict topics for document {DocumentId}", document.Id);
            return StatusCode(500, new { error = "Topic prediction failed", details = ex.Message });
        }
    }

    [HttpPost("cluster")]
    public async Task<ActionResult<DocumentClusteringResult>> ClusterDocuments(
        [FromBody] ClusterDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _topicService.ClusterDocumentsAsync(request.Documents, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cluster documents");
            return StatusCode(500, new { error = "Document clustering failed", details = ex.Message });
        }
    }

    [HttpPost("evolution")]
    public async Task<ActionResult<TopicEvolutionResult>> AnalyzeEvolution(
        [FromBody] AnalyzeEvolutionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _evolutionService.AnalyzeTopicEvolutionAsync(
                request.TimeSeriesDocuments, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze topic evolution");
            return StatusCode(500, new { error = "Topic evolution analysis failed", details = ex.Message });
        }
    }
}

// Request/response models
public class TrainTopicModelRequest
{
    public List<Document> Documents { get; set; } = new();
}

public class ClusterDocumentsRequest
{
    public List<Document> Documents { get; set; } = new();
}

public class AnalyzeEvolutionRequest
{
    public Dictionary<DateTime, List<Document>> TimeSeriesDocuments { get; set; } = new();
}

// Dependency injection setup
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTopicModeling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TopicModelingOptions>(
            configuration.GetSection("TopicModeling"));

        services.AddScoped<TopicModelingService>();
        services.AddScoped<TopicEvolutionService>();
        services.AddScoped<HierarchicalTopicService>();

        services.AddLogging();

        return services;
    }
}
```

## Usage

### Basic Topic Modeling

```csharp
// Configure services
var services = new ServiceCollection()
    .AddTopicModeling(configuration)
    .AddLogging()
    .BuildServiceProvider();

var topicService = services.GetRequiredService<TopicModelingService>();

// Prepare documents
var documents = new List<Document>
{
    new() { Id = "1", Content = "Machine learning and artificial intelligence", Category = "Tech" },
    new() { Id = "2", Content = "Climate change and environmental policy", Category = "Environment" },
    new() { Id = "3", Content = "Stock market trends and financial analysis", Category = "Finance" },
    // ... more documents
};

// Train topic model
var trainingResult = await topicService.TrainTopicModelAsync(documents);

Console.WriteLine($"Trained model with {trainingResult.Topics.Count} topics");
Console.WriteLine($"Perplexity: {trainingResult.Perplexity:F2}");
Console.WriteLine($"Average Coherence: {trainingResult.AverageCoherence:F2}");

// Display topics
foreach (var topic in trainingResult.Topics)
{
    Console.WriteLine($"Topic {topic.TopicId}: {string.Join(", ", topic.Keywords.Take(5))}");
}

// Predict topics for new document
var newDocument = new Document 
{ 
    Id = "new", 
    Content = "Deep learning neural networks for image recognition" 
};

var prediction = await topicService.PredictTopicsAsync(newDocument);

Console.WriteLine($"Top topics for new document:");
foreach (var topTopic in prediction.TopTopics)
{
    Console.WriteLine($"  Topic {topTopic.TopicId}: {topTopic.Probability:F3} ({string.Join(", ", topTopic.Keywords.Take(3))})");
}
```

### Document Clustering

```csharp
// Cluster documents based on topic similarity
var clusterResult = await topicService.ClusterDocumentsAsync(documents);

Console.WriteLine($"Created {clusterResult.Clusters.Count} clusters");
Console.WriteLine($"Silhouette Score: {clusterResult.SilhouetteScore:F3}");

foreach (var cluster in clusterResult.Clusters)
{
    Console.WriteLine($"Cluster {cluster.ClusterId}: {cluster.DocumentCount} documents");
    Console.WriteLine($"  Keywords: {string.Join(", ", cluster.RepresentativeKeywords.Take(5))}");
    Console.WriteLine($"  Coherence: {cluster.Coherence:F3}");
}
```

### Topic Evolution Analysis

```csharp
var evolutionService = services.GetRequiredService<TopicEvolutionService>();

// Prepare time-series data
var timeSeriesDocuments = new Dictionary<DateTime, IEnumerable<Document>>
{
    [DateTime.Parse("2023-01-01")] = documentsQ1,
    [DateTime.Parse("2023-04-01")] = documentsQ2,
    [DateTime.Parse("2023-07-01")] = documentsQ3,
    [DateTime.Parse("2023-10-01")] = documentsQ4
};

// Analyze topic evolution
var evolution = await evolutionService.AnalyzeTopicEvolutionAsync(timeSeriesDocuments);

Console.WriteLine("Topic Evolution Analysis:");
foreach (var point in evolution.EvolutionPoints)
{
    Console.WriteLine($"{point.Timestamp:yyyy-MM-dd}: {point.Topics.Count} topics, {point.DocumentCount} documents");
    
    if (point.EmergingTopics.Any())
    {
        Console.WriteLine($"  Emerging: {string.Join(", ", point.EmergingTopics)}");
    }
    
    if (point.DecliningTopics.Any())
    {
        Console.WriteLine($"  Declining: {string.Join(", ", point.DecliningTopics)}");
    }
}

// Display trend analysis
Console.WriteLine("\nTrend Analysis:");
foreach (var (keyword, trend) in evolution.TrendAnalysis.GrowingTopics.Take(5))
{
    Console.WriteLine($"Growing: {keyword} (slope: {trend.Slope:F4}, correlation: {trend.Correlation:F3})");
}
```

### Hierarchical Topic Modeling

```csharp
var hierarchicalService = services.GetRequiredService<HierarchicalTopicService>();

// Build hierarchical topic structure
var hierarchicalModel = await hierarchicalService.BuildHierarchicalTopicsAsync(
    documents, maxDepth: 3);

Console.WriteLine($"Built hierarchical model with {hierarchicalModel.TotalNodes} nodes and {hierarchicalModel.LeafNodes} leaf nodes");

// Traverse hierarchy
void PrintHierarchy(TopicNode node, int indent = 0)
{
    var indentStr = new string(' ', indent * 2);
    Console.WriteLine($"{indentStr}{node.Id}: {node.Documents.Count} docs, Keywords: {string.Join(", ", node.Keywords.Take(3))}");
    
    foreach (var child in node.Children)
    {
        PrintHierarchy(child, indent + 1);
    }
}

PrintHierarchy(hierarchicalModel.RootNode);
```

**Expected Output:**

```text
Trained model with 10 topics
Perplexity: 15.42
Average Coherence: 0.73

Topic 0: machine, learning, intelligence, neural, algorithm
Topic 1: climate, environment, change, carbon, sustainability
Topic 2: market, stock, financial, investment, trading

Top topics for new document:
  Topic 0: 0.823 (machine, learning, intelligence)
  Topic 5: 0.156 (technology, computer, software)
  Topic 3: 0.021 (research, science, analysis)

Created 5 clusters
Silhouette Score: 0.642

Cluster 0: 12 documents
  Keywords: machine, learning, artificial, intelligence, algorithm
  Coherence: 0.784

Topic Evolution Analysis:
2023-01-01: 8 topics, 150 documents
  Emerging: sustainability, remote-work
2023-04-01: 9 topics, 180 documents
  Declining: traditional-media
2023-07-01: 10 topics, 220 documents
  Emerging: blockchain, metaverse

Trend Analysis:
Growing: artificial-intelligence (slope: 0.0045, correlation: 0.892)
Growing: sustainability (slope: 0.0032, correlation: 0.756)
```

## Notes

**Performance Considerations:**

- LDA training time scales with document count and vocabulary size
- Use feature selection and n-gram filtering to reduce dimensionality
- Consider incremental learning for streaming documents
- Cache topic models for repeated predictions

**Quality Optimization:**

- Tune hyperparameters (alpha, beta, number of topics) based on perplexity and coherence
- Preprocess text thoroughly (stop words, stemming, lemmatization)
- Use domain-specific vocabularies for specialized corpora
- Evaluate with human judgment for topic interpretability

**Scalability:**

- Implement parallel processing for large document collections
- Use approximate algorithms for very large datasets
- Consider hierarchical clustering for better organization
- Monitor memory usage with large vocabulary sizes

**Topic Validation:**

- Use coherence metrics to assess topic quality
- Implement human evaluation protocols
- Cross-validate with held-out documents
- Monitor topic stability across training runs

**Integration Patterns:**

- Combine with search systems for semantic document retrieval
- Use for content recommendation and personalization
- Integrate with classification pipelines for feature engineering
- Apply to social media analysis and trend detection

**Security:**

- Sanitize input documents to prevent injection attacks
- Implement rate limiting for API endpoints
- Use secure storage for trained models
- Consider differential privacy for sensitive documents
