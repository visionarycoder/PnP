namespace CSharp.CacheAside;

// Core cache-aside interfaces

public class CacheAsideOptions
{
    public TimeSpan? Expiration { get; set; }
    public bool AllowNullValues { get; set; } = true;
    public bool UseStaleWhileRevalidate { get; set; } = false;
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentFactoryCalls { get; set; } = Environment.ProcessorCount;
    public bool EnableStatistics { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

// Cache entry wrapper

// Cache level enumeration

// Cache statistics interface and implementation