# Local ML Development with Azure Migration Path

**Description**: Comprehensive guide for setting up local ML development environments using Azure emulators, local models, and provider patterns that enable seamless migration to Azure-hosted ML services.

**Technology**: .NET Aspire + ML.NET + Azurite + Local Models + Provider Pattern

## Overview

This guide demonstrates how to build a flexible ML architecture that supports local development with emulators and local models, while maintaining the ability to easily migrate to Azure-hosted services without code changes. The provider pattern enables switching between implementations based on configuration.

## Architecture Principles

- **Provider Pattern** - Abstract ML services behind interfaces with multiple implementations
- **Configuration-Driven** - Switch between local and Azure implementations via configuration
- **Local Emulation** - Use Azurite and local alternatives for Azure services
- **Gradual Migration** - Migrate services incrementally to Azure
- **Development Parity** - Maintain consistency between local and cloud environments

## Local Development Setup

### .NET Aspire App Host Configuration

```csharp
namespace DocumentProcessor.LocalDev.AppHost;

using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        
        // Configuration for local vs Azure
        var useAzureServices = builder.Configuration.GetValue<bool>("UseAzureServices", false);
        
        // Local storage emulation with Azurite
        var storage = useAzureServices 
            ? builder.AddAzureStorage("storage")
            : builder.AddAzureStorage("storage").RunAsEmulator();
            
        // Local Redis for caching
        var cache = builder.AddRedis("ml-cache")
            .WithRedisCommander();
            
        // Local PostgreSQL for ML metadata
        var postgres = builder.AddPostgres("postgres")
            .WithPgAdmin()
            .AddDatabase("ml-metadata");
            
        // Local vector database (Qdrant)
        var vectorDb = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
            .WithHttpEndpoint(port: 6333, targetPort: 6333, name: "http")
            .WithEndpoint(port: 6334, targetPort: 6334, name: "grpc")
            .WithBindMount("./data/qdrant", "/qdrant/storage")
            .WithEnvironment("QDRANT__SERVICE__HTTP_PORT", "6333")
            .WithEnvironment("QDRANT__SERVICE__GRPC_PORT", "6334");

        // Ollama for local LLM hosting
        var ollama = builder.AddContainer("ollama", "ollama/ollama:latest")
            .WithHttpEndpoint(port: 11434, targetPort: 11434)
            .WithBindMount("./data/ollama", "/root/.ollama")
            .WithEnvironment("OLLAMA_HOST", "0.0.0.0");
            
        // Text generation web UI (optional - for model management)
        var textGenUI = builder.AddContainer("text-generation-webui", "ghcr.io/oobabooga/text-generation-webui:latest")
            .WithHttpEndpoint(port: 7860, targetPort: 7860)
            .WithBindMount("./models", "/app/models")
            .WithBindMount("./data/text-gen-ui", "/app/text-generation-webui")
            .WithEnvironment("CLI_ARGS", "--listen --api");

        // ML Service with local and Azure providers
        var mlService = builder.AddProject<Projects.DocumentProcessor_ML_LocalDev>("ml-service")
            .WithReference(storage)
            .WithReference(cache)
            .WithReference(postgres)
            .WithReference(vectorDb)
            .WithReference(ollama)
            .WithEnvironment("MLProviders__UseLocal", (!useAzureServices).ToString())
            .WithEnvironment("MLProviders__ModelPath", "/app/models")
            .WithBindMount("./models", "/app/models")
            .WithBindMount("./data/ml-cache", "/app/cache");

        // Document processor
        var documentProcessor = builder.AddProject<Projects.DocumentProcessor_API>("document-processor")
            .WithReference(mlService)
            .WithReference(postgres)
            .WithHttpsEndpoint(port: 7001, name: "https");
            
        // Model management service
        var modelManager = builder.AddProject<Projects.DocumentProcessor_ModelManager>("model-manager")
            .WithReference(mlService)
            .WithReference(storage)
            .WithHttpsEndpoint(port: 7002, name: "https");

        var app = builder.Build();
        app.Run();
    }
}
```

### ML Provider Interface Design

```csharp
namespace DocumentProcessor.ML.Abstractions;

// Base ML service interfaces
public interface ITextAnalysisProvider
{
    Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);
    Task<KeyPhraseResult> ExtractKeyPhrasesAsync(string text, CancellationToken cancellationToken = default);
    Task<EntityResult> RecognizeEntitiesAsync(string text, CancellationToken cancellationToken = default);
    Task<LanguageResult> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public interface ITextEmbeddingProvider
{
    Task<EmbeddingResult> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<EmbeddingResult[]> GenerateEmbeddingsAsync(string[] texts, CancellationToken cancellationToken = default);
    Task<SimilarityResult> ComputeSimilarityAsync(float[] embedding1, float[] embedding2, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public interface ITextGenerationProvider
{
    Task<GenerationResult> GenerateTextAsync(string prompt, GenerationOptions? options = null, CancellationToken cancellationToken = default);
    Task<GenerationResult> SummarizeAsync(string text, SummarizationOptions? options = null, CancellationToken cancellationToken = default);
    Task<GenerationResult> ClassifyAsync(string text, string[] categories, CancellationToken cancellationToken = default);
    IAsyncEnumerable<GenerationChunk> GenerateStreamAsync(string prompt, GenerationOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public interface ICustomModelProvider
{
    Task<PredictionResult> PredictAsync<TInput, TOutput>(string modelId, TInput input, CancellationToken cancellationToken = default) 
        where TInput : class where TOutput : class;
    Task<BatchPredictionResult> PredictBatchAsync<TInput, TOutput>(string modelId, TInput[] inputs, CancellationToken cancellationToken = default) 
        where TInput : class where TOutput : class;
    Task LoadModelAsync(string modelId, ModelConfiguration configuration, CancellationToken cancellationToken = default);
    Task<ModelInfo> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);
    Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

// Provider factory for creating appropriate implementations
public interface IMLProviderFactory
{
    ITextAnalysisProvider CreateTextAnalysisProvider();
    ITextEmbeddingProvider CreateTextEmbeddingProvider();
    ITextGenerationProvider CreateTextGenerationProvider();
    ICustomModelProvider CreateCustomModelProvider();
}

// Configuration classes
public class MLProvidersConfiguration
{
    public bool UseLocal { get; set; } = true;
    public string ModelPath { get; set; } = "./models";
    public LocalProvidersConfiguration Local { get; set; } = new();
    public AzureProvidersConfiguration Azure { get; set; } = new();
}

public class LocalProvidersConfiguration
{
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string DefaultEmbeddingModel { get; set; } = "nomic-embed-text";
    public string DefaultGenerationModel { get; set; } = "llama3.1:8b";
    public string MLNetModelsPath { get; set; } = "./models/mlnet";
    public int MaxConcurrentRequests { get; set; } = 10;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class AzureProvidersConfiguration
{
    public string TextAnalyticsEndpoint { get; set; } = string.Empty;
    public string TextAnalyticsKey { get; set; } = string.Empty;
    public string OpenAIEndpoint { get; set; } = string.Empty;
    public string OpenAIKey { get; set; } = string.Empty;
    public string CognitiveServicesEndpoint { get; set; } = string.Empty;
    public string CognitiveServicesKey { get; set; } = string.Empty;
}
```

### Local Implementation Providers

```csharp
namespace DocumentProcessor.ML.Local;

using Microsoft.ML;
using System.Net.Http.Json;
using System.Text.Json;

// Local text analysis using ML.NET and rule-based approaches
public class LocalTextAnalysisProvider : ITextAnalysisProvider
{
    private readonly MLContext mlContext;
    private readonly ILogger<LocalTextAnalysisProvider> logger;
    private readonly LocalProvidersConfiguration configuration;
    private readonly Dictionary<string, ITransformer> loadedModels;

    public LocalTextAnalysisProvider(
        MLContext mlContext,
        IOptions<MLProvidersConfiguration> options,
        ILogger<LocalTextAnalysisProvider> logger)
    {
        mlContext = mlContext;
        configuration = options.Value.Local;
        logger = logger;
        loadedModels = new Dictionary<string, ITransformer>();
        
        _ = Task.Run(LoadModelsAsync);
    }

    public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (loadedModels.TryGetValue("sentiment", out var sentimentModel))
            {
                // Use ML.NET model for sentiment analysis
                var predictionEngine = mlContext.Model.CreatePredictionEngine<TextInput, SentimentPrediction>(sentimentModel);
                var prediction = predictionEngine.Predict(new TextInput { Text = text });
                
                return new SentimentResult(
                    prediction.IsPositive ? SentimentClass.Positive : SentimentClass.Negative,
                    prediction.Probability,
                    prediction.Score);
            }
            else
            {
                // Fallback to simple rule-based sentiment analysis
                return AnalyzeSentimentRuleBased(text);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze sentiment for text");
            return new SentimentResult(SentimentClass.Neutral, 0.5f, 0.0f);
        }
    }

    public async Task<KeyPhraseResult> ExtractKeyPhrasesAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple keyword extraction using TF-IDF
            var words = text.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3 && !IsStopWord(word))
                .GroupBy(word => word)
                .OrderByDescending(group => group.Count())
                .Take(10)
                .Select(group => group.Key)
                .ToArray();

            return new KeyPhraseResult(words, 0.8f);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract key phrases");
            return new KeyPhraseResult(Array.Empty<string>(), 0.0f);
        }
    }

    public async Task<EntityResult> RecognizeEntitiesAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple named entity recognition using patterns
            var entities = new List<EntityInfo>();
            
            // Email pattern
            var emailMatches = System.Text.RegularExpressions.Regex.Matches(text, 
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
            entities.AddRange(emailMatches.Select(m => new EntityInfo(m.Value, "Email", 0.9f)));
            
            // Phone pattern
            var phoneMatches = System.Text.RegularExpressions.Regex.Matches(text,
                @"\b\d{3}-\d{3}-\d{4}\b|\b\(\d{3}\)\s*\d{3}-\d{4}\b");
            entities.AddRange(phoneMatches.Select(m => new EntityInfo(m.Value, "Phone", 0.9f)));
            
            // Date pattern
            var dateMatches = System.Text.RegularExpressions.Regex.Matches(text,
                @"\b\d{1,2}\/\d{1,2}\/\d{4}\b|\b\d{4}-\d{2}-\d{2}\b");
            entities.AddRange(dateMatches.Select(m => new EntityInfo(m.Value, "Date", 0.8f)));

            return new EntityResult(entities.ToArray(), 0.7f);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recognize entities");
            return new EntityResult(Array.Empty<EntityInfo>(), 0.0f);
        }
    }

    public async Task<LanguageResult> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple language detection based on character patterns and common words
            var languageScores = new Dictionary<string, float>
            {
                ["en"] = CalculateEnglishScore(text),
                ["es"] = CalculateSpanishScore(text),
                ["fr"] = CalculateFrenchScore(text)
            };

            var detectedLanguage = languageScores.OrderByDescending(kvp => kvp.Value).First();
            
            return new LanguageResult(
                detectedLanguage.Key, 
                GetLanguageName(detectedLanguage.Key),
                detectedLanguage.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to detect language");
            return new LanguageResult("en", "English", 0.5f);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if ML.NET models are loaded
            return loadedModels.Any();
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadModelsAsync()
    {
        try
        {
            var modelsPath = configuration.MLNetModelsPath;
            if (Directory.Exists(modelsPath))
            {
                // Load sentiment model if it exists
                var sentimentModelPath = Path.Combine(modelsPath, "sentiment-model.zip");
                if (File.Exists(sentimentModelPath))
                {
                    var sentimentModel = mlContext.Model.Load(sentimentModelPath, out _);
                    _loadedModels["sentiment"] = sentimentModel;
                    logger.LogInformation("Loaded sentiment model from {Path}", sentimentModelPath);
                }

                // Load other models as needed
                var classificationModelPath = Path.Combine(modelsPath, "classification-model.zip");
                if (File.Exists(classificationModelPath))
                {
                    var classificationModel = mlContext.Model.Load(classificationModelPath, out _);
                    _loadedModels["classification"] = classificationModel;
                    logger.LogInformation("Loaded classification model from {Path}", classificationModelPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load ML.NET models");
        }
    }

    private SentimentResult AnalyzeSentimentRuleBased(string text)
    {
        var positiveWords = new[] { "good", "great", "excellent", "amazing", "wonderful", "fantastic", "love", "like" };
        var negativeWords = new[] { "bad", "terrible", "awful", "horrible", "hate", "dislike", "disappointing" };
        
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var positiveCount = words.Count(word => positiveWords.Contains(word));
        var negativeCount = words.Count(word => negativeWords.Contains(word));
        
        if (positiveCount > negativeCount)
        {
            var confidence = Math.Min(0.9f, 0.6f + (float)(positiveCount - negativeCount) / words.Length);
            return new SentimentResult(SentimentClass.Positive, confidence, confidence * 2 - 1);
        }
        else if (negativeCount > positiveCount)
        {
            var confidence = Math.Min(0.9f, 0.6f + (float)(negativeCount - positiveCount) / words.Length);
            return new SentimentResult(SentimentClass.Negative, confidence, -(confidence * 2 - 1));
        }
        else
        {
            return new SentimentResult(SentimentClass.Neutral, 0.5f, 0.0f);
        }
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should"
        };
        
        return stopWords.Contains(word);
    }

    private float CalculateEnglishScore(string text)
    {
        var englishWords = new[] { "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "man", "new", "now", "old", "see", "two", "way", "who", "boy", "did", "its", "let", "put", "say", "she", "too", "use" };
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var englishWordCount = words.Count(word => englishWords.Contains(word));
        return words.Length > 0 ? (float)englishWordCount / words.Length : 0f;
    }

    private float CalculateSpanishScore(string text)
    {
        var spanishWords = new[] { "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "del", "los", "las", "una", "todo", "esta", "como", "pero", "hay", "muy", "sin", "más", "vez", "ser", "dos" };
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var spanishWordCount = words.Count(word => spanishWords.Contains(word));
        return words.Length > 0 ? (float)spanishWordCount / words.Length : 0f;
    }

    private float CalculateFrenchScore(string text)
    {
        var frenchWords = new[] { "le", "de", "et", "à", "un", "il", "être", "et", "en", "avoir", "que", "pour", "dans", "ce", "son", "une", "sur", "avec", "ne", "se", "pas", "tout", "plus", "par", "grand", "il", "me", "même", "y", "ces", "là", "chez", "est", "elle", "vous", "ou", "au", "lui", "nous" };
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var frenchWordCount = words.Count(word => frenchWords.Contains(word));
        return words.Length > 0 ? (float)frenchWordCount / words.Length : 0f;
    }

    private string GetLanguageName(string languageCode)
    {
        return languageCode switch
        {
            "en" => "English",
            "es" => "Spanish", 
            "fr" => "French",
            _ => "Unknown"
        };
    }
}

// Local embedding provider using Ollama
public class LocalTextEmbeddingProvider : ITextEmbeddingProvider
{
    private readonly HttpClient httpClient;
    private readonly LocalProvidersConfiguration configuration;
    private readonly ILogger<LocalTextEmbeddingProvider> logger;
    private readonly JsonSerializerOptions jsonOptions;

    public LocalTextEmbeddingProvider(
        HttpClient httpClient,
        IOptions<MLProvidersConfiguration> options,
        ILogger<LocalTextEmbeddingProvider> logger)
    {
        httpClient = httpClient;
        configuration = options.Value.Local;
        logger = logger;
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        httpClient.BaseAddress = new Uri(configuration.OllamaEndpoint);
        httpClient.Timeout = configuration.RequestTimeout;
    }

    public async Task<EmbeddingResult> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = configuration.DefaultEmbeddingModel,
                prompt = text
            };

            var response = await httpClient.PostAsJsonAsync("/api/embeddings", request, jsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Ollama embedding request failed with status {StatusCode}", response.StatusCode);
                return new EmbeddingResult(Array.Empty<float>(), 0.0f);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(content, jsonOptions);
            
            return new EmbeddingResult(embeddingResponse?.Embedding ?? Array.Empty<float>(), 1.0f);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate embedding using Ollama");
            return new EmbeddingResult(Array.Empty<float>(), 0.0f);
        }
    }

    public async Task<EmbeddingResult[]> GenerateEmbeddingsAsync(string[] texts, CancellationToken cancellationToken = default)
    {
        var tasks = texts.Select(text => GenerateEmbeddingAsync(text, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    public async Task<SimilarityResult> ComputeSimilarityAsync(float[] embedding1, float[] embedding2, CancellationToken cancellationToken = default)
    {
        if (embedding1.Length != embedding2.Length)
        {
            return new SimilarityResult(0.0f, "Different embedding dimensions");
        }

        // Compute cosine similarity
        var dotProduct = embedding1.Zip(embedding2, (a, b) => a * b).Sum();
        var magnitude1 = Math.Sqrt(embedding1.Sum(a => a * a));
        var magnitude2 = Math.Sqrt(embedding2.Sum(a => a * a));
        
        var similarity = magnitude1 > 0 && magnitude2 > 0 ? dotProduct / (magnitude1 * magnitude2) : 0.0;
        
        return new SimilarityResult((float)similarity, "Cosine similarity");
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private record OllamaEmbeddingResponse(float[] Embedding);
}

// Local text generation using Ollama
public class LocalTextGenerationProvider : ITextGenerationProvider
{
    private readonly HttpClient httpClient;
    private readonly LocalProvidersConfiguration configuration;
    private readonly ILogger<LocalTextGenerationProvider> logger;
    private readonly JsonSerializerOptions jsonOptions;

    public LocalTextGenerationProvider(
        HttpClient httpClient,
        IOptions<MLProvidersConfiguration> options,
        ILogger<LocalTextGenerationProvider> logger)
    {
        httpClient = httpClient;
        configuration = options.Value.Local;
        logger = logger;
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        httpClient.BaseAddress = new Uri(configuration.OllamaEndpoint);
        httpClient.Timeout = configuration.RequestTimeout;
    }

    public async Task<GenerationResult> GenerateTextAsync(string prompt, GenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            options ??= new GenerationOptions();
            
            var request = new
            {
                model = configuration.DefaultGenerationModel,
                prompt = prompt,
                options = new
                {
                    temperature = options.Temperature,
                    top_p = options.TopP,
                    max_tokens = options.MaxTokens
                }
            };

            var response = await httpClient.PostAsJsonAsync("/api/generate", request, jsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Ollama generation request failed with status {StatusCode}", response.StatusCode);
                return new GenerationResult(string.Empty, 0.0f, "Generation failed");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var generatedText = string.Empty;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var generationResponse = JsonSerializer.Deserialize<OllamaGenerationResponse>(line, jsonOptions);
                if (generationResponse?.Response != null)
                {
                    generatedText += generationResponse.Response;
                }
                
                if (generationResponse?.Done == true)
                {
                    break;
                }
            }
            
            return new GenerationResult(generatedText.Trim(), 1.0f, "Success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate text using Ollama");
            return new GenerationResult(string.Empty, 0.0f, ex.Message);
        }
    }

    public async Task<GenerationResult> SummarizeAsync(string text, SummarizationOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new SummarizationOptions();
        
        var prompt = $"""
            Please provide a {options.Length} summary of the following text:
            
            {text}
            
            Summary:
            """;
            
        return await GenerateTextAsync(prompt, new GenerationOptions
        {
            Temperature = 0.3f,
            MaxTokens = options.MaxTokens
        }, cancellationToken);
    }

    public async Task<GenerationResult> ClassifyAsync(string text, string[] categories, CancellationToken cancellationToken = default)
    {
        var categoriesText = string.Join(", ", categories);
        
        var prompt = $"""
            Classify the following text into one of these categories: {categoriesText}
            
            Text: {text}
            
            Category:
            """;
            
        return await GenerateTextAsync(prompt, new GenerationOptions
        {
            Temperature = 0.1f,
            MaxTokens = 50
        }, cancellationToken);
    }

    public async IAsyncEnumerable<GenerationChunk> GenerateStreamAsync(
        string prompt, 
        GenerationOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new GenerationOptions();
        
        var request = new
        {
            model = configuration.DefaultGenerationModel,
            prompt = prompt,
            stream = true,
            options = new
            {
                temperature = options.Temperature,
                top_p = options.TopP,
                max_tokens = options.MaxTokens
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(request, options: jsonOptions)
        };

        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var generationResponse = JsonSerializer.Deserialize<OllamaGenerationResponse>(line, jsonOptions);
            if (generationResponse?.Response != null)
            {
                yield return new GenerationChunk(generationResponse.Response, !generationResponse.Done);
            }
            
            if (generationResponse?.Done == true)
            {
                break;
            }
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private record OllamaGenerationResponse(string? Response, bool Done);
}
```

### Service Registration and Configuration

```csharp
namespace DocumentProcessor.ML.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Azure.AI.TextAnalytics;
using Azure.AI.OpenAI;

public static class MLServicesExtensions
{
    public static IServiceCollection AddMLServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mlConfig = configuration.GetSection("MLProviders").Get<MLProvidersConfiguration>() ?? new();
        services.Configure<MLProvidersConfiguration>(configuration.GetSection("MLProviders"));

        // Register ML.NET context
        services.AddSingleton<MLContext>();

        // Register provider factory
        services.AddScoped<IMLProviderFactory>(provider => 
        {
            return mlConfig.UseLocal 
                ? new LocalMLProviderFactory(provider)
                : new AzureMLProviderFactory(provider);
        });

        if (mlConfig.UseLocal)
        {
            // Register local implementations
            services.AddHttpClient<LocalTextEmbeddingProvider>();
            services.AddHttpClient<LocalTextGenerationProvider>();
            services.AddScoped<ITextAnalysisProvider, LocalTextAnalysisProvider>();
            services.AddScoped<ITextEmbeddingProvider, LocalTextEmbeddingProvider>();
            services.AddScoped<ITextGenerationProvider, LocalTextGenerationProvider>();
            services.AddScoped<ICustomModelProvider, LocalCustomModelProvider>();
        }
        else
        {
            // Register Azure implementations
            services.AddSingleton<TextAnalyticsClient>(provider =>
                new TextAnalyticsClient(
                    new Uri(mlConfig.Azure.TextAnalyticsEndpoint),
                    new AzureKeyCredential(mlConfig.Azure.TextAnalyticsKey)));
                    
            services.AddSingleton<OpenAIClient>(provider =>
                new OpenAIClient(
                    new Uri(mlConfig.Azure.OpenAIEndpoint),
                    new AzureKeyCredential(mlConfig.Azure.OpenAIKey)));

            services.AddScoped<ITextAnalysisProvider, AzureTextAnalysisProvider>();
            services.AddScoped<ITextEmbeddingProvider, AzureTextEmbeddingProvider>();
            services.AddScoped<ITextGenerationProvider, AzureTextGenerationProvider>();
            services.AddScoped<ICustomModelProvider, AzureCustomModelProvider>();
        }

        // Register high-level ML orchestrator
        services.AddScoped<IMLOrchestrator, MLOrchestrator>();

        // Register health checks
        services.AddHealthChecks()
            .AddCheck<MLProvidersHealthCheck>("ml-providers");

        return services;
    }
}

public class LocalMLProviderFactory : IMLProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    public LocalMLProviderFactory(IServiceProvider serviceProvider)
    {
        serviceProvider = serviceProvider;
    }

    public ITextAnalysisProvider CreateTextAnalysisProvider() =>
        serviceProvider.GetRequiredService<LocalTextAnalysisProvider>();

    public ITextEmbeddingProvider CreateTextEmbeddingProvider() =>
        serviceProvider.GetRequiredService<LocalTextEmbeddingProvider>();

    public ITextGenerationProvider CreateTextGenerationProvider() =>
        serviceProvider.GetRequiredService<LocalTextGenerationProvider>();

    public ICustomModelProvider CreateCustomModelProvider() =>
        serviceProvider.GetRequiredService<LocalCustomModelProvider>();
}

public class AzureMLProviderFactory : IMLProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    public AzureMLProviderFactory(IServiceProvider serviceProvider)
    {
        serviceProvider = serviceProvider;
    }

    public ITextAnalysisProvider CreateTextAnalysisProvider() =>
        serviceProvider.GetRequiredService<AzureTextAnalysisProvider>();

    public ITextEmbeddingProvider CreateTextEmbeddingProvider() =>
        serviceProvider.GetRequiredService<AzureTextEmbeddingProvider>();

    public ITextGenerationProvider CreateTextGenerationProvider() =>
        serviceProvider.GetRequiredService<AzureTextGenerationProvider>();

    public ICustomModelProvider CreateCustomModelProvider() =>
        serviceProvider.GetRequiredService<AzureCustomModelProvider>();
}
```

### Configuration Files

```json
// appsettings.Development.json
{
  "MLProviders": {
    "UseLocal": true,
    "ModelPath": "./models",
    "Local": {
      "OllamaEndpoint": "http://localhost:11434",
      "DefaultEmbeddingModel": "nomic-embed-text", 
      "DefaultGenerationModel": "llama3.1:8b",
      "MLNetModelsPath": "./models/mlnet",
      "MaxConcurrentRequests": 5,
      "RequestTimeout": "00:05:00"
    }
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=ml-metadata;Username=postgres;Password=postgres",
    "Redis": "localhost:6379",
    "Qdrant": "http://localhost:6333"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DocumentProcessor.ML": "Debug"
    }
  }
}
```

```json
// appsettings.Production.json
{
  "MLProviders": {
    "UseLocal": false,
    "Azure": {
      "TextAnalyticsEndpoint": "https://your-text-analytics.cognitiveservices.azure.com/",
      "TextAnalyticsKey": "${AZURE_TEXT_ANALYTICS_KEY}",
      "OpenAIEndpoint": "https://your-openai.openai.azure.com/",
      "OpenAIKey": "${AZURE_OPENAI_KEY}",
      "CognitiveServicesEndpoint": "https://your-cognitive-services.cognitiveservices.azure.com/",
      "CognitiveServicesKey": "${AZURE_COGNITIVE_SERVICES_KEY}"
    }
  },
  "ConnectionStrings": {
    "PostgreSQL": "${AZURE_POSTGRES_CONNECTION_STRING}",
    "Redis": "${AZURE_REDIS_CONNECTION_STRING}",
    "Storage": "${AZURE_STORAGE_CONNECTION_STRING}"
  }
}
```

### Docker Compose for Local Development

```yaml
# docker-compose.local.yml
version: '3.8'

services:
  # Local ML infrastructure
  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
    volumes:
      - ./data/ollama:/root/.ollama
    environment:
      - OLLAMA_HOST=0.0.0.0
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:11434/api/tags"]
      interval: 30s
      timeout: 10s
      retries: 3

  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333" # HTTP
      - "6334:6334" # gRPC
    volumes:
      - ./data/qdrant:/qdrant/storage
    environment:
      - QDRANT__SERVICE__HTTP_PORT=6333
      - QDRANT__SERVICE__GRPC_PORT=6334
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  postgres:
    image: postgres:15-alpine
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_DB=ml-metadata
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    volumes:
      - ./data/postgres:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Azure emulators
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    ports:
      - "10000:10000" # Blob service
      - "10001:10001" # Queue service 
      - "10002:10002" # Table service
    volumes:
      - ./data/azurite:/workspace
    command: azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --location /workspace --debug /workspace/debug.log

  # Optional: Text generation WebUI for model management
  text-generation-webui:
    image: ghcr.io/oobabooga/text-generation-webui:latest
    ports:
      - "7860:7860"
    volumes:
      - ./models:/app/models
      - ./data/text-gen-ui:/app/text-generation-webui
    environment:
      - CLI_ARGS=--listen --api --model-dir /app/models
    dependson:
      - ollama
```

### Model Setup Scripts

```bash
#!/bin/bash
# setup-local-models.sh

echo "Setting up local ML models..."

# Create directories
mkdir -p ./models/mlnet
mkdir -p ./models/huggingface
mkdir -p ./data/ollama
mkdir -p ./data/qdrant
mkdir -p ./data/postgres
mkdir -p ./data/redis

# Pull Ollama models
echo "Pulling Ollama models..."
ollama pull nomic-embed-text
ollama pull llama3.1:8b
ollama pull mistral:7b

# Download sample ML.NET models (you would replace these with your actual models)
echo "Setting up ML.NET models..."
# Example: download pre-trained sentiment model
# wget -O ./models/mlnet/sentiment-model.zip "https://example.com/sentiment-model.zip"

echo "Local ML setup complete!"
echo "Start the services with: docker-compose -f docker-compose.local.yml up -d"
```

```powershell
# setup-local-models.ps1
Write-Host "Setting up local ML models..." -ForegroundColor Green

# Create directories
New-Item -ItemType Directory -Force -Path ".\models\mlnet"
New-Item -ItemType Directory -Force -Path ".\models\huggingface" 
New-Item -ItemType Directory -Force -Path ".\data\ollama"
New-Item -ItemType Directory -Force -Path ".\data\qdrant"
New-Item -ItemType Directory -Force -Path ".\data\postgres"
New-Item -ItemType Directory -Force -Path ".\data\redis"

# Pull Ollama models
Write-Host "Pulling Ollama models..." -ForegroundColor Yellow
ollama pull nomic-embed-text
ollama pull llama3.1:8b
ollama pull mistral:7b

Write-Host "Local ML setup complete!" -ForegroundColor Green
Write-Host "Start the services with: docker-compose -f docker-compose.local.yml up -d"
```

## Migration Path to Azure

### Environment-Based Configuration

```csharp
namespace DocumentProcessor.Configuration;

public class EnvironmentMLConfiguration
{
    public static MLProvidersConfiguration GetConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var mlConfig = new MLProvidersConfiguration();
        
        // Default to local for development
        if (environment.IsDevelopment())
        {
            mlConfig.UseLocal = configuration.GetValue<bool>("MLProviders:UseLocal", true);
        }
        else if (environment.IsStaging())
        {
            // Staging can use either local or Azure based on configuration
            mlConfig.UseLocal = configuration.GetValue<bool>("MLProviders:UseLocal", false);
        }
        else if (environment.IsProduction())
        {
            // Production should use Azure by default
            mlConfig.UseLocal = configuration.GetValue<bool>("MLProviders:UseLocal", false);
        }

        configuration.GetSection("MLProviders").Bind(mlConfig);
        return mlConfig;
    }
}

// Usage in Program.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure ML services based on environment
        var mlConfig = EnvironmentMLConfiguration.GetConfiguration(
            builder.Configuration, 
            builder.Environment);
            
        builder.Services.Configure<MLProvidersConfiguration>(options =>
        {
            options.UseLocal = mlConfig.UseLocal;
            options.Local = mlConfig.Local;
            options.Azure = mlConfig.Azure;
        });
        
        builder.Services.AddMLServices(builder.Configuration);
        
        var app = builder.Build();
        app.Run();
    }
}
```

### Gradual Migration Strategy

1. **Phase 1: Local Development**
   - Use local models and emulators
   - Develop and test all functionality locally
   - Validate provider pattern works correctly

2. **Phase 2: Hybrid Approach**
   - Migrate text analytics to Azure Text Analytics
   - Keep embeddings and generation local
   - Test Azure connectivity and performance

3. **Phase 3: Cloud-First**
   - Migrate embeddings to Azure OpenAI
   - Use Azure AI services for all operations
   - Keep local fallbacks for development

4. **Phase 4: Full Azure**
   - Deploy custom models to Azure ML
   - Use Azure Container Instances for custom processing
   - Maintain local development environment

## Best Practices

### Local Development

- **Model Versioning** - Use consistent model versions across environments
- **Resource Management** - Monitor local resource usage (CPU, memory, disk)
- **Caching Strategy** - Implement aggressive caching for development speed
- **Health Monitoring** - Add health checks for all local services

### Azure Migration

- **Configuration Management** - Use Azure Key Vault for secrets
- **Cost Optimization** - Monitor Azure AI service costs and optimize usage
- **Performance Testing** - Compare local vs Azure performance
- **Disaster Recovery** - Implement fallback to local services if needed

### Security

- **API Key Management** - Never commit API keys to source control
- **Network Security** - Use secure connections for all external services  
- **Data Privacy** - Ensure local data doesn't contain sensitive information
- **Access Control** - Implement proper authentication for all services

---

**Key Benefits**: Flexible development environment, seamless Azure migration, cost-effective local development, consistent API patterns

**When to Use**: ML development teams, Azure migration projects, cost-conscious development, hybrid deployment scenarios

**Performance**: Local models for development speed, Azure services for production scale, configurable provider switching