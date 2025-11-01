namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Abstract creator class
/// </summary>
public abstract class LoggerFactory
{
    /// <summary>
    /// Factory method - subclasses implement this
    /// </summary>
    public abstract ILogger CreateLogger();

    /// <summary>
    /// Template method that uses the factory method
    /// </summary>
    public void LogMessage(string message)
    {
        var logger = CreateLogger();
        logger.Log($"Factory: {GetType().Name} - {message}");
    }
}