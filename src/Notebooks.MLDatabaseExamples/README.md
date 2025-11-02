# ML Database Examples - Interactive Notebooks

This project contains interactive Jupyter notebooks demonstrating machine learning workflows with various database technologies. The notebooks are designed to run in Visual Studio Code with the .NET Interactive extension.

## Overview

This collection showcases practical implementations of:
- **Vector Databases**: PostgreSQL with pgvector extension and Chroma vector store
- **Analytics Databases**: DuckDB for fast analytical queries
- **Machine Learning**: Embedding generation, similarity search, and data analysis
- **Visualization**: Interactive charts and plots using Plotly.NET

## Project Structure

```
src/Notebooks.MLDatabaseExamples/
├── Notebooks.MLDatabaseExamples.csproj   # Project file with dependencies
├── NotebookConfiguration.cs              # Configuration helper class
├── postgresql-examples.ipynb             # PostgreSQL + pgvector examples
├── chroma-examples.ipynb                 # Chroma vector database examples
├── duckdb-analytics.ipynb               # DuckDB analytics examples
└── README.md                            # This file
```

## Prerequisites

### Software Requirements
- **.NET 9.0 SDK** or later
- **Visual Studio Code** with the following extensions:
  - .NET Interactive Notebooks
  - C# Dev Kit (optional but recommended)
- **PostgreSQL** with pgvector extension (for PostgreSQL examples)
- **Python 3.8+** (for some database drivers)

### Database Setup

#### PostgreSQL with pgvector
```sql
-- Install pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Create sample table
CREATE TABLE items (
    id SERIAL PRIMARY KEY,
    name TEXT,
    embedding vector(384)
);
```

#### Chroma Vector Database
```bash
# Install Chroma using pip
pip install chromadb

# Or using conda
conda install -c conda-forge chromadb
```

#### DuckDB
```bash
# Install DuckDB CLI (optional)
# DuckDB .NET driver is included in project dependencies
```

## Getting Started

### 1. Build the Project
```powershell
# Navigate to the project directory
cd src/Notebooks.MLDatabaseExamples

# Restore dependencies and build
dotnet build
```

### 2. Configure Connection Strings
Update the connection strings in `NotebookConfiguration.cs` or use environment variables:

```csharp
// Environment variables (recommended)
Environment.SetEnvironmentVariable("POSTGRESQL_CONNECTION", "your_connection_string");
Environment.SetEnvironmentVariable("CHROMA_HOST", "localhost");
Environment.SetEnvironmentVariable("CHROMA_PORT", "8000");
```

### 3. Open Notebooks in VS Code
1. Open the project folder in VS Code
2. Navigate to any `.ipynb` file
3. Select **.NET Interactive** as the kernel
4. Run cells sequentially using Shift+Enter

## Notebook Descriptions

### PostgreSQL Examples (`postgresql-examples.ipynb`)
**Focus**: Vector similarity search with PostgreSQL and pgvector

**Key Features**:
- Vector embedding storage and retrieval
- Similarity search using cosine distance
- Index optimization for large datasets
- Integration with ML.NET for embeddings

**Sample Operations**:
```csharp
// Store vectors
var embedding = GenerateEmbedding(text);
await connection.ExecuteAsync(
    "INSERT INTO items (name, embedding) VALUES (@name, @embedding)",
    new { name = text, embedding = embedding.ToArray() });

// Similarity search
var similar = await connection.QueryAsync<SimilarItem>(
    "SELECT name, 1 - (embedding <=> @query) AS similarity " +
    "FROM items ORDER BY embedding <=> @query LIMIT 10",
    new { query = queryEmbedding.ToArray() });
```

### Chroma Examples (`chroma-examples.ipynb`)
**Focus**: Vector database operations with ChromaDB

**Key Features**:
- Collection management and metadata filtering
- Document embeddings and retrieval
- Multi-modal search capabilities
- Persistence and backup strategies

**Sample Operations**:
```csharp
// Create collection
var collection = await client.CreateCollectionAsync("documents");

// Add documents with metadata
await collection.AddAsync(
    documents: new[] { "Sample document text" },
    metadatas: new[] { new Dictionary<string, object> { ["category"] = "tech" } },
    ids: new[] { "doc1" });

// Query with filters
var results = await collection.QueryAsync(
    queryTexts: new[] { "search query" },
    nResults: 5,
    where: new Dictionary<string, object> { ["category"] = "tech" });
```

### DuckDB Analytics (`duckdb-analytics.ipynb`)
**Focus**: Fast analytical queries and data processing

**Key Features**:
- High-performance analytical queries
- Data import from various formats (CSV, Parquet, JSON)
- Statistical analysis and aggregations
- Integration with plotting libraries

**Sample Operations**:
```csharp
// Load data from file
await connection.ExecuteAsync(
    "CREATE TABLE sales AS SELECT * FROM read_csv_auto('sales_data.csv')");

// Analytical queries
var monthlyStats = await connection.QueryAsync(
    @"SELECT 
        DATE_TRUNC('month', order_date) as month,
        COUNT(*) as order_count,
        SUM(amount) as total_amount,
        AVG(amount) as avg_amount
      FROM sales 
      GROUP BY month 
      ORDER BY month");

// Generate plots
var chart = Chart2D.Chart.Column<DateTime, decimal, string>(
    monthlyStats.Select(x => x.month),
    monthlyStats.Select(x => x.total_amount))
    .WithTitle("Monthly Sales");
```

## Key Dependencies

### Database Drivers
- **Npgsql** (5.0.18): PostgreSQL .NET driver
- **Pgvector** (0.2.0): Vector extension support
- **ChromaDB.Client** (latest): ChromaDB .NET client
- **DuckDB.NET.Data** (1.1.3): DuckDB .NET driver

### Machine Learning
- **Microsoft.ML** (3.0.1): ML.NET framework
- **Microsoft.ML.OnnxRuntime** (1.19.2): ONNX model inference

### Visualization
- **Plotly.NET** (5.0.0): Interactive plotting
- **Plotly.NET.Interactive** (5.0.0): Jupyter integration
- **Microsoft.Data.Analysis** (0.21.1): Data manipulation

### Interactive Notebooks
- **Microsoft.DotNet.Interactive** (1.0.522904): Notebook kernel
- **Microsoft.DotNet.Interactive.Formatting** (1.0.522904): Output formatting

## Configuration Class

The `NotebookConfiguration` class provides helper methods for database connections:

```csharp
public static class NotebookConfiguration
{
    public static string GetPostgreSqlConnectionString()
    {
        return Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION") 
            ?? "Host=localhost;Database=vectordb;Username=postgres;Password=password";
    }

    public static (string host, int port) GetChromaConfiguration()
    {
        var host = Environment.GetEnvironmentVariable("CHROMA_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("CHROMA_PORT") ?? "8000");
        return (host, port);
    }

    public static string GetDuckDbConnectionString()
    {
        return Environment.GetEnvironmentVariable("DUCKDB_CONNECTION") 
            ?? ":memory:"; // In-memory database
    }
}
```

## Performance Considerations

### PostgreSQL + pgvector
- **Indexing**: Create HNSW or IVFFlat indexes for large datasets
- **Batch Operations**: Use batch inserts for better performance
- **Connection Pooling**: Configure appropriate pool sizes

```sql
-- Create HNSW index for better performance
CREATE INDEX ON items USING hnsw (embedding vector_cosine_ops);
```

### Chroma
- **Batch Size**: Process documents in batches of 100-1000
- **Persistence**: Use persistent storage for production workloads
- **Metadata**: Keep metadata lightweight for better performance

### DuckDB
- **Columnar Storage**: Leverage Parquet format for analytical workloads
- **Memory Management**: Configure memory limits for large datasets
- **Parallel Processing**: Enable multi-threading for complex queries

## Troubleshooting

### Common Issues

1. **PostgreSQL Connection Errors**
   - Verify PostgreSQL is running and accessible
   - Check connection string format and credentials
   - Ensure pgvector extension is installed

2. **Chroma Service Unavailable**
   - Start Chroma server: `chroma run --host localhost --port 8000`
   - Check firewall settings and port availability

3. **DuckDB Memory Issues**
   - Increase memory limit in connection string
   - Process data in smaller chunks
   - Use streaming operations for large datasets

4. **Notebook Kernel Issues**
   - Restart the .NET Interactive kernel
   - Clear notebook outputs and restart VS Code
   - Verify .NET 9.0 SDK installation

### Performance Optimization

- **Use async/await** for all database operations
- **Implement connection pooling** for production scenarios
- **Cache embeddings** to avoid repeated computations
- **Monitor memory usage** with large datasets

## Next Steps

1. **Run the notebooks** sequentially to understand each database technology
2. **Experiment with different datasets** using your own data
3. **Optimize configurations** based on your specific use cases
4. **Extend examples** with additional ML algorithms and database operations
5. **Deploy solutions** using the patterns demonstrated in the notebooks

## Related Documentation

- [PostgreSQL pgvector Documentation](https://github.com/pgvector/pgvector)
- [ChromaDB Documentation](https://docs.trychroma.com/)
- [DuckDB Documentation](https://duckdb.org/docs/)
- [ML.NET Documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/)
- [Plotly.NET Documentation](https://plotly.net/)

## Contributing

When adding new notebooks or examples:
1. Follow the existing naming convention
2. Include comprehensive documentation in markdown cells
3. Add error handling and logging where appropriate
4. Test with sample datasets before committing
5. Update this README with new examples and dependencies