namespace CSharp.CacheInvalidation;

public class CacheWarmingOptions
{
    public TimeSpan WarmingInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AnalysisPeriod { get; set; } = TimeSpan.FromHours(1);
    public int TopKeysCount { get; set; } = 100;
}