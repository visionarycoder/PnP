namespace Snippets.DesignPatterns.Creational.Singleton;

/// <summary>
/// Thread-safe Singleton implementation using Lazy&lt;T&gt;
/// </summary>
public sealed class Singleton
{

    private static readonly Lazy<Singleton> instance = new(() => new Singleton());

    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static Singleton Instance => instance.Value;

    // Private constructor prevents instantiation from outside
    private Singleton()
    {
        InitializeResources();
    }

    private void InitializeResources()
    {
        // Initialize expensive resources here
        Console.WriteLine("Singleton instance created");
    }

    public void DoWork()
    {
        Console.WriteLine("Singleton is doing work...");
    }

    // Example property
    public string Name { get; set; } = "DefaultSingleton";
}