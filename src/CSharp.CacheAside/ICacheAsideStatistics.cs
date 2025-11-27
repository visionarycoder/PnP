namespace CSharp.CacheAside;

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