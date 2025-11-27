using System.Text.Json;

namespace Database.MLDatabases;

public class MLDatabaseStackService
{
    private readonly PostgresMLProvider postgresProvider;
    private readonly ChromaVectorProvider chromaProvider;
    private readonly DuckDBMLProvider duckdbProvider;
    private readonly ClickHouseMLMetricsProvider clickhouseProvider;
    private readonly SQLiteMLProvider sqliteProvider;
    
    public MLDatabaseStackService()
    {
        postgresProvider = new PostgresMLProvider();
        chromaProvider = new ChromaVectorProvider();
        duckdbProvider = new DuckDBMLProvider();
        clickhouseProvider = new ClickHouseMLMetricsProvider();
        sqliteProvider = new SQLiteMLProvider();
    }
    
    public async Task RunCompleteStackDemo()
    {
        Console.WriteLine("Better Database Technologies for Local ML Development");
        Console.WriteLine("===================================================");
        Console.WriteLine("Demonstrating PostgreSQL, ChromaDB, DuckDB, ClickHouse, and SQLite for ML workloads");
        Console.WriteLine("");

        await DemonstratePostgreSQLML();
        await DemonstrateChromaVectorDB();
        await DemonstrateDuckDBAnalytics();
        await DemonstrateClickHouseMetrics();
        await DemonstrateSQLiteLightweight();
        await DemonstrateIntegratedWorkflow();

        DisplayTechnologyRecommendations();
    }
    
    private async Task DemonstratePostgreSQLML()
    {
        Console.WriteLine("1. PostgreSQL with pgvector Extensions");
        Console.WriteLine("   🐘 Primary database for ML experiments and vector storage");
        
        // Create ML experiments
        var sentimentExperiment = await postgresProvider.CreateExperimentAsync(
            new MLExperimentRequest
            {
                Name = "Sentiment Analysis Model v2",
                ModelType = "TextClassification",
                Parameters = new Dictionary<string, object>
                {
                    ["learning_rate"] = 0.001,
                    ["batch_size"] = 32,
                    ["model_architecture"] = "transformer"
                }
            });
        
        var imageExperiment = await postgresProvider.CreateExperimentAsync(
            new MLExperimentRequest
            {
                Name = "Product Image Classification",
                ModelType = "ImageClassification",
                Parameters = new Dictionary<string, object>
                {
                    ["learning_rate"] = 0.01,
                    ["batch_size"] = 64,
                    ["epochs"] = 25
                }
            });
        
        // Add document embeddings
        await postgresProvider.AddDocumentEmbeddingAsync(new DocumentEmbedding
        {
            Id = Guid.NewGuid(),
            DocumentId = "technical_manual_v1",
            Vector = GenerateRandomEmbedding(384),
            ModelName = "sentence-transformers/all-MiniLM-L6-v2",
            Metadata = JsonDocument.Parse("""{"category": "technical", "language": "en"}"""),
            CreatedAt = DateTime.UtcNow
        });
        
        // Update experiment status
        await postgresProvider.UpdateExperimentStatusAsync(sentimentExperiment, ExperimentStatus.Completed,
            new Dictionary<string, object>
            {
                ["accuracy"] = 0.91,
                ["f1_score"] = 0.89,
                ["training_time"] = 1200
            });
        
        var experiments = await postgresProvider.GetExperimentsAsync();
        Console.WriteLine($"   📊 Total experiments managed: {experiments.Count}");
        Console.WriteLine("");
    }
    
    private async Task DemonstrateChromaVectorDB()
    {
        Console.WriteLine("2. ChromaDB Vector Database");
        Console.WriteLine("   🔍 Purpose-built for vector similarity search");
        
        var collectionId = await chromaProvider.CreateCollectionAsync(
            "product_embeddings", 
            new Dictionary<string, object> { ["domain"] = "ecommerce" });
        
        // Add product embeddings
        var productEmbeddings = new[]
        {
            new DocumentEmbedding
            {
                Id = Guid.NewGuid(),
                DocumentId = "smartphone_premium",
                Vector = GenerateRandomEmbedding(384),
                ModelName = "product-embeddings-v1",
                Metadata = JsonDocument.Parse("""{"category": "electronics", "price_range": "premium"}""")
            },
            new DocumentEmbedding
            {
                Id = Guid.NewGuid(),
                DocumentId = "laptop_business",
                Vector = GenerateRandomEmbedding(384),
                ModelName = "product-embeddings-v1",
                Metadata = JsonDocument.Parse("""{"category": "electronics", "price_range": "business"}""")
            },
            new DocumentEmbedding
            {
                Id = Guid.NewGuid(),
                DocumentId = "headphones_wireless",
                Vector = GenerateRandomEmbedding(384),
                ModelName = "product-embeddings-v1",
                Metadata = JsonDocument.Parse("""{"category": "audio", "price_range": "mid"}""")
            }
        };
        
        await chromaProvider.AddEmbeddingsAsync(collectionId, productEmbeddings);
        
        // Query for similar products
        var queryVector = GenerateRandomEmbedding(384);
        var similarProducts = await chromaProvider.QuerySimilarAsync(
            collectionId, 
            queryVector, 
            topK: 3,
            filter: new Dictionary<string, object> { ["category"] = "electronics" });
        
        Console.WriteLine($"   🎯 Found {similarProducts.Length} similar electronics products");
        foreach (var result in similarProducts.Take(2))
        {
            var category = result.Metadata.GetValueOrDefault("category", "unknown");
            Console.WriteLine($"     - {result.Document} (similarity: {1-result.Distance:F3}, category: {category})");
        }
        Console.WriteLine("");
    }
    
    private async Task DemonstrateDuckDBAnalytics()
    {
        Console.WriteLine("3. DuckDB Analytics Engine");
        Console.WriteLine("   📊 High-performance analytics for ML metrics");
        
        // Record performance metrics for analysis
        var performanceMetrics = new[]
        {
            new ModelPerformanceMetric
            {
                ExperimentId = "exp_001",
                ModelName = "RandomForestClassifier",
                ModelVersion = "v1.0",
                Accuracy = 0.87,
                Precision = 0.85,
                Recall = 0.89,
                F1Score = 0.87,
                InferenceTimeMs = 15,
                MemoryUsageMb = 256.0,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new ModelPerformanceMetric
            {
                ExperimentId = "exp_001",
                ModelName = "GradientBoostingClassifier",
                ModelVersion = "v1.0",
                Accuracy = 0.91,
                Precision = 0.89,
                Recall = 0.93,
                F1Score = 0.91,
                InferenceTimeMs = 22,
                MemoryUsageMb = 384.0,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new ModelPerformanceMetric
            {
                ExperimentId = "exp_001",
                ModelName = "DeepNeuralNetwork",
                ModelVersion = "v1.0",
                Accuracy = 0.94,
                Precision = 0.92,
                Recall = 0.96,
                F1Score = 0.94,
                InferenceTimeMs = 45,
                MemoryUsageMb = 768.0,
                Timestamp = DateTime.UtcNow
            }
        };
        
        foreach (var metric in performanceMetrics)
        {
            await duckdbProvider.RecordModelPerformanceAsync(metric);
        }
        
        // Analyze model performance
        var analyticsResult = await duckdbProvider.AnalyzeModelPerformanceAsync("exp_001");
        
        Console.WriteLine("   🏆 Model Performance Comparison:");
        Console.WriteLine("   Model                      | Accuracy | Std Dev | Runs | P95 Time | Memory");
        Console.WriteLine("   ----------------------------|----------|---------|------|----------|--------");
        
        foreach (var analysis in analyticsResult.PerformanceAnalysis.OrderByDescending(a => a.AverageAccuracy))
        {
            Console.WriteLine($"   {analysis.ModelName,-26} | {analysis.AverageAccuracy,6:F3} | {analysis.AccuracyStandardDeviation,5:F3} | {analysis.TotalRuns,2} | {analysis.P95InferenceTime,6}ms | {analysis.AverageMemoryUsage,4:F0}MB");
        }
        Console.WriteLine("");
    }
    
    private async Task DemonstrateClickHouseMetrics()
    {
        Console.WriteLine("4. ClickHouse Time-Series Metrics");
        Console.WriteLine("   ⏰ Optimized for ML performance monitoring over time");
        
        // Record time-series performance data
        var baseTime = DateTime.UtcNow.AddHours(-6);
        for (int hour = 0; hour < 6; hour++)
        {
            var timestamp = baseTime.AddHours(hour);
            
            await clickhouseProvider.RecordModelPerformanceAsync(new ModelPerformanceMetric
            {
                ExperimentId = "ts_exp_001",
                ModelName = "ProductionModel",
                ModelVersion = "v2.1",
                Accuracy = 0.88 + (0.02 * Math.Sin(hour * Math.PI / 3)), // Simulate variance
                Precision = 0.86 + (0.01 * Math.Sin(hour * Math.PI / 4)),
                Recall = 0.90 + (0.015 * Math.Cos(hour * Math.PI / 3)),
                F1Score = 0.88 + (0.01 * Math.Sin(hour * Math.PI / 3)),
                InferenceTimeMs = 20 + (int)(5 * Math.Sin(hour * Math.PI / 2)),
                MemoryUsageMb = 512 + (50 * Math.Sin(hour * Math.PI / 4)),
                Timestamp = timestamp
            });
        }
        
        // Get performance trends
        var trends = await clickhouseProvider.GetPerformanceTrendAsync("ProductionModel", TimeSpan.FromHours(8));
        
        Console.WriteLine($"   📈 Generated {trends.Length} hourly performance trends");
        if (trends.Length > 0)
        {
            var latestTrend = trends.Last();
            Console.WriteLine($"   📊 Latest hour: Avg Accuracy={latestTrend.AverageAccuracy:F3}, Requests={latestTrend.RequestCount}, P95={latestTrend.P95InferenceTime:F1}ms");
        }
        Console.WriteLine("");
    }
    
    private async Task DemonstrateSQLiteLightweight()
    {
        Console.WriteLine("5. SQLite Lightweight Development");
        Console.WriteLine("   🗄️ Simple, file-based storage for single-developer ML workflows");
        
        var expId = await sqliteProvider.CreateExperimentAsync(new MLExperimentRequest
        {
            Name = "Feature Engineering Pipeline",
            ModelType = "FeatureExtraction",
            Parameters = new Dictionary<string, object>
            {
                ["technique"] = "pca",
                ["n_components"] = 50,
                ["standardization"] = true
            }
        });
        
        // Add model artifacts
        await sqliteProvider.AddModelArtifactAsync(expId, "model", "/models/feature_extractor.pkl", 
            new Dictionary<string, object> { ["size_mb"] = 12.5, ["framework"] = "scikit-learn" });
        
        await sqliteProvider.AddModelArtifactAsync(expId, "scaler", "/models/standard_scaler.pkl",
            new Dictionary<string, object> { ["size_mb"] = 0.8, ["framework"] = "scikit-learn" });
        
        // Add training data reference
        await sqliteProvider.AddTrainingDataReferenceAsync(expId, "customer_features", "/data/customers.csv", 10000, 47);
        
        var artifacts = await sqliteProvider.GetExperimentArtifactsAsync(expId);
        Console.WriteLine($"   📦 Stored {artifacts.Count} model artifacts for lightweight tracking");
        Console.WriteLine("");
    }
    
    private async Task DemonstrateIntegratedWorkflow()
    {
        Console.WriteLine("6. Integrated ML Workflow Across All Technologies");
        Console.WriteLine("   🔗 Demonstrating how all databases work together");
        
        // PostgreSQL: Main experiment tracking
        var workflowExpId = await postgresProvider.CreateExperimentAsync(new MLExperimentRequest
        {
            Name = "Multi-Modal Product Recommendation",
            ModelType = "Recommendation",
            Parameters = new Dictionary<string, object>
            {
                ["approach"] = "hybrid",
                ["text_model"] = "transformer",
                ["image_model"] = "resnet50",
                ["fusion_strategy"] = "late"
            }
        });
        
        // ChromaDB: Store product embeddings for similarity search
        var recommendationCollection = await chromaProvider.CreateCollectionAsync("recommendation_products");
        var productVectors = new[]
        {
            new DocumentEmbedding { Id = Guid.NewGuid(), DocumentId = "prod_123", Vector = GenerateRandomEmbedding(384), ModelName = "hybrid-v1" },
            new DocumentEmbedding { Id = Guid.NewGuid(), DocumentId = "prod_456", Vector = GenerateRandomEmbedding(384), ModelName = "hybrid-v1" },
            new DocumentEmbedding { Id = Guid.NewGuid(), DocumentId = "prod_789", Vector = GenerateRandomEmbedding(384), ModelName = "hybrid-v1" }
        };
        
        await chromaProvider.AddEmbeddingsAsync(recommendationCollection, productVectors);
        
        // DuckDB: Store batch recommendation results for analysis
        var recommendations = Enumerable.Range(1, 100).Select(i => new MLPrediction
        {
            Id = Guid.NewGuid().ToString(),
            ModelName = "HybridRecommender",
            InputFeatures = new Dictionary<string, object> { ["user_id"] = $"user_{i}", ["session_id"] = $"session_{i}" },
            Result = new Dictionary<string, object> { ["recommended_products"] = new[] { "prod_123", "prod_456" }, ["scores"] = new[] { 0.89, 0.76 } },
            Confidence = 0.85 + (0.1 * new Random().NextDouble()),
            CreatedAt = DateTime.UtcNow
        });
        
        await duckdbProvider.StoreBatchPredictionsAsync(recommendations);
        
        // ClickHouse: Record real-time performance metrics
        await clickhouseProvider.RecordModelPerformanceAsync(new ModelPerformanceMetric
        {
            ExperimentId = workflowExpId,
            ModelName = "HybridRecommender",
            ModelVersion = "v1.0",
            Accuracy = 0.89,
            Precision = 0.87,
            Recall = 0.91,
            F1Score = 0.89,
            InferenceTimeMs = 35,
            MemoryUsageMb = 896.0,
            Timestamp = DateTime.UtcNow
        });
        
        // SQLite: Store lightweight artifacts
        await sqliteProvider.AddModelArtifactAsync(workflowExpId, "config", "/models/hybrid_config.json",
            new Dictionary<string, object> { ["weights"] = "text:0.6,image:0.4" });
        
        // PostgreSQL: Complete experiment
        await postgresProvider.UpdateExperimentStatusAsync(workflowExpId, ExperimentStatus.Completed,
            new Dictionary<string, object>
            {
                ["recommendations_generated"] = 100,
                ["avg_confidence"] = 0.87,
                ["processing_time_minutes"] = 15
            });
        
        Console.WriteLine("   ✅ Integrated workflow completed successfully!");
        Console.WriteLine("      🐘 PostgreSQL: Experiment lifecycle management");
        Console.WriteLine("      🔍 ChromaDB: Vector similarity search");
        Console.WriteLine("      📊 DuckDB: Batch analytics and feature engineering");
        Console.WriteLine("      ⏰ ClickHouse: Real-time performance monitoring");
        Console.WriteLine("      🗄️ SQLite: Lightweight artifact storage");
        Console.WriteLine("");
    }
    
    private static void DisplayTechnologyRecommendations()
    {
        Console.WriteLine("Technology Selection Guide");
        Console.WriteLine("=========================");
        Console.WriteLine();
        Console.WriteLine("📊 Use Case Recommendations:");
        Console.WriteLine("┌─────────────────────────────┬─────────────────────┬────────────────────────────────────┐");
        Console.WriteLine("│ Use Case                    │ Recommended Tech    │ Why                                │");
        Console.WriteLine("├─────────────────────────────┼─────────────────────┼────────────────────────────────────┤");
        Console.WriteLine("│ ML Metadata & Experiments   │ PostgreSQL+pgvector │ ACID compliance, JSON, vectors     │");
        Console.WriteLine("│ Vector Embeddings           │ ChromaDB            │ Purpose-built for vectors          │");
        Console.WriteLine("│ ML Analytics & Metrics      │ DuckDB              │ Optimized for analytics, OLAP      │");
        Console.WriteLine("│ Time-series Performance     │ ClickHouse          │ High-performance time-series       │");
        Console.WriteLine("│ Lightweight Development     │ SQLite              │ Simple, file-based, no setup       │");
        Console.WriteLine("│ Feature Store               │ PostgreSQL+Redis    │ Structured+fast access             │");
        Console.WriteLine("│ Real-time Inference         │ Redis+PostgreSQL    │ Fast lookups+persistence            │");
        Console.WriteLine("└─────────────────────────────┴─────────────────────┴────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("🚀 Benefits Over Traditional Approaches:");
        Console.WriteLine("   • Native SQL querying vs REST API calls");
        Console.WriteLine("   • Specialized indexes for ML workloads");
        Console.WriteLine("   • Better concurrent access for teams");
        Console.WriteLine("   • Rich ecosystem of monitoring tools");
        Console.WriteLine("   • ACID transactions for experiment consistency");
        Console.WriteLine();
        Console.WriteLine("🔧 Docker Setup:");
        Console.WriteLine("   docker-compose up postgres-ml chroma clickhouse redis");
        Console.WriteLine("   # See documentation for complete docker-compose.yml");
    }
    
    private static float[] GenerateRandomEmbedding(int dimensions)
    {
        var random = new Random();
        var embedding = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }
        return embedding;
    }
}