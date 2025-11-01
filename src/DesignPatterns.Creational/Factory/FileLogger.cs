namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Concrete product: File logger
/// </summary>
public class FileLogger(string filePath = "app.log") : ILogger
{
    private readonly string filePath = filePath;

    public void Log(string message)
    {
        Console.WriteLine($"[FILE LOG] {DateTime.Now}: {message}");
        // In real implementation, write to file
    }

    public void LogError(string error)
    {
        Console.WriteLine($"[FILE ERROR] {DateTime.Now}: {error}");
    }
}