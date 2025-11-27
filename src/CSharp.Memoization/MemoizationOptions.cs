namespace CSharp.Memoization;

/// <summary>
/// Options for configuring memoization behavior.
/// </summary>
public class MemoizationOptions
{
    public int InitialCapacity { get; set; } = 16;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int MaxCacheSize { get; set; } = 1000;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(60);
    public bool EnableAutoCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}