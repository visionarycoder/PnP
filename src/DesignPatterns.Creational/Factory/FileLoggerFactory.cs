namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Concrete creator: File logger factory
/// </summary>
public class FileLoggerFactory(string filePath = "app.log") : LoggerFactory
{
    public override ILogger CreateLogger()
    {
        return new FileLogger(filePath);
    }
}