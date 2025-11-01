# Memoization and Function Caching Patterns

**Description**: Comprehensive memoization patterns including function result caching, lazy evaluation, cache invalidation strategies, thread-safe memoization, weak reference caching, and advanced caching decorators for optimizing expensive computations and improving application performance.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Caching;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Core memoization interface and implementation
public interface IMemoizer<TKey, TResult>
{
    TResult GetOrCompute(TKey key, Func<TKey, TResult> factory);
    Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory);
    bool TryGetValue(TKey key, out TResult result);
    void Invalidate(TKey key);
    void InvalidateAll();
    void InvalidateWhere(Func<TKey, bool> predicate);
    IMemoizationStatistics GetStatistics();
}

public class Memoizer<TKey, TResult> : IMemoizer<TKey, TResult>, IDisposable
{
    private readonly ConcurrentDictionary<TKey, CacheEntry<TResult>> cache;
    private readonly MemoizationOptions options;
    private readonly Timer cleanupTimer;
    private readonly ILogger logger;
    private long hitCount = 0;
    private long missCount = 0;
    private bool disposed = false;

    public Memoizer(MemoizationOptions options = null, ILogger logger = null)
    {
        this.options = options ?? new MemoizationOptions();
        this.logger = logger;
        
        cache = new ConcurrentDictionary<TKey, CacheEntry<TResult>>(
            this.options.InitialCapacity, 
            this.options.MaxConcurrency);
        
        if (this.options.EnableAutoCleanup && this.options.CleanupInterval > TimeSpan.Zero)
        {
            cleanupTimer = new Timer(PerformCleanup, null, 
                this.options.CleanupInterval, this.options.CleanupInterval);
        }
    }

    public TResult GetOrCompute(TKey key, Func<TKey, TResult> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var now = DateTime.UtcNow;
        
        if (cache.TryGetValue(key, out var cachedEntry))
        {
            if (!cachedEntry.IsExpired(now))
            {
                cachedEntry.UpdateLastAccessed(now);
                Interlocked.Increment(ref hitCount);
                logger?.LogTrace("Cache hit for key {Key}", key);
                return cachedEntry.Value;
            }
            
            // Entry is expired, remove it
            cache.TryRemove(key, out _);
            logger?.LogTrace("Cache entry expired for key {Key}", key);
        }

        Interlocked.Increment(ref missCount);
        logger?.LogTrace("Cache miss for key {Key}", key);

        // Check cache size limit before adding new entry
        if (options.MaxCacheSize > 0 && cache.Count >= options.MaxCacheSize)
        {
            EvictOldestEntries();
        }

        var newEntry = new CacheEntry<TResult>(
            factory(key), 
            now, 
            options.DefaultExpiration);
        
        cache.TryAdd(key, newEntry);
        return newEntry.Value;
    }

    public async Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var now = DateTime.UtcNow;
        
        if (cache.TryGetValue(key, out var cachedEntry))
        {
            if (!cachedEntry.IsExpired(now))
            {
                cachedEntry.UpdateLastAccessed(now);
                Interlocked.Increment(ref hitCount);
                logger?.LogTrace("Async cache hit for key {Key}", key);
                return cachedEntry.Value;
            }
            
            cache.TryRemove(key, out _);
            logger?.LogTrace("Async cache entry expired for key {Key}", key);
        }

        Interlocked.Increment(ref missCount);
        logger?.LogTrace("Async cache miss for key {Key}", key);

        if (options.MaxCacheSize > 0 && cache.Count >= options.MaxCacheSize)
        {
            EvictOldestEntries();
        }

        var result = await factory(key).ConfigureAwait(false);
        var newEntry = new CacheEntry<TResult>(result, now, options.DefaultExpiration);
        
        cache.TryAdd(key, newEntry);
        return result;
    }

    public bool TryGetValue(TKey key, out TResult result)
    {
        result = default(TResult);
        
        if (cache.TryGetValue(key, out var cachedEntry))
        {
            if (!cachedEntry.IsExpired(DateTime.UtcNow))
            {
                result = cachedEntry.Value;
                return true;
            }
            
            cache.TryRemove(key, out _);
        }
        
        return false;
    }

    public void Invalidate(TKey key)
    {
        if (cache.TryRemove(key, out _))
        {
            logger?.LogTrace("Invalidated cache entry for key {Key}", key);
        }
    }

    public void InvalidateAll()
    {
        var count = cache.Count;
        cache.Clear();
        logger?.LogInformation("Invalidated all {Count} cache entries", count);
    }

    public void InvalidateWhere(Func<TKey, bool> predicate)
    {
        var keysToRemove = cache.Keys.Where(predicate).ToList();
        
        foreach (var key in keysToRemove)
        {
            cache.TryRemove(key, out _);
        }
        
        logger?.LogInformation("Invalidated {Count} cache entries matching predicate", keysToRemove.Count);
    }

    public IMemoizationStatistics GetStatistics()
    {
        return new MemoizationStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            EntryCount = cache.Count,
            HitRate = hitCount + missCount > 0 ? (double)hitCount / (hitCount + missCount) : 0
        };
    }

    private void EvictOldestEntries()
    {
        var entriesToRemove = cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(Math.Max(1, cache.Count / 4))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in entriesToRemove)
        {
            cache.TryRemove(key, out _);
        }
        
        logger?.LogTrace("Evicted {Count} oldest cache entries", entriesToRemove.Count);
    }

    private void PerformCleanup(object state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = cache
            .Where(kvp => kvp.Value.IsExpired(now))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            cache.TryRemove(key, out _);
        }
        
        if (expiredKeys.Count > 0)
        {
            logger?.LogTrace("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            cleanupTimer?.Dispose();
            cache.Clear();
            disposed = true;
        }
    }
}

// Cache entry with expiration and access tracking
internal class CacheEntry<TResult>
{
    public TResult Value { get; }
    public DateTime Created { get; }
    public DateTime LastAccessed { get; private set; }
    public TimeSpan? Expiration { get; }
    public DateTime? ExpiresAt { get; }

    public CacheEntry(TResult value, DateTime created, TimeSpan? expiration = null)
    {
        Value = value;
        Created = created;
        LastAccessed = created;
        Expiration = expiration;
        ExpiresAt = expiration.HasValue ? created.Add(expiration.Value) : null;
    }

    public bool IsExpired(DateTime now)
    {
        return ExpiresAt.HasValue && now > ExpiresAt.Value;
    }

    public void UpdateLastAccessed(DateTime now)
    {
        LastAccessed = now;
    }
}

// Memoization options and statistics
public class MemoizationOptions
{
    public int MaxCacheSize { get; set; } = 1000;
    public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableAutoCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int InitialCapacity { get; set; } = 16;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
}

public interface IMemoizationStatistics
{
    long HitCount { get; }
    long MissCount { get; }
    int EntryCount { get; }
    double HitRate { get; }
}

public class MemoizationStatistics : IMemoizationStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public int EntryCount { get; set; }
    public double HitRate { get; set; }
}

// Weak reference memoizer for memory-conscious caching
public class WeakReferenceMemoizer<TKey, TResult> : IMemoizer<TKey, TResult>, IDisposable
    where TResult : class
{
    private readonly ConcurrentDictionary<TKey, WeakReference<TResult>> cache;
    private readonly Timer cleanupTimer;
    private readonly ILogger logger;
    private long hitCount = 0;
    private long missCount = 0;
    private bool disposed = false;

    public WeakReferenceMemoizer(ILogger logger = null)
    {
        this.logger = logger;
        cache = new ConcurrentDictionary<TKey, WeakReference<TResult>>();
        
        // Clean up dead weak references periodically
        cleanupTimer = new Timer(CleanupDeadReferences, null, 
            TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    public TResult GetOrCompute(TKey key, Func<TKey, TResult> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var result))
        {
            Interlocked.Increment(ref hitCount);
            logger?.LogTrace("Weak reference cache hit for key {Key}", key);
            return result;
        }

        Interlocked.Increment(ref missCount);
        logger?.LogTrace("Weak reference cache miss for key {Key}", key);

        var newResult = factory(key);
        cache.AddOrUpdate(key, 
            new WeakReference<TResult>(newResult),
            (k, oldRef) => new WeakReference<TResult>(newResult));
        
        return newResult;
    }

    public async Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var result))
        {
            Interlocked.Increment(ref hitCount);
            logger?.LogTrace("Async weak reference cache hit for key {Key}", key);
            return result;
        }

        Interlocked.Increment(ref missCount);
        logger?.LogTrace("Async weak reference cache miss for key {Key}", key);

        var newResult = await factory(key).ConfigureAwait(false);
        cache.AddOrUpdate(key, 
            new WeakReference<TResult>(newResult),
            (k, oldRef) => new WeakReference<TResult>(newResult));
        
        return newResult;
    }

    public bool TryGetValue(TKey key, out TResult result)
    {
        result = default(TResult);
        
        if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out result))
        {
            return true;
        }
        
        // Remove dead reference
        if (weakRef != null)
        {
            cache.TryRemove(key, out _);
        }
        
        return false;
    }

    public void Invalidate(TKey key)
    {
        if (cache.TryRemove(key, out _))
        {
            logger?.LogTrace("Invalidated weak reference cache entry for key {Key}", key);
        }
    }

    public void InvalidateAll()
    {
        var count = cache.Count;
        cache.Clear();
        logger?.LogInformation("Invalidated all {Count} weak reference cache entries", count);
    }

    public void InvalidateWhere(Func<TKey, bool> predicate)
    {
        var keysToRemove = cache.Keys.Where(predicate).ToList();
        
        foreach (var key in keysToRemove)
        {
            cache.TryRemove(key, out _);
        }
        
        logger?.LogInformation("Invalidated {Count} weak reference cache entries matching predicate", keysToRemove.Count);
    }

    public IMemoizationStatistics GetStatistics()
    {
        return new MemoizationStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            EntryCount = cache.Count,
            HitRate = hitCount + missCount > 0 ? (double)hitCount / (hitCount + missCount) : 0
        };
    }

    private void CleanupDeadReferences(object state)
    {
        var deadKeys = new List<TKey>();
        
        foreach (var kvp in cache)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                deadKeys.Add(kvp.Key);
            }
        }
        
        foreach (var key in deadKeys)
        {
            cache.TryRemove(key, out _);
        }
        
        if (deadKeys.Count > 0)
        {
            logger?.LogTrace("Cleaned up {Count} dead weak reference cache entries", deadKeys.Count);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            cleanupTimer?.Dispose();
            cache.Clear();
            disposed = true;
        }
    }
}

// Function-specific memoization decorators
public static class MemoizationExtensions
{
    public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> function, 
        MemoizationOptions options = null, ILogger logger = null)
    {
        var memoizer = new Memoizer<T, TResult>(options, logger);
        return key => memoizer.GetOrCompute(key, function);
    }

    public static Func<T1, T2, TResult> Memoize<T1, T2, TResult>(this Func<T1, T2, TResult> function,
        MemoizationOptions options = null, ILogger logger = null)
    {
        var memoizer = new Memoizer<(T1, T2), TResult>(options, logger);
        return (arg1, arg2) => memoizer.GetOrCompute((arg1, arg2), key => function(key.arg1, key.arg2));
    }

    public static Func<T1, T2, T3, TResult> Memoize<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> function,
        MemoizationOptions options = null, ILogger logger = null)
    {
        var memoizer = new Memoizer<(T1, T2, T3), TResult>(options, logger);
        return (arg1, arg2, arg3) => memoizer.GetOrCompute((arg1, arg2, arg3), 
            key => function(key.arg1, key.arg2, key.arg3));
    }

    public static Func<T, Task<TResult>> MemoizeAsync<T, TResult>(this Func<T, Task<TResult>> function,
        MemoizationOptions options = null, ILogger logger = null)
    {
        var memoizer = new Memoizer<T, TResult>(options, logger);
        return key => memoizer.GetOrComputeAsync(key, function);
    }

    public static Func<T1, T2, Task<TResult>> MemoizeAsync<T1, T2, TResult>(this Func<T1, T2, Task<TResult>> function,
        MemoizationOptions options = null, ILogger logger = null)
    {
        var memoizer = new Memoizer<(T1, T2), TResult>(options, logger);
        return (arg1, arg2) => memoizer.GetOrComputeAsync((arg1, arg2), 
            key => function(key.arg1, key.arg2));
    }

    public static Func<T, TResult> MemoizeWeak<T, TResult>(this Func<T, TResult> function, ILogger logger = null)
        where TResult : class
    {
        var memoizer = new WeakReferenceMemoizer<T, TResult>(logger);
        return key => memoizer.GetOrCompute(key, function);
    }
}

// Advanced memoization with custom key generation
public interface IKeyGenerator<in TInput, out TKey>
{
    TKey GenerateKey(TInput input);
}

public class JsonKeyGenerator<TInput> : IKeyGenerator<TInput, string>
{
    public string GenerateKey(TInput input)
    {
        return JsonSerializer.Serialize(input);
    }
}

public class HashCodeKeyGenerator<TInput> : IKeyGenerator<TInput, int>
{
    public int GenerateKey(TInput input)
    {
        return input?.GetHashCode() ?? 0;
    }
}

public class CustomMemoizer<TInput, TKey, TResult> : IMemoizer<TInput, TResult>, IDisposable
{
    private readonly IMemoizer<TKey, TResult> innerMemoizer;
    private readonly IKeyGenerator<TInput, TKey> keyGenerator;

    public CustomMemoizer(IKeyGenerator<TInput, TKey> keyGenerator, 
        IMemoizer<TKey, TResult> innerMemoizer = null,
        MemoizationOptions options = null,
        ILogger logger = null)
    {
        this.keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        this.innerMemoizer = innerMemoizer ?? new Memoizer<TKey, TResult>(options, logger);
    }

    public TResult GetOrCompute(TInput input, Func<TInput, TResult> factory)
    {
        var key = keyGenerator.GenerateKey(input);
        return innerMemoizer.GetOrCompute(key, _ => factory(input));
    }

    public Task<TResult> GetOrComputeAsync(TInput input, Func<TInput, Task<TResult>> factory)
    {
        var key = keyGenerator.GenerateKey(input);
        return innerMemoizer.GetOrComputeAsync(key, _ => factory(input));
    }

    public bool TryGetValue(TInput input, out TResult result)
    {
        var key = keyGenerator.GenerateKey(input);
        return innerMemoizer.TryGetValue(key, out result);
    }

    public void Invalidate(TInput input)
    {
        var key = keyGenerator.GenerateKey(input);
        innerMemoizer.Invalidate(key);
    }

    public void InvalidateAll()
    {
        innerMemoizer.InvalidateAll();
    }

    public void InvalidateWhere(Func<TInput, bool> predicate)
    {
        // This is challenging without storing input->key mapping
        // For now, just clear all
        innerMemoizer.InvalidateAll();
    }

    public IMemoizationStatistics GetStatistics()
    {
        return innerMemoizer.GetStatistics();
    }

    public void Dispose()
    {
        innerMemoizer?.Dispose();
    }
}

// Lazy memoization with deferred computation
public class LazyMemoizer<TKey, TResult> : IMemoizer<TKey, TResult>, IDisposable
{
    private readonly ConcurrentDictionary<TKey, Lazy<TResult>> cache;
    private readonly MemoizationOptions options;
    private readonly ILogger logger;
    private long hitCount = 0;
    private long missCount = 0;
    private bool disposed = false;

    public LazyMemoizer(MemoizationOptions options = null, ILogger logger = null)
    {
        this.options = options ?? new MemoizationOptions();
        this.logger = logger;
        cache = new ConcurrentDictionary<TKey, Lazy<TResult>>();
    }

    public TResult GetOrCompute(TKey key, Func<TKey, TResult> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var lazy = cache.GetOrAdd(key, k =>
        {
            Interlocked.Increment(ref missCount);
            logger?.LogTrace("Lazy cache miss for key {Key}", key);
            return new Lazy<TResult>(() => factory(k), LazyThreadSafetyMode.ExecutionAndPublication);
        });

        if (cache.ContainsKey(key))
        {
            Interlocked.Increment(ref hitCount);
            logger?.LogTrace("Lazy cache hit for key {Key}", key);
        }

        return lazy.Value;
    }

    public async Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory)
    {
        // For async operations, we need to handle Task<TResult> specially
        var taskLazy = cache.GetOrAdd(key, k =>
        {
            Interlocked.Increment(ref missCount);
            logger?.LogTrace("Async lazy cache miss for key {Key}", key);
            return new Lazy<TResult>(() => factory(k).GetAwaiter().GetResult(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        });

        if (cache.ContainsKey(key))
        {
            Interlocked.Increment(ref hitCount);
            logger?.LogTrace("Async lazy cache hit for key {Key}", key);
        }

        return taskLazy.Value;
    }

    public bool TryGetValue(TKey key, out TResult result)
    {
        result = default(TResult);
        
        if (cache.TryGetValue(key, out var lazy) && lazy.IsValueCreated)
        {
            result = lazy.Value;
            return true;
        }
        
        return false;
    }

    public void Invalidate(TKey key)
    {
        if (cache.TryRemove(key, out _))
        {
            logger?.LogTrace("Invalidated lazy cache entry for key {Key}", key);
        }
    }

    public void InvalidateAll()
    {
        var count = cache.Count;
        cache.Clear();
        logger?.LogInformation("Invalidated all {Count} lazy cache entries", count);
    }

    public void InvalidateWhere(Func<TKey, bool> predicate)
    {
        var keysToRemove = cache.Keys.Where(predicate).ToList();
        
        foreach (var key in keysToRemove)
        {
            cache.TryRemove(key, out _);
        }
        
        logger?.LogInformation("Invalidated {Count} lazy cache entries matching predicate", keysToRemove.Count);
    }

    public IMemoizationStatistics GetStatistics()
    {
        return new MemoizationStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            EntryCount = cache.Count,
            HitRate = hitCount + missCount > 0 ? (double)hitCount / (hitCount + missCount) : 0
        };
    }

    public void Dispose()
    {
        if (!disposed)
        {
            cache.Clear();
            disposed = true;
        }
    }
}

// Time-based and size-based cache eviction policies
public interface ICacheEvictionPolicy<TKey>
{
    bool ShouldEvict<TResult>(TKey key, CacheEntry<TResult> entry, DateTime now);
    IEnumerable<TKey> SelectKeysForEviction<TResult>(
        IEnumerable<KeyValuePair<TKey, CacheEntry<TResult>>> entries, int count);
}

public class LruEvictionPolicy<TKey> : ICacheEvictionPolicy<TKey>
{
    public bool ShouldEvict<TResult>(TKey key, CacheEntry<TResult> entry, DateTime now)
    {
        return entry.IsExpired(now);
    }

    public IEnumerable<TKey> SelectKeysForEviction<TResult>(
        IEnumerable<KeyValuePair<TKey, CacheEntry<TResult>>> entries, int count)
    {
        return entries
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(count)
            .Select(kvp => kvp.Key);
    }
}

public class LfuEvictionPolicy<TKey> : ICacheEvictionPolicy<TKey>
{
    private readonly ConcurrentDictionary<TKey, long> accessCounts = new();

    public bool ShouldEvict<TResult>(TKey key, CacheEntry<TResult> entry, DateTime now)
    {
        return entry.IsExpired(now);
    }

    public IEnumerable<TKey> SelectKeysForEviction<TResult>(
        IEnumerable<KeyValuePair<TKey, CacheEntry<TResult>>> entries, int count)
    {
        return entries
            .OrderBy(kvp => accessCounts.GetValueOrDefault(kvp.Key, 0))
            .ThenBy(kvp => kvp.Value.LastAccessed)
            .Take(count)
            .Select(kvp => kvp.Key);
    }

    public void RecordAccess(TKey key)
    {
        accessCounts.AddOrUpdate(key, 1, (k, count) => count + 1);
    }

    public void RemoveKey(TKey key)
    {
        accessCounts.TryRemove(key, out _);
    }
}

// Hierarchical memoization with namespaces
public interface IHierarchicalMemoizer
{
    IMemoizer<TKey, TResult> GetMemoizer<TKey, TResult>(string nameSpace);
    void InvalidateNamespace(string nameSpace);
    void InvalidateAll();
    IDictionary<string, IMemoizationStatistics> GetAllStatistics();
}

public class HierarchicalMemoizer : IHierarchicalMemoizer, IDisposable
{
    private readonly ConcurrentDictionary<string, object> memoizers;
    private readonly MemoizationOptions defaultOptions;
    private readonly ILogger logger;
    private bool disposed = false;

    public HierarchicalMemoizer(MemoizationOptions defaultOptions = null, ILogger logger = null)
    {
        this.defaultOptions = defaultOptions ?? new MemoizationOptions();
        this.logger = logger;
        memoizers = new ConcurrentDictionary<string, object>();
    }

    public IMemoizer<TKey, TResult> GetMemoizer<TKey, TResult>(string nameSpace)
    {
        if (string.IsNullOrEmpty(nameSpace))
            throw new ArgumentException("Namespace cannot be null or empty", nameof(nameSpace));

        var key = $"{nameSpace}:{typeof(TKey).FullName}->{typeof(TResult).FullName}";
        
        return (IMemoizer<TKey, TResult>)memoizers.GetOrAdd(key, _ =>
        {
            logger?.LogTrace("Creating new memoizer for namespace {Namespace}", nameSpace);
            return new Memoizer<TKey, TResult>(defaultOptions, logger);
        });
    }

    public void InvalidateNamespace(string nameSpace)
    {
        var keysToRemove = memoizers.Keys
            .Where(key => key.StartsWith($"{nameSpace}:", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            if (memoizers.TryRemove(key, out var memoizer))
            {
                if (memoizer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        logger?.LogInformation("Invalidated namespace {Namespace} with {Count} memoizers", 
            nameSpace, keysToRemove.Count);
    }

    public void InvalidateAll()
    {
        var count = memoizers.Count;
        
        foreach (var memoizer in memoizers.Values)
        {
            if (memoizer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        memoizers.Clear();
        logger?.LogInformation("Invalidated all namespaces with {Count} memoizers", count);
    }

    public IDictionary<string, IMemoizationStatistics> GetAllStatistics()
    {
        var statistics = new Dictionary<string, IMemoizationStatistics>();

        foreach (var kvp in memoizers)
        {
            if (kvp.Value is IMemoizer<object, object> memoizer)
            {
                statistics[kvp.Key] = memoizer.GetStatistics();
            }
        }

        return statistics;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            InvalidateAll();
            disposed = true;
        }
    }
}

// Memoization with dependency injection support
public interface IMemoizationService
{
    IMemoizer<TKey, TResult> CreateMemoizer<TKey, TResult>(string name = null, 
        MemoizationOptions options = null);
    void InvalidateMemoizer(string name);
    void InvalidateAll();
    IDictionary<string, IMemoizationStatistics> GetStatistics();
}

public class MemoizationService : IMemoizationService, IDisposable
{
    private readonly ConcurrentDictionary<string, object> namedMemoizers;
    private readonly MemoizationOptions defaultOptions;
    private readonly ILogger<MemoizationService> logger;
    private bool disposed = false;

    public MemoizationService(IOptions<MemoizationOptions> options = null, 
        ILogger<MemoizationService> logger = null)
    {
        this.defaultOptions = options?.Value ?? new MemoizationOptions();
        this.logger = logger;
        namedMemoizers = new ConcurrentDictionary<string, object>();
    }

    public IMemoizer<TKey, TResult> CreateMemoizer<TKey, TResult>(string name = null, 
        MemoizationOptions options = null)
    {
        var memoizerName = name ?? $"{typeof(TKey).Name}_{typeof(TResult).Name}_{Guid.NewGuid():N}";
        var effectiveOptions = options ?? defaultOptions;

        var memoizer = new Memoizer<TKey, TResult>(effectiveOptions, logger);
        namedMemoizers.TryAdd(memoizerName, memoizer);

        logger?.LogInformation("Created memoizer {MemoizerName} for {KeyType} -> {ResultType}",
            memoizerName, typeof(TKey).Name, typeof(TResult).Name);

        return memoizer;
    }

    public void InvalidateMemoizer(string name)
    {
        if (namedMemoizers.TryRemove(name, out var memoizer))
        {
            if (memoizer is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            logger?.LogInformation("Invalidated memoizer {MemoizerName}", name);
        }
    }

    public void InvalidateAll()
    {
        var count = namedMemoizers.Count;
        
        foreach (var memoizer in namedMemoizers.Values)
        {
            if (memoizer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        namedMemoizers.Clear();
        logger?.LogInformation("Invalidated all {Count} memoizers", count);
    }

    public IDictionary<string, IMemoizationStatistics> GetStatistics()
    {
        var statistics = new Dictionary<string, IMemoizationStatistics>();

        foreach (var kvp in namedMemoizers)
        {
            if (kvp.Value is IMemoizer<object, object> memoizer)
            {
                statistics[kvp.Key] = memoizer.GetStatistics();
            }
        }

        return statistics;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            InvalidateAll();
            disposed = true;
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Function Memoization
Console.WriteLine("Basic Memoization Examples:");

// Simple function memoization
Func<int, long> fibonacci = null;
fibonacci = n => n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);

var memoizedFibonacci = fibonacci.Memoize(new MemoizationOptions
{
    MaxCacheSize = 100,
    DefaultExpiration = TimeSpan.FromMinutes(10)
});

// Test performance difference
var sw = Stopwatch.StartNew();
var result1 = fibonacci(35);
sw.Stop();
Console.WriteLine($"Regular fibonacci(35): {result1} in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var result2 = memoizedFibonacci(35);
sw.Stop();
Console.WriteLine($"Memoized fibonacci(35): {result2} in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var result3 = memoizedFibonacci(35); // Second call should be very fast
sw.Stop();
Console.WriteLine($"Cached fibonacci(35): {result3} in {sw.ElapsedMilliseconds}ms");

// Multi-parameter function memoization
Func<int, int, string> expensiveOperation = (x, y) =>
{
    Thread.Sleep(100); // Simulate expensive operation
    return $"Result for ({x}, {y}): {x * y + x + y}";
};

var memoizedOperation = expensiveOperation.Memoize();

sw.Restart();
var op1 = memoizedOperation(5, 10);
sw.Stop();
Console.WriteLine($"First call: {op1} in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var op2 = memoizedOperation(5, 10); // Should be cached
sw.Stop();
Console.WriteLine($"Second call: {op2} in {sw.ElapsedMilliseconds}ms");

// Example 2: Async Function Memoization
Console.WriteLine("\nAsync Memoization Examples:");

Func<string, Task<string>> asyncDataFetch = async url =>
{
    await Task.Delay(500); // Simulate network call
    return $"Data from {url} at {DateTime.Now:HH:mm:ss.fff}";
};

var memoizedAsyncFetch = asyncDataFetch.MemoizeAsync(new MemoizationOptions
{
    DefaultExpiration = TimeSpan.FromSeconds(30)
});

sw.Restart();
var data1 = await memoizedAsyncFetch("https://api.example.com/data");
sw.Stop();
Console.WriteLine($"First async call: {data1} in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var data2 = await memoizedAsyncFetch("https://api.example.com/data");
sw.Stop();
Console.WriteLine($"Second async call: {data2} in {sw.ElapsedMilliseconds}ms");

// Example 3: Custom Memoizer with Statistics
Console.WriteLine("\nCustom Memoizer Examples:");

var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<Memoizer<string, decimal>>();

using var memoizer = new Memoizer<string, decimal>(new MemoizationOptions
{
    MaxCacheSize = 50,
    DefaultExpiration = TimeSpan.FromMinutes(5),
    EnableAutoCleanup = true
}, logger);

// Simulate database queries
Func<string, decimal> databaseQuery = productId =>
{
    Thread.Sleep(Random.Shared.Next(50, 200)); // Simulate DB latency
    return Random.Shared.Next(100, 1000) + (decimal)Random.Shared.NextDouble();
};

var products = new[] { "PROD-001", "PROD-002", "PROD-003", "PROD-001", "PROD-002" };

foreach (var productId in products)
{
    sw.Restart();
    var price = memoizer.GetOrCompute(productId, databaseQuery);
    sw.Stop();
    
    Console.WriteLine($"Price for {productId}: ${price:F2} in {sw.ElapsedMilliseconds}ms");
}

var stats = memoizer.GetStatistics();
Console.WriteLine($"\nMemoizer Statistics:");
Console.WriteLine($"  Hit Count: {stats.HitCount}");
Console.WriteLine($"  Miss Count: {stats.MissCount}");
Console.WriteLine($"  Hit Rate: {stats.HitRate:P1}");
Console.WriteLine($"  Cache Size: {stats.EntryCount}");

// Example 4: Weak Reference Memoization
Console.WriteLine("\nWeak Reference Memoization Examples:");

using var weakMemoizer = new WeakReferenceMemoizer<int, string>();

Func<int, string> largeObjectFactory = size =>
{
    var data = new string('*', size * 1000);
    Console.WriteLine($"Created large object of size {data.Length:N0} chars");
    return data;
};

// Create some large objects
var obj1 = weakMemoizer.GetOrCompute(100, largeObjectFactory);
var obj2 = weakMemoizer.GetOrCompute(200, largeObjectFactory);
var obj3 = weakMemoizer.GetOrCompute(100, largeObjectFactory); // Should be cached

Console.WriteLine($"Object 1 length: {obj1.Length:N0}");
Console.WriteLine($"Object 3 same reference: {ReferenceEquals(obj1, obj3)}");

// Clear strong references
obj1 = null;
obj2 = null;
obj3 = null;

// Force garbage collection
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

// Try to get objects again - they might be collected
var obj4 = weakMemoizer.GetOrCompute(100, largeObjectFactory); // Might recreate
Console.WriteLine($"Object 4 length: {obj4.Length:N0}");

var weakStats = weakMemoizer.GetStatistics();
Console.WriteLine($"Weak memoizer hit rate: {weakStats.HitRate:P1}");

// Example 5: Lazy Memoization
Console.WriteLine("\nLazy Memoization Examples:");

using var lazyMemoizer = new LazyMemoizer<string, ExpensiveCalculationResult>();

Func<string, ExpensiveCalculationResult> expensiveCalculation = input =>
{
    Console.WriteLine($"Performing expensive calculation for: {input}");
    Thread.Sleep(300); // Simulate expensive computation
    
    return new ExpensiveCalculationResult
    {
        Input = input,
        Result = input.GetHashCode() * 42,
        CalculatedAt = DateTime.UtcNow
    };
};

// Multiple threads requesting the same calculation
var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
{
    var threadId = Thread.CurrentThread.ManagedThreadId;
    Console.WriteLine($"Thread {threadId} requesting calculation for 'test-data'");
    
    var result = lazyMemoizer.GetOrCompute("test-data", expensiveCalculation);
    Console.WriteLine($"Thread {threadId} got result: {result.Result} calculated at {result.CalculatedAt:HH:mm:ss.fff}");
    
    return result;
})).ToArray();

var results = await Task.WhenAll(tasks);
Console.WriteLine($"All threads got same instance: {results.All(r => ReferenceEquals(r, results[0]))}");

// Example 6: Hierarchical Memoization
Console.WriteLine("\nHierarchical Memoization Examples:");

using var hierarchicalMemoizer = new HierarchicalMemoizer();

// Create memoizers for different domains
var userMemoizer = hierarchicalMemoizer.GetMemoizer<int, UserProfile>("users");
var productMemoizer = hierarchicalMemoizer.GetMemoizer<string, ProductInfo>("products");
var analyticsMemoizer = hierarchicalMemoizer.GetMemoizer<string, AnalyticsData>("analytics");

// Simulate operations
var userProfile = userMemoizer.GetOrCompute(123, userId => new UserProfile 
{ 
    Id = userId, 
    Name = $"User {userId}",
    CreatedAt = DateTime.UtcNow
});

var productInfo = productMemoizer.GetOrCompute("LAPTOP-001", productId => new ProductInfo
{
    Id = productId,
    Name = "Gaming Laptop",
    Price = 1299.99m
});

Console.WriteLine($"User: {userProfile.Name}");
Console.WriteLine($"Product: {productInfo.Name} - ${productInfo.Price}");

// Get statistics for all namespaces
var allStats = hierarchicalMemoizer.GetAllStatistics();
foreach (var stat in allStats)
{
    Console.WriteLine($"Namespace {stat.Key}: {stat.Value.EntryCount} entries");
}

// Invalidate specific namespace
hierarchicalMemoizer.InvalidateNamespace("analytics");
Console.WriteLine("Analytics namespace invalidated");

// Example 7: Custom Key Generation
Console.WriteLine("\nCustom Key Generation Examples:");

var jsonKeyGenerator = new JsonKeyGenerator<SearchCriteria>();
using var customMemoizer = new CustomMemoizer<SearchCriteria, string, SearchResult>(
    jsonKeyGenerator, 
    new Memoizer<string, SearchResult>());

Func<SearchCriteria, SearchResult> searchFunction = criteria =>
{
    Thread.Sleep(150); // Simulate search operation
    
    return new SearchResult
    {
        Query = criteria.Query,
        CategoryId = criteria.CategoryId,
        Results = Enumerable.Range(1, criteria.MaxResults)
            .Select(i => $"Result {i} for '{criteria.Query}'")
            .ToList(),
        SearchedAt = DateTime.UtcNow
    };
};

var searchCriteria1 = new SearchCriteria 
{ 
    Query = "laptop", 
    CategoryId = "electronics", 
    MaxResults = 10 
};

var searchCriteria2 = new SearchCriteria 
{ 
    Query = "laptop", 
    CategoryId = "electronics", 
    MaxResults = 10 
}; // Same content, should use cached result

sw.Restart();
var search1 = customMemoizer.GetOrCompute(searchCriteria1, searchFunction);
sw.Stop();
Console.WriteLine($"First search: {search1.Results.Count} results in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var search2 = customMemoizer.GetOrCompute(searchCriteria2, searchFunction);
sw.Stop();
Console.WriteLine($"Second search: {search2.Results.Count} results in {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Same instance returned: {ReferenceEquals(search1, search2)}");

// Example 8: Memoization Service with DI
Console.WriteLine("\nMemoization Service Examples:");

var services = new ServiceCollection()
    .Configure<MemoizationOptions>(opts =>
    {
        opts.MaxCacheSize = 200;
        opts.DefaultExpiration = TimeSpan.FromMinutes(15);
    })
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<IMemoizationService, MemoizationService>()
    .BuildServiceProvider();

var memoizationService = services.GetRequiredService<IMemoizationService>();

// Create named memoizers for different use cases
var weatherMemoizer = memoizationService.CreateMemoizer<string, WeatherData>("weather-cache");
var stockMemoizer = memoizationService.CreateMemoizer<string, StockPrice>("stock-cache");

Func<string, WeatherData> getWeather = city =>
{
    Thread.Sleep(200);
    return new WeatherData
    {
        City = city,
        Temperature = Random.Shared.Next(-10, 35),
        Condition = "Sunny",
        UpdatedAt = DateTime.UtcNow
    };
};

Func<string, StockPrice> getStockPrice = symbol =>
{
    Thread.Sleep(100);
    return new StockPrice
    {
        Symbol = symbol,
        Price = (decimal)(Random.Shared.NextDouble() * 1000),
        UpdatedAt = DateTime.UtcNow
    };
};

// Use the memoizers
var weather = weatherMemoizer.GetOrCompute("New York", getWeather);
var stock = stockMemoizer.GetOrCompute("AAPL", getStockPrice);

Console.WriteLine($"Weather in {weather.City}: {weather.Temperature}°C, {weather.Condition}");
Console.WriteLine($"Stock {stock.Symbol}: ${stock.Price:F2}");

// Get service statistics
var serviceStats = memoizationService.GetStatistics();
foreach (var stat in serviceStats)
{
    Console.WriteLine($"Service {stat.Key}: {stat.Value.EntryCount} entries, {stat.Value.HitRate:P1} hit rate");
}

Console.WriteLine("\nMemoization patterns examples completed!");
```

**Helper classes for examples:**

```csharp
public class ExpensiveCalculationResult
{
    public string Input { get; set; }
    public long Result { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class AnalyticsData
{
    public string Key { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class SearchCriteria
{
    public string Query { get; set; }
    public string CategoryId { get; set; }
    public int MaxResults { get; set; }
}

public class SearchResult
{
    public string Query { get; set; }
    public string CategoryId { get; set; }
    public List<string> Results { get; set; } = new();
    public DateTime SearchedAt { get; set; }
}

public class WeatherData
{
    public string City { get; set; }
    public int Temperature { get; set; }
    public string Condition { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StockPrice
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Notes**:

- Use memoization to cache results of expensive computations and avoid redundant calculations
- Implement thread-safe memoization using ConcurrentDictionary for multi-threaded applications  
- Configure expiration times to balance cache effectiveness with memory usage
- Use weak references for large objects to allow garbage collection when memory is needed
- Implement custom key generation for complex input types that need special equality semantics
- Set up cache size limits and eviction policies to prevent unbounded memory growth
- Monitor cache statistics (hit rate, entry count) to optimize cache configuration
- Use lazy memoization to ensure expensive operations are only performed once per key
- Implement hierarchical memoization to organize caches by domain or namespace
- Integrate with dependency injection for centralized cache management
- Consider cache invalidation strategies based on data freshness requirements
- Use async memoization for I/O-bound operations to avoid blocking threads

**Prerequisites**:

- Understanding of concurrent collections and thread-safe programming in .NET
- Knowledge of memory management, garbage collection, and weak references
- Familiarity with caching strategies and performance optimization techniques
- Experience with dependency injection and service lifetime management
- Understanding of lazy evaluation and deferred computation patterns

**Related Snippets**:

- [Async Lazy Loading](async-lazy-loading.md) - Asynchronous lazy initialization patterns
- [Memory Pools](memory-pools.md) - Memory management and object pooling strategies
- [Performance Optimization](micro-optimizations.md) - General performance improvement techniques
- [Distributed Cache](distributed-cache.md) - Redis and distributed caching patterns
