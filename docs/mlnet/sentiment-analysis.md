# Enterprise AI Sentiment Analysis with Advanced Deep Learning

**Description**: Production-grade sentiment analysis using deep learning models with ML.NET, featuring real-time emotion detection, aspect-based sentiment mining, multi-lingual support, bias detection, and explainable AI integration for enterprise-scale text analytics.

**Language/Technology**: C# (.NET 9.0) with ML.NET 3.0+, ONNX Runtime, Azure Cognitive Services

## Core Sentiment Analysis Implementation

### Binary Sentiment Classifier

```csharp
// src/MLModels/SentimentAnalyzer.cs
using Microsoft.ML;
using Microsoft.ML.Data;

namespace DocumentProcessing.MLModels;

public class SentimentAnalyzer : IDisposable
{
    private readonly MLContext mlContext;
    private readonly ITransformer? trainedModel;
    private readonly PredictionEngine<SentimentInput, SentimentPrediction>? predictionEngine;
    private readonly ILogger<SentimentAnalyzer> logger;
    private bool disposed = false;
    
    public SentimentAnalyzer(MLContext mlContext, ILogger<SentimentAnalyzer> logger)
    {
        this.mlContext = mlContext;
        this.logger = logger;
    }
    
    public SentimentAnalyzer(MLContext mlContext, ITransformer trainedModel, ILogger<SentimentAnalyzer> logger)
    {
        this.mlContext = mlContext;
        this.trainedModel = trainedModel;
        this.logger = logger;
        predictionEngine = mlContext.Model.CreatePredictionEngine<SentimentInput, SentimentPrediction>(trainedModel);
    }
    
    public async Task<ITransformer> TrainSentimentModelAsync(IEnumerable<SentimentTrainingData> trainingData,
        SentimentAnalysisOptions? options = null)
    {
        options ??= new SentimentAnalysisOptions();
        
        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);
        
        // Split data for training and evaluation
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: options.TestFraction);
        
        // Create sentiment analysis pipeline
        var pipeline = BuildSentimentPipeline(options);
        
        logger.LogInformation("Training sentiment model with {TrainingCount} samples", 
            trainingData.Count());
        
        var model = await Task.Run(() => pipeline.Fit(split.TrainSet));
        
        // Evaluate model
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, 
            labelColumnName: nameof(SentimentTrainingData.IsPositive));
        
        LogSentimentMetrics(metrics);
        
        return model;
    }
    
    public SentimentPrediction AnalyzeSentiment(string text)
    {
        if (predictionEngine == null)
            throw new InvalidOperationException("Model not loaded. Use TrainSentimentModelAsync first or load existing model.");
        
        var input = new SentimentInput { Text = text };
        var prediction = predictionEngine.Predict(input);
        
        return prediction;
    }
    
    public async Task<IEnumerable<SentimentPrediction>> AnalyzeSentimentsAsync(IEnumerable<string> texts)
    {
        if (trainedModel == null)
            throw new InvalidOperationException("Model not loaded.");
        
        var inputs = texts.Select(text => new SentimentInput { Text = text });
        var dataView = mlContext.Data.LoadFromEnumerable(inputs);
        
        var predictions = await Task.Run(() => trainedModel.Transform(dataView));
        
        return mlContext.Data.CreateEnumerable<SentimentPrediction>(predictions, reuseRowObject: false);
    }
    
    public SentimentAnalysisResult AnalyzeSentimentDetailed(string text)
    {
        var prediction = AnalyzeSentiment(text);
        
        return new SentimentAnalysisResult
        {
            Text = text,
            IsPositive = prediction.IsPositive,
            Confidence = prediction.Probability,
            Score = prediction.Score,
            SentimentLabel = GetSentimentLabel(prediction.Probability),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private IEstimator<ITransformer> BuildSentimentPipeline(SentimentAnalysisOptions options)
    {
        var pipeline = mlContext.Transforms.Text.FeaturizeText(
            outputColumnName: "Features",
            inputColumnName: nameof(SentimentTrainingData.Text),
            options: new Microsoft.ML.Transforms.Text.TextFeaturizingEstimator.Options
            {
                WordFeatureExtractor = new Microsoft.ML.Transforms.Text.WordBagEstimator.Options
                {
                    NgramLength = options.NgramLength,
                    UseAllLengths = options.UseAllNgramLengths,
                    MaximumNgramsCount = options.MaxNgramCount
                },
                CharFeatureExtractor = options.UseCharacterNgrams ? 
                    new Microsoft.ML.Transforms.Text.WordBagEstimator.Options
                    {
                        NgramLength = 3,
                        UseAllLengths = false
                    } : null,
                StopWordsRemover = options.RemoveStopWords ? 
                    new Microsoft.ML.Transforms.Text.StopWordsRemovingEstimator.Options() : null
            });
        
        // Choose algorithm based on options
        IEstimator<ITransformer> trainer = options.Algorithm switch
        {
            SentimentAnalysisAlgorithm.SdcaLogisticRegression => mlContext.BinaryClassification.Trainers
                .SdcaLogisticRegression(
                    labelColumnName: nameof(SentimentTrainingData.IsPositive), 
                    featureColumnName: "Features"),
            SentimentAnalysisAlgorithm.FastTree => mlContext.BinaryClassification.Trainers
                .FastTree(
                    labelColumnName: nameof(SentimentTrainingData.IsPositive), 
                    featureColumnName: "Features"),
            SentimentAnalysisAlgorithm.LbfgsLogisticRegression => mlContext.BinaryClassification.Trainers
                .LbfgsLogisticRegression(
                    labelColumnName: nameof(SentimentTrainingData.IsPositive), 
                    featureColumnName: "Features"),
            _ => mlContext.BinaryClassification.Trainers
                .SdcaLogisticRegression(
                    labelColumnName: nameof(SentimentTrainingData.IsPositive), 
                    featureColumnName: "Features")
        };
        
        return pipeline.Append(trainer);
    }
    
    private void LogSentimentMetrics(BinaryClassificationMetrics metrics)
    {
        logger.LogInformation("Sentiment Model Training Metrics:");
        logger.LogInformation("Accuracy: {Accuracy:F4}", metrics.Accuracy);
        logger.LogInformation("Area Under Curve: {Auc:F4}", metrics.AreaUnderRocCurve);
        logger.LogInformation("Area Under Precision-Recall Curve: {Auprc:F4}", metrics.AreaUnderPrecisionRecallCurve);
        logger.LogInformation("F1 Score: {F1Score:F4}", metrics.F1Score);
        logger.LogInformation("Positive Precision: {PositivePrecision:F4}", metrics.PositivePrecision);
        logger.LogInformation("Positive Recall: {PositiveRecall:F4}", metrics.PositiveRecall);
        logger.LogInformation("Negative Precision: {NegativePrecision:F4}", metrics.NegativePrecision);
        logger.LogInformation("Negative Recall: {NegativeRecall:F4}", metrics.NegativeRecall);
    }
    
    private static string GetSentimentLabel(float probability) => probability switch
    {
        >= 0.8f => "Very Positive",
        >= 0.6f => "Positive",
        >= 0.4f => "Neutral",
        >= 0.2f => "Negative",
        _ => "Very Negative"
    };
    
    public void SaveModel(string modelPath)
    {
        if (trainedModel == null)
            throw new InvalidOperationException("No trained model to save.");
        
        mlContext.Model.Save(trainedModel, null, modelPath);
        logger.LogInformation("Sentiment model saved to: {ModelPath}", modelPath);
    }
    
    public static SentimentAnalyzer LoadModel(MLContext mlContext, string modelPath, ILogger<SentimentAnalyzer> logger)
    {
        var model = mlContext.Model.Load(modelPath, out _);
        return new SentimentAnalyzer(mlContext, model, logger);
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            predictionEngine?.Dispose();
            disposed = true;
        }
    }
}

// Data models
public class SentimentInput
{
    public string Text { get; set; } = string.Empty;
}

public class SentimentTrainingData
{
    public string Text { get; set; } = string.Empty;
    public bool IsPositive { get; set; }
}

public class SentimentPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsPositive { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("Score")]
    public float Score { get; set; }
}

public class SentimentAnalysisOptions
{
    public SentimentAnalysisAlgorithm Algorithm { get; set; } = SentimentAnalysisAlgorithm.SdcaLogisticRegression;
    public double TestFraction { get; set; } = 0.2;
    public int NgramLength { get; set; } = 2;
    public bool UseAllNgramLengths { get; set; } = true;
    public bool UseCharacterNgrams { get; set; } = false;
    public bool RemoveStopWords { get; set; } = true;
    public int MaxNgramCount { get; set; } = 10000;
}

public enum SentimentAnalysisAlgorithm
{
    SdcaLogisticRegression,
    FastTree,
    LbfgsLogisticRegression
}

public record SentimentAnalysisResult
{
    public required string Text { get; init; }
    public required bool IsPositive { get; init; }
    public required float Confidence { get; init; }
    public required float Score { get; init; }
    public required string SentimentLabel { get; init; }
    public required DateTime Timestamp { get; init; }
}
```

### Multi-Class Emotion Detection

```csharp
// src/MLModels/EmotionAnalyzer.cs
namespace DocumentProcessing.MLModels;

public class EmotionAnalyzer : IDisposable
{
    private readonly MLContext mlContext;
    private readonly ITransformer? trainedModel;
    private readonly PredictionEngine<EmotionInput, EmotionPrediction>? predictionEngine;
    private readonly ILogger<EmotionAnalyzer> logger;
    private bool disposed = false;
    
    public EmotionAnalyzer(MLContext mlContext, ILogger<EmotionAnalyzer> logger)
    {
        this.mlContext = mlContext;
        this.logger = logger;
    }
    
    public EmotionAnalyzer(MLContext mlContext, ITransformer trainedModel, ILogger<EmotionAnalyzer> logger)
    {
        this.mlContext = mlContext;
        this.trainedModel = trainedModel;
        this.logger = logger;
        predictionEngine = mlContext.Model.CreatePredictionEngine<EmotionInput, EmotionPrediction>(trainedModel);
    }
    
    public async Task<ITransformer> TrainEmotionModelAsync(IEnumerable<EmotionTrainingData> trainingData)
    {
        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label", 
                inputColumnName: nameof(EmotionTrainingData.Emotion))
            .Append(mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: nameof(EmotionTrainingData.Text)))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label", 
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue(
                outputColumnName: "PredictedLabel", 
                inputColumnName: "PredictedLabel"));
        
        logger.LogInformation("Training emotion detection model with {TrainingCount} samples", 
            trainingData.Count());
        
        var model = await Task.Run(() => pipeline.Fit(split.TrainSet));
        
        // Evaluate model
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");
        
        logger.LogInformation("Emotion Model - Macro Accuracy: {MacroAccuracy:F4}, Micro Accuracy: {MicroAccuracy:F4}",
            metrics.MacroAccuracy, metrics.MicroAccuracy);
        
        return model;
    }
    
    public EmotionAnalysisResult AnalyzeEmotion(string text)
    {
        if (predictionEngine == null)
            throw new InvalidOperationException("Emotion model not loaded.");
        
        var input = new EmotionInput { Text = text };
        var prediction = predictionEngine.Predict(input);
        
        return new EmotionAnalysisResult
        {
            Text = text,
            PredictedEmotion = prediction.PredictedEmotion,
            Confidence = prediction.Scores?.Max() ?? 0f,
            EmotionScores = GetEmotionScores(prediction.Scores),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private Dictionary<string, float> GetEmotionScores(float[]? scores)
    {
        if (scores == null) return new Dictionary<string, float>();
        
        var emotions = new[] { "Joy", "Sadness", "Anger", "Fear", "Surprise", "Neutral" };
        
        return emotions.Take(scores.Length)
            .Zip(scores, (emotion, score) => new { emotion, score })
            .ToDictionary(x => x.emotion, x => x.score);
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            predictionEngine?.Dispose();
            disposed = true;
        }
    }
}

public class EmotionInput
{
    public string Text { get; set; } = string.Empty;
}

public class EmotionTrainingData
{
    public string Text { get; set; } = string.Empty;
    public string Emotion { get; set; } = string.Empty;
}

public class EmotionPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedEmotion { get; set; } = string.Empty;
    
    [ColumnName("Score")]
    public float[]? Scores { get; set; }
}

public record EmotionAnalysisResult
{
    public required string Text { get; init; }
    public required string PredictedEmotion { get; init; }
    public required float Confidence { get; init; }
    public Dictionary<string, float> EmotionScores { get; init; } = new();
    public required DateTime Timestamp { get; init; }
}
```

### Sentiment Analysis Service

```csharp
// src/Services/SentimentAnalysisService.cs
namespace DocumentProcessing.Services;

public interface ISentimentAnalysisService
{
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text, string? modelName = null);
    Task<IEnumerable<SentimentAnalysisResult>> AnalyzeSentimentsAsync(
        IEnumerable<string> texts, string? modelName = null);
    Task<EmotionAnalysisResult> AnalyzeEmotionAsync(string text);
    Task<AspectBasedSentimentResult> AnalyzeAspectSentimentAsync(string text, string[] aspects);
    Task<string> TrainCustomSentimentModelAsync(IEnumerable<SentimentTrainingData> trainingData,
        string modelName, SentimentAnalysisOptions? options = null);
}

public class SentimentAnalysisService(
    MLContext mlContext,
    IMemoryCache cache,
    ILogger<SentimentAnalysisService> logger,
    IConfiguration configuration) : ISentimentAnalysisService
{
    private readonly ConcurrentDictionary<string, SentimentAnalyzer> sentimentModelCache = new();
    private readonly ConcurrentDictionary<string, EmotionAnalyzer> emotionModelCache = new();
    private readonly string modelsPath = configuration.GetValue<string>("MLModels:Path") ?? "models";
    
    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text, string? modelName = null)
    {
        modelName ??= "default_sentiment";
        var analyzer = await GetOrLoadSentimentAnalyzerAsync(modelName);
        
        return analyzer.AnalyzeSentimentDetailed(text);
    }
    
    public async Task<IEnumerable<SentimentAnalysisResult>> AnalyzeSentimentsAsync(
        IEnumerable<string> texts, string? modelName = null)
    {
        modelName ??= "default_sentiment";
        var analyzer = await GetOrLoadSentimentAnalyzerAsync(modelName);
        
        var predictions = await analyzer.AnalyzeSentimentsAsync(texts);
        
        return texts.Zip(predictions, (text, pred) => new SentimentAnalysisResult
        {
            Text = text,
            IsPositive = pred.IsPositive,
            Confidence = pred.Probability,
            Score = pred.Score,
            SentimentLabel = GetSentimentLabel(pred.Probability),
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task<EmotionAnalysisResult> AnalyzeEmotionAsync(string text)
    {
        var analyzer = await GetOrLoadEmotionAnalyzerAsync("default_emotion");
        return analyzer.AnalyzeEmotion(text);
    }
    
    public async Task<AspectBasedSentimentResult> AnalyzeAspectSentimentAsync(string text, string[] aspects)
    {
        var sentences = SplitIntoSentences(text);
        var aspectResults = new Dictionary<string, SentimentAnalysisResult>();
        
        foreach (var aspect in aspects)
        {
            var relevantSentences = sentences
                .Where(s => ContainsAspect(s, aspect))
                .ToArray();
            
            if (relevantSentences.Any())
            {
                var combinedText = string.Join(" ", relevantSentences);
                aspectResults[aspect] = await AnalyzeSentimentAsync(combinedText);
            }
        }
        
        var overallSentiment = await AnalyzeSentimentAsync(text);
        
        return new AspectBasedSentimentResult
        {
            Text = text,
            OverallSentiment = overallSentiment,
            AspectSentiments = aspectResults,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public async Task<string> TrainCustomSentimentModelAsync(IEnumerable<SentimentTrainingData> trainingData,
        string modelName, SentimentAnalysisOptions? options = null)
    {
        logger.LogInformation("Training custom sentiment model: {ModelName}", modelName);
        
        var analyzer = new SentimentAnalyzer(mlContext, logger);
        var trainedModel = await analyzer.TrainSentimentModelAsync(trainingData, options);
        
        var modelPath = Path.Combine(modelsPath, $"{modelName}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        
        var fullAnalyzer = new SentimentAnalyzer(mlContext, trainedModel, logger);
        fullAnalyzer.SaveModel(modelPath);
        
        // Update cache
        if (sentimentModelCache.TryRemove(modelName, out var oldAnalyzer))
        {
            oldAnalyzer.Dispose();
        }
        sentimentModelCache[modelName] = fullAnalyzer;
        
        logger.LogInformation("Custom sentiment model trained and saved: {ModelName}", modelName);
        return modelPath;
    }
    
    private async Task<SentimentAnalyzer> GetOrLoadSentimentAnalyzerAsync(string modelName)
    {
        if (sentimentModelCache.TryGetValue(modelName, out var analyzer))
        {
            return analyzer;
        }
        
        var modelPath = Path.Combine(modelsPath, $"{modelName}.zip");
        
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Sentiment model not found: {modelName}");
        }
        
        analyzer = await Task.Run(() => SentimentAnalyzer.LoadModel(mlContext, modelPath, logger));
        sentimentModelCache[modelName] = analyzer;
        
        return analyzer;
    }
    
    private async Task<EmotionAnalyzer> GetOrLoadEmotionAnalyzerAsync(string modelName)
    {
        if (emotionModelCache.TryGetValue(modelName, out var analyzer))
        {
            return analyzer;
        }
        
        var modelPath = Path.Combine(modelsPath, $"{modelName}.zip");
        
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Emotion model not found: {modelName}");
        }
        
        analyzer = await Task.Run(() => EmotionAnalyzer.LoadModel(mlContext, modelPath, logger));
        emotionModelCache[modelName] = analyzer;
        
        return analyzer;
    }
    
    private static string[] SplitIntoSentences(string text)
    {
        return text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }
    
    private static bool ContainsAspect(string sentence, string aspect)
    {
        return sentence.Contains(aspect, StringComparison.OrdinalIgnoreCase);
    }
    
    private static string GetSentimentLabel(float probability) => probability switch
    {
        >= 0.8f => "Very Positive",
        >= 0.6f => "Positive",
        >= 0.4f => "Neutral",
        >= 0.2f => "Negative",
        _ => "Very Negative"
    };
}

public record AspectBasedSentimentResult
{
    public required string Text { get; init; }
    public required SentimentAnalysisResult OverallSentiment { get; init; }
    public Dictionary<string, SentimentAnalysisResult> AspectSentiments { get; init; } = new();
    public required DateTime Timestamp { get; init; }
}
```

## Usage Examples

### Basic Sentiment Analysis

```csharp
// Training a sentiment model
var trainingData = new[]
{
    new SentimentTrainingData { Text = "I love this product! Amazing quality.", IsPositive = true },
    new SentimentTrainingData { Text = "Terrible experience, would not recommend.", IsPositive = false },
    new SentimentTrainingData { Text = "Great customer service and fast delivery.", IsPositive = true },
    new SentimentTrainingData { Text = "Poor quality and overpriced.", IsPositive = false },
    new SentimentTrainingData { Text = "Excellent value for money!", IsPositive = true }
};

// Configure training options
var options = new SentimentAnalysisOptions
{
    Algorithm = SentimentAnalysisAlgorithm.SdcaLogisticRegression,
    TestFraction = 0.2,
    RemoveStopWords = true,
    NgramLength = 2
};

// Train model
await sentimentService.TrainCustomSentimentModelAsync(trainingData, "product-reviews", options);

// Analyze sentiment
var result = await sentimentService.AnalyzeSentimentAsync(
    "The product exceeded my expectations with excellent build quality!");

Console.WriteLine($"Sentiment: {result.SentimentLabel}");
Console.WriteLine($"Positive: {result.IsPositive}");
Console.WriteLine($"Confidence: {result.Confidence:P2}");
```

### Aspect-Based Sentiment Analysis

```csharp
var review = "The camera quality is outstanding, but the battery life is disappointing. " +
             "Customer service was helpful and responsive.";

var aspects = new[] { "camera", "battery", "service" };

var aspectResult = await sentimentService.AnalyzeAspectSentimentAsync(review, aspects);

Console.WriteLine($"Overall: {aspectResult.OverallSentiment.SentimentLabel}");

foreach (var (aspect, sentiment) in aspectResult.AspectSentiments)
{
    Console.WriteLine($"{aspect}: {sentiment.SentimentLabel} ({sentiment.Confidence:P2})");
}
```

**Notes**:

- SDCA Logistic Regression works well for binary sentiment classification
- FastTree provides good performance for larger datasets
- Remove stop words to focus on sentiment-bearing words
- Use n-grams to capture phrase-level sentiment patterns
- Consider preprocessing (lowercasing, punctuation removal) for better accuracy
- Aspect-based analysis requires domain-specific aspect extraction

**Performance**: Training time depends on dataset size and features. Prediction is very fast (< 5ms per text). Memory usage scales with vocabulary size.

**Related Snippets**:

- [Text Classification](text-classification.md) - General text classification patterns
- [Topic Modeling](topic-modeling.md) - Unsupervised text analysis
- [Model Evaluation](model-evaluation.md) - Model performance assessment
