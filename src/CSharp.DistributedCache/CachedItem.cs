namespace CSharp.DistributedCache;

/// <summary>
/// Cached item wrapper with metadata
/// </summary>
public class CachedItem<T>
{
    public T Value { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}