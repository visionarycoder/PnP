namespace CSharp.CacheInvalidation;

public class MockCacheWarmingStrategy : ICacheWarmingStrategy
{
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache cache;

    public MockCacheWarmingStrategy(Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        this.cache = cache;
    }

    public async Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            // Simulate loading data and caching it
            var data = $"Warmed data for {key}";
            await cache.SetStringAsync(key, data, token);
        }
    }
}