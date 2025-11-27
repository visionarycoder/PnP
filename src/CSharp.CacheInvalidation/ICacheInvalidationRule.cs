namespace CSharp.CacheInvalidation;

public interface ICacheInvalidationRule
{
    string Name { get; }
    bool ShouldInvalidate(CacheInvalidationContext context);
    Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default);
}