namespace CSharp.CacheInvalidation;

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