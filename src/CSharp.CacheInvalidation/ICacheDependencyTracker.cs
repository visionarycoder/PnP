namespace CSharp.CacheInvalidation;

public interface ICacheDependencyTracker
{
    Task AddDependencyAsync(string key, string dependsOn, CancellationToken token = default);
    Task<IEnumerable<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependencyAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependenciesAsync(string key, CancellationToken token = default);
    Task CleanupExpiredDependenciesAsync(CancellationToken token = default);
}