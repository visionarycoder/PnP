namespace CSharp.Memoization;

/// <summary>
/// Statistics about memoization cache performance.
/// </summary>
public class MemoizationStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public int CacheSize { get; set; }
    public double HitRatio => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;
    public DateTime LastCleanup { get; set; }
}