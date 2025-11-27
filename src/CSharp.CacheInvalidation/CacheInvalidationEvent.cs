namespace CSharp.CacheInvalidation;

public class CacheInvalidationEvent
{
    public string[] Keys { get; set; } = Array.Empty<string>();
    public CacheInvalidationType InvalidationType { get; set; }
    public DateTime Timestamp { get; set; }
    public CacheInvalidationContext? Context { get; set; }
    public string? Pattern { get; set; }
    public string[]? Tags { get; set; }
    public string? DependencyKey { get; set; }
    public string? HierarchyKey { get; set; }
}