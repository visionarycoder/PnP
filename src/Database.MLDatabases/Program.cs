using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;

namespace Database.MLDatabases;

// PostgreSQL ML Service with pgvector capabilities

// ChromaDB Vector Provider for semantic search

// DuckDB Analytics Provider for ML performance analysis

// ClickHouse ML Metrics Provider for time-series performance data

// SQLite ML Provider for lightweight development

// Data Models

// Supporting classes

// Main demonstration service

/// <summary>
/// Demonstrates better database technologies for local ML development including PostgreSQL with pgvector,
/// ChromaDB for vectors, DuckDB for analytics, ClickHouse for time-series metrics, and SQLite for
/// lightweight development workflows - providing superior alternatives to Azurite for ML workloads.
/// </summary>
public static class Program
{
    public static async Task Main()
    {
        var demoService = new MLDatabaseStackService();
        await demoService.RunCompleteStackDemo();
    }
}
