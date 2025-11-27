namespace CSharp.CacheInvalidation;

public interface ICacheWarmingStrategy
{
    Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default);
}