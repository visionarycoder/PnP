namespace Patterns;

/// <summary>
/// Lazy-loading Singleton pattern implementation using .NET's Lazy<T> class.
/// Thread-safe and defers instantiation until first access.
/// </summary>
/// <typeparam name="T">The type to implement as a singleton. Must have a parameterless constructor.</typeparam>
public class LazySingleton<T> where T : new()
{
    private static readonly Lazy<T> _instance = new(() => new T());

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static T Instance => _instance.Value;

    /// <summary>
    /// Protected constructor to prevent direct instantiation.
    /// </summary>
    protected LazySingleton()
    {
    }
}
