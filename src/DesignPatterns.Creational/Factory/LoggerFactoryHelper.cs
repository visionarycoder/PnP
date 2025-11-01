namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Simple factory helper for common scenarios
/// </summary>
public static class LoggerFactoryHelper
{
    public static ILogger CreateLogger(LoggerType type, string? parameter = null)
    {
        return type switch
        {
            LoggerType.File => new FileLogger(parameter ?? "app.log"),
            LoggerType.Console => new ConsoleLogger(),
            LoggerType.Database => new DatabaseLogger(parameter ?? "DefaultConnection"),
            _ => throw new ArgumentException($"Unknown logger type: {type}")
        };
    }
}