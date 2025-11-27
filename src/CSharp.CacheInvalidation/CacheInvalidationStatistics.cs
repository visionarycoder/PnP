namespace CSharp.CacheInvalidation;

public class CacheInvalidationStatistics : ICacheInvalidationStatistics
{
    public long TotalInvalidations { get; set; }
    public long PatternInvalidations { get; set; }
    public long TagInvalidations { get; set; }
    public long DependencyInvalidations { get; set; }
    public int ActiveRules { get; set; }
    public DateTime LastUpdated { get; set; }
}