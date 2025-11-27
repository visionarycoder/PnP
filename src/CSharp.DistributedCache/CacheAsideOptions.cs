namespace CSharp.DistributedCache;

/// <summary>
/// Cache aside pattern options
/// </summary>
public class CacheAsideOptions
{
    public TimeSpan? Expiration { get; set; }
    public bool RefreshAhead { get; set; } = false;
    public TimeSpan RefreshWindow { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentRefresh { get; set; } = 3;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool UseWriteThrough { get; set; } = false;
    public bool UseWriteBehind { get; set; } = false;
    public TimeSpan WriteBehindDelay { get; set; } = TimeSpan.FromSeconds(5);
}