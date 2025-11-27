namespace CSharp.AsyncLazyLoading;

public class CacheService<TKey, TValue> where TKey : notnull
{
    private readonly AsyncMemoizer<TKey, TValue> memoizer;

    public CacheService(Func<TKey, Task<TValue>> valueFactory)
    {
        memoizer = new AsyncMemoizer<TKey, TValue>(valueFactory);
    }

    public Task<TValue> GetAsync(TKey key) => memoizer.GetAsync(key);
    
    public void Invalidate(TKey key) => memoizer.Invalidate(key);
    
    public void Clear() => memoizer.Clear();
    
    public int CacheSize => memoizer.CacheSize;
}