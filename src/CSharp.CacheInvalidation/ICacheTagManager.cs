namespace CSharp.CacheInvalidation;

public interface ICacheTagManager
{
    Task AddTagAsync(string key, string tag, CancellationToken token = default);
    Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default);
    Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default);
    Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default);
    Task RemoveTagAsync(string tag, CancellationToken token = default);
    Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
    Task CleanupExpiredTagsAsync(CancellationToken token = default);
}