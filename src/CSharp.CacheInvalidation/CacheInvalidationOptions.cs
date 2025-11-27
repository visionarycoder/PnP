namespace CSharp.CacheInvalidation;

public class CacheInvalidationOptions
{
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);
    public int EventBufferSize { get; set; } = 100;
    public int EventBufferSeconds { get; set; } = 10;
}