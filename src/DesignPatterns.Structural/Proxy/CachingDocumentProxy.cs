namespace Snippets.DesignPatterns.Structural.Proxy;

public class CachingDocumentProxy(IDocumentService documentService, TimeSpan cacheExpiry) : IDocumentService
{
    private readonly Dictionary<string, (string content, DateTime cachedAt)> cache = new();

    public async Task<string> ReadDocument(string documentId)
    {
        // Check cache first
        if (cache.TryGetValue(documentId, out var cached))
        {
            if (DateTime.Now - cached.cachedAt <= cacheExpiry)
            {
                Console.WriteLine($"📋 Cache hit for document: {documentId}");
                return cached.content;
            }
            else
            {
                Console.WriteLine($"⏰ Cache expired for document: {documentId}");
                cache.Remove(documentId);
            }
        }

        Console.WriteLine($"📋 Cache miss for document: {documentId}");
        var content = await documentService.ReadDocument(documentId);

        // Cache the result
        cache[documentId] = (content, DateTime.Now);
        return content;
    }

    public async Task WriteDocument(string documentId, string content)
    {
        await documentService.WriteDocument(documentId, content);

        // Invalidate cache entry
        if (cache.ContainsKey(documentId))
        {
            cache.Remove(documentId);
            Console.WriteLine($"🗑️ Cache invalidated for document: {documentId}");
        }
    }

    public async Task DeleteDocument(string documentId)
    {
        await documentService.DeleteDocument(documentId);

        // Remove from cache
        if (cache.ContainsKey(documentId))
        {
            cache.Remove(documentId);
            Console.WriteLine($"🗑️ Cache entry removed for document: {documentId}");
        }
    }

    public async Task<IEnumerable<string>> ListDocuments()
    {
        // Note: For simplicity, we're not caching the list operation
        return await documentService.ListDocuments();
    }

    public void ClearCache()
    {
        cache.Clear();
        Console.WriteLine("🧹 Cache cleared");
    }

    public int GetCacheSize() => cache.Count;
}