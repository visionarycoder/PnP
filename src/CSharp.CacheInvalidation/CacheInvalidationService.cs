using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharp.CacheInvalidation;

// Core cache invalidation interfaces
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

public interface ICacheInvalidationRule
{
    string Name { get; }
    bool ShouldInvalidate(CacheInvalidationContext context);
    Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default);
}

public class CacheInvalidationContext
{
    public string TriggerKey { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

// Comprehensive cache invalidation service
public class CacheInvalidationService : ICacheInvalidationService, IDisposable
{
    private readonly IDistributedCache distributedCache;
    private readonly IMemoryCache? memoryCache;
    private readonly ICacheDependencyTracker dependencyTracker;
    private readonly ICacheTagManager tagManager;
    private readonly ILogger? logger;
    private readonly CacheInvalidationOptions options;
    private readonly List<ICacheInvalidationRule> invalidationRules;
    private readonly Subject<CacheInvalidationEvent> invalidationStream;
    private readonly Timer cleanupTimer;
    private long totalInvalidations = 0;
    private long patternInvalidations = 0;
    private long tagInvalidations = 0;
    private long dependencyInvalidations = 0;
    private bool disposed = false;

    public CacheInvalidationService(
        IDistributedCache distributedCache,
        IMemoryCache? memoryCache = null,
        ICacheDependencyTracker? dependencyTracker = null,
        ICacheTagManager? tagManager = null,
        IOptions<CacheInvalidationOptions>? options = null,
        ILogger<CacheInvalidationService>? logger = null)
    {
        this.distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        this.memoryCache = memoryCache;
        this.dependencyTracker = dependencyTracker ?? new CacheDependencyTracker();
        this.tagManager = tagManager ?? new CacheTagManager(distributedCache, logger);
        this.options = options?.Value ?? new CacheInvalidationOptions();
        this.logger = logger;
        
        invalidationRules = new();
        invalidationStream = new Subject<CacheInvalidationEvent>();
        
        // Set up cleanup timer for expired invalidation records
        cleanupTimer = new Timer(PerformCleanup, null, 
            this.options.CleanupInterval, this.options.CleanupInterval);
        
        // Set up reactive stream processing for invalidation events
        SetupInvalidationStream();
    }

    public async Task InvalidateAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerKey = key,
                TriggerType = "direct",
                Timestamp = DateTime.UtcNow
            };

            // Apply invalidation rules
            await ApplyInvalidationRulesAsync(context, token).ConfigureAwait(false);

            // Remove from distributed cache
            await distributedCache.RemoveAsync(key, token).ConfigureAwait(false);
            
            // Remove from memory cache if available
            memoryCache?.Remove(key);
            
            // Remove dependencies
            await dependencyTracker.RemoveDependenciesAsync(key, token).ConfigureAwait(false);
            
            // Publish invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = new[] { key },
                InvalidationType = CacheInvalidationType.Direct,
                Timestamp = DateTime.UtcNow,
                Context = context
            });

            Interlocked.Increment(ref totalInvalidations);
            logger?.LogTrace("Cache key {Key} invalidated", key);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error invalidating cache key {Key}", key);
            throw;
        }
    }

    public async Task InvalidateAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        var keyList = keys?.ToList() ?? throw new ArgumentNullException(nameof(keys));
        if (keyList.Count == 0) return;

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerType = "bulk",
                Timestamp = DateTime.UtcNow
            };

            // Apply invalidation rules for each key
            var allKeysToInvalidate = new HashSet<string>(keyList);
            foreach (var key in keyList)
            {
                context.TriggerKey = key;
                var additionalKeys = await GetKeysFromRulesAsync(context, token).ConfigureAwait(false);
                foreach (var additionalKey in additionalKeys)
                {
                    allKeysToInvalidate.Add(additionalKey);
                }
            }

            // Execute bulk invalidation
            var tasks = allKeysToInvalidate.Select(async key =>
            {
                await distributedCache.RemoveAsync(key, token).ConfigureAwait(false);
                memoryCache?.Remove(key);
                await dependencyTracker.RemoveDependenciesAsync(key, token).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Publish invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = allKeysToInvalidate.ToArray(),
                InvalidationType = CacheInvalidationType.Bulk,
                Timestamp = DateTime.UtcNow,
                Context = context
            });

            Interlocked.Add(ref totalInvalidations, allKeysToInvalidate.Count);
            logger?.LogTrace("Bulk invalidated {Count} cache keys", allKeysToInvalidate.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during bulk cache invalidation");
            throw;
        }
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerKey = pattern,
                TriggerType = "pattern",
                Timestamp = DateTime.UtcNow
            };

            // Get keys matching pattern (implementation depends on cache provider)
            var matchingKeys = await GetKeysMatchingPatternAsync(pattern, token).ConfigureAwait(false);
            
            if (matchingKeys.Any())
            {
                await InvalidateAsync(matchingKeys, token).ConfigureAwait(false);
            }

            // Publish pattern invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = matchingKeys.ToArray(),
                InvalidationType = CacheInvalidationType.Pattern,
                Timestamp = DateTime.UtcNow,
                Context = context,
                Pattern = pattern
            });

            Interlocked.Increment(ref patternInvalidations);
            logger?.LogTrace("Pattern invalidation for {Pattern} affected {Count} keys", 
                pattern, matchingKeys.Count());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during pattern invalidation for {Pattern}", pattern);
            throw;
        }
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(tag))
            throw new ArgumentException("Tag cannot be null or empty", nameof(tag));

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerKey = tag,
                TriggerType = "tag",
                Timestamp = DateTime.UtcNow
            };

            // Get keys with the specified tag
            var keysWithTag = await tagManager.GetKeysByTagAsync(tag, token).ConfigureAwait(false);
            
            if (keysWithTag.Any())
            {
                await InvalidateAsync(keysWithTag, token).ConfigureAwait(false);
            }

            // Remove the tag itself
            await tagManager.RemoveTagAsync(tag, token).ConfigureAwait(false);

            // Publish tag invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = keysWithTag.ToArray(),
                InvalidationType = CacheInvalidationType.Tag,
                Timestamp = DateTime.UtcNow,
                Context = context,
                Tags = new[] { tag }
            });

            Interlocked.Increment(ref tagInvalidations);
            logger?.LogTrace("Tag invalidation for {Tag} affected {Count} keys", 
                tag, keysWithTag.Count());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during tag invalidation for {Tag}", tag);
            throw;
        }
    }

    public async Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        var tagList = tags?.ToList() ?? throw new ArgumentNullException(nameof(tags));
        if (tagList.Count == 0) return;

        try
        {
            var allKeysToInvalidate = new HashSet<string>();
            
            foreach (var tag in tagList)
            {
                var keysWithTag = await tagManager.GetKeysByTagAsync(tag, token).ConfigureAwait(false);
                foreach (var key in keysWithTag)
                {
                    allKeysToInvalidate.Add(key);
                }
            }

            if (allKeysToInvalidate.Count > 0)
            {
                await InvalidateAsync(allKeysToInvalidate, token).ConfigureAwait(false);
            }

            // Remove all tags
            await tagManager.RemoveTagsAsync(tagList, token).ConfigureAwait(false);

            // Publish multi-tag invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = allKeysToInvalidate.ToArray(),
                InvalidationType = CacheInvalidationType.Tag,
                Timestamp = DateTime.UtcNow,
                Tags = tagList.ToArray()
            });

            logger?.LogTrace("Multi-tag invalidation for {Tags} affected {Count} keys", 
                string.Join(", ", tagList), allKeysToInvalidate.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during multi-tag invalidation");
            throw;
        }
    }

    public async Task InvalidateDependenciesAsync(string dependencyKey, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(dependencyKey))
            throw new ArgumentException("Dependency key cannot be null or empty", nameof(dependencyKey));

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerKey = dependencyKey,
                TriggerType = "dependency",
                Timestamp = DateTime.UtcNow
            };

            // Get all keys that depend on this key
            var dependentKeys = await dependencyTracker.GetDependentKeysAsync(dependencyKey, token)
                .ConfigureAwait(false);
            
            if (dependentKeys.Any())
            {
                await InvalidateAsync(dependentKeys, token).ConfigureAwait(false);
            }

            // Remove dependency relationships
            await dependencyTracker.RemoveDependencyAsync(dependencyKey, token).ConfigureAwait(false);

            // Publish dependency invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = dependentKeys.ToArray(),
                InvalidationType = CacheInvalidationType.Dependency,
                Timestamp = DateTime.UtcNow,
                Context = context,
                DependencyKey = dependencyKey
            });

            Interlocked.Increment(ref dependencyInvalidations);
            logger?.LogTrace("Dependency invalidation for {DependencyKey} affected {Count} keys", 
                dependencyKey, dependentKeys.Count());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during dependency invalidation for {DependencyKey}", dependencyKey);
            throw;
        }
    }

    public async Task InvalidateHierarchyAsync(string hierarchyKey, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(hierarchyKey))
            throw new ArgumentException("Hierarchy key cannot be null or empty", nameof(hierarchyKey));

        try
        {
            var context = new CacheInvalidationContext
            {
                TriggerKey = hierarchyKey,
                TriggerType = "hierarchy",
                Timestamp = DateTime.UtcNow
            };

            // Get all keys in the hierarchy (all keys that start with hierarchyKey)
            var hierarchyPattern = $"{hierarchyKey}*";
            var keysInHierarchy = await GetKeysMatchingPatternAsync(hierarchyPattern, token)
                .ConfigureAwait(false);

            if (keysInHierarchy.Any())
            {
                await InvalidateAsync(keysInHierarchy, token).ConfigureAwait(false);
            }

            // Publish hierarchy invalidation event
            PublishInvalidationEvent(new CacheInvalidationEvent
            {
                Keys = keysInHierarchy.ToArray(),
                InvalidationType = CacheInvalidationType.Hierarchy,
                Timestamp = DateTime.UtcNow,
                Context = context,
                HierarchyKey = hierarchyKey
            });

            logger?.LogTrace("Hierarchy invalidation for {HierarchyKey} affected {Count} keys", 
                hierarchyKey, keysInHierarchy.Count());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during hierarchy invalidation for {HierarchyKey}", hierarchyKey);
            throw;
        }
    }

    public void RegisterInvalidationRule(ICacheInvalidationRule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        
        invalidationRules.Add(rule);
        logger?.LogInformation("Registered cache invalidation rule: {RuleName}", rule.Name);
    }

    public Task<ICacheInvalidationStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        var statistics = new CacheInvalidationStatistics
        {
            TotalInvalidations = totalInvalidations,
            PatternInvalidations = patternInvalidations,
            TagInvalidations = tagInvalidations,
            DependencyInvalidations = dependencyInvalidations,
            ActiveRules = invalidationRules.Count,
            LastUpdated = DateTime.UtcNow
        };

        return Task.FromResult<ICacheInvalidationStatistics>(statistics);
    }

    private async Task ApplyInvalidationRulesAsync(CacheInvalidationContext context, 
        CancellationToken token)
    {
        var additionalKeys = await GetKeysFromRulesAsync(context, token).ConfigureAwait(false);
        
        if (additionalKeys.Any())
        {
            await InvalidateAsync(additionalKeys, token).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<string>> GetKeysFromRulesAsync(CacheInvalidationContext context,
        CancellationToken token)
    {
        var allAdditionalKeys = new List<string>();

        foreach (var rule in invalidationRules)
        {
            try
            {
                if (rule.ShouldInvalidate(context))
                {
                    var keysFromRule = await rule.GetKeysToInvalidateAsync(context, token)
                        .ConfigureAwait(false);
                    allAdditionalKeys.AddRange(keysFromRule);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error applying invalidation rule {RuleName}", rule.Name);
            }
        }

        return allAdditionalKeys.Distinct();
    }

    private async Task<IEnumerable<string>> GetKeysMatchingPatternAsync(string pattern, 
        CancellationToken token)
    {
        // This is a simplified implementation
        // In a real scenario, you would need to implement pattern matching
        // based on your cache provider (Redis, SQL Server, etc.)
        
        // For Redis, you could use the KEYS command (not recommended in production)
        // or maintain a separate index of cache keys
        
        // For now, return empty collection
        await Task.Delay(1, token).ConfigureAwait(false);
        return Enumerable.Empty<string>();
    }

    private void SetupInvalidationStream()
    {
        // Set up reactive stream for processing invalidation events
        invalidationStream
            .Buffer(TimeSpan.FromSeconds(options.EventBufferSeconds), options.EventBufferSize)
            .Where(events => events.Count > 0)
            .Subscribe(events =>
            {
                try
                {
                    ProcessInvalidationEvents(events);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error processing invalidation events");
                }
            });
    }

    private void ProcessInvalidationEvents(IList<CacheInvalidationEvent> events)
    {
        // Process batched invalidation events
        logger?.LogTrace("Processing {Count} invalidation events", events.Count);
        
        // Here you could implement additional logic like:
        // - Sending notifications to other cache nodes
        // - Updating cache statistics
        // - Triggering cache warming for frequently accessed keys
        // - Logging cache invalidation metrics
    }

    private void PublishInvalidationEvent(CacheInvalidationEvent invalidationEvent)
    {
        invalidationStream.OnNext(invalidationEvent);
    }

    private void PerformCleanup(object? state)
    {
        try
        {
            // Clean up expired dependency relationships
            _ = Task.Run(async () =>
            {
                await dependencyTracker.CleanupExpiredDependenciesAsync().ConfigureAwait(false);
                await tagManager.CleanupExpiredTagsAsync().ConfigureAwait(false);
            });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during cache invalidation cleanup");
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            cleanupTimer?.Dispose();
            invalidationStream?.Dispose();
            disposed = true;
        }
    }
}