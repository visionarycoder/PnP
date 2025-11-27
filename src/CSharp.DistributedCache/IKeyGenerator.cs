namespace CSharp.DistributedCache;

/// <summary>
/// Key generator interface for cache keys
/// </summary>
public interface IKeyGenerator<TKey>
{
    string GenerateKey(TKey key);
}