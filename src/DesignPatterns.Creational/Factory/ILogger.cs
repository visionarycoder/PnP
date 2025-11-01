namespace Snippets.DesignPatterns.Creational.Factory;

/// <summary>
/// Abstract product interface
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogError(string error);
}