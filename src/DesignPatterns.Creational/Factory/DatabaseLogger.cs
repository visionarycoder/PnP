namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Concrete product: Database logger
/// </summary>
public class DatabaseLogger(string connectionString) : ILogger
{
    private readonly string connectionString = connectionString;

    public void Log(string message)
    {
        Console.WriteLine($"[DATABASE] Logged to DB: {message}");
        // In real implementation, insert into database
    }

    public void LogError(string error)
    {
        Console.WriteLine($"[DATABASE ERROR] Error logged to DB: {error}");
    }
}