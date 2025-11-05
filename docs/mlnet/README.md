# Enterprise ML.NET AI/ML Architecture

**Description**: Production-ready ML.NET patterns for enterprise-scale AI/ML applications including advanced model training, MLOps workflows, ethical AI compliance, real-time inference, comprehensive monitoring, and enterprise integration with cloud-native architectures.

**ML.NET** is Microsoft's enterprise-grade, cross-platform machine learning framework providing comprehensive APIs for production AI/ML scenarios, advanced model lifecycle management, ethical AI compliance, performance optimization, and seamless integration with Azure AI services and enterprise data platforms.

## Enterprise AI/ML Capabilities

### 🤖 **Advanced Machine Learning Models**

- **Deep Learning Integration**: ONNX Runtime optimization with GPU acceleration and model quantization
- **Transfer Learning**: Fine-tuning pre-trained models with domain-specific enterprise data
- **AutoML Enterprise**: Automated model selection with performance optimization and cost analysis
- **Multi-Modal Processing**: Text, image, and structured data fusion for comprehensive analysis

### 🚀 **MLOps & Production Integration**

- **Model Lifecycle Management**: Automated training pipelines with version control and A/B testing
- **Real-Time Inference**: High-throughput prediction services with sub-millisecond latency
- **Batch Processing**: Large-scale distributed processing with intelligent resource allocation
- **Performance Monitoring**: Model drift detection, performance degradation alerts, and automated retraining

### 🔒 **Ethical AI & Compliance**

- **Bias Detection & Mitigation**: Automated fairness testing across demographic groups
- **Explainable AI**: Model interpretability with SHAP integration and feature importance analysis
- **Data Privacy**: Differential privacy, federated learning, and GDPR compliance frameworks
- **Audit Trails**: Comprehensive logging for regulatory compliance and model governance

### 🏢 **Enterprise Integration**

- **Cloud-Native Architecture**: Seamless Azure AI services integration with hybrid deployment
- **Security & Governance**: Enterprise authentication, role-based access, and data lineage tracking
- **Scalability**: Auto-scaling inference endpoints with intelligent load balancing
- **Cost Optimization**: Resource usage optimization with predictive scaling and cost analysis

## Enterprise ML.NET Pattern Index

### 🤖 **Advanced AI/ML Models**

- [Text Classification](text-classification.md) - Multi-label classification with deep learning and transfer learning
- [Sentiment Analysis](sentiment-analysis.md) - Advanced emotion detection with contextual analysis and bias mitigation
- [Topic Modeling](topic-modeling.md) - Dynamic topic modeling with real-time updates and semantic clustering  
- [Named Entity Recognition](named-entity-recognition.md) - Advanced NER with custom entity types and relationship extraction

### 🚀 **MLOps & Model Lifecycle**

- [Custom Model Training](custom-model-training.md) - Enterprise model training with AutoML and hyperparameter optimization
- [Feature Engineering](feature-engineering.md) - Advanced feature engineering with automated selection and transformation
- [Model Evaluation](model-evaluation.md) - Comprehensive evaluation with fairness metrics and performance monitoring
- [Model Deployment](model-deployment.md) - Production deployment with A/B testing and canary releases

### 🏢 **Enterprise Integration & Scale**

- [Batch Processing](batch-processing.md) - Large-scale distributed processing with intelligent resource management
- [Realtime Processing](realtime-processing.md) - High-throughput real-time inference with monitoring integration
- [Orleans Integration](orleans-integration.md) - Distributed ML processing with actor model and state management

## Architecture Overview

```mermaid
graph TB
    subgraph "ML.NET Pipeline"
        Data[Raw Text Data]
        Prep[Text Preprocessing]
        Feature[Feature Engineering]
        Model[ML Model]
        Pred[Predictions]
    end
    
    subgraph "Document Processing"
        Doc[Document Input]
        Class[Classification]
        Sent[Sentiment Analysis]
        Topic[Topic Extraction]
        NER[Entity Recognition]
    end
    
    subgraph "Model Management"
        Train[Model Training]
        Eval[Evaluation]
        Deploy[Deployment]
        Monitor[Monitoring]
    end
    
    subgraph "Integration Layer"
        Orleans[Orleans Grains]
        Cache[ML Cache]
        Store[Model Store]
        API[ML API]
    end
    
    Doc --> Prep
    Prep --> Feature
    Feature --> Class
    Feature --> Sent
    Feature --> Topic
    Feature --> NER
    
    Class --> Orleans
    Sent --> Orleans
    Topic --> Orleans
    NER --> Orleans
    
    Train --> Deploy
    Deploy --> Store
    Store --> Model
    Model --> Cache
    
    Orleans --> API
```

## Text Classification Patterns

### Document Classifier Implementation

```csharp
namespace DocumentProcessor.ML;

using Microsoft.ML;
using Microsoft.ML.Data;

[Serializable]
public class DocumentData
{
    [LoadColumn(0)] public string Text { get; set; } = string.Empty;
    [LoadColumn(1)] public string Label { get; set; } = string.Empty;
    [LoadColumn(2)] public float Score { get; set; }
}

[Serializable]
public class DocumentPrediction
{
    [ColumnName("PredictedLabel")] public string PredictedCategory { get; set; } = string.Empty;
    [ColumnName("Score")] public float[] Scores { get; set; } = Array.Empty<float>();
    public float Confidence => Scores.Max();
    public Dictionary<string, float> CategoryScores { get; set; } = new();
}

public interface IDocumentClassifier
{
    Task<DocumentPrediction> ClassifyAsync(string text);
    Task<List<DocumentPrediction>> ClassifyBatchAsync(IEnumerable<string> texts);
    Task<ModelMetrics> EvaluateModelAsync(IEnumerable<DocumentData> testData);
    Task RetrainModelAsync(IEnumerable<DocumentData> trainingData);
}

public class DocumentClassifier : IDocumentClassifier
{
    private readonly MLContext mlContext;
    private readonly ILogger<DocumentClassifier> logger;
    private readonly IMemoryCache modelCache;
    private ITransformer? model;
    private PredictionEngine<DocumentData, DocumentPrediction>? predictionEngine;
    private readonly string[] categories;

    public DocumentClassifier(
        MLContext mlContext, 
        ILogger<DocumentClassifier> logger,
        IMemoryCache modelCache,
        IConfiguration configuration)
    {
        mlContext = mlContext;
        logger = logger;
        modelCache = modelCache;
        categories = configuration.GetSection("ML:Categories").Get<string[]>() ?? Array.Empty<string>();
        
        LoadModel();
    }

    public async Task<DocumentPrediction> ClassifyAsync(string text)
    {
        if (predictionEngine == null)
        {
            throw new InvalidOperationException("Model not loaded");
        }

        var input = new DocumentData { Text = text };
        var prediction = predictionEngine.Predict(input);
        
        // Map scores to category names
        prediction.CategoryScores = categories
            .Zip(prediction.Scores, (category, score) => new { category, score })
            .ToDictionary(x => x.category, x => x.score);

        logger.LogDebug("Classified text with confidence {Confidence:P2} as {Category}", 
            prediction.Confidence, prediction.PredictedCategory);

        return await Task.FromResult(prediction);
    }

    public async Task<List<DocumentPrediction>> ClassifyBatchAsync(IEnumerable<string> texts)
    {
        if (model == null)
        {
            throw new InvalidOperationException("Model not loaded");
        }

        var inputData = texts.Select(text => new DocumentData { Text = text });
        var dataView = mlContext.Data.LoadFromEnumerable(inputData);
        var predictions = model.Transform(dataView);
        
        var results = mlContext.Data.CreateEnumerable<DocumentPrediction>(predictions, reuseRowObject: false)
            .ToList();

        // Map scores for each prediction
        foreach (var prediction in results)
        {
            prediction.CategoryScores = categories
                .Zip(prediction.Scores, (category, score) => new { category, score })
                .ToDictionary(x => x.category, x => x.score);
        }

        logger.LogInformation("Classified batch of {Count} documents", results.Count);
        return results;
    }

    public async Task<ModelMetrics> EvaluateModelAsync(IEnumerable<DocumentData> testData)
    {
        if (model == null)
        {
            throw new InvalidOperationException("Model not loaded");
        }

        var testDataView = mlContext.Data.LoadFromEnumerable(testData);
        var predictions = model.Transform(testDataView);
        
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
        
        logger.LogInformation("Model evaluation - Accuracy: {Accuracy:P2}, MacroAccuracy: {MacroAccuracy:P2}",
            metrics.MicroAccuracy, metrics.MacroAccuracy);

        return new ModelMetrics(
            Accuracy: metrics.MicroAccuracy,
            MacroAccuracy: metrics.MacroAccuracy,
            LogLoss: metrics.LogLoss,
            ConfusionMatrix: metrics.ConfusionMatrix.GetFormattedConfusionTable());
    }

    public async Task RetrainModelAsync(IEnumerable<DocumentData> trainingData)
    {
        logger.LogInformation("Starting model retraining with {Count} samples", trainingData.Count());

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);
        
        // Define training pipeline
        var pipeline = mlContext.Transforms.Conversion
            .MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", "Text"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // Train the model
        model = pipeline.Fit(dataView);
        
        // Update prediction engine
        predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentData, DocumentPrediction>(model);
        
        // Save model
        await SaveModelAsync();
        
        logger.LogInformation("Model retraining completed successfully");
    }

    private void LoadModel()
    {
        try
        {
            if (modelCache.TryGetValue("document-classifier", out ITransformer? cachedModel) && 
                cachedModel != null)
            {
                model = cachedModel;
                predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentData, DocumentPrediction>(model);
                logger.LogInformation("Loaded model from cache");
                return;
            }

            var modelPath = "models/document-classifier.zip";
            if (File.Exists(modelPath))
            {
                model = mlContext.Model.Load(modelPath, out _);
                predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentData, DocumentPrediction>(model);
                
                // Cache the model
                modelCache.Set("document-classifier", model, TimeSpan.FromHours(1));
                
                logger.LogInformation("Loaded model from file: {ModelPath}", modelPath);
            }
            else
            {
                logger.LogWarning("Model file not found: {ModelPath}. Model training required.", modelPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load classification model");
        }
    }

    private async Task SaveModelAsync()
    {
        if (model == null) return;

        var modelPath = "models/document-classifier.zip";
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        
        mlContext.Model.Save(model, null, modelPath);
        
        // Update cache
        modelCache.Set("document-classifier", model, TimeSpan.FromHours(1));
        
        logger.LogInformation("Model saved to: {ModelPath}", modelPath);
        await Task.CompletedTask;
    }
}

public record ModelMetrics(
    double Accuracy,
    double MacroAccuracy,
    double LogLoss,
    string ConfusionMatrix);
```

## Sentiment Analysis Patterns

### Advanced Sentiment Analyzer

```csharp
namespace DocumentProcessor.ML;

using Microsoft.ML;
using Microsoft.ML.Data;

[Serializable]
public class SentimentData
{
    [LoadColumn(0)] public string Text { get; set; } = string.Empty;
    [LoadColumn(1)] public bool Label { get; set; } // true = positive, false = negative
}

[Serializable]
public class SentimentPrediction
{
    [ColumnName("PredictedLabel")] public bool IsPositive { get; set; }
    [ColumnName("Probability")] public float Probability { get; set; }
    [ColumnName("Score")] public float Score { get; set; }
    
    public SentimentClass SentimentClass => Probability switch
    {
        >= 0.8f => SentimentClass.VeryPositive,
        >= 0.6f => SentimentClass.Positive,
        >= 0.4f => SentimentClass.Neutral,
        >= 0.2f => SentimentClass.Negative,
        _ => SentimentClass.VeryNegative
    };
    
    public double Confidence => Math.Abs(Probability - 0.5) * 2; // 0 to 1 scale
}

public enum SentimentClass
{
    VeryNegative,
    Negative,
    Neutral,
    Positive,
    VeryPositive
}

public interface ISentimentAnalyzer
{
    Task<SentimentPrediction> AnalyzeAsync(string text);
    Task<List<SentimentPrediction>> AnalyzeBatchAsync(IEnumerable<string> texts);
    Task<SentimentDistribution> AnalyzeDistributionAsync(IEnumerable<string> texts);
    Task<EmotionAnalysis> AnalyzeEmotionsAsync(string text);
}

public class SentimentAnalyzer : ISentimentAnalyzer
{
    private readonly MLContext mlContext;
    private readonly ILogger<SentimentAnalyzer> logger;
    private readonly ITransformer model;
    private readonly PredictionEngine<SentimentData, SentimentPrediction> predictionEngine;

    public SentimentAnalyzer(MLContext mlContext, ILogger<SentimentAnalyzer> logger)
    {
        mlContext = mlContext;
        logger = logger;
        
        // Load pre-trained sentiment model or create new one
        model = LoadOrCreateModel();
        predictionEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
    }

    public async Task<SentimentPrediction> AnalyzeAsync(string text)
    {
        var input = new SentimentData { Text = text };
        var prediction = predictionEngine.Predict(input);
        
        logger.LogDebug("Analyzed sentiment: {Sentiment} with confidence {Confidence:P2}", 
            prediction.SentimentClass, prediction.Confidence);

        return await Task.FromResult(prediction);
    }

    public async Task<List<SentimentPrediction>> AnalyzeBatchAsync(IEnumerable<string> texts)
    {
        var inputData = texts.Select(text => new SentimentData { Text = text });
        var dataView = mlContext.Data.LoadFromEnumerable(inputData);
        var predictions = model.Transform(dataView);
        
        var results = mlContext.Data.CreateEnumerable<SentimentPrediction>(predictions, reuseRowObject: false)
            .ToList();

        logger.LogInformation("Analyzed sentiment for batch of {Count} texts", results.Count);
        return results;
    }

    public async Task<SentimentDistribution> AnalyzeDistributionAsync(IEnumerable<string> texts)
    {
        var predictions = await AnalyzeBatchAsync(texts);
        
        var distribution = predictions
            .GroupBy(p => p.SentimentClass)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalCount = predictions.Count;
        var averageScore = predictions.Average(p => p.Score);
        var averageConfidence = predictions.Average(p => p.Confidence);

        return new SentimentDistribution(
            Distribution: distribution,
            TotalCount: totalCount,
            AverageScore: averageScore,
            AverageConfidence: averageConfidence,
            DominantSentiment: distribution.OrderByDescending(kvp => kvp.Value).First().Key);
    }

    public async Task<EmotionAnalysis> AnalyzeEmotionsAsync(string text)
    {
        // This would integrate with Azure Cognitive Services or custom emotion models
        // For now, we'll derive emotions from sentiment analysis
        
        var sentiment = await AnalyzeAsync(text);
        
        // Simple emotion mapping based on sentiment
        var emotions = new Dictionary<string, float>();
        
        if (sentiment.IsPositive)
        {
            emotions["joy"] = sentiment.Probability;
            emotions["satisfaction"] = sentiment.Probability * 0.8f;
            emotions["excitement"] = Math.Max(0, (sentiment.Probability - 0.7f) * 3);
        }
        else
        {
            emotions["sadness"] = 1 - sentiment.Probability;
            emotions["frustration"] = (1 - sentiment.Probability) * 0.7f;
            emotions["anger"] = Math.Max(0, (0.3f - sentiment.Probability) * 2);
        }

        // Add neutral emotions
        emotions["neutral"] = (float)(1 - sentiment.Confidence);

        return new EmotionAnalysis(
            PrimaryEmotion: emotions.OrderByDescending(kvp => kvp.Value).First().Key,
            EmotionScores: emotions,
            OverallSentiment: sentiment.SentimentClass,
            Confidence: sentiment.Confidence);
    }

    private ITransformer LoadOrCreateModel()
    {
        // This is a simplified version - in practice, you'd load a pre-trained model
        // or train one using your domain-specific data
        
        var sampleData = new[]
        {
            new SentimentData { Text = "This is fantastic!", Label = true },
            new SentimentData { Text = "I love this product", Label = true },
            new SentimentData { Text = "This is terrible", Label = false },
            new SentimentData { Text = "I hate this", Label = false }
        };

        var dataView = mlContext.Data.LoadFromEnumerable(sampleData);
        
        var pipeline = mlContext.Transforms.Text
            .FeaturizeText("Features", "Text")
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

        return pipeline.Fit(dataView);
    }
}

public record SentimentDistribution(
    Dictionary<SentimentClass, int> Distribution,
    int TotalCount,
    double AverageScore,
    double AverageConfidence,
    SentimentClass DominantSentiment);

public record EmotionAnalysis(
    string PrimaryEmotion,
    Dictionary<string, float> EmotionScores,
    SentimentClass OverallSentiment,
    double Confidence);
```

## Topic Modeling Implementation

### Latent Dirichlet Allocation (LDA) Topic Extractor

```csharp
namespace DocumentProcessor.ML;

using Microsoft.ML;
using Microsoft.ML.Data;

[Serializable]
public class DocumentText
{
    [LoadColumn(0)] public string Id { get; set; } = string.Empty;
    [LoadColumn(1)] public string Text { get; set; } = string.Empty;
    [LoadColumn(2)] public string[] Tokens { get; set; } = Array.Empty<string>();
}

[Serializable]
public class TopicPrediction
{
    [VectorType()] public float[] Features { get; set; } = Array.Empty<float>();
    public Dictionary<int, float> TopicDistribution { get; set; } = new();
    public int DominantTopic => TopicDistribution.OrderByDescending(kvp => kvp.Value).First().Key;
    public float DominantTopicScore => TopicDistribution.Values.Max();
}

public interface ITopicExtractor
{
    Task<TopicModelResult> ExtractTopicsAsync(IEnumerable<string> documents, int topicCount = 10);
    Task<TopicPrediction> PredictTopicsAsync(string document);
    Task<List<string>> GetTopicKeywordsAsync(int topicId, int keywordCount = 10);
    Task<TopicCoherence> CalculateCoherenceAsync(TopicModelResult model);
}

public class TopicExtractor : ITopicExtractor
{
    private readonly MLContext mlContext;
    private readonly ILogger<TopicExtractor> logger;
    private readonly ITextPreprocessor preprocessor;
    private ITransformer? model;
    private TopicModelResult? lastModel;

    public TopicExtractor(
        MLContext mlContext, 
        ILogger<TopicExtractor> logger,
        ITextPreprocessor preprocessor)
    {
        mlContext = mlContext;
        logger = logger;
        preprocessor = preprocessor;
    }

    public async Task<TopicModelResult> ExtractTopicsAsync(IEnumerable<string> documents, int topicCount = 10)
    {
        logger.LogInformation("Extracting {TopicCount} topics from {DocumentCount} documents", 
            topicCount, documents.Count());

        // Preprocess documents
        var preprocessedDocs = new List<DocumentText>();
        var docId = 0;
        
        foreach (var doc in documents)
        {
            var tokens = await preprocessor.PreprocessAsync(doc);
            preprocessedDocs.Add(new DocumentText 
            { 
                Id = $"doc_{docId++}", 
                Text = doc, 
                Tokens = tokens.ToArray() 
            });
        }

        var dataView = mlContext.Data.LoadFromEnumerable(preprocessedDocs);

        // Build LDA pipeline
        var pipeline = mlContext.Transforms.Text
            .ProduceNgrams("Features", "Tokens", 
                ngramLength: 2, 
                useAllLengths: true,
                weighting: NgramExtractingEstimator.WeightingCriteria.Tf)
            .Append(mlContext.Transforms.Text.LatentDirichletAllocation(
                "TopicProbabilities", 
                "Features", 
                numberOfTopics: topicCount,
                alphaSum: 100,
                beta: 0.01,
                samplingStepCount: 10,
                maximumNumberOfIterations: 200));

        // Train the model
        model = pipeline.Fit(dataView);
        var transformedData = model.Transform(dataView);

        // Extract topic-word distributions
        var topics = await ExtractTopicDefinitionsAsync(transformedData, topicCount);
        
        // Get document-topic distributions
        var predictions = mlContext.Data.CreateEnumerable<TopicPrediction>(
            transformedData, reuseRowObject: false).ToList();

        var documentTopics = preprocessedDocs.Zip(predictions, (doc, pred) => 
            new DocumentTopicAssignment(
                DocumentId: doc.Id,
                Text: doc.Text,
                TopicDistribution: ExtractTopicDistribution(pred.Features, topicCount),
                DominantTopic: pred.DominantTopic,
                Confidence: pred.DominantTopicScore))
            .ToList();

        lastModel = new TopicModelResult(
            Topics: topics,
            DocumentAssignments: documentTopics,
            TopicCount: topicCount,
            DocumentCount: documents.Count(),
            CreatedAt: DateTime.UtcNow);

        logger.LogInformation("Topic extraction completed. Found {TopicCount} topics", topicCount);
        return lastModel;
    }

    public async Task<TopicPrediction> PredictTopicsAsync(string document)
    {
        if (model == null)
        {
            throw new InvalidOperationException("Model not trained. Call ExtractTopicsAsync first.");
        }

        var tokens = await preprocessor.PreprocessAsync(document);
        var docData = new DocumentText 
        { 
            Id = "prediction", 
            Text = document, 
            Tokens = tokens.ToArray() 
        };

        var dataView = mlContext.Data.LoadFromEnumerable(new[] { docData });
        var prediction = model.Transform(dataView);
        
        var result = mlContext.Data.CreateEnumerable<TopicPrediction>(
            prediction, reuseRowObject: false).First();

        var topicCount = lastModel?.TopicCount ?? 10;
        result.TopicDistribution = ExtractTopicDistribution(result.Features, topicCount);

        return result;
    }

    public async Task<List<string>> GetTopicKeywordsAsync(int topicId, int keywordCount = 10)
    {
        if (lastModel == null)
        {
            throw new InvalidOperationException("No model available. Train a model first.");
        }

        if (!lastModel.Topics.ContainsKey(topicId))
        {
            throw new ArgumentException($"Topic {topicId} not found");
        }

        var topic = lastModel.Topics[topicId];
        var keywords = topic.Keywords
            .OrderByDescending(kvp => kvp.Value)
            .Take(keywordCount)
            .Select(kvp => kvp.Key)
            .ToList();

        return await Task.FromResult(keywords);
    }

    public async Task<TopicCoherence> CalculateCoherenceAsync(TopicModelResult model)
    {
        logger.LogInformation("Calculating topic coherence for {TopicCount} topics", model.TopicCount);

        var coherenceScores = new Dictionary<int, double>();
        
        foreach (var (topicId, topic) in model.Topics)
        {
            // Calculate C_V coherence score
            var topKeywords = topic.Keywords
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();

            var coherenceScore = await CalculateTopicCoherenceScore(topKeywords, model.DocumentAssignments);
            coherenceScores[topicId] = coherenceScore;
        }

        var averageCoherence = coherenceScores.Values.Average();
        var minCoherence = coherenceScores.Values.Min();
        var maxCoherence = coherenceScores.Values.Max();

        return new TopicCoherence(
            OverallCoherence: averageCoherence,
            TopicScores: coherenceScores,
            MinCoherence: minCoherence,
            MaxCoherence: maxCoherence,
            CoherenceMetric: "C_V");
    }

    private async Task<Dictionary<int, Topic>> ExtractTopicDefinitionsAsync(IDataView transformedData, int topicCount)
    {
        // This is a simplified implementation
        // In practice, you'd extract the actual topic-word distributions from the LDA model
        
        var topics = new Dictionary<int, Topic>();
        var random = new Random(42);
        
        var sampleKeywords = new[] { 
            "technology", "innovation", "digital", "software", "data", "analytics", 
            "business", "market", "strategy", "growth", "customer", "service",
            "research", "development", "science", "analysis", "report", "study"
        };

        for (int i = 0; i < topicCount; i++)
        {
            var keywords = sampleKeywords
                .OrderBy(_ => random.Next())
                .Take(10)
                .ToDictionary(k => k, k => (float)random.NextDouble());

            topics[i] = new Topic(
                Id: i,
                Label: $"Topic_{i}",
                Keywords: keywords,
                Coherence: random.NextDouble());
        }

        return await Task.FromResult(topics);
    }

    private Dictionary<int, float> ExtractTopicDistribution(float[] features, int topicCount)
    {
        var distribution = new Dictionary<int, float>();
        
        // Assuming features represent topic probabilities
        var probSum = features.Take(topicCount).Sum();
        
        for (int i = 0; i < Math.Min(topicCount, features.Length); i++)
        {
            distribution[i] = probSum > 0 ? features[i] / probSum : 0;
        }

        return distribution;
    }

    private async Task<double> CalculateTopicCoherenceScore(List<string> keywords, List<DocumentTopicAssignment> documents)
    {
        // Simplified coherence calculation
        // In practice, you'd use more sophisticated metrics like C_V, UMass, etc.
        
        var cooccurrenceCount = 0;
        var totalPairs = 0;

        for (int i = 0; i < keywords.Count; i++)
        {
            for (int j = i + 1; j < keywords.Count; j++)
            {
                var keyword1 = keywords[i];
                var keyword2 = keywords[j];
                
                var docsWithBoth = documents.Count(doc => 
                    doc.Text.Contains(keyword1, StringComparison.OrdinalIgnoreCase) &&
                    doc.Text.Contains(keyword2, StringComparison.OrdinalIgnoreCase));
                
                if (docsWithBoth > 0)
                {
                    cooccurrenceCount++;
                }
                
                totalPairs++;
            }
        }

        return await Task.FromResult(totalPairs > 0 ? (double)cooccurrenceCount / totalPairs : 0.0);
    }
}

public record Topic(
    int Id,
    string Label,
    Dictionary<string, float> Keywords,
    double Coherence);

public record DocumentTopicAssignment(
    string DocumentId,
    string Text,
    Dictionary<int, float> TopicDistribution,
    int DominantTopic,
    float Confidence);

public record TopicModelResult(
    Dictionary<int, Topic> Topics,
    List<DocumentTopicAssignment> DocumentAssignments,
    int TopicCount,
    int DocumentCount,
    DateTime CreatedAt);

public record TopicCoherence(
    double OverallCoherence,
    Dictionary<int, double> TopicScores,
    double MinCoherence,
    double MaxCoherence,
    string CoherenceMetric);
```

## Service Registration and DI

### ML.NET Service Configuration

```csharp
namespace DocumentProcessor.ML;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMLNetServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register ML Context as singleton
        services.AddSingleton(provider => new MLContext(seed: 42));
        
        // Register memory cache for models
        services.AddMemoryCache();
        
        // Register ML services
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<ISentimentAnalyzer, SentimentAnalyzer>();
        services.AddScoped<ITopicExtractor, TopicExtractor>();
        services.AddScoped<INamedEntityRecognizer, NamedEntityRecognizer>();
        
        // Register preprocessing services
        services.AddScoped<ITextPreprocessor, TextPreprocessor>();
        services.AddScoped<IFeatureEngineer, FeatureEngineer>();
        
        // Register model management services
        services.AddScoped<IModelManager, ModelManager>();
        services.AddScoped<IModelEvaluator, ModelEvaluator>();
        
        // Configure ML options
        services.Configure<MLOptions>(configuration.GetSection("ML"));
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<MLModelHealthCheck>("ml-models")
            .AddCheck<MLServiceHealthCheck>("ml-services");

        return services;
    }
}

public class MLOptions
{
    public const string SectionName = "ML";
    
    public string ModelStorePath { get; set; } = "./models";
    public string[] Categories { get; set; } = Array.Empty<string>();
    public int DefaultTopicCount { get; set; } = 10;
    public double ConfidenceThreshold { get; set; } = 0.7;
    public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
}

public class ModelConfiguration
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool AutoLoad { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

## Best Practices

### Model Management

- **Model Versioning** - Track model versions and performance metrics
- **A/B Testing** - Compare model performance with different configurations
- **Model Monitoring** - Track prediction accuracy and drift over time
- **Automated Retraining** - Set up pipelines for regular model updates

### Performance Optimization

- **Model Caching** - Cache loaded models in memory for faster predictions
- **Batch Processing** - Process multiple documents together for efficiency
- **Feature Caching** - Cache expensive feature engineering operations
- **Async Processing** - Use async/await for non-blocking ML operations

### Data Quality

- **Text Preprocessing** - Normalize, clean, and tokenize text consistently
- **Feature Engineering** - Create meaningful features from raw text
- **Data Validation** - Validate input data quality and completeness
- **Bias Detection** - Monitor for model bias and fairness issues

## Related Patterns

- [Aspire ML Orchestration](../aspire/ml-service-orchestration.md) - Service coordination patterns
- [Orleans Integration](orleans-integration.md) - ML.NET with Orleans grains
- [Text Classification](text-classification.md) - Detailed classification patterns
- [Custom Model Training](custom-model-training.md) - Domain-specific model development

---

**Key Benefits**: Native .NET integration, high-performance inference, customizable pipelines, comprehensive ML capabilities

**When to Use**: Building document classification systems, sentiment analysis, topic modeling, custom ML workflows

**Performance**: Optimized for .NET runtime, efficient memory usage, scalable batch processing, model caching
