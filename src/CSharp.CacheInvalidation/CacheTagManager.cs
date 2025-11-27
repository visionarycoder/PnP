using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CSharp.CacheInvalidation;

public class CacheTagManager : ICacheTagManager
{
    private readonly IDistributedCache cache;
    private readonly ILogger? logger;
    private readonly string tagPrefix = "tag:";
    private readonly string keyTagsPrefix = "key-tags:";

    public CacheTagManager(IDistributedCache cache, ILogger? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger;
    }

    public async Task AddTagAsync(string key, string tag, CancellationToken token = default)
    {
        await AddTagsAsync(key, new[] { tag }, token).ConfigureAwait(false);
    }

    public async Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default)
    {
        var tagList = tags.ToList();
        
        foreach (var tag in tagList)
        {
            // Add key to tag's key list
            var tagKey = $"{tagPrefix}{tag}";
            var existingKeys = await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
            existingKeys.Add(key);
            
            var serializedKeys = JsonSerializer.Serialize(existingKeys);
            await cache.SetStringAsync(tagKey, serializedKeys, token).ConfigureAwait(false);
        }
        
        // Add tags to key's tag list
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        var existingTags = await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
        existingTags.UnionWith(tagList);
        
        var serializedTags = JsonSerializer.Serialize(existingTags);
        await cache.SetStringAsync(keyTagsKey, serializedTags, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        return await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default)
    {
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        return await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        await cache.RemoveAsync(tagKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        var tasks = tags.Select(tag => RemoveTagAsync(tag, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task CleanupExpiredTagsAsync(CancellationToken token = default)
    {
        // In a real implementation, you would need to:
        // 1. Get all tag keys
        // 2. Check if the keys they reference still exist
        // 3. Remove tags that reference non-existent keys
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetKeysFromTagStorage(string tagKey, CancellationToken token)
    {
        var serializedKeys = await cache.GetStringAsync(tagKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedKeys))
            return new HashSet<string>();
            
        try
        {
            var keys = JsonSerializer.Deserialize<string[]>(serializedKeys);
            return new HashSet<string>(keys ?? Array.Empty<string>());
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private async Task<HashSet<string>> GetTagsFromKeyStorage(string keyTagsKey, CancellationToken token)
    {
        var serializedTags = await cache.GetStringAsync(keyTagsKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedTags))
            return new HashSet<string>();
            
        try
        {
            var tags = JsonSerializer.Deserialize<string[]>(serializedTags);
            return new HashSet<string>(tags ?? Array.Empty<string>());
        }
        catch
        {
            return new HashSet<string>();
        }
    }
}