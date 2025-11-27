namespace CSharp.CacheInvalidation;

public interface ICacheAccessTracker
{
    Task RecordAccessAsync(string key, CancellationToken token = default);
    Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default);
}