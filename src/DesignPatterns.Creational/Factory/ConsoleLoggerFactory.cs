namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Concrete creator: Console logger factory
/// </summary>
public class ConsoleLoggerFactory : LoggerFactory
{
    public override ILogger CreateLogger()
    {
        return new ConsoleLogger();
    }
}