namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Concrete creator: Database logger factory
/// </summary>
public class DatabaseLoggerFactory(string connectionString) : LoggerFactory
{
    public override ILogger CreateLogger()
    {
        return new DatabaseLogger(connectionString);
    }
}