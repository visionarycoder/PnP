namespace CSharp.CacheAside;

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