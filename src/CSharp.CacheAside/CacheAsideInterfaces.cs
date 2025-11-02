namespace CSharp.CacheAside;

// Core cache-aside interfaces
public interface ICacheAsideService<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        TimeSpan? expiration = null, CancellationToken token = default);
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token = default);
    Task<IEnumerable<TValue>> GetManyAsync(IEnumerable<TKey> keys, 
        Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        TimeSpan? expiration = null, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null, 
        CancellationToken token = default);
    Task SetManyAsync(IDictionary<TKey, TValue> keyValuePairs, TimeSpan? expiration = null, 
        CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task RemoveManyAsync(IEnumerable<TKey> keys, CancellationToken token = default);
    Task<bool> ExistsAsync(TKey key, CancellationToken token = default);
    Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CancellationToken token = default);
    Task WarmupAsync(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        CancellationToken token = default);
    Task<ICacheAsideStatistics> GetStatisticsAsync(CancellationToken token = default);
}

public class CacheAsideOptions
{
    public TimeSpan? Expiration { get; set; }
    public bool AllowNullValues { get; set; } = true;
    public bool UseStaleWhileRevalidate { get; set; } = false;
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentFactoryCalls { get; set; } = Environment.ProcessorCount;
    public bool EnableStatistics { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

// Cache entry wrapper
public class CacheEntry<TValue>
{
    public TValue Value { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public string[] Tags { get; set; } = Array.Empty<string>();
}

// Cache level enumeration
public enum CacheLevel
{
    Memory,
    Distributed,
    Source
}

// Cache statistics interface and implementation
public interface ICacheAsideStatistics
{
    long MemoryHits { get; }
    long DistributedHits { get; }
    long Misses { get; }
    long StaleHits { get; }
    double HitRatio { get; }
    TimeSpan AverageOperationTime { get; }
    long TotalOperations { get; }
    DateTime LastResetTime { get; }
    void Reset();
}

public class CacheAsideStatistics : ICacheAsideStatistics
{
    private long memoryHits;
    private long distributedHits;
    private long misses;
    private long staleHits;
    private long totalOperations;
    private TimeSpan totalOperationTime;

    public long MemoryHits => Interlocked.Read(ref memoryHits);
    public long DistributedHits => Interlocked.Read(ref distributedHits);
    public long Misses => Interlocked.Read(ref misses);
    public long StaleHits => Interlocked.Read(ref staleHits);
    
    public double HitRatio
    {
        get
        {
            var totalOps = TotalOperations;
            return totalOps > 0 ? (double)(MemoryHits + DistributedHits) / totalOps : 0.0;
        }
    }

    public TimeSpan AverageOperationTime
    {
        get
        {
            var totalOps = TotalOperations;
            return totalOps > 0 ? TimeSpan.FromTicks(totalOperationTime.Ticks / totalOps) : TimeSpan.Zero;
        }
    }

    public long TotalOperations => Interlocked.Read(ref totalOperations);
    public DateTime LastResetTime { get; private set; } = DateTime.UtcNow;

    internal void RecordMemoryHit(bool isStale = false)
    {
        Interlocked.Increment(ref memoryHits);
        Interlocked.Increment(ref totalOperations);
        
        if (isStale)
        {
            Interlocked.Increment(ref staleHits);
        }
    }

    internal void RecordDistributedHit(bool isStale = false)
    {
        Interlocked.Increment(ref distributedHits);
        Interlocked.Increment(ref totalOperations);
        
        if (isStale)
        {
            Interlocked.Increment(ref staleHits);
        }
    }

    internal void RecordMiss()
    {
        Interlocked.Increment(ref misses);
        Interlocked.Increment(ref totalOperations);
    }

    internal void RecordOperationTime(TimeSpan duration)
    {
        var currentTotal = totalOperationTime;
        var newTotal = currentTotal.Add(duration);
        
        // Use compare exchange to ensure thread safety
        while (Interlocked.CompareExchange(ref totalOperationTime, newTotal, currentTotal) != currentTotal)
        {
            currentTotal = totalOperationTime;
            newTotal = currentTotal.Add(duration);
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref memoryHits, 0);
        Interlocked.Exchange(ref distributedHits, 0);
        Interlocked.Exchange(ref misses, 0);
        Interlocked.Exchange(ref staleHits, 0);
        Interlocked.Exchange(ref totalOperations, 0);
        Interlocked.Exchange(ref totalOperationTime, TimeSpan.Zero);
        LastResetTime = DateTime.UtcNow;
    }
}