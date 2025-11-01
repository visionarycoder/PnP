namespace Snippets.DesignPatterns.Creational.Singleton;

/// <summary>
/// Generic Singleton base class for inheritance
/// </summary>
public abstract class SingletonBase<T> where T : class, new()
{
    private static readonly Lazy<T> instance = new(() => new T());

    public static T Instance => instance.Value;

    protected SingletonBase()
    {
    }
}