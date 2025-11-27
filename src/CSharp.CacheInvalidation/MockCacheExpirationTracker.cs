namespace CSharp.CacheInvalidation;

public class MockCacheExpirationTracker : ICacheExpirationTracker
{
    public Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default)
    {
        // Simulate finding expired keys
        var expiredKeys = new[] { "expired:key1", "expired:key2" };
        return Task.FromResult<IEnumerable<string>>(expiredKeys);
    }
}