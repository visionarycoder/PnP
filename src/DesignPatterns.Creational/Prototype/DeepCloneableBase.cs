using System.Text.Json;

namespace Snippets.DesignPatterns.Creational.Prototype;

/// <summary>
/// Deep cloneable base class using serialization
/// </summary>
public abstract class DeepCloneableBase<T> : IPrototype<T> where T : class
{
    /// <summary>
    /// Creates a deep copy using JSON serialization (recommended for .NET Core/.NET 5+)
    /// </summary>
    public virtual T Clone()
    {
        var json = JsonSerializer.Serialize(this, GetType());
        return JsonSerializer.Deserialize(json, GetType()) as T ??
               throw new InvalidOperationException("Failed to deserialize clone");
    }
}