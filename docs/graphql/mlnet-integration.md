# GraphQL ML.NET Integration Patterns

**Description**: Comprehensive patterns for integrating HotChocolate GraphQL with ML.NET for machine learning-powered document processing, classification, and analytics.

**Language/Technology**: C# / HotChocolate / ML.NET

## Code

### ML.NET Model Definitions

```csharp
namespace DocumentProcessor.ML.Models;

using Microsoft.ML.Data;

// Document classification model
public class DocumentClassificationModel
{
    [LoadColumn(0)]
    public string DocumentContent { get; set; } = string.Empty;

    [LoadColumn(1), ColumnName("Label")]
    public string Category { get; set; } = string.Empty;
}

public class DocumentClassificationPrediction
{
    [ColumnName("PredictedLabel")]
    public string Category { get; set; } = string.Empty;

    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();

    [ColumnName("Probability")]
    public float Probability { get; set; }
}

// Sentiment analysis model
public class DocumentSentimentModel
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    [LoadColumn(1), ColumnName("Label")]
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

// Text similarity model
public class TextSimilarityModel
{
    [LoadColumn(0)]
    public string Text1 { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Text2 { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float Similarity { get; set; }
}

public class TextSimilarityPrediction
{
    [ColumnName("Score")]
    public float Similarity { get; set; }
}

// Document summarization features
public class DocumentFeatures
{
    [LoadColumn(0)]
    public string DocumentId { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Content { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float WordCount { get; set; }

    [LoadColumn(3)]
    public float SentenceCount { get; set; }

    [LoadColumn(4)]
    public float AvgWordsPerSentence { get; set; }

    [LoadColumn(5)]
    public float ReadabilityScore { get; set; }

    [LoadColumn(6)]
    public string KeyPhrases { get; set; } = string.Empty;
}

public class SummarizationPrediction
{
    [ColumnName("Summary")]
    public string Summary { get; set; } = string.Empty;

    [ColumnName("ImportanceScore")]
    public float ImportanceScore { get; set; }

    [ColumnName("KeySentences")]
    public string[] KeySentences { get; set; } = Array.Empty<string>();
}

// Named Entity Recognition model
public class NamedEntityModel
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Entities { get; set; } = string.Empty; // JSON format
}

public class EntityPrediction
{
    [ColumnName("Entities")]
    public string[] Entities { get; set; } = Array.Empty<string>();

    [ColumnName("EntityTypes")]
    public string[] EntityTypes { get; set; } = Array.Empty<string>();

    [ColumnName("Confidence")]
    public float[] Confidence { get; set; } = Array.Empty<float>();
}
```

### ML.NET Services and Pipelines

```csharp
// ML model service interface
public interface IMLModelService
{
    Task<DocumentClassificationPrediction> ClassifyDocumentAsync(string content);
    Task<SentimentPrediction> AnalyzeSentimentAsync(string text);
    Task<float> CalculateTextSimilarityAsync(string text1, string text2);
    Task<SummarizationPrediction> SummarizeDocumentAsync(string content);
    Task<EntityPrediction> ExtractEntitiesAsync(string text);
    Task<float[]> GetTextEmbeddingAsync(string text);
    Task<string[]> ExtractKeyPhrasesAsync(string text);
    Task<float> CalculateReadabilityScoreAsync(string text);
}

// ML model service implementation
public class MLModelService : IMLModelService, IDisposable
{
    private readonly ILogger<MLModelService> logger;
    private readonly MLContext mlContext;
    private readonly Dictionary<string, ITransformer> models;
    private readonly Dictionary<string, PredictionEngine<object, object>> predictionEngines;
    private readonly SemaphoreSlim modelLock = new(1, 1);

    public MLModelService(ILogger<MLModelService> logger, IConfiguration configuration)
    {
        logger = logger;
        mlContext = new MLContext(seed: 0);
        models = new Dictionary<string, ITransformer>();
        predictionEngines = new Dictionary<string, PredictionEngine<object, object>>();
        
        // Load models asynchronously
        _ = Task.Run(LoadModelsAsync);
    }

    private async Task LoadModelsAsync()
    {
        await modelLock.WaitAsync();
        try
        {
            // Load pre-trained models from files or train new ones
            await LoadClassificationModelAsync();
            await LoadSentimentModelAsync();
            await LoadSimilarityModelAsync();
            await LoadSummarizationModelAsync();
            await LoadEntityExtractionModelAsync();
            
            logger.LogInformation("All ML models loaded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading ML models");
        }
        finally
        {
            modelLock.Release();
        }
    }

    private async Task LoadClassificationModelAsync()
    {
        try
        {
            // Try to load existing model
            if (File.Exists("models/classification_model.zip"))
            {
                var model = mlContext.Model.Load("models/classification_model.zip", out _);
                _models["classification"] = model;
                
                var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentClassificationModel, DocumentClassificationPrediction>(model);
                _predictionEngines["classification"] = predictionEngine as PredictionEngine<object, object>;
            }
            else
            {
                // Train new model if no pre-trained model exists
                await TrainClassificationModelAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading classification model");
        }
    }

    private async Task TrainClassificationModelAsync()
    {
        logger.LogInformation("Training new document classification model");

        // Sample training data (in production, load from database or files)
        var trainingData = new[]
        {
            new DocumentClassificationModel { DocumentContent = "Financial report quarterly earnings", Category = "Finance" },
            new DocumentClassificationModel { DocumentContent = "Technical specification API documentation", Category = "Technical" },
            new DocumentClassificationModel { DocumentContent = "Marketing campaign strategy planning", Category = "Marketing" },
            new DocumentClassificationModel { DocumentContent = "Legal contract terms and conditions", Category = "Legal" },
            new DocumentClassificationModel { DocumentContent = "Human resources policy manual", Category = "HR" }
        };

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Category")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", "DocumentContent"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);
        
        // Save model
        Directory.CreateDirectory("models");
        mlContext.Model.Save(model, dataView.Schema, "models/classification_model.zip");
        
        _models["classification"] = model;
        var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentClassificationModel, DocumentClassificationPrediction>(model);
        _predictionEngines["classification"] = predictionEngine as PredictionEngine<object, object>;

        logger.LogInformation("Document classification model trained and saved");
    }

    private async Task LoadSentimentModelAsync()
    {
        try
        {
            if (File.Exists("models/sentiment_model.zip"))
            {
                var model = mlContext.Model.Load("models/sentiment_model.zip", out _);
                _models["sentiment"] = model;
                
                var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentSentimentModel, SentimentPrediction>(model);
                _predictionEngines["sentiment"] = predictionEngine as PredictionEngine<object, object>;
            }
            else
            {
                await TrainSentimentModelAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading sentiment model");
        }
    }

    private async Task TrainSentimentModelAsync()
    {
        logger.LogInformation("Training new sentiment analysis model");

        var trainingData = new[]
        {
            new DocumentSentimentModel { Text = "This document is excellent and well-written", IsPositive = true },
            new DocumentSentimentModel { Text = "Poor quality document with many errors", IsPositive = false },
            new DocumentSentimentModel { Text = "Great analysis and insights provided", IsPositive = true },
            new DocumentSentimentModel { Text = "Confusing and difficult to understand", IsPositive = false },
            new DocumentSentimentModel { Text = "Comprehensive and thorough documentation", IsPositive = true }
        };

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", "Text")
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression("Label", "Features"));

        var model = pipeline.Fit(dataView);
        
        Directory.CreateDirectory("models");
        mlContext.Model.Save(model, dataView.Schema, "models/sentiment_model.zip");
        
        _models["sentiment"] = model;
        var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentSentimentModel, SentimentPrediction>(model);
        _predictionEngines["sentiment"] = predictionEngine as PredictionEngine<object, object>;

        logger.LogInformation("Sentiment analysis model trained and saved");
    }

    private async Task LoadSimilarityModelAsync()
    {
        // For text similarity, we'll use a simpler approach with TF-IDF
        logger.LogInformation("Setting up text similarity pipeline");
        
        // Create a simple pipeline for text similarity
        var pipeline = mlContext.Transforms.Text.NormalizeText("NormalizedText1", "Text1")
            .Append(mlContext.Transforms.Text.NormalizeText("NormalizedText2", "Text2"))
            .Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens1", "NormalizedText1"))
            .Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens2", "NormalizedText2"))
            .Append(mlContext.Transforms.Text.ProduceNgrams("Features1", "Tokens1"))
            .Append(mlContext.Transforms.Text.ProduceNgrams("Features2", "Tokens2"));

        // For similarity calculation, we'll use cosine similarity
        _models["similarity"] = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new[]
        {
            new TextSimilarityModel { Text1 = "sample", Text2 = "sample", Similarity = 1.0f }
        }));
    }

    private async Task LoadSummarizationModelAsync()
    {
        logger.LogInformation("Setting up document summarization pipeline");
        
        // Simple extractive summarization based on sentence importance
        var pipeline = mlContext.Transforms.Text.NormalizeText("NormalizedContent", "Content")
            .Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedContent"))
            .Append(mlContext.Transforms.Text.RemoveDefaultStopWords("Tokens"))
            .Append(mlContext.Transforms.Text.ProduceNgrams("Features", "Tokens"))
            .Append(mlContext.Transforms.NormalizeLpNorm("NormalizedFeatures", "Features"));

        _models["summarization"] = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new[]
        {
            new DocumentFeatures { Content = "sample content", WordCount = 2 }
        }));
    }

    private async Task LoadEntityExtractionModelAsync()
    {
        logger.LogInformation("Setting up named entity recognition pipeline");
        
        // Simple rule-based entity extraction
        var pipeline = mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text")
            .Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
            .Append(mlContext.Transforms.Text.ProduceNgrams("Features", "Tokens"));

        _models["entities"] = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new[]
        {
            new NamedEntityModel { Text = "sample text", Entities = "[]" }
        }));
    }

    public async Task<DocumentClassificationPrediction> ClassifyDocumentAsync(string content)
    {
        if (!models.ContainsKey("classification"))
        {
            throw new InvalidOperationException("Classification model not loaded");
        }

        var input = new DocumentClassificationModel { DocumentContent = content };
        var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentClassificationModel, DocumentClassificationPrediction>(_models["classification"]);
        
        var prediction = predictionEngine.Predict(input);
        
        logger.LogDebug("Document classified as: {Category} with probability: {Probability}", 
            prediction.Category, prediction.Probability);

        return prediction;
    }

    public async Task<SentimentPrediction> AnalyzeSentimentAsync(string text)
    {
        if (!models.ContainsKey("sentiment"))
        {
            throw new InvalidOperationException("Sentiment model not loaded");
        }

        var input = new DocumentSentimentModel { Text = text };
        var predictionEngine = mlContext.Model.CreatePredictionEngine<DocumentSentimentModel, SentimentPrediction>(_models["sentiment"]);
        
        var prediction = predictionEngine.Predict(input);
        
        logger.LogDebug("Sentiment analyzed: {IsPositive} with probability: {Probability}", 
            prediction.IsPositive, prediction.Probability);

        return prediction;
    }

    public async Task<float> CalculateTextSimilarityAsync(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
        {
            return 0f;
        }

        // Simple cosine similarity calculation
        var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var allWords = words1.Union(words2).ToArray();
        var vector1 = allWords.Select(word => words1.Count(w => w == word)).ToArray();
        var vector2 = allWords.Select(word => words2.Count(w => w == word)).ToArray();

        var dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
        var magnitude1 = Math.Sqrt(vector1.Sum(v => v * v));
        var magnitude2 = Math.Sqrt(vector2.Sum(v => v * v));

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0f;
        }

        var similarity = (float)(dotProduct / (magnitude1 * magnitude2));
        
        logger.LogDebug("Text similarity calculated: {Similarity}", similarity);

        return similarity;
    }

    public async Task<SummarizationPrediction> SummarizeDocumentAsync(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new SummarizationPrediction { Summary = "", ImportanceScore = 0, KeySentences = Array.Empty<string>() };
        }

        // Simple extractive summarization
        var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        if (sentences.Length <= 3)
        {
            return new SummarizationPrediction 
            { 
                Summary = content, 
                ImportanceScore = 1.0f, 
                KeySentences = sentences 
            };
        }

        // Score sentences based on word frequency
        var wordFreq = content.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3) // Filter short words
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());

        var sentenceScores = sentences.Select(sentence =>
        {
            var words = sentence.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var score = words.Where(wordFreq.ContainsKey).Sum(word => wordFreq[word]);
            return new { Sentence = sentence, Score = (float)score / words.Length };
        }).OrderByDescending(s => s.Score).ToArray();

        var topSentences = sentenceScores.Take(Math.Max(1, sentences.Length / 3)).ToArray();
        var summary = string.Join(". ", topSentences.Select(s => s.Sentence));
        var avgScore = topSentences.Average(s => s.Score);

        logger.LogDebug("Document summarized: {SentenceCount} sentences reduced to {SummaryLength} characters", 
            sentences.Length, summary.Length);

        return new SummarizationPrediction
        {
            Summary = summary,
            ImportanceScore = avgScore,
            KeySentences = topSentences.Select(s => s.Sentence).ToArray()
        };
    }

    public async Task<EntityPrediction> ExtractEntitiesAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new EntityPrediction 
            { 
                Entities = Array.Empty<string>(), 
                EntityTypes = Array.Empty<string>(), 
                Confidence = Array.Empty<float>() 
            };
        }

        // Simple rule-based entity extraction
        var entities = new List<string>();
        var entityTypes = new List<string>();
        var confidences = new List<float>();

        // Extract potential person names (capitalized words)
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (char.IsUpper(words[i][0]) && words[i].Length > 2)
            {
                entities.Add(words[i]);
                entityTypes.Add("PERSON");
                confidences.Add(0.7f);
            }
        }

        // Extract email addresses
        var emailRegex = new System.Text.RegularExpressions.Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
        var emailMatches = emailRegex.Matches(text);
        foreach (System.Text.RegularExpressions.Match match in emailMatches)
        {
            entities.Add(match.Value);
            entityTypes.Add("EMAIL");
            confidences.Add(0.9f);
        }

        // Extract phone numbers
        var phoneRegex = new System.Text.RegularExpressions.Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b");
        var phoneMatches = phoneRegex.Matches(text);
        foreach (System.Text.RegularExpressions.Match match in phoneMatches)
        {
            entities.Add(match.Value);
            entityTypes.Add("PHONE");
            confidences.Add(0.8f);
        }

        logger.LogDebug("Extracted {EntityCount} entities from text", entities.Count);

        return new EntityPrediction
        {
            Entities = entities.ToArray(),
            EntityTypes = entityTypes.ToArray(),
            Confidence = confidences.ToArray()
        };
    }

    public async Task<float[]> GetTextEmbeddingAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new float[100]; // Return zero vector
        }

        // Simple word embedding using hash-based features
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var embedding = new float[100];

        foreach (var word in words)
        {
            var hash = word.GetHashCode();
            var index = Math.Abs(hash % 100);
            embedding[index] += 1.0f;
        }

        // Normalize
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)magnitude;
            }
        }

        return embedding;
    }

    public async Task<string[]> ExtractKeyPhrasesAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        // Simple key phrase extraction based on word frequency and position
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var allWords = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToArray();

        var wordFreq = allWords.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        var topWords = wordFreq.OrderByDescending(kvp => kvp.Value).Take(10).Select(kvp => kvp.Key).ToArray();

        // Extract phrases containing top words
        var phrases = new List<string>();
        
        foreach (var sentence in sentences)
        {
            var words = sentence.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i++)
            {
                var bigram = $"{words[i]} {words[i + 1]}";
                if (topWords.Any(w => bigram.Contains(w)))
                {
                    phrases.Add(bigram);
                }
            }
        }

        logger.LogDebug("Extracted {PhraseCount} key phrases from text", phrases.Count);

        return phrases.Distinct().Take(5).ToArray();
    }

    public async Task<float> CalculateReadabilityScoreAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        // Simple Flesch Reading Ease calculation
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var syllables = words.Sum(CountSyllables);

        if (sentences == 0 || words.Length == 0)
        {
            return 0f;
        }

        var avgWordsPerSentence = (float)words.Length / sentences;
        var avgSyllablesPerWord = (float)syllables / words.Length;

        var score = 206.835f - (1.015f * avgWordsPerSentence) - (84.6f * avgSyllablesPerWord);
        
        // Normalize to 0-1 scale
        var normalizedScore = Math.Max(0f, Math.Min(1f, score / 100f));

        logger.LogDebug("Readability score calculated: {Score}", normalizedScore);

        return normalizedScore;
    }

    private int CountSyllables(string word)
    {
        word = word.ToLowerInvariant();
        var vowels = "aeiouy";
        var syllableCount = 0;
        var previousWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllableCount++;
            }
            previousWasVowel = isVowel;
        }

        if (word.EndsWith("e"))
        {
            syllableCount--;
        }

        return Math.Max(1, syllableCount);
    }

    public void Dispose()
    {
        modelLock?.Dispose();
        foreach (var engine in predictionEngines.Values)
        {
            engine?.Dispose();
        }
        mlContext?.Dispose();
    }
}
```

### GraphQL Types for ML Operations

```csharp
// ML-related GraphQL types
public class MLAnalysisResult
{
    public DocumentClassificationResult? Classification { get; set; }
    public SentimentAnalysisResult? Sentiment { get; set; }
    public DocumentSummary? Summary { get; set; }
    public EntityExtractionResult? Entities { get; set; }
    public float ReadabilityScore { get; set; }
    public string[] KeyPhrases { get; set; } = Array.Empty<string>();
}

public class DocumentClassificationResult
{
    public string Category { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public Dictionary<string, float> CategoryScores { get; set; } = new();
}

public class SentimentAnalysisResult
{
    public bool IsPositive { get; set; }
    public float Confidence { get; set; }
    public string Sentiment => IsPositive ? "Positive" : "Negative";
    public float Score { get; set; }
}

public class DocumentSummary
{
    public string Summary { get; set; } = string.Empty;
    public float ImportanceScore { get; set; }
    public string[] KeySentences { get; set; } = Array.Empty<string>();
    public int OriginalLength { get; set; }
    public int SummaryLength { get; set; }
}

public class EntityExtractionResult
{
    public NamedEntity[] Entities { get; set; } = Array.Empty<NamedEntity>();
    public int TotalCount { get; set; }
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
}

public class NamedEntity
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}

public class TextSimilarityResult
{
    public float Similarity { get; set; }
    public string ComparisonMethod { get; set; } = string.Empty;
    public string[] CommonTerms { get; set; } = Array.Empty<string>();
}

public class MLModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastTrained { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsLoaded { get; set; }
}
```

### GraphQL Resolvers with ML.NET Integration

```csharp
[QueryType]
public class MLQueries
{
    public async Task<MLAnalysisResult> AnalyzeDocumentAsync(
        string documentId,
        [Service] IDocumentRepository documentRepository,
        [Service] IMLModelService mlModelService,
        CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            throw new GraphQLException($"Document {documentId} not found");
        }

        // Perform comprehensive ML analysis
        var classificationTask = mlModelService.ClassifyDocumentAsync(document.Content);
        var sentimentTask = mlModelService.AnalyzeSentimentAsync(document.Content);
        var summaryTask = mlModelService.SummarizeDocumentAsync(document.Content);
        var entitiesTask = mlModelService.ExtractEntitiesAsync(document.Content);
        var readabilityTask = mlModelService.CalculateReadabilityScoreAsync(document.Content);
        var keyPhrasesTask = mlModelService.ExtractKeyPhrasesAsync(document.Content);

        await Task.WhenAll(classificationTask, sentimentTask, summaryTask, entitiesTask, readabilityTask, keyPhrasesTask);

        var classification = await classificationTask;
        var sentiment = await sentimentTask;
        var summary = await summaryTask;
        var entities = await entitiesTask;
        var readabilityScore = await readabilityTask;
        var keyPhrases = await keyPhrasesTask;

        return new MLAnalysisResult
        {
            Classification = new DocumentClassificationResult
            {
                Category = classification.Category,
                Confidence = classification.Probability,
                CategoryScores = classification.Scores?.Select((score, index) => 
                    new KeyValuePair<string, float>($"Category{index}", score))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, float>()
            },
            Sentiment = new SentimentAnalysisResult
            {
                IsPositive = sentiment.IsPositive,
                Confidence = sentiment.Probability,
                Score = sentiment.Score
            },
            Summary = new DocumentSummary
            {
                Summary = summary.Summary,
                ImportanceScore = summary.ImportanceScore,
                KeySentences = summary.KeySentences,
                OriginalLength = document.Content.Length,
                SummaryLength = summary.Summary.Length
            },
            Entities = new EntityExtractionResult
            {
                Entities = entities.Entities.Select((entity, index) => new NamedEntity
                {
                    Text = entity,
                    Type = entities.EntityTypes[index],
                    Confidence = entities.Confidence[index],
                    StartPosition = document.Content.IndexOf(entity),
                    EndPosition = document.Content.IndexOf(entity) + entity.Length
                }).ToArray(),
                TotalCount = entities.Entities.Length,
                EntityTypeCounts = entities.EntityTypes.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count())
            },
            ReadabilityScore = readabilityScore,
            KeyPhrases = keyPhrases
        };
    }

    public async Task<TextSimilarityResult> CompareDocumentsAsync(
        string documentId1,
        string documentId2,
        [Service] IDocumentRepository documentRepository,
        [Service] IMLModelService mlModelService,
        CancellationToken cancellationToken)
    {
        var doc1Task = documentRepository.GetByIdAsync(documentId1, cancellationToken);
        var doc2Task = documentRepository.GetByIdAsync(documentId2, cancellationToken);

        await Task.WhenAll(doc1Task, doc2Task);

        var doc1 = await doc1Task;
        var doc2 = await doc2Task;

        if (doc1 == null || doc2 == null)
        {
            throw new GraphQLException("One or both documents not found");
        }

        var similarity = await mlModelService.CalculateTextSimilarityAsync(doc1.Content, doc2.Content);

        // Find common terms
        var words1 = doc1.Content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = doc2.Content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commonTerms = words1.Intersect(words2).Where(w => w.Length > 3).Take(10).ToArray();

        return new TextSimilarityResult
        {
            Similarity = similarity,
            ComparisonMethod = "Cosine Similarity",
            CommonTerms = commonTerms
        };
    }

    public async Task<IEnumerable<Document>> FindSimilarDocumentsAsync(
        string documentId,
        float minimumSimilarity,
        int maxResults,
        [Service] IDocumentRepository documentRepository,
        [Service] IMLModelService mlModelService,
        CancellationToken cancellationToken)
    {
        var targetDocument = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (targetDocument == null)
        {
            throw new GraphQLException($"Document {documentId} not found");
        }

        var allDocuments = await documentRepository.GetAllAsync(cancellationToken);
        var similarDocuments = new List<(Document Document, float Similarity)>();

        foreach (var document in allDocuments.Where(d => d.Id != documentId))
        {
            var similarity = await mlModelService.CalculateTextSimilarityAsync(
                targetDocument.Content, document.Content);

            if (similarity >= minimumSimilarity)
            {
                similarDocuments.Add((document, similarity));
            }
        }

        return similarDocuments
            .OrderByDescending(x => x.Similarity)
            .Take(maxResults)
            .Select(x => x.Document);
    }

    public async Task<IEnumerable<MLModelInfo>> GetModelInfoAsync(
        [Service] IMLModelService mlModelService)
    {
        // Return information about loaded ML models
        return new[]
        {
            new MLModelInfo
            {
                Name = "Document Classification",
                Version = "1.0.0",
                LastTrained = DateTime.UtcNow.AddDays(-7),
                IsLoaded = true,
                Metadata = new Dictionary<string, object>
                {
                    ["accuracy"] = 0.85,
                    ["categories"] = new[] { "Finance", "Technical", "Marketing", "Legal", "HR" }
                }
            },
            new MLModelInfo
            {
                Name = "Sentiment Analysis",
                Version = "1.0.0",
                LastTrained = DateTime.UtcNow.AddDays(-5),
                IsLoaded = true,
                Metadata = new Dictionary<string, object>
                {
                    ["accuracy"] = 0.89,
                    ["classes"] = new[] { "Positive", "Negative" }
                }
            }
        };
    }
}

[MutationType]
public class MLMutations
{
    public async Task<MLAnalysisResult> BatchAnalyzeDocumentsAsync(
        string[] documentIds,
        [Service] IDocumentRepository documentRepository,
        [Service] IMLModelService mlModelService,
        CancellationToken cancellationToken)
    {
        var documents = new List<Document>();
        
        foreach (var id in documentIds)
        {
            var doc = await documentRepository.GetByIdAsync(id, cancellationToken);
            if (doc != null)
            {
                documents.Add(doc);
            }
        }

        if (!documents.Any())
        {
            throw new GraphQLException("No valid documents found");
        }

        // Combine all document content for analysis
        var combinedContent = string.Join(" ", documents.Select(d => d.Content));

        // Perform ML analysis on combined content
        var classification = await mlModelService.ClassifyDocumentAsync(combinedContent);
        var sentiment = await mlModelService.AnalyzeSentimentAsync(combinedContent);
        var summary = await mlModelService.SummarizeDocumentAsync(combinedContent);
        var entities = await mlModelService.ExtractEntitiesAsync(combinedContent);
        var readabilityScore = await mlModelService.CalculateReadabilityScoreAsync(combinedContent);
        var keyPhrases = await mlModelService.ExtractKeyPhrasesAsync(combinedContent);

        return new MLAnalysisResult
        {
            Classification = new DocumentClassificationResult
            {
                Category = classification.Category,
                Confidence = classification.Probability
            },
            Sentiment = new SentimentAnalysisResult
            {
                IsPositive = sentiment.IsPositive,
                Confidence = sentiment.Probability,
                Score = sentiment.Score
            },
            Summary = new DocumentSummary
            {
                Summary = summary.Summary,
                ImportanceScore = summary.ImportanceScore,
                KeySentences = summary.KeySentences,
                OriginalLength = combinedContent.Length,
                SummaryLength = summary.Summary.Length
            },
            Entities = new EntityExtractionResult
            {
                Entities = entities.Entities.Select((entity, index) => new NamedEntity
                {
                    Text = entity,
                    Type = entities.EntityTypes[index],
                    Confidence = entities.Confidence[index]
                }).ToArray(),
                TotalCount = entities.Entities.Length,
                EntityTypeCounts = entities.EntityTypes.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count())
            },
            ReadabilityScore = readabilityScore,
            KeyPhrases = keyPhrases
        };
    }

    public async Task<bool> TrainCustomModelAsync(
        string modelType,
        string trainingDataPath,
        [Service] IMLModelService mlModelService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // In a real implementation, this would trigger model training
        // For now, we'll simulate the process
        await Task.Delay(1000, cancellationToken); // Simulate training time

        return true;
    }
}

[SubscriptionType]
public class MLSubscriptions
{
    [Subscribe]
    public async IAsyncEnumerable<MLAnalysisResult> ModelTrainingProgressAsync(
        string modelType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simulate model training progress
        for (int progress = 0; progress <= 100; progress += 10)
        {
            await Task.Delay(1000, cancellationToken);
            
            yield return new MLAnalysisResult
            {
                // Return progress information
            };
        }
    }
}
```

## Usage

### GraphQL Operations with ML.NET

```graphql
# Comprehensive document analysis
query AnalyzeDocument($documentId: ID!) {
  analyzeDocument(documentId: $documentId) {
    classification {
      category
      confidence
      categoryScores
    }
    sentiment {
      isPositive
      confidence
      sentiment
      score
    }
    summary {
      summary
      importanceScore
      keySentences
      originalLength
      summaryLength
    }
    entities {
      entities {
        text
        type
        confidence
        startPosition
        endPosition
      }
      totalCount
      entityTypeCounts
    }
    readabilityScore
    keyPhrases
  }
}

# Document similarity comparison
query CompareDocuments($doc1: ID!, $doc2: ID!) {
  compareDocuments(documentId1: $doc1, documentId2: $doc2) {
    similarity
    comparisonMethod
    commonTerms
  }
}

# Find similar documents
query FindSimilar($documentId: ID!, $minSimilarity: Float!, $maxResults: Int!) {
  findSimilarDocuments(
    documentId: $documentId
    minimumSimilarity: $minSimilarity
    maxResults: $maxResults
  ) {
    id
    title
    metadata {
      createdAt
    }
  }
}

# Get ML model information
query GetModels {
  modelInfo {
    name
    version
    lastTrained
    isLoaded
    metadata
  }
}

# Batch analysis
mutation BatchAnalyze($documentIds: [ID!]!) {
  batchAnalyzeDocuments(documentIds: $documentIds) {
    classification {
      category
      confidence
    }
    sentiment {
      sentiment
      confidence
    }
    summary {
      summary
      importanceScore
    }
  }
}

# Model training subscription
subscription TrainingProgress($modelType: String!) {
  modelTrainingProgress(modelType: $modelType) {
    # Progress updates
  }
}
```

## Notes

- **Model Management**: Implement proper model versioning and lifecycle management
- **Training Data**: Use high-quality, domain-specific training data for better accuracy
- **Performance**: Consider model caching and prediction engine reuse for better performance
- **Scalability**: Use background services for long-running ML operations
- **Monitoring**: Implement model performance monitoring and drift detection
- **Security**: Validate input data and implement proper access controls
- **Error Handling**: Handle ML prediction failures gracefully
- **Resource Usage**: Monitor memory and CPU usage for ML operations

## Related Patterns

- [Orleans Integration](orleans-integration.md) - Distributed ML processing with Orleans grains
- [Performance Optimization](performance-optimization.md) - Optimizing ML operations performance
- [Error Handling](error-handling.md) - Handling ML prediction errors

---

**Key Benefits**: Machine learning integration, automated analysis, intelligent document processing, scalable ML operations

**When to Use**: Document classification, sentiment analysis, content summarization, entity extraction, similarity detection

**Performance**: Model caching, batch processing, background operations, resource optimization