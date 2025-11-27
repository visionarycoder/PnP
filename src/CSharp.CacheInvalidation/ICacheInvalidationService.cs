namespace CSharp.CacheInvalidation;

public interface ICacheInvalidationService
{
    Task InvalidateAsync(string key, CancellationToken token = default);
    Task InvalidateAsync(IEnumerable<string> keys, CancellationToken token = default);
    Task InvalidateByPatternAsync(string pattern, CancellationToken token = default);
    Task InvalidateByTagAsync(string tag, CancellationToken token = default);
    Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
    Task InvalidateDependenciesAsync(string dependencyKey, CancellationToken token = default);
    Task InvalidateHierarchyAsync(string hierarchyKey, CancellationToken token = default);
    void RegisterInvalidationRule(ICacheInvalidationRule rule);
    Task<ICacheInvalidationStatistics> GetStatisticsAsync(CancellationToken token = default);
}