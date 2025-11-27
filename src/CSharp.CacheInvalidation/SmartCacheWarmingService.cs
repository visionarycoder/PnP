using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSharp.CacheInvalidation;

public class SmartCacheWarmingService : BackgroundService
{
    private readonly IDistributedCache cache;
    private readonly ICacheAccessTracker accessTracker;
    private readonly ICacheWarmingStrategy warmingStrategy;
    private readonly ILogger? logger;
    private readonly CacheWarmingOptions options;

    public SmartCacheWarmingService(
        IDistributedCache cache,
        ICacheAccessTracker accessTracker,
        ICacheWarmingStrategy warmingStrategy,
        Microsoft.Extensions.Options.IOptions<CacheWarmingOptions>? options = null,
        ILogger<SmartCacheWarmingService>? logger = null)
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

        var keysToWarm = new List<string>();
        
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