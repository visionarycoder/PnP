namespace CSharp.CacheInvalidation;

public interface ICacheInvalidationStatistics
{
    long TotalInvalidations { get; }
    long PatternInvalidations { get; }
    long TagInvalidations { get; }
    long DependencyInvalidations { get; }
    int ActiveRules { get; }
    DateTime LastUpdated { get; }
}