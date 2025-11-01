# Cache Invalidation Strategies

**Description**: Comprehensive cache invalidation patterns including time-based expiration, event-driven invalidation, dependency-based invalidation, cache tagging, hierarchical cache invalidation, distributed cache synchronization, and intelligent cache warming strategies for maintaining data consistency and freshness.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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
    public string TriggerKey { get; set; }
    public string TriggerType { get; set; }
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new();
    public string UserId { get; set; }
    public string TenantId { get; set; }
}

// Comprehensive cache invalidation service
public class CacheInvalidationService : ICacheInvalidationService, IDisposable
{
    private readonly IDistributedCache distributedCache;
    private readonly IMemoryCache memoryCache;
    private readonly ICacheDependencyTracker dependencyTracker;
    private readonly ICacheTagManager tagManager;
    private readonly ILogger logger;
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
        IMemoryCache memoryCache = null,
        ICacheDependencyTracker dependencyTracker = null,
        ICacheTagManager tagManager = null,
        IOptions<CacheInvalidationOptions> options = null,
        ILogger<CacheInvalidationService> logger = null)
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
        var allAdditionalKeys = new();

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

    private void PerformCleanup(object state)
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

// Time-based cache invalidation
public class TimeBasedInvalidationService : BackgroundService
{
    private readonly ICacheInvalidationService invalidationService;
    private readonly ICacheExpirationTracker expirationTracker;
    private readonly ILogger logger;
    private readonly TimeSpan checkInterval;

    public TimeBasedInvalidationService(
        ICacheInvalidationService invalidationService,
        ICacheExpirationTracker expirationTracker,
        ILogger<TimeBasedInvalidationService> logger,
        TimeSpan? checkInterval = null)
    {
        this.invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
        this.expirationTracker = expirationTracker ?? throw new ArgumentNullException(nameof(expirationTracker));
        this.logger = logger;
        this.checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredKeys = await expirationTracker.GetExpiredKeysAsync(stoppingToken)
                    .ConfigureAwait(false);
                
                if (expiredKeys.Any())
                {
                    await invalidationService.InvalidateAsync(expiredKeys, stoppingToken)
                        .ConfigureAwait(false);
                    
                    logger?.LogTrace("Time-based invalidation processed {Count} expired keys", 
                        expiredKeys.Count());
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during time-based cache invalidation");
            }

            await Task.Delay(checkInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}

// Event-driven cache invalidation
public class EventDrivenInvalidationService : IDisposable
{
    private readonly ICacheInvalidationService invalidationService;
    private readonly IEventSubscriber eventSubscriber;
    private readonly Dictionary<string, Func<object, Task>> eventHandlers;
    private readonly ILogger logger;
    private bool disposed = false;

    public EventDrivenInvalidationService(
        ICacheInvalidationService invalidationService,
        IEventSubscriber eventSubscriber,
        ILogger<EventDrivenInvalidationService> logger = null)
    {
        this.invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
        this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
        this.logger = logger;
        
        eventHandlers = new Dictionary<string, Func<object, Task>>();
        SetupEventHandlers();
    }

    public void RegisterEventHandler(string eventType, Func<object, Task> handler)
    {
        eventHandlers[eventType] = handler;
        logger?.LogInformation("Registered event handler for {EventType}", eventType);
    }

    private void SetupEventHandlers()
    {
        // User-related events
        RegisterEventHandler("user.updated", async eventData =>
        {
            if (eventData is UserUpdatedEvent userEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"user:{userEvent.UserId}*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync("user-profiles")
                    .ConfigureAwait(false);
            }
        });

        // Product-related events  
        RegisterEventHandler("product.updated", async eventData =>
        {
            if (eventData is ProductUpdatedEvent productEvent)
            {
                await invalidationService.InvalidateAsync($"product:{productEvent.ProductId}")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync($"category:{productEvent.CategoryId}")
                    .ConfigureAwait(false);
            }
        });

        // Order-related events
        RegisterEventHandler("order.created", async eventData =>
        {
            if (eventData is OrderCreatedEvent orderEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"inventory:*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateAsync($"user:{orderEvent.UserId}:orders")
                    .ConfigureAwait(false);
            }
        });

        // Configuration changes
        RegisterEventHandler("config.changed", async eventData =>
        {
            if (eventData is ConfigChangedEvent configEvent)
            {
                await invalidationService.InvalidateByTagAsync("configuration")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByPatternAsync($"config:*")
                    .ConfigureAwait(false);
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.SubscribeAsync(HandleEventAsync, cancellationToken)
            .ConfigureAwait(false);
        
        logger?.LogInformation("Event-driven cache invalidation service started");
    }

    private async Task HandleEventAsync(EventMessage eventMessage)
    {
        try
        {
            if (eventHandlers.TryGetValue(eventMessage.EventType, out var handler))
            {
                await handler(eventMessage.Data).ConfigureAwait(false);
                logger?.LogTrace("Processed cache invalidation for event {EventType}", 
                    eventMessage.EventType);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling cache invalidation event {EventType}", 
                eventMessage.EventType);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.UnsubscribeAsync(cancellationToken).ConfigureAwait(false);
        logger?.LogInformation("Event-driven cache invalidation service stopped");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            eventSubscriber?.Dispose();
            disposed = true;
        }
    }
}

// Smart cache warming service
public class SmartCacheWarmingService : BackgroundService
{
    private readonly IDistributedCache cache;
    private readonly ICacheAccessTracker accessTracker;
    private readonly ICacheWarmingStrategy warmingStrategy;
    private readonly ILogger logger;
    private readonly CacheWarmingOptions options;

    public SmartCacheWarmingService(
        IDistributedCache cache,
        ICacheAccessTracker accessTracker,
        ICacheWarmingStrategy warmingStrategy,
        IOptions<CacheWarmingOptions> options = null,
        ILogger<SmartCacheWarmingService> logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.accessTracker = accessTracker ?? throw new ArgumentNullException(nameof(accessTracker));
        this.warmingStrategy = warmingStrategy ?? throw new ArgumentNullException(nameof(warmingStrategy));
        this.options = options?.Value ?? new CacheWarmingOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get frequently accessed but expired/missing keys
                var keysToWarm = await GetKeysRequiringWarmup(stoppingToken).ConfigureAwait(false);
                
                if (keysToWarm.Any())
                {
                    await warmingStrategy.WarmCacheAsync(keysToWarm, stoppingToken)
                        .ConfigureAwait(false);
                    
                    logger?.LogInformation("Cache warming completed for {Count} keys", 
                        keysToWarm.Count());
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during cache warming");
            }

            await Task.Delay(options.WarmingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<string>> GetKeysRequiringWarmup(CancellationToken token)
    {
        // Get most frequently accessed keys in the last period
        var frequentKeys = await accessTracker.GetMostFrequentKeysAsync(
            options.AnalysisPeriod, 
            options.TopKeysCount, 
            token).ConfigureAwait(false);

        var keysToWarm = new();
        
        foreach (var key in frequentKeys)
        {
            // Check if key exists in cache
            var cachedValue = await cache.GetAsync(key, token).ConfigureAwait(false);
            
            if (cachedValue == null)
            {
                keysToWarm.Add(key);
            }
        }

        return keysToWarm;
    }
}

// Conditional invalidation rules
public class ConditionalInvalidationRule : ICacheInvalidationRule
{
    public string Name { get; }
    private readonly Func<CacheInvalidationContext, bool> condition;
    private readonly Func<CacheInvalidationContext, Task<IEnumerable<string>>> keySelector;

    public ConditionalInvalidationRule(
        string name,
        Func<CacheInvalidationContext, bool> condition,
        Func<CacheInvalidationContext, Task<IEnumerable<string>>> keySelector)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public bool ShouldInvalidate(CacheInvalidationContext context)
    {
        return condition(context);
    }

    public Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default)
    {
        return keySelector(context);
    }
}

public class TimeBasedInvalidationRule : ICacheInvalidationRule
{
    public string Name => "TimeBasedInvalidation";
    private readonly TimeSpan maxAge;
    private readonly Func<CacheInvalidationContext, Task<DateTime?>> lastModifiedSelector;

    public TimeBasedInvalidationRule(
        TimeSpan maxAge,
        Func<CacheInvalidationContext, Task<DateTime?>> lastModifiedSelector)
    {
        this.maxAge = maxAge;
        this.lastModifiedSelector = lastModifiedSelector ?? throw new ArgumentNullException(nameof(lastModifiedSelector));
    }

    public bool ShouldInvalidate(CacheInvalidationContext context)
    {
        var lastModified = lastModifiedSelector(context).GetAwaiter().GetResult();
        
        if (!lastModified.HasValue)
            return false;
            
        return DateTime.UtcNow - lastModified.Value > maxAge;
    }

    public async Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default)
    {
        if (ShouldInvalidate(context))
        {
            // Return the context trigger key itself
            return new[] { context.TriggerKey };
        }
        
        return Enumerable.Empty<string>();
    }
}

// Supporting interfaces and classes
public interface ICacheDependencyTracker
{
    Task AddDependencyAsync(string key, string dependsOn, CancellationToken token = default);
    Task<IEnumerable<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependencyAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependenciesAsync(string key, CancellationToken token = default);
    Task CleanupExpiredDependenciesAsync(CancellationToken token = default);
}

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
        var keysToRemove = new();
        
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

public interface ICacheTagManager
{
    Task AddTagAsync(string key, string tag, CancellationToken token = default);
    Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default);
    Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default);
    Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default);
    Task RemoveTagAsync(string tag, CancellationToken token = default);
    Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
    Task CleanupExpiredTagsAsync(CancellationToken token = default);
}

public class CacheTagManager : ICacheTagManager
{
    private readonly IDistributedCache cache;
    private readonly ILogger logger;
    private readonly string tagPrefix = "tag:";
    private readonly string keyTagsPrefix = "key-tags:";

    public CacheTagManager(IDistributedCache cache, ILogger logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger;
    }

    public async Task AddTagAsync(string key, string tag, CancellationToken token = default)
    {
        await AddTagsAsync(key, new[] { tag }, token).ConfigureAwait(false);
    }

    public async Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default)
    {
        var tagList = tags.ToList();
        
        foreach (var tag in tagList)
        {
            // Add key to tag's key list
            var tagKey = $"{tagPrefix}{tag}";
            var existingKeys = await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
            existingKeys.Add(key);
            
            var serializedKeys = JsonSerializer.Serialize(existingKeys);
            await cache.SetStringAsync(tagKey, serializedKeys, token).ConfigureAwait(false);
        }
        
        // Add tags to key's tag list
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        var existingTags = await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
        existingTags.UnionWith(tagList);
        
        var serializedTags = JsonSerializer.Serialize(existingTags);
        await cache.SetStringAsync(keyTagsKey, serializedTags, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        return await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default)
    {
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        return await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        await cache.RemoveAsync(tagKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        var tasks = tags.Select(tag => RemoveTagAsync(tag, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task CleanupExpiredTagsAsync(CancellationToken token = default)
    {
        // In a real implementation, you would need to:
        // 1. Get all tag keys
        // 2. Check if the keys they reference still exist
        // 3. Remove tags that reference non-existent keys
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetKeysFromTagStorage(string tagKey, CancellationToken token)
    {
        var serializedKeys = await cache.GetStringAsync(tagKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedKeys))
            return new HashSet<string>();
            
        try
        {
            var keys = JsonSerializer.Deserialize<string[]>(serializedKeys);
            return new HashSet<string>(keys);
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private async Task<HashSet<string>> GetTagsFromKeyStorage(string keyTagsKey, CancellationToken token)
    {
        var serializedTags = await cache.GetStringAsync(keyTagsKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedTags))
            return new HashSet<string>();
            
        try
        {
            var tags = JsonSerializer.Deserialize<string[]>(serializedTags);
            return new HashSet<string>(tags);
        }
        catch
        {
            return new HashSet<string>();
        }
    }
}

// Event system interfaces
public interface IEventSubscriber : IDisposable
{
    Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default);
    Task UnsubscribeAsync(CancellationToken token = default);
}

public class EventMessage
{
    public string EventType { get; set; }
    public object Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; }
}

// Event data classes
public class UserUpdatedEvent
{
    public int UserId { get; set; }
    public string[] ChangedFields { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string[] ChangedFields { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int[] ProductIds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfigChangedEvent
{
    public string ConfigKey { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}

// Cache invalidation events and statistics
public class CacheInvalidationEvent
{
    public string[] Keys { get; set; }
    public CacheInvalidationType InvalidationType { get; set; }
    public DateTime Timestamp { get; set; }
    public CacheInvalidationContext Context { get; set; }
    public string Pattern { get; set; }
    public string[] Tags { get; set; }
    public string DependencyKey { get; set; }
    public string HierarchyKey { get; set; }
}

public enum CacheInvalidationType
{
    Direct,
    Bulk,
    Pattern,
    Tag,
    Dependency,
    Hierarchy,
    TimeBased,
    EventDriven
}

public interface ICacheInvalidationStatistics
{
    long TotalInvalidations { get; }
    long PatternInvalidations { get; }
    long TagInvalidations { get; }
    long DependencyInvalidations { get; }
    int ActiveRules { get; }
    DateTime LastUpdated { get; }
}

public class CacheInvalidationStatistics : ICacheInvalidationStatistics
{
    public long TotalInvalidations { get; set; }
    public long PatternInvalidations { get; set; }
    public long TagInvalidations { get; set; }
    public long DependencyInvalidations { get; set; }
    public int ActiveRules { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Configuration classes
public class CacheInvalidationOptions
{
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);
    public int EventBufferSize { get; set; } = 100;
    public int EventBufferSeconds { get; set; } = 10;
}

public class CacheWarmingOptions
{
    public TimeSpan WarmingInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AnalysisPeriod { get; set; } = TimeSpan.FromHours(1);
    public int TopKeysCount { get; set; } = 100;
}

// Cache access tracking and warming strategy interfaces
public interface ICacheAccessTracker
{
    Task RecordAccessAsync(string key, CancellationToken token = default);
    Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default);
}

public interface ICacheWarmingStrategy
{
    Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default);
}

public interface ICacheExpirationTracker
{
    Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default);
}
```

**Usage**:

```csharp
// Example 1: Basic Cache Invalidation Setup
Console.WriteLine("Basic Cache Invalidation Examples:");

var services = new ServiceCollection()
    .AddDistributedMemoryCache()
    .AddMemoryCache()
    .AddLogging(builder => builder.AddConsole())
    .Configure<CacheInvalidationOptions>(opts =>
    {
        opts.CleanupInterval = TimeSpan.FromMinutes(10);
        opts.EventBufferSize = 50;
    })
    .AddSingleton<ICacheDependencyTracker, CacheDependencyTracker>()
    .AddSingleton<ICacheTagManager, CacheTagManager>()
    .AddSingleton<ICacheInvalidationService, CacheInvalidationService>()
    .BuildServiceProvider();

var invalidationService = services.GetRequiredService<ICacheInvalidationService>();
var cache = services.GetRequiredService<IDistributedCache>();

// Set up some test data
await cache.SetStringAsync("user:123", JsonSerializer.Serialize(new { Id = 123, Name = "John" }));
await cache.SetStringAsync("user:124", JsonSerializer.Serialize(new { Id = 124, Name = "Jane" }));
await cache.SetStringAsync("product:456", JsonSerializer.Serialize(new { Id = 456, Name = "Laptop" }));

// Direct key invalidation
await invalidationService.InvalidateAsync("user:123");
Console.WriteLine("Invalidated user:123");

// Bulk invalidation
await invalidationService.InvalidateAsync(new[] { "user:124", "product:456" });
Console.WriteLine("Bulk invalidated multiple keys");

// Pattern-based invalidation
await invalidationService.InvalidateByPatternAsync("user:*");
Console.WriteLine("Invalidated all user keys by pattern");

// Example 2: Tag-Based Invalidation
Console.WriteLine("\nTag-Based Invalidation Examples:");

var tagManager = services.GetRequiredService<ICacheTagManager>();

// Add tags to cache entries
await tagManager.AddTagsAsync("profile:123", new[] { "user-data", "profile" });
await tagManager.AddTagsAsync("profile:124", new[] { "user-data", "profile" });
await tagManager.AddTagsAsync("settings:123", new[] { "user-data", "settings" });

// Cache some data with tags
await cache.SetStringAsync("profile:123", JsonSerializer.Serialize(new { UserId = 123, Name = "John" }));
await cache.SetStringAsync("profile:124", JsonSerializer.Serialize(new { UserId = 124, Name = "Jane" }));
await cache.SetStringAsync("settings:123", JsonSerializer.Serialize(new { Theme = "Dark" }));

// Invalidate by tag
await invalidationService.InvalidateByTagAsync("profile");
Console.WriteLine("Invalidated all profile entries");

// Get keys by tag
var userDataKeys = await tagManager.GetKeysByTagAsync("user-data");
Console.WriteLine($"Found {userDataKeys.Count()} keys with 'user-data' tag");

// Example 3: Dependency-Based Invalidation
Console.WriteLine("\nDependency-Based Invalidation Examples:");

var dependencyTracker = services.GetRequiredService<ICacheDependencyTracker>();

// Set up dependencies
await dependencyTracker.AddDependencyAsync("user:123:profile", "user:123");
await dependencyTracker.AddDependencyAsync("user:123:orders", "user:123");
await dependencyTracker.AddDependencyAsync("user:123:preferences", "user:123");

// Cache dependent data
await cache.SetStringAsync("user:123", JsonSerializer.Serialize(new { Id = 123, Name = "John" }));
await cache.SetStringAsync("user:123:profile", JsonSerializer.Serialize(new { Bio = "Developer" }));
await cache.SetStringAsync("user:123:orders", JsonSerializer.Serialize(new[] { 1, 2, 3 }));

// Invalidate dependencies
await invalidationService.InvalidateDependenciesAsync("user:123");
Console.WriteLine("Invalidated all dependencies of user:123");

// Example 4: Custom Invalidation Rules
Console.WriteLine("\nCustom Invalidation Rules Examples:");

// Time-based invalidation rule
var timeBasedRule = new TimeBasedInvalidationRule(
    TimeSpan.FromMinutes(30),
    async context =>
    {
        // In a real scenario, you'd check the last modified time from database
        return DateTime.UtcNow.AddMinutes(-45); // Simulate old data
    });

invalidationService.RegisterInvalidationRule(timeBasedRule);

// Conditional invalidation rule
var conditionalRule = new ConditionalInvalidationRule(
    "UserProfileInvalidation",
    context => context.TriggerKey.StartsWith("user:") && context.TriggerType == "direct",
    async context =>
    {
        var userId = context.TriggerKey.Split(':')[1];
        return new[]
        {
            $"user:{userId}:profile",
            $"user:{userId}:avatar",
            $"user:{userId}:permissions"
        };
    });

invalidationService.RegisterInvalidationRule(conditionalRule);
Console.WriteLine("Registered custom invalidation rules");

// Trigger rule-based invalidation
await invalidationService.InvalidateAsync("user:999");
Console.WriteLine("Triggered rule-based invalidation");

// Example 5: Event-Driven Invalidation
Console.WriteLine("\nEvent-Driven Invalidation Examples:");

// Mock event subscriber for demonstration
var eventSubscriber = new MockEventSubscriber();
var eventInvalidationService = new EventDrivenInvalidationService(
    invalidationService, 
    eventSubscriber);

await eventInvalidationService.StartAsync();

// Simulate events
await eventSubscriber.PublishEventAsync(new EventMessage
{
    EventType = "user.updated",
    Data = new UserUpdatedEvent 
    { 
        UserId = 123, 
        ChangedFields = new[] { "Name", "Email" },
        UpdatedAt = DateTime.UtcNow
    }
});

await eventSubscriber.PublishEventAsync(new EventMessage
{
    EventType = "product.updated",
    Data = new ProductUpdatedEvent 
    { 
        ProductId = 456, 
        CategoryId = 10,
        ChangedFields = new[] { "Price" },
        UpdatedAt = DateTime.UtcNow
    }
});

Console.WriteLine("Published invalidation events");

// Example 6: Hierarchical Invalidation
Console.WriteLine("\nHierarchical Invalidation Examples:");

// Set up hierarchical cache data
await cache.SetStringAsync("app:config", "Global config");
await cache.SetStringAsync("app:config:database", "DB config");
await cache.SetStringAsync("app:config:database:connection", "Connection string");
await cache.SetStringAsync("app:config:logging", "Logging config");
await cache.SetStringAsync("app:config:logging:level", "Debug");

// Invalidate entire hierarchy
await invalidationService.InvalidateHierarchyAsync("app:config");
Console.WriteLine("Invalidated entire app:config hierarchy");

// Example 7: Smart Cache Warming
Console.WriteLine("\nSmart Cache Warming Examples:");

// Mock implementations for demonstration
var accessTracker = new MockCacheAccessTracker();
var warmingStrategy = new MockCacheWarmingStrategy(cache);

var warmingService = new SmartCacheWarmingService(
    cache,
    accessTracker,
    warmingStrategy,
    Options.Create(new CacheWarmingOptions
    {
        WarmingInterval = TimeSpan.FromMinutes(5),
        TopKeysCount = 10
    }));

// Simulate access patterns
await accessTracker.RecordAccessAsync("popular:item1");
await accessTracker.RecordAccessAsync("popular:item1");
await accessTracker.RecordAccessAsync("popular:item2");
await accessTracker.RecordAccessAsync("popular:item1"); // Most popular

// The warming service would run in background and warm popular but missing keys
Console.WriteLine("Smart cache warming configured");

// Example 8: Cache Invalidation Statistics
Console.WriteLine("\nCache Invalidation Statistics:");

var statistics = await invalidationService.GetStatisticsAsync();
Console.WriteLine($"Total Invalidations: {statistics.TotalInvalidations}");
Console.WriteLine($"Pattern Invalidations: {statistics.PatternInvalidations}");
Console.WriteLine($"Tag Invalidations: {statistics.TagInvalidations}");
Console.WriteLine($"Dependency Invalidations: {statistics.DependencyInvalidations}");
Console.WriteLine($"Active Rules: {statistics.ActiveRules}");
Console.WriteLine($"Last Updated: {statistics.LastUpdated}");

// Example 9: Time-Based Background Invalidation
Console.WriteLine("\nTime-Based Background Invalidation:");

var expirationTracker = new MockCacheExpirationTracker();
var timeBasedInvalidationService = new TimeBasedInvalidationService(
    invalidationService,
    expirationTracker,
    services.GetRequiredService<ILogger<TimeBasedInvalidationService>>(),
    TimeSpan.FromSeconds(30));

// This would run as a background service
// await timeBasedInvalidationService.StartAsync(CancellationToken.None);
Console.WriteLine("Time-based invalidation service configured");

Console.WriteLine("\nCache invalidation strategies examples completed!");
```

**Helper classes for examples:**

```csharp
public class MockEventSubscriber : IEventSubscriber
{
    private Func<EventMessage, Task> handler;

    public Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default)
    {
        this.handler = handler;
        return Task.CompletedTask;
    }

    public async Task PublishEventAsync(EventMessage eventMessage)
    {
        if (handler != null)
        {
            await handler(eventMessage);
        }
    }

    public Task UnsubscribeAsync(CancellationToken token = default)
    {
        handler = null;
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class MockCacheAccessTracker : ICacheAccessTracker
{
    private readonly ConcurrentDictionary<string, int> accessCounts = new();

    public Task RecordAccessAsync(string key, CancellationToken token = default)
    {
        accessCounts.AddOrUpdate(key, 1, (k, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default)
    {
        var topKeys = accessCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key);
            
        return Task.FromResult(topKeys);
    }
}

public class MockCacheWarmingStrategy : ICacheWarmingStrategy
{
    private readonly IDistributedCache cache;

    public MockCacheWarmingStrategy(IDistributedCache cache)
    {
        this.cache = cache;
    }

    public async Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            // Simulate loading data and caching it
            var data = $"Warmed data for {key}";
            await cache.SetStringAsync(key, data, token);
        }
    }
}

public class MockCacheExpirationTracker : ICacheExpirationTracker
{
    public Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default)
    {
        // Simulate finding expired keys
        var expiredKeys = new[] { "expired:key1", "expired:key2" };
        return Task.FromResult<IEnumerable<string>>(expiredKeys);
    }
}
```

**Notes**:

- Implement comprehensive invalidation strategies to maintain cache consistency and data freshness
- Use tag-based invalidation to efficiently invalidate related cache entries as a group
- Set up dependency tracking to automatically invalidate dependent cache entries when source data changes
- Implement pattern-based invalidation for bulk cache management using key naming conventions
- Use event-driven invalidation to respond to data changes in real-time across distributed systems
- Configure time-based invalidation for automated cleanup of stale cache entries
- Implement hierarchical invalidation for nested or structured cache keys
- Use reactive programming patterns for efficient event processing and batching
- Set up intelligent cache warming to proactively load frequently accessed but missing data
- Monitor invalidation statistics to optimize cache performance and identify patterns
- Implement custom invalidation rules for complex business logic and conditional invalidation
- Use background services for automated cache maintenance and cleanup operations
- Configure appropriate buffer sizes and intervals for event processing to balance performance and responsiveness

**Prerequisites**:

- Understanding of distributed caching concepts and cache consistency models
- Knowledge of reactive programming patterns and event-driven architectures
- Familiarity with dependency injection and background service patterns in .NET
- Experience with JSON serialization and distributed system coordination
- Understanding of cache performance optimization and monitoring techniques

**Related Snippets**:

- [Distributed Cache](distributed-cache.md) - Redis and distributed caching implementations
- [Memoization](memoization.md) - In-memory function result caching patterns
- [Cache-Aside Pattern](cache-aside.md) - Cache-aside and write-through cache patterns
