namespace CSharp.Memoization;

/// <summary>
/// Cache entry with expiration and access tracking.
/// </summary>
/// <typeparam name="TResult">The type of the cached result.</typeparam>
public class CacheEntry<TResult>
{
    public TResult Value { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastAccessed { get; private set; }
    public TimeSpan? Expiration { get; }

    public CacheEntry(TResult value, DateTime createdAt, TimeSpan? expiration = null)
    {
        Value = value;
        CreatedAt = createdAt;
        LastAccessed = createdAt;
        Expiration = expiration;
    }

    public bool IsExpired(DateTime now)
    {
        return Expiration.HasValue && now - CreatedAt > Expiration.Value;
    }

    public void UpdateLastAccessed(DateTime accessTime)
    {
        LastAccessed = accessTime;
    }
}