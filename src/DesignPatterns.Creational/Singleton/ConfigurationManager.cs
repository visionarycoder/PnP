namespace Snippets.DesignPatterns.Creational.Singleton;

/// <summary>
/// Example of inheriting from generic singleton
/// </summary>
public class ConfigurationManager : SingletonBase<ConfigurationManager>
{
    public string ConnectionString { get; set; } = "DefaultConnection";

    public void LoadConfiguration()
    {
        Console.WriteLine("Loading configuration...");
    }
}