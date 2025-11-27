namespace CSharp.DistributedCache;

/// <summary>
/// Cache statistics implementation
/// </summary>
public class CacheStatistics : ICacheStatistics
{
    public long HitCount { get; init; }
    public long MissCount { get; init; }
    public double HitRate { get; init; }
    public long UsedMemory { get; init; }
    public long MaxMemory { get; init; }
    public int KeyCount { get; init; }
    public DateTimeOffset CollectionTime { get; init; } = DateTimeOffset.UtcNow;
}