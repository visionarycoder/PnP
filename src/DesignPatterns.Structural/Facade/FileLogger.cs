namespace Snippets.DesignPatterns.Structural.Facade;

public class FileLogger(string logPath = "app.log")
{
    private readonly string logPath = logPath;

    public void Log(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Console.WriteLine($"📝 Writing to log: {logEntry}");

        // Simulate file writing
        Thread.Sleep(10);
    }

    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARN", message);
    public void Error(string message) => Log("ERROR", message);
    public void Debug(string message) => Log("DEBUG", message);
}