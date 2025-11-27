using System.ComponentModel.DataAnnotations;

namespace Database.MLDatabaseExamples;

// ML Database Technologies - Supporting Models and Services

// PostgreSQL ML Service (simplified for demo)

// ChromaDB Service (simplified for demo)

// DuckDB Analytics Service (simplified for demo)

// Supporting data models

// Main demo service

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
