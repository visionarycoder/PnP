using System.Collections.Concurrent;

namespace CSharp.CacheInvalidation;

public class CacheDependencyTracker : ICacheDependencyTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> dependencies = new();
    private readonly ConcurrentDictionary<string, DateTime> dependencyTimestamps = new();

    public Task AddDependencyAsync(string key, string dependsOn, CancellationToken token = default)
    {
        dependencies.AddOrUpdate(dependsOn,
            new HashSet<string> { key },
            (k, existing) =>
            {
                existing.Add(key);
                return existing;
            });
        
        dependencyTimestamps[dependsOn] = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken token = default)
    {
        dependencies.TryGetValue(dependencyKey, out var dependentKeys);
        return Task.FromResult(dependentKeys?.AsEnumerable() ?? Enumerable.Empty<string>());
    }

    public Task RemoveDependencyAsync(string dependencyKey, CancellationToken token = default)
    {
        dependencies.TryRemove(dependencyKey, out _);
        dependencyTimestamps.TryRemove(dependencyKey, out _);
        return Task.CompletedTask;
    }

    public Task RemoveDependenciesAsync(string key, CancellationToken token = default)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in dependencies)
        {
            if (kvp.Value.Contains(key))
            {
                kvp.Value.Remove(key);
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (var keyToRemove in keysToRemove)
        {
            dependencies.TryRemove(keyToRemove, out _);
            dependencyTimestamps.TryRemove(keyToRemove, out _);
        }
        
        return Task.CompletedTask;
    }

    public Task CleanupExpiredDependenciesAsync(CancellationToken token = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24); // Remove dependencies older than 24 hours
        var expiredKeys = dependencyTimestamps
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            dependencies.TryRemove(key, out _);
            dependencyTimestamps.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}