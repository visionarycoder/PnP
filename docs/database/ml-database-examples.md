# ML Database Technologies - Complete Beginner Examples

**Description**: Step-by-step examples and tutorials for setting up and using ML-focused databases from scratch, designed for developers with no prior experience in these technologies.

**Technology**: PostgreSQL, Chroma, DuckDB, ClickHouse, MLflow

## Getting Started - Complete Setup Guide

### Prerequisites

Before we begin, ensure you have:

- Docker Desktop installed and running
- .NET 8+ SDK installed
- Visual Studio Code or Visual Studio

### Step 1: Create Project Structure

```bash
# Create a new ML project
mkdir MLDatabaseExamples
cd MLDatabaseExamples

# Create directory structure
mkdir -p src/ML.Examples
mkdir -p docker
mkdir -p data/postgres
mkdir -p data/chroma
mkdir -p data/clickhouse
mkdir -p data/duckdb
mkdir -p sql/init
mkdir -p notebooks
mkdir -p models
```

### Step 2: Docker Compose Setup

Create `docker/docker-compose.yml`:

```yaml
version: '3.8'

services:
  # PostgreSQL with vector support
  postgres:
    image: pgvector/pgvector:pg16
    container_name: ml_postgres
    ports:
      - "5432:5432"
    environment:
      POSTGRESDB: ml_examples
      POSTGRESUSER: ml_user
      POSTGRESPASSWORD: ml_pass123
    volumes:
      - ../data/postgres:/var/lib/postgresql/data
      - ../sql/init:/docker-entrypoint-initdb.d/
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ml_user -d ml_examples"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Chroma vector database
  chroma:
    image: chromadb/chroma:latest
    container_name: ml_chroma
    ports:
      - "8000:8000"
    volumes:
      - ../data/chroma:/chroma/chroma
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/api/v1/heartbeat"]
      interval: 30s
      timeout: 10s
      retries: 3

  # ClickHouse for analytics
  clickhouse:
    image: clickhouse/clickhouse-server:latest
    container_name: ml_clickhouse
    ports:
      - "8123:8123"  # HTTP interface
      - "9000:9000"  # Native TCP interface
    volumes:
      - ../data/clickhouse:/var/lib/clickhouse
    environment:
      CLICKHOUSEDB: ml_analytics
      CLICKHOUSEUSER: ml_user
      CLICKHOUSEPASSWORD: ml_pass123
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8123/ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Redis for caching
  redis:
    image: redis:7-alpine
    container_name: ml_redis
    ports:
      - "6379:6379"
    volumes:
      - ../data/redis:/data
    command: redis-server --appendonly yes
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Jupyter for experimentation
  jupyter:
    image: jupyter/datascience-notebook:latest
    container_name: ml_jupyter
    ports:
      - "8888:8888"
    volumes:
      - ../notebooks:/home/jovyan/work
      - ../data:/home/jovyan/data
    environment:
      JUPYTER_ENABLE_LAB=yes
      JUPYTER_TOKEN=ml_examples_token
    restart: unless-stopped
```

### Step 3: Database Initialization Scripts

Create `sql/init/01-setup-extensions.sql`:

```sql
-- Enable required extensions for ML workloads
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS btree_gin;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Create ML user and schema
CREATE SCHEMA IF NOT EXISTS ml_schema;
GRANT ALL PRIVILEGES ON SCHEMA ml_schema TO ml_user;
```

Create `sql/init/02-create-tables.sql`:

```sql
-- ML Experiments tracking
CREATE TABLE IF NOT EXISTS ml_schema.experiments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    model_type VARCHAR(100) NOT NULL,
    parameters JSONB,
    metrics JSONB,
    status VARCHAR(50) DEFAULT 'created',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE
);

-- Document embeddings storage
CREATE TABLE IF NOT EXISTS ml_schema.document_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id VARCHAR(255) NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
    embedding vector(1536), -- OpenAI embedding dimension
    modelName VARCHAR(100) NOT NULL,
    chunk_index INTEGER DEFAULT 0,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Model artifacts tracking
CREATE TABLE IF NOT EXISTS ml_schema.model_artifacts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experimentId UUID REFERENCES ml_schema.experiments(id),
    artifact_name VARCHAR(255) NOT NULL,
    artifact_type VARCHAR(100) NOT NULL, -- 'model', 'scaler', 'vectorizer'
    file_path TEXT NOT NULL,
    file_size_bytes BIGINT,
    checksum VARCHAR(64),
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_experiments_status ON ml_schema.experiments(status);
CREATE INDEX IF NOT EXISTS idx_experiments_model_type ON ml_schema.experiments(model_type);
CREATE INDEX IF NOT EXISTS idx_experiments_created_at ON ml_schema.experiments(created_at);

CREATE INDEX IF NOT EXISTS idx_embeddings_document_id ON ml_schema.document_embeddings(document_id);
CREATE INDEX IF NOT EXISTS idx_embeddings_modelName ON ml_schema.document_embeddings(modelName);
CREATE INDEX IF NOT EXISTS idx_embeddings_vector ON ml_schema.document_embeddings USING ivfflat (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_artifacts_experimentId ON ml_schema.model_artifacts(experimentId);
CREATE INDEX IF NOT EXISTS idx_artifacts_type ON ml_schema.model_artifacts(artifact_type);
```

## Example 1: PostgreSQL with pgvector

### Setting Up the .NET Project

Create `src/ML.Examples/ML.Examples.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.0.3" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>
</Project>
```

### PostgreSQL Example Code

Create `src/ML.Examples/PostgresExample.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ML.Examples;

// Entity models
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
    public DateTime UpdatedAt { get; set; }    [Column("completed_at")]
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
    
    public Vector? Embedding { get; set; }
    
    [Column("modelName")]
    public string ModelName { get; set; } = string.Empty;
    
    [Column("chunk_index")]
    public int ChunkIndex { get; set; }
    
    public JsonDocument? Metadata { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

// DbContext
public class MLContext : DbContext
{
    public MLContext(DbContextOptions<MLContext> options) : base(options) { }
    
    public DbSet<MLExperiment> Experiments { get; set; }
    public DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure vector column
        modelBuilder.HasPostgresExtension("vector");
        
        modelBuilder.Entity<DocumentEmbedding>(entity =>
        {
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)");
        });
        
        // Configure JSON columns
        modelBuilder.Entity<MLExperiment>(entity =>
        {
            entity.Property(e => e.Parameters)
                .HasColumnType("jsonb");
            entity.Property(e => e.Metrics)
                .HasColumnType("jsonb");
        });
        
        modelBuilder.Entity<DocumentEmbedding>(entity =>
        {
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
        });
    }
}

// Service for ML operations
public class PostgresMLService(MLContext context)
{
    
    // Create a new ML experiment
    public async Task<Guid> CreateExperimentAsync(
        string name, 
        string modelType, 
        Dictionary<string, object> parameters,
        string? description = null)
    {
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
        
        context.Experiments.Add(experiment);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"Created experiment: {experiment.Id} - {name}");
        return experiment.Id;
    }
    
    // Store document embedding
    public async Task StoreEmbeddingAsync(
        string documentId, 
        float[] embedding, 
        string modelName,
        Dictionary<string, object>? metadata = null)
    {
        var contentHash = ComputeHash(documentId);
        
        var docEmbedding = new DocumentEmbedding
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ContentHash = contentHash,
            Embedding = new Vector(embedding),
            ModelName = modelName,
            ChunkIndex = 0,
            Metadata = metadata != null ? JsonDocument.Parse(JsonSerializer.Serialize(metadata)) : null,
            CreatedAt = DateTime.UtcNow
        };
        
        context.DocumentEmbeddings.Add(docEmbedding);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"Stored embedding for document: {documentId}");
    }
    
    // Find similar documents using vector similarity
    public async Task<List<SimilarDocument>> FindSimilarDocumentsAsync(
        float[] queryEmbedding, 
        int limit = 10,
        double threshold = 0.7)
    {
        var queryVector = new Vector(queryEmbedding);
        
        var results = await context.DocumentEmbeddings
            .Select(e => new SimilarDocument
            {
                DocumentId = e.DocumentId,
                ModelName = e.ModelName,
                Similarity = e.Embedding!.CosineDistance(queryVector),
                Metadata = e.Metadata
            })
            .Where(r => r.Similarity >= threshold)
            .OrderBy(r => r.Similarity)
            .Take(limit)
            .ToListAsync();
        
        Console.WriteLine($"Found {results.Count} similar documents");
        return results;
    }
    
    // Update experiment with results
    public async Task UpdateExperimentResultsAsync(
        Guid experimentId, 
        Dictionary<string, object> metrics,
        string status = "completed")
    {
        var experiment = await context.Experiments.FindAsync(experimentId);
        if (experiment != null)
        {
            experiment.Metrics = JsonDocument.Parse(JsonSerializer.Serialize(metrics));
            experiment.Status = status;
            experiment.UpdatedAt = DateTime.UtcNow;
            experiment.CompletedAt = status == "completed" ? DateTime.UtcNow : null;
            
            await context.SaveChangesAsync();
            Console.WriteLine($"Updated experiment {experimentId} with results");
        }
    }
    
    // Get experiment statistics
    public async Task<ExperimentStats> GetExperimentStatsAsync()
    {
        var stats = await context.Experiments
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        
        var totalExperiments = await context.Experiments.CountAsync();
        var totalEmbeddings = await context.DocumentEmbeddings.CountAsync();
        
        return new ExperimentStats
        {
            TotalExperiments = totalExperiments,
            TotalEmbeddings = totalEmbeddings,
            StatusCounts = stats.ToDictionary(s => s.Status, s => s.Count)
        };
    }
    
    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}

// Result models
public class SimilarDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public JsonDocument? Metadata { get; set; }
}

public class ExperimentStats
{
    public int TotalExperiments { get; set; }
    public int TotalEmbeddings { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}
```

## Example 2: Chroma Vector Database

Create `src/ML.Examples/ChromaExample.cs`:

```csharp
using System.Text.Json;

namespace ML.Examples;

public class ChromaVectorService(HttpClient httpClient)
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    // Initialize base address in static field or configure via DI
    private readonly HttpClient client = ConfigureClient(httpClient);
    
    private static HttpClient ConfigureClient(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri("http://localhost:8000");
        return httpClient;
    }
    
    // Create a new collection
    public async Task<string> CreateCollectionAsync(
        string name, 
        Dictionary<string, object>? metadata = null)
    {
        var request = new
        {
            name = name,
            metadata = metadata ?? new Dictionary<string, object>()
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/collections", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create collection: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<ChromaCollection>();
        Console.WriteLine($"Created collection: {name} with ID: {result!.Name}");
        return result.Name;
    }
    
    // Add documents with embeddings
    public async Task AddDocumentsAsync(
        string collectionName,
        List<ChromaDocument> documents)
    {
        var request = new
        {
            ids = documents.Select(d => d.Id).ToArray(),
            embeddings = documents.Select(d => d.Embedding).ToArray(),
            documents = documents.Select(d => d.Content).ToArray(),
            metadatas = documents.Select(d => d.Metadata).ToArray()
        };
        
        var response = await client.PostAsJsonAsync(
            $"/api/v1/collections/{collectionName}/add", 
            request,
            jsonOptions);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to add documents: {error}");
        }
        
        Console.WriteLine($"Added {documents.Count} documents to collection {collectionName}");
    }
    
    // Query similar documents
    public async Task<List<ChromaQueryResult>> QuerySimilarAsync(
        string collectionName,
        float[] queryEmbedding,
        int nResults = 10,
        Dictionary<string, object>? filter = null)
    {
        var request = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = nResults,
            where = filter,
            include = new[] { "metadatas", "documents", "distances" }
        };
        
        var response = await client.PostAsJsonAsync(
            $"/api/v1/collections/{collectionName}/query",
            request,
            jsonOptions);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to query collection: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>(jsonOptions);
        
        var results = new List<ChromaQueryResult>();
        for (int i = 0; i < result!.Ids[0].Length; i++)
        {
            results.Add(new ChromaQueryResult
            {
                Id = result.Ids[0][i],
                Document = result.Documents[0][i],
                Distance = result.Distances[0][i],
                Metadata = result.Metadatas[0][i]
            });
        }
        
        Console.WriteLine($"Found {results.Count} similar documents");
        return results;
    }
    
    // Get collection information
    public async Task<ChromaCollection> GetCollectionAsync(string name)
    {
        var response = await client.GetAsync($"/api/v1/collections/{name}");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get collection: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<ChromaCollection>(jsonOptions);
        return result!;
    }
    
    // List all collections
    public async Task<List<ChromaCollection>> ListCollectionsAsync()
    {
        var response = await client.GetAsync("/api/v1/collections");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to list collections: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<List<ChromaCollection>>(jsonOptions);
        return result ?? new List<ChromaCollection>();
    }
}

// Data models for Chroma
public class ChromaDocument
{
    public string Id { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChromaCollection
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChromaQueryResult
{
    public string Id { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public double Distance { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChromaQueryResponse
{
    public string[][] Ids { get; set; } = Array.Empty<string[]>();
    public string[][] Documents { get; set; } = Array.Empty<string[]>();
    public double[][] Distances { get; set; } = Array.Empty<double[]>();
    public Dictionary<string, object>[][] Metadatas { get; set; } = Array.Empty<Dictionary<string, object>[]>();
}
```

## Example 3: DuckDB for Analytics

Create `src/ML.Examples/DuckDBExample.cs`:

```csharp
using System.Data.Common;
using System.Data;
using DuckDB.NET.Data;

namespace ML.Examples;

public class DuckDBAnalyticsService(string databasePath = "./data/duckdb/ml_analytics.duckdb")
{
    private readonly string connectionString = $"Data Source={databasePath}";
    
    // Initialize in constructor body
    public void Initialize() => InitializeDatabase();
    
    private void InitializeDatabase()
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        
        var initScript = """
            -- Create experiments performance table
            CREATE TABLE IF NOT EXISTS experiment_performance (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                experimentId UUID NOT NULL,
                modelName VARCHAR NOT NULL,
                dataset_name VARCHAR NOT NULL,
                accuracy DOUBLE,
                precision_score DOUBLE,
                recall DOUBLE,
                f1_score DOUBLE,
                training_time_seconds INTEGER,
                inference_time_ms DOUBLE,
                memory_usage_mb DOUBLE,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            -- Create model predictions table for batch analysis
            CREATE TABLE IF NOT EXISTS model_predictions (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                modelName VARCHAR NOT NULL,
                input_features JSON,
                predicted_value DOUBLE,
                actual_value DOUBLE,
                confidence DOUBLE,
                prediction_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            -- Create feature importance table
            CREATE TABLE IF NOT EXISTS feature_importance (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                experimentId UUID NOT NULL,
                feature_name VARCHAR NOT NULL,
                importance_score DOUBLE,
                feature_type VARCHAR, -- 'numerical', 'categorical', 'text'
                createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            """;
        
        using var command = new DuckDBCommand(initScript, connection);
        command.ExecuteNonQuery();
        
        Console.WriteLine("DuckDB database initialized successfully");
    }
    
    // Record experiment performance metrics
    public async Task RecordExperimentPerformanceAsync(ExperimentPerformance performance)
    {
        using var connection = new DuckDBConnection(connectionString);
        await connection.OpenAsync();
        
        var insertQuery = """
            INSERT INTO experiment_performance 
            (experimentId, modelName, dataset_name, accuracy, precision_score, recall, 
             f1_score, training_time_seconds, inference_time_ms, memory_usage_mb)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
            """;
        
        using var command = new DuckDBCommand(insertQuery, connection);
        command.Parameters.Add(new DuckDBParameter("@1", performance.ExperimentId));
        command.Parameters.Add(new DuckDBParameter("@2", performance.ModelName));
        command.Parameters.Add(new DuckDBParameter("@3", performance.DatasetName));
        command.Parameters.Add(new DuckDBParameter("@4", performance.Accuracy));
        command.Parameters.Add(new DuckDBParameter("@5", performance.Precision));
        command.Parameters.Add(new DuckDBParameter("@6", performance.Recall));
        command.Parameters.Add(new DuckDBParameter("@7", performance.F1Score));
        command.Parameters.Add(new DuckDBParameter("@8", performance.TrainingTimeSeconds));
        command.Parameters.Add(new DuckDBParameter("@9", performance.InferenceTimeMs));
        command.Parameters.Add(new DuckDBParameter("@10", performance.MemoryUsageMb));
        
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Recorded performance for experiment: {performance.ExperimentId}");
    }
    
    // Analyze model performance trends
    public async Task<List<ModelPerformanceTrend>> AnalyzeModelTrendsAsync(
        string modelName, 
        int lastDays = 30)
    {
        using var connection = new DuckDBConnection(connectionString);
        await connection.OpenAsync();
        
        var query = """
            SELECT 
                DATE_TRUNC('day', timestamp) as date,
                modelName,
                AVG(accuracy) as avg_accuracy,
                STDDEV(accuracy) as accuracy_stddev,
                AVG(inference_time_ms) as avg_inference_time,
                PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY inference_time_ms) as p95_inference_time,
                COUNT(*) as experiment_count
            FROM experiment_performance 
            WHERE modelName = ? 
              AND timestamp >= CURRENT_DATE - INTERVAL ? DAY
            GROUP BY DATE_TRUNC('day', timestamp), modelName
            ORDER BY date DESC;
            """;
        
        using var command = new DuckDBCommand(query, connection);
        command.Parameters.Add(new DuckDBParameter("@1", modelName));
        command.Parameters.Add(new DuckDBParameter("@2", lastDays));
        
        var trends = new List<ModelPerformanceTrend>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            trends.Add(new ModelPerformanceTrend
            {
                Date = reader.GetDateTime("date"),
                ModelName = reader.GetString("modelName"),
                AverageAccuracy = reader.IsDBNull("avg_accuracy") ? 0 : reader.GetDouble("avg_accuracy"),
                AccuracyStdDev = reader.IsDBNull("accuracy_stddev") ? 0 : reader.GetDouble("accuracy_stddev"),
                AverageInferenceTime = reader.IsDBNull("avg_inference_time") ? 0 : reader.GetDouble("avg_inference_time"),
                P95InferenceTime = reader.IsDBNull("p95_inference_time") ? 0 : reader.GetDouble("p95_inference_time"),
                ExperimentCount = reader.GetInt32("experiment_count")
            });
        }
        
        Console.WriteLine($"Analyzed {trends.Count} days of performance data for {modelName}");
        return trends;
    }
    
    // Compare multiple models
    public async Task<List<ModelComparison>> CompareModelsAsync(string[] modelNames)
    {
        using var connection = new DuckDBConnection(connectionString);
        await connection.OpenAsync();
        
        var modelList = string.Join("','", modelNames);
        var query = $"""
            SELECT 
                modelName,
                COUNT(*) as total_experiments,
                AVG(accuracy) as avg_accuracy,
                MAX(accuracy) as best_accuracy,
                MIN(accuracy) as worst_accuracy,
                AVG(training_time_seconds) as avg_training_time,
                AVG(inference_time_ms) as avg_inference_time,
                AVG(f1_score) as avg_f1_score
            FROM experiment_performance 
            WHERE modelName IN ('{modelList}')
            GROUP BY modelName
            ORDER BY avg_accuracy DESC;
            """;
        
        using var command = new DuckDBCommand(query, connection);
        
        var comparisons = new List<ModelComparison>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            comparisons.Add(new ModelComparison
            {
                ModelName = reader.GetString("modelName"),
                TotalExperiments = reader.GetInt32("total_experiments"),
                AverageAccuracy = reader.GetDouble("avg_accuracy"),
                BestAccuracy = reader.GetDouble("best_accuracy"),
                WorstAccuracy = reader.GetDouble("worst_accuracy"),
                AverageTrainingTime = reader.GetDouble("avg_training_time"),
                AverageInferenceTime = reader.GetDouble("avg_inference_time"),
                AverageF1Score = reader.GetDouble("avg_f1_score")
            });
        }
        
        Console.WriteLine($"Compared {comparisons.Count} models");
        return comparisons;
    }
    
    // Batch insert predictions for analysis
    public async Task StoreBatchPredictionsAsync(List<ModelPrediction> predictions)
    {
        using var connection = new DuckDBConnection(connectionString);
        await connection.OpenAsync();
        
        var insertQuery = """
            INSERT INTO model_predictions 
            (modelName, input_features, predicted_value, actual_value, confidence)
            VALUES (?, ?, ?, ?, ?);
            """;
        
        using var transaction = connection.BeginTransaction();
        
        foreach (var prediction in predictions)
        {
            using var command = new DuckDBCommand(insertQuery, connection, transaction);
            command.Parameters.Add(new DuckDBParameter("@1", prediction.ModelName));
            command.Parameters.Add(new DuckDBParameter("@2", JsonSerializer.Serialize(prediction.InputFeatures)));
            command.Parameters.Add(new DuckDBParameter("@3", prediction.PredictedValue));
            command.Parameters.Add(new DuckDBParameter("@4", prediction.ActualValue));
            command.Parameters.Add(new DuckDBParameter("@5", prediction.Confidence));
            
            await command.ExecuteNonQueryAsync();
        }
        
        await transaction.CommitAsync();
        Console.WriteLine($"Stored {predictions.Count} predictions for batch analysis");
    }
}

// Data models
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
```

## Example 4: Complete Demo Application

Create `src/ML.Examples/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ML.Examples;

// Create host and configure services
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // PostgreSQL with Entity Framework
        services.AddDbContext<MLContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5432;Database=ml_examples;Username=ml_user;Password=ml_pass123")
                   .UseVector());
        
        // HTTP client for Chroma
        services.AddHttpClient<ChromaVectorService>();
        
        // Other services
        services.AddScoped<PostgresMLService>();
        services.AddScoped<DuckDBAnalyticsService>();
        services.AddScoped<MLDemoService>();
    })
    .Build();

// Run the demo
var demoService = host.Services.GetRequiredService<MLDemoService>();
await demoService.RunCompleteDemo();

public class MLDemoService(
    PostgresMLService postgresService,
    ChromaVectorService chromaService, 
    DuckDBAnalyticsService duckdbService,
    ILogger<MLDemoService> logger)
    
    public async Task RunCompleteDemo()
    {
        logger.LogInformation("Starting ML Database Technologies Demo");
        
        try
        {
            await RunPostgreSQLDemo();
            await RunChromaDemo();
            await RunDuckDBDemo();
            
            logger.LogInformation("Demo completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed");
        }
    }
    
    private async Task RunPostgreSQLDemo()
    {
        logger.LogInformation("=== PostgreSQL Demo ===");
        
        // Create ML experiment
        var experimentId = await postgresService.CreateExperimentAsync(
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
        
        // Store some sample embeddings
        var sampleEmbeddings = GenerateSampleEmbeddings();
        foreach (var (docId, embedding, metadata) in sampleEmbeddings)
        {
            await postgresService.StoreEmbeddingAsync(docId, embedding, "text-embedding-ada-002", metadata);
        }
        
        // Find similar documents
        var queryEmbedding = sampleEmbeddings[0].embedding;
        var similarDocs = await postgresService.FindSimilarDocumentsAsync(queryEmbedding, limit: 5);
        
        // Update experiment with results
        await postgresService.UpdateExperimentResultsAsync(experimentId, new Dictionary<string, object>
        {
            ["accuracy"] = 0.85,
            ["precision"] = 0.83,
            ["recall"] = 0.87,
            ["f1_score"] = 0.85
        });
        
        // Get statistics
        var stats = await postgresService.GetExperimentStatsAsync();
        logger.LogInformation($"Experiments: {stats.TotalExperiments}, Embeddings: {stats.TotalEmbeddings}");
    }
    
    private async Task RunChromaDemo()
    {
        logger.LogInformation("=== Chroma Vector Database Demo ===");
        
        // Create collection
        var collectionName = await chromaService.CreateCollectionAsync("demo_documents", new Dictionary<string, object>
        {
            ["description"] = "Demo document collection",
            ["model"] = "text-embedding-ada-002"
        });
        
        // Add documents
        var documents = new List<ChromaDocument>
        {
            new() 
            { 
                Id = "doc1", 
                Content = "This is a positive review about the product",
                Embedding = GenerateRandomEmbedding(1536),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "positive", ["category"] = "review" }
            },
            new() 
            { 
                Id = "doc2", 
                Content = "Negative feedback about service quality",
                Embedding = GenerateRandomEmbedding(1536),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "negative", ["category"] = "feedback" }
            },
            new() 
            { 
                Id = "doc3", 
                Content = "Neutral comment about the website design",
                Embedding = GenerateRandomEmbedding(1536),
                Metadata = new Dictionary<string, object> { ["sentiment"] = "neutral", ["category"] = "comment" }
            }
        };
        
        await chromaService.AddDocumentsAsync(collectionName, documents);
        
        // Query for similar documents
        var queryEmbedding = GenerateRandomEmbedding(1536);
        var results = await chromaService.QuerySimilarAsync(
            collectionName, 
            queryEmbedding, 
            nResults: 3,
            filter: new Dictionary<string, object> { ["category"] = "review" });
        
        logger.LogInformation($"Found {results.Count} similar documents in Chroma");
    }
    
    private async Task RunDuckDBDemo()
    {
        logger.LogInformation("=== DuckDB Analytics Demo ===");
        
        // Record some sample experiment performance data
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
            }
        };
        
        foreach (var performance in performances)
        {
            await duckdbService.RecordExperimentPerformanceAsync(performance);
        }
        
        // Compare models
        var comparison = await duckdbService.CompareModelsAsync(new[] { "RandomForest", "XGBoost", "NeuralNetwork" });
        
        foreach (var result in comparison)
        {
            logger.LogInformation($"Model: {result.ModelName}, Avg Accuracy: {result.AverageAccuracy:F3}, Avg Inference: {result.AverageInferenceTime:F1}ms");
        }
    }
    
    private static List<(string docId, float[] embedding, Dictionary<string, object> metadata)> GenerateSampleEmbeddings()
    {
        return new List<(string, float[], Dictionary<string, object>)>
        {
            ("doc_1", GenerateRandomEmbedding(1536), new() { ["type"] = "article", ["length"] = 1200 }),
            ("doc_2", GenerateRandomEmbedding(1536), new() { ["type"] = "review", ["length"] = 800 }),
            ("doc_3", GenerateRandomEmbedding(1536), new() { ["type"] = "comment", ["length"] = 300 }),
        };
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
```

## Quick Start Guide

### 1. Start the Services

```bash
# Navigate to docker directory
cd docker

# Start all services
docker-compose up -d

# Check service health
docker-compose ps
```

### 2. Run the Demo

```bash
# Navigate to project directory
cd ../src/ML.Examples

# Add missing packages
dotnet add package DuckDB.NET.Data

# Run the complete demo
dotnet run
```

### 3. Explore the Data

**PostgreSQL:**

```bash
# Connect to PostgreSQL
docker exec -it ml_postgres psql -U ml_user -d ml_examples

# Query experiments
SELECT * FROM ml_schema.experiments;

# Query embeddings
SELECT document_id, modelName, createdAt FROM ml_schema.document_embeddings;
```

**Chroma:**

```bash
# Check Chroma collections
curl http://localhost:8000/api/v1/collections
```

**Jupyter Notebooks:**

```bash
# Access Jupyter Lab
# Open browser to: http://localhost:8888/lab?token=ml_examples_token
```

## Next Steps

1. **Experiment with Real Data**: Replace sample data with your actual documents and embeddings
2. **Optimize Performance**: Add proper indexes and tune database configurations
3. **Add Monitoring**: Implement health checks and performance monitoring
4. **Scale Up**: Move from development to production configurations
5. **Integration**: Connect with your ML pipelines and applications

This complete example gives you hands-on experience with each database technology, showing practical patterns for ML development workflows.

## Kubernetes Deployment

### Container Runtime Options

Modern Kubernetes clusters use **containerd** or **CRI-O** instead of Docker:

```yaml
# Check your cluster's container runtime
kubectl get nodes -o wide
# CONTAINER-RUNTIME column shows: containerd://1.6.x or cri-o://1.x.x
```

### Production Kubernetes Manifests

**Namespace and ConfigMap:**

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: ml-platform
  labels:
    name: ml-platform
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: ml-config
  namespace: ml-platform
data:
  POSTGRESDB: "ml_examples"
  POSTGRESUSER: "ml_user"
  CHROMAHOST: "chroma-service"
  CHROMAPORT: "8000"
  DUCKDBPATH: "/data/analytics.duckdb"
```

**PostgreSQL with Persistent Storage:**

```yaml
# k8s/postgres.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
  namespace: ml-platform
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-ml
  namespace: ml-platform
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres-ml
  template:
    metadata:
      labels:
        app: postgres-ml
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_DB
          valueFrom:
            configMapKeyRef:
              name: ml-config
              key: POSTGRES_DB
        - name: POSTGRES_USER
          valueFrom:
            configMapKeyRef:
              name: ml-config
              key: POSTGRES_USER
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: ml-secrets
              key: postgres-password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: ml-platform
spec:
  selector:
    app: postgres-ml
  ports:
  - port: 5432
    targetPort: 5432
  type: ClusterIP
```

**Chroma Vector Database:**

```yaml
# k8s/chroma.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: chroma-db
  namespace: ml-platform
spec:
  replicas: 1
  selector:
    matchLabels:
      app: chroma-db
  template:
    metadata:
      labels:
        app: chroma-db
    spec:
      containers:
      - name: chroma
        image: chromadb/chroma:latest
        ports:
        - containerPort: 8000
        env:
        - name: CHROMA_DB_IMPL
          value: "clickhouse"
        - name: CLICKHOUSE_HOST
          value: "clickhouse-service"
        - name: CLICKHOUSE_PORT
          value: "8123"
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "200m"
---
apiVersion: v1
kind: Service
metadata:
  name: chroma-service
  namespace: ml-platform
spec:
  selector:
    app: chroma-db
  ports:
  - port: 8000
    targetPort: 8000
  type: ClusterIP
```

**ML Application Deployment:**

```yaml
# k8s/ml-app.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ml-examples-app
  namespace: ml-platform
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ml-examples-app
  template:
    metadata:
      labels:
        app: ml-examples-app
    spec:
      containers:
      - name: ml-app
        image: ml-examples:latest
        ports:
        - containerPort: 8080
        env:
        - name: POSTGRES_CONNECTION
          value: "Host=postgres-service;Database=ml_examples;Username=ml_user;Password=$(POSTGRES_PASSWORD)"
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: ml-secrets
              key: postgres-password
        - name: CHROMA_URL
          value: "http://chroma-service:8000"
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "300m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: ml-app-service
  namespace: ml-platform
spec:
  selector:
    app: ml-examples-app
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

### Helm Chart Structure

```bash
# Create Helm chart
helm create ml-platform

# Directory structure:
ml-platform/
├── Chart.yaml
├── values.yaml
├── templates/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   └── ingress.yaml
└── charts/
```

**Helm values.yaml:**

```yaml
# values.yaml
global:
  namespace: ml-platform

postgres:
  enabled: true
  image:
    repository: postgres
    tag: "15"
  persistence:
    enabled: true
    size: 10Gi
  resources:
    requests:
      memory: 512Mi
      cpu: 250m
    limits:
      memory: 1Gi
      cpu: 500m

chroma:
  enabled: true
  image:
    repository: chromadb/chroma
    tag: "latest"
  resources:
    requests:
      memory: 256Mi
      cpu: 100m

mlApp:
  image:
    repository: ml-examples
    tag: "latest"
  replicas: 3
  service:
    type: LoadBalancer
    port: 80

ingress:
  enabled: true
  className: nginx
  hosts:
    - host: ml-platform.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: ml-platform-tls
      hosts:
        - ml-platform.example.com
```

### Container Build Without Docker

**Using Podman (Local):**

```bash
# Build with Podman instead of Docker
podman build -t ml-examples:latest .

# Push to registry
podman push ml-examples:latest registry.example.com/ml-examples:latest
```

**Using Buildah (CI/CD):**

```bash
#!/bin/bash
# build-container.sh

# Create container from base image
container=$(buildah from mcr.microsoft.com/dotnet/aspnet:8.0)

# Copy application files
buildah copy $container ./publish /app

# Set working directory and entry point
buildah config --workingdir /app $container
buildah config --entrypoint '["dotnet", "MLExamples.dll"]' $container
buildah config --port 8080 $container

# Add health check
buildah config --healthcheck 'CMD curl -f http://localhost:8080/health || exit 1' $container

# Commit and tag
buildah commit $container ml-examples:latest
buildah tag ml-examples:latest registry.example.com/ml-examples:latest

# Push to registry
buildah push registry.example.com/ml-examples:latest
```

**GitHub Actions with Buildah:**

```yaml
# .github/workflows/build-deploy.yml
name: Build and Deploy ML Platform

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
        
    - name: Publish Application
      run: dotnet publish -c Release -o ./publish
      
    - name: Install Buildah
      run: |
        sudo apt-get update
        sudo apt-get install -y buildah
        
    - name: Build Container Image
      run: |
        buildah bud -t ${{ secrets.REGISTRY }}/ml-examples:${{ github.sha }} .
        
    - name: Push to Registry
      run: |
        echo "${{ secrets.REGISTRY_PASSWORD }}" | buildah login -u "${{ secrets.REGISTRY_USER }}" --password-stdin ${{ secrets.REGISTRY }}
        buildah push ${{ secrets.REGISTRY }}/ml-examples:${{ github.sha }}
        
  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v1
      with:
        manifests: |
          k8s/namespace.yaml
          k8s/configmap.yaml
          k8s/secrets.yaml
          k8s/postgres.yaml
          k8s/chroma.yaml
          k8s/ml-app.yaml
        images: ${{ secrets.REGISTRY }}/ml-examples:${{ github.sha }}
        kubectl-version: 'latest'
```

### Deploy Commands

```bash
# Create secrets first
kubectl create secret generic ml-secrets \
  --from-literal=postgres-password=secure_password \
  -n ml-platform

# Apply all manifests
kubectl apply -f k8s/

# Or use Helm
helm install ml-platform ./ml-platform \
  --namespace ml-platform \
  --create-namespace \
  --set postgres.auth.password=secure_password

# Check deployment status
kubectl get pods -n ml-platform
kubectl get services -n ml-platform

# Port forward for testing
kubectl port-forward service/ml-app-service 8080:80 -n ml-platform
```

This Kubernetes setup provides a production-ready ML platform that works with modern container runtimes like **containerd** and **CRI-O**, eliminating the Docker dependency while maintaining full compatibility.
