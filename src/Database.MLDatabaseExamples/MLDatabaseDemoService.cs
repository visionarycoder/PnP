namespace Database.MLDatabaseExamples;

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