namespace CSharp.DistributedCache;

/// <summary>
/// Cache statistics interface
/// </summary>
public interface ICacheStatistics
{
    long HitCount { get; }
    long MissCount { get; }
    double HitRate { get; }
    long UsedMemory { get; }
    long MaxMemory { get; }
    int KeyCount { get; }
    DateTimeOffset CollectionTime { get; }
}