namespace CSharp.CacheInvalidation;

public interface ICacheExpirationTracker
{
    Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default);
}