using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Database.MLDatabaseExamples;

// ML Database Technologies - Supporting Models and Services
[Table("experiments", Schema = "ml_schema")]
public class MLExperiment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [Column("model_type")]
    public string ModelType { get; set; } = string.Empty;
    
    public JsonDocument? Parameters { get; set; }
    public JsonDocument? Metrics { get; set; }
    public string Status { get; set; } = "created";
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

[Table("document_embeddings", Schema = "ml_schema")]
public class DocumentEmbedding
{
    public Guid Id { get; set; }
    
    [Column("document_id")]
    public string DocumentId { get; set; } = string.Empty;
    
    [Column("content_hash")]
    public string ContentHash { get; set; } = string.Empty;
    
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    [Column("modelName")]
    public string ModelName { get; set; } = string.Empty;
    
    [Column("chunk_index")]
    public int ChunkIndex { get; set; }
    
    public JsonDocument? Metadata { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

// PostgreSQL ML Service (simplified for demo)
public class PostgresMLService
{
    private readonly List<MLExperiment> experiments = new();
    private readonly List<DocumentEmbedding> embeddings = new();
    
    public async Task<Guid> CreateExperimentAsync(
        string name, 
        string modelType, 
        Dictionary<string, object> parameters,
        string? description = null)
    {
        await Task.Delay(10); // Simulate async operation
        
        var experiment = new MLExperiment
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ModelType = modelType,
            Parameters = JsonDocument.Parse(JsonSerializer.Serialize(parameters)),
            Status = "created",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        experiments.Add(experiment);
        return experiment.Id;
    }
    
    public async Task<List<DocumentEmbedding>> FindSimilarDocumentsAsync(
        float[] queryEmbedding, 
        string modelName, 
        int limit = 10)
    {
        await Task.Delay(10);
        
        return embeddings
            .Where(e => e.ModelName == modelName)
            .OrderBy(e => CalculateCosineSimilarity(queryEmbedding, e.Embedding))
            .Take(limit)
            .ToList();
    }
    
    public async Task UpdateExperimentStatusAsync(Guid experimentId, string status, Dictionary<string, object>? metrics = null)
    {
        await Task.Delay(10);
        
        var experiment = experiments.FirstOrDefault(e => e.Id == experimentId);
        if (experiment != null)
        {
            experiment.Status = status;
            experiment.UpdatedAt = DateTime.UtcNow;
            if (status == "completed")
            {
                experiment.CompletedAt = DateTime.UtcNow;
            }
            if (metrics != null)
            {
                experiment.Metrics = JsonDocument.Parse(JsonSerializer.Serialize(metrics));
            }
        }
    }
    
    public Task<List<MLExperiment>> GetExperimentsAsync(string? modelType = null)
    {
        var result = modelType == null 
            ? experiments.ToList() 
            : experiments.Where(e => e.ModelType == modelType).ToList();
        return Task.FromResult(result);
    }
    
    private static double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

// ChromaDB Service (simplified for demo)
public class ChromaVectorService
{
    private readonly Dictionary<string, List<ChromaDocument>> collections = new();
    
    public async Task<string> CreateCollectionAsync(
        string name, 
        Dictionary<string, object>? metadata = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(name))
        {
            collections[name] = new List<ChromaDocument>();
        }
        
        return name;
    }
    
    public async Task AddDocumentsAsync(
        string collectionName,
        List<ChromaDocument> documents)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionName))
        {
            collections[collectionName] = new List<ChromaDocument>();
        }
        
        collections[collectionName].AddRange(documents);
    }
    
    public async Task<List<ChromaQueryResult>> QuerySimilarAsync(
        string collectionName,
        float[] queryEmbedding,
        int nResults = 10,
        Dictionary<string, object>? filter = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionName))
        {
            return new List<ChromaQueryResult>();
        }
        
        var documents = collections[collectionName];
        
        // Apply filter if provided
        if (filter != null)
        {
            documents = documents.Where(d => 
                filter.All(f => d.Metadata.ContainsKey(f.Key) && 
                               d.Metadata[f.Key].Equals(f.Value))).ToList();
        }
        
        var results = documents
            .Select(d => new ChromaQueryResult
            {
                Id = d.Id,
                Document = d.Content,
                Distance = 1.0 - CalculateCosineSimilarity(queryEmbedding, d.Embedding),
                Metadata = d.Metadata
            })
            .OrderBy(r => r.Distance)
            .Take(nResults)
            .ToList();
        
        return results;
    }
    
    private static double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

// DuckDB Analytics Service (simplified for demo)
public class DuckDBAnalyticsService
{
    private readonly List<ExperimentPerformance> performances = new();
    private readonly List<ModelPrediction> predictions = new();
    
    public async Task RecordExperimentPerformanceAsync(ExperimentPerformance performance)
    {
        await Task.Delay(10);
        performances.Add(performance);
    }
    
    public async Task<List<ModelComparison>> CompareModelsAsync(string[] modelNames)
    {
        await Task.Delay(10);
        
        var comparisons = new List<ModelComparison>();
        
        foreach (var modelName in modelNames)
        {
            var modelPerformances = performances.Where(p => p.ModelName == modelName).ToList();
            
            if (modelPerformances.Any())
            {
                comparisons.Add(new ModelComparison
                {
                    ModelName = modelName,
                    TotalExperiments = modelPerformances.Count,
                    AverageAccuracy = modelPerformances.Average(p => p.Accuracy),
                    BestAccuracy = modelPerformances.Max(p => p.Accuracy),
                    WorstAccuracy = modelPerformances.Min(p => p.Accuracy),
                    AverageTrainingTime = modelPerformances.Average(p => p.TrainingTimeSeconds),
                    AverageInferenceTime = modelPerformances.Average(p => p.InferenceTimeMs),
                    AverageF1Score = modelPerformances.Average(p => p.F1Score)
                });
            }
        }
        
        return comparisons.OrderByDescending(c => c.AverageAccuracy).ToList();
    }
    
    public async Task StoreBatchPredictionsAsync(List<ModelPrediction> newPredictions)
    {
        await Task.Delay(10);
        predictions.AddRange(newPredictions);
    }
    
    public async Task<List<ModelPerformanceTrend>> GetPerformanceTrendsAsync(string modelName, int days = 30)
    {
        await Task.Delay(10);
        
        var startDate = DateTime.UtcNow.AddDays(-days);
        var modelPerformances = performances
            .Where(p => p.ModelName == modelName)
            .GroupBy(p => p.ExperimentId) // Group by experiment for trend analysis
            .Select((g, index) => new ModelPerformanceTrend
            {
                Date = startDate.AddDays(index), // Simulate dates for demo
                ModelName = modelName,
                AverageAccuracy = g.Average(p => p.Accuracy),
                AccuracyStdDev = CalculateStandardDeviation(g.Select(p => p.Accuracy)),
                AverageInferenceTime = g.Average(p => p.InferenceTimeMs),
                P95InferenceTime = g.Select(p => p.InferenceTimeMs).OrderByDescending(x => x).Take((int)Math.Ceiling(g.Count() * 0.05)).FirstOrDefault(),
                ExperimentCount = g.Count()
            })
            .ToList();
        
        return modelPerformances;
    }
    
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var enumerable = values.ToList();
        var avg = enumerable.Average();
        var sum = enumerable.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sum / enumerable.Count);
    }
}

// Supporting data models
public class ChromaDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChromaQueryResult
{
    public string Id { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public double Distance { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ExperimentPerformance
{
    public Guid ExperimentId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DatasetName { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int TrainingTimeSeconds { get; set; }
    public double InferenceTimeMs { get; set; }
    public double MemoryUsageMb { get; set; }
}

public class ModelPerformanceTrend
{
    public DateTime Date { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public double AccuracyStdDev { get; set; }
    public double AverageInferenceTime { get; set; }
    public double P95InferenceTime { get; set; }
    public int ExperimentCount { get; set; }
}

public class ModelComparison
{
    public string ModelName { get; set; } = string.Empty;
    public int TotalExperiments { get; set; }
    public double AverageAccuracy { get; set; }
    public double BestAccuracy { get; set; }
    public double WorstAccuracy { get; set; }
    public double AverageTrainingTime { get; set; }
    public double AverageInferenceTime { get; set; }
    public double AverageF1Score { get; set; }
}

public class ModelPrediction
{
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, object> InputFeatures { get; set; } = new();
    public double PredictedValue { get; set; }
    public double ActualValue { get; set; }
    public double Confidence { get; set; }
}

// Main demo service
public class MLDatabaseDemoService
{
    private readonly PostgresMLService postgresService;
    private readonly ChromaVectorService chromaService;
    private readonly DuckDBAnalyticsService duckdbService;
    
    public MLDatabaseDemoService()
    {
        postgresService = new PostgresMLService();
        chromaService = new ChromaVectorService();
        duckdbService = new DuckDBAnalyticsService();
    }
    
    public async Task RunCompleteDemo()
    {
        Console.WriteLine("ML Database Technologies - Complete Beginner Examples");
        Console.WriteLine("====================================================");

        await RunPostgreSQLDemo();
        await RunChromaDemo();
        await RunDuckDBDemo();
        await RunIntegratedWorkflowDemo();

        Console.WriteLine("\nML Database demonstration completed!");
        Console.WriteLine("Technologies demonstrated:");
        Console.WriteLine("- PostgreSQL with pgvector for ML experiments and embeddings");
        Console.WriteLine("- ChromaDB for vector similarity search");
        Console.WriteLine("- DuckDB for ML analytics and performance tracking");
        Console.WriteLine("- Integrated ML workflow combining all technologies");
    }
    
    private async Task RunPostgreSQLDemo()
    {
        Console.WriteLine("\n1. PostgreSQL with pgvector Demo:");
        Console.WriteLine("   Setting up ML experiment tracking...");
        
        // Create ML experiments
        var experimentId1 = await postgresService.CreateExperimentAsync(
            name: "Sentiment Analysis v1",
            modelType: "TextClassification",
            parameters: new Dictionary<string, object>
            {
                ["learning_rate"] = 0.001,
                ["batch_size"] = 32,
                ["epochs"] = 10
            },
            description: "Initial sentiment analysis model training"
        );
        
        var experimentId2 = await postgresService.CreateExperimentAsync(
            name: "Image Classification v1",
            modelType: "ImageClassification",
            parameters: new Dictionary<string, object>
            {
                ["learning_rate"] = 0.01,
                ["batch_size"] = 64,
                ["epochs"] = 20
            },
            description: "Convolutional neural network for image classification"
        );

        Console.WriteLine($"   ✅ Created experiment: {experimentId1} (Sentiment Analysis)");
        Console.WriteLine($"   ✅ Created experiment: {experimentId2} (Image Classification)");
        
        // Update experiment statuses
        await postgresService.UpdateExperimentStatusAsync(experimentId1, "completed", 
            new Dictionary<string, object> { ["accuracy"] = 0.89, ["f1_score"] = 0.87 });
        
        await postgresService.UpdateExperimentStatusAsync(experimentId2, "running");
        
        Console.WriteLine("   ✅ Updated experiment statuses and metrics");
        
        // Query experiments
        var allExperiments = await postgresService.GetExperimentsAsync();
        var textExperiments = await postgresService.GetExperimentsAsync("TextClassification");
        
        Console.WriteLine($"   📊 Total experiments: {allExperiments.Count}");
        Console.WriteLine($"   📊 Text classification experiments: {textExperiments.Count}");
    }
    
    private async Task RunChromaDemo()
    {
        Console.WriteLine("\n2. ChromaDB Vector Database Demo:");
        Console.WriteLine("   Setting up document similarity search...");
        
        // Create collection
        var collectionName = "document_embeddings";
        await chromaService.CreateCollectionAsync(collectionName);
        Console.WriteLine($"   ✅ Created collection: {collectionName}");
        
        // Add sample documents
        var documents = new List<ChromaDocument>
        {
            new() 
            { 
                Id = "doc1", 
                Content = "Positive review about the product quality",
                Embedding = GenerateRandomEmbedding(384), // Typical sentence transformer dimension
                Metadata = new Dictionary<string, object> { ["sentiment"] = "positive", ["category"] = "review" }
            },
            new() 
            { 
                Id = "doc2", 
                Content = "Negative feedback about service quality",
                Embedding = GenerateRandomEmbedding(384),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "negative", ["category"] = "feedback" }
            },
            new() 
            { 
                Id = "doc3", 
                Content = "Neutral comment about the website design",
                Embedding = GenerateRandomEmbedding(384),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "neutral", ["category"] = "comment" }
            },
            new() 
            { 
                Id = "doc4", 
                Content = "Excellent product recommendation for users",
                Embedding = GenerateRandomEmbedding(384),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "positive", ["category"] = "review" }
            }
        };
        
        await chromaService.AddDocumentsAsync(collectionName, documents);
        Console.WriteLine($"   ✅ Added {documents.Count} documents with embeddings");
        
        // Query for similar documents
        var queryEmbedding = GenerateRandomEmbedding(384);
        var allResults = await chromaService.QuerySimilarAsync(collectionName, queryEmbedding, nResults: 3);
        
        var filteredResults = await chromaService.QuerySimilarAsync(
            collectionName, 
            queryEmbedding, 
            nResults: 2,
            filter: new Dictionary<string, object> { ["category"] = "review" });
        
        Console.WriteLine($"   🔍 Found {allResults.Count} similar documents (unfiltered)");
        Console.WriteLine($"   🔍 Found {filteredResults.Count} similar documents (reviews only)");
        
        // Display results
        Console.WriteLine("   Top similar documents:");
        foreach (var result in allResults.Take(2))
        {
            var sentiment = result.Metadata.GetValueOrDefault("sentiment", "unknown");
            var truncatedContent = result.Document.Length > 50 ? result.Document[..50] + "..." : result.Document;
            Console.WriteLine($"     - {result.Id}: \"{truncatedContent}\" (similarity: {1-result.Distance:F3}, sentiment: {sentiment})");
        }
    }
    
    private async Task RunDuckDBDemo()
    {
        Console.WriteLine("\n3. DuckDB Analytics Demo:");
        Console.WriteLine("   Setting up ML performance analytics...");
        
        // Record sample experiment performance data
        var performances = new List<ExperimentPerformance>
        {
            new() 
            {
                ExperimentId = Guid.NewGuid(),
                ModelName = "RandomForest",
                DatasetName = "sentiment_dataset_v1",
                Accuracy = 0.85,
                Precision = 0.83,
                Recall = 0.87,
                F1Score = 0.85,
                TrainingTimeSeconds = 120,
                InferenceTimeMs = 15.5,
                MemoryUsageMb = 256.0
            },
            new() 
            {
                ExperimentId = Guid.NewGuid(),
                ModelName = "XGBoost",
                DatasetName = "sentiment_dataset_v1", 
                Accuracy = 0.88,
                Precision = 0.86,
                Recall = 0.90,
                F1Score = 0.88,
                TrainingTimeSeconds = 180,
                InferenceTimeMs = 12.3,
                MemoryUsageMb = 320.0
            },
            new() 
            {
                ExperimentId = Guid.NewGuid(),
                ModelName = "NeuralNetwork",
                DatasetName = "sentiment_dataset_v1",
                Accuracy = 0.91,
                Precision = 0.89,
                Recall = 0.93,
                F1Score = 0.91,
                TrainingTimeSeconds = 450,
                InferenceTimeMs = 25.7,
                MemoryUsageMb = 512.0
            },
            new() 
            {
                ExperimentId = Guid.NewGuid(),
                ModelName = "RandomForest",
                DatasetName = "image_dataset_v1",
                Accuracy = 0.78,
                Precision = 0.76,
                Recall = 0.81,
                F1Score = 0.78,
                TrainingTimeSeconds = 200,
                InferenceTimeMs = 18.2,
                MemoryUsageMb = 280.0
            }
        };
        
        foreach (var performance in performances)
        {
            await duckdbService.RecordExperimentPerformanceAsync(performance);
        }
        Console.WriteLine($"   ✅ Recorded {performances.Count} experiment performance records");
        
        // Compare models
        var comparison = await duckdbService.CompareModelsAsync(new[] { "RandomForest", "XGBoost", "NeuralNetwork" });
        
        Console.WriteLine("   📊 Model Performance Comparison:");
        Console.WriteLine("   Model             | Avg Accuracy | Best Accuracy | Avg Training | Avg Inference | F1 Score");
        Console.WriteLine("   ------------------|--------------|---------------|--------------|---------------|----------");
        
        foreach (var result in comparison)
        {
            Console.WriteLine($"   {result.ModelName,-17} | {result.AverageAccuracy,10:F3} | {result.BestAccuracy,11:F3} | {result.AverageTrainingTime,10:F0}s | {result.AverageInferenceTime,11:F1}ms | {result.AverageF1Score,6:F3}");
        }
        
        // Get performance trends
        var trends = await duckdbService.GetPerformanceTrendsAsync("RandomForest");
        Console.WriteLine($"   📈 Performance trends for RandomForest: {trends.Count} data points");
    }
    
    private async Task RunIntegratedWorkflowDemo()
    {
        Console.WriteLine("\n4. Integrated ML Workflow Demo:");
        Console.WriteLine("   Demonstrating cross-database ML pipeline...");
        
        // Step 1: Create experiment in PostgreSQL
        var workflowExperimentId = await postgresService.CreateExperimentAsync(
            name: "Document Classification Pipeline",
            modelType: "DocumentClassification",
            parameters: new Dictionary<string, object>
            {
                ["model_type"] = "transformer",
                ["embedding_dim"] = 384,
                ["num_classes"] = 5
            },
            description: "End-to-end document classification with semantic search"
        );
        Console.WriteLine($"   1️⃣ Created integrated experiment: {workflowExperimentId}");
        
        // Step 2: Store document embeddings in Chroma
        var workflowCollection = "classification_pipeline";
        await chromaService.CreateCollectionAsync(workflowCollection);
        
        var workflowDocs = new List<ChromaDocument>
        {
            new() { Id = "train_1", Content = "Technical documentation about API design", 
                   Embedding = GenerateRandomEmbedding(384), 
                   Metadata = new() { ["label"] = "technical", ["split"] = "train" }},
            new() { Id = "train_2", Content = "Marketing material for product launch", 
                   Embedding = GenerateRandomEmbedding(384), 
                   Metadata = new() { ["label"] = "marketing", ["split"] = "train" }},
            new() { Id = "test_1", Content = "User manual for software installation", 
                   Embedding = GenerateRandomEmbedding(384), 
                   Metadata = new() { ["label"] = "technical", ["split"] = "test" }}
        };
        
        await chromaService.AddDocumentsAsync(workflowCollection, workflowDocs);
        Console.WriteLine($"   2️⃣ Stored {workflowDocs.Count} training documents with embeddings");
        
        // Step 3: Simulate model training and record performance
        await Task.Delay(500); // Simulate training time
        
        var workflowPerformance = new ExperimentPerformance
        {
            ExperimentId = workflowExperimentId,
            ModelName = "DocumentClassifier-Transformer",
            DatasetName = "mixed_documents_v1",
            Accuracy = 0.92,
            Precision = 0.91,
            Recall = 0.93,
            F1Score = 0.92,
            TrainingTimeSeconds = 1200,
            InferenceTimeMs = 45.6,
            MemoryUsageMb = 768.0
        };
        
        await duckdbService.RecordExperimentPerformanceAsync(workflowPerformance);
        Console.WriteLine("   3️⃣ Recorded model performance in analytics database");
        
        // Step 4: Test similarity search for classification
        var testQuery = GenerateRandomEmbedding(384);
        var similarDocs = await chromaService.QuerySimilarAsync(
            workflowCollection, 
            testQuery, 
            nResults: 2,
            filter: new Dictionary<string, object> { ["split"] = "train" });
        
        Console.WriteLine($"   4️⃣ Found {similarDocs.Count} similar training examples for classification");
        
        // Step 5: Update experiment status
        await postgresService.UpdateExperimentStatusAsync(workflowExperimentId, "completed",
            new Dictionary<string, object> 
            { 
                ["final_accuracy"] = 0.92,
                ["documents_processed"] = workflowDocs.Count,
                ["pipeline_duration_minutes"] = 20
            });
        Console.WriteLine("   5️⃣ Updated experiment with final results");
        
        Console.WriteLine("   ✅ Integrated workflow completed successfully!");
        Console.WriteLine("      - Experiment tracking: PostgreSQL");
        Console.WriteLine("      - Semantic search: ChromaDB");
        Console.WriteLine("      - Performance analytics: DuckDB");
    }
    
    private static float[] GenerateRandomEmbedding(int dimensions)
    {
        var random = new Random();
        var embedding = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range [-1, 1]
        }
        return embedding;
    }
}

/// <summary>
/// Demonstrates ML Database Technologies including PostgreSQL with pgvector for ML experiments,
/// ChromaDB for vector similarity search, DuckDB for analytics, and integrated ML workflows
/// combining all technologies for complete beginner understanding.
/// </summary>
public static class Program
{
    public static async Task Main()
    {
        var demoService = new MLDatabaseDemoService();
        await demoService.RunCompleteDemo();
        
        Console.WriteLine("\nSetup Instructions:");
        Console.WriteLine("==================");
        Console.WriteLine("To run with real databases:");
        Console.WriteLine("1. Install Docker Desktop");
        Console.WriteLine("2. Create docker-compose.yml with PostgreSQL (pgvector), ChromaDB, DuckDB");
        Console.WriteLine("3. Add Entity Framework packages for PostgreSQL");
        Console.WriteLine("4. Add ChromaDB.Client and DuckDB.NET.Data packages");
        Console.WriteLine("5. Configure connection strings and run migrations");
        Console.WriteLine("");
        Console.WriteLine("Required NuGet packages:");
        Console.WriteLine("- Npgsql.EntityFrameworkCore.PostgreSQL");
        Console.WriteLine("- Pgvector.EntityFrameworkCore");
        Console.WriteLine("- DuckDB.NET.Data");
        Console.WriteLine("- System.Text.Json");
    }
}
