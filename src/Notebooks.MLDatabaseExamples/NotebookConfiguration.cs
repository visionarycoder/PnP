namespace Notebooks.MLDatabaseExamples;

/// <summary>
/// Configuration settings for ML database examples notebooks
/// </summary>
public static class NotebookConfiguration
{
    /// <summary>
    /// Default PostgreSQL connection configuration for examples
    /// </summary>
    public static class PostgreSQL
    {
        public const string DefaultHost = "localhost";
        public const int DefaultPort = 5432;
        public const string DefaultDatabase = "ml_examples";
        public const string DefaultUser = "ml_user";
        public const string DefaultPassword = "ml_pass123";
        
        public static string GetConnectionString(string? host = null, int? port = null, 
            string? database = null, string? user = null, string? password = null) =>
            $"Host={host ?? DefaultHost};Port={port ?? DefaultPort};Database={database ?? DefaultDatabase};" +
            $"Username={user ?? DefaultUser};Password={password ?? DefaultPassword}";
    }

    /// <summary>
    /// Chroma vector database configuration
    /// </summary>
    public static class Chroma
    {
        public const string DefaultBaseUrl = "http://localhost:8000";
        public const string DefaultApiVersion = "v1";
        
        public static string GetApiUrl(string? baseUrl = null, string? version = null) =>
            $"{baseUrl ?? DefaultBaseUrl}/api/{version ?? DefaultApiVersion}";
    }

    /// <summary>
    /// DuckDB configuration for analytics
    /// </summary>
    public static class DuckDB
    {
        public const string DefaultPath = "./data/duckdb/ml_analytics.duckdb";
        
        public static string GetConnectionString(string? path = null) =>
            $"Data Source={path ?? DefaultPath}";
    }
}