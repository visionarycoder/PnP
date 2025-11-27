namespace CSharp.CacheAside;

public class CacheEntry<TValue>
{
    public TValue Value { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public string[] Tags { get; set; } = Array.Empty<string>();
}