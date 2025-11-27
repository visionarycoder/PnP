namespace CSharp.DistributedCache;

/// <summary>
/// Default string-based key generator
/// </summary>
public class DefaultKeyGenerator<TKey> : IKeyGenerator<TKey>
{
    public string GenerateKey(TKey key)
    {
        return key?.ToString() ?? throw new ArgumentNullException(nameof(key));
    }
}