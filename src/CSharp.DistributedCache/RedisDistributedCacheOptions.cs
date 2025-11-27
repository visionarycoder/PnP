using Microsoft.Extensions.Logging;

namespace CSharp.DistributedCache;

/// <summary>
/// Redis distributed cache options
/// </summary>
public class RedisDistributedCacheOptions
{
    public string KeyPrefix { get; set; } = "";
    public int DatabaseId { get; set; } = 0;
    public int MaxConcurrentOperations { get; set; } = 100;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableLogging { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
}