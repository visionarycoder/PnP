namespace CSharp.CacheInvalidation;

public class MockCacheAccessTracker : ICacheAccessTracker
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> accessCounts = new();

    public Task RecordAccessAsync(string key, CancellationToken token = default)
    {
        accessCounts.AddOrUpdate(key, 1, (k, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default)
    {
        var topKeys = accessCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key);
            
        return Task.FromResult(topKeys);
    }
}