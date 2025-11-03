# Proxy Pattern

**Description**: Provides a placeholder or surrogate for another object to control access to it. The proxy acts as an intermediary, adding functionality like lazy loading, access control, caching, or logging without changing the original object's interface.

**Language/Technology**: C#

**Code**:

## 1. Virtual Proxy (Lazy Loading)

```csharp
// Subject interface
public interface IImage
{
    void Display();
    void Resize(int width, int height);
    string GetMetadata();
}

// Real subject - expensive to create
public class HighResolutionImage : IImage
{
    private readonly string filename;
    private byte[] imageData;
    private int width, height;
    
    public HighResolutionImage(string filename)
    {
        filename = filename;
        LoadImage(); // Expensive operation
    }
    
    private void LoadImage()
    {
        Console.WriteLine($"Loading high-resolution image: {_filename}");
        // Simulate expensive loading operation
        Thread.Sleep(2000);
        imageData = new byte[10_000_000]; // 10MB image
        width = 4096;
        height = 2160;
        Console.WriteLine("Image loaded into memory");
    }
    
    public void Display()
    {
        Console.WriteLine($"Displaying {_filename} ({_width}x{_height})");
    }
    
    public void Resize(int width, int height)
    {
        width = width;
        height = height;
        Console.WriteLine($"Image resized to {_width}x{_height}");
    }
    
    public string GetMetadata()
    {
        return $"File: {_filename}, Size: {_width}x{_height}, Data: {imageData.Length} bytes";
    }
}

// Virtual Proxy - delays creation until needed
public class ImageProxy : IImage
{
    private readonly string filename;
    private HighResolutionImage? realImage;
    private readonly object lock = new();
    
    public ImageProxy(string filename)
    {
        filename = filename;
        Console.WriteLine($"Image proxy created for: {_filename}");
    }
    
    private HighResolutionImage GetRealImage()
    {
        if (realImage == null)
        {
            lock (lock)
            {
                realImage ??= new HighResolutionImage(filename);
            }
        }
        return realImage;
    }
    
    public void Display()
    {
        GetRealImage().Display();
    }
    
    public void Resize(int width, int height)
    {
        GetRealImage().Resize(width, height);
    }
    
    public string GetMetadata()
    {
        return GetRealImage().GetMetadata();
    }
}
```

## 2. Protection Proxy (Access Control)

```csharp
public enum UserRole
{
    Guest,
    User,
    Moderator,
    Admin
}

public class User
{
    public string Username { get; init; } = "";
    public UserRole Role { get; init; }
    public DateTime LastLogin { get; init; }
}

public interface IDocumentService
{
    Task<string> ReadDocument(string documentId);
    Task WriteDocument(string documentId, string content);
    Task DeleteDocument(string documentId);
    Task<IEnumerable<string>> ListDocuments();
}

// Real subject
public class DocumentService : IDocumentService
{
    private readonly Dictionary<string, string> documents = new()
    {
        ["doc1"] = "Public document content",
        ["doc2"] = "Confidential document content",
        ["doc3"] = "Top secret document content"
    };
    
    public Task<string> ReadDocument(string documentId)
    {
        if (documents.TryGetValue(documentId, out var content))
        {
            Console.WriteLine($"Reading document: {documentId}");
            return Task.FromResult(content);
        }
        throw new FileNotFoundException($"Document {documentId} not found");
    }
    
    public Task WriteDocument(string documentId, string content)
    {
        _documents[documentId] = content;
        Console.WriteLine($"Document {documentId} written successfully");
        return Task.CompletedTask;
    }
    
    public Task DeleteDocument(string documentId)
    {
        if (documents.Remove(documentId))
        {
            Console.WriteLine($"Document {documentId} deleted");
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<string>> ListDocuments()
    {
        return Task.FromResult(documents.Keys.AsEnumerable());
    }
}

// Protection Proxy
public class SecureDocumentProxy : IDocumentService
{
    private readonly IDocumentService documentService;
    private readonly User currentUser;
    private readonly Dictionary<string, UserRole> documentPermissions = new()
    {
        ["doc1"] = UserRole.Guest,
        ["doc2"] = UserRole.User,
        ["doc3"] = UserRole.Admin
    };
    
    public SecureDocumentProxy(IDocumentService documentService, User currentUser)
    {
        documentService = documentService;
        currentUser = currentUser;
    }
    
    public async Task<string> ReadDocument(string documentId)
    {
        if (!CanRead(documentId))
        {
            throw new UnauthorizedAccessException($"User {currentUser.Username} cannot read document {documentId}");
        }
        
        Console.WriteLine($"Access granted for {currentUser.Username} to read {documentId}");
        return await documentService.ReadDocument(documentId);
    }
    
    public async Task WriteDocument(string documentId, string content)
    {
        if (!CanWrite(documentId))
        {
            throw new UnauthorizedAccessException($"User {currentUser.Username} cannot write document {documentId}");
        }
        
        Console.WriteLine($"Access granted for {currentUser.Username} to write {documentId}");
        await documentService.WriteDocument(documentId, content);
    }
    
    public async Task DeleteDocument(string documentId)
    {
        if (currentUser.Role < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can delete documents");
        }
        
        Console.WriteLine($"Admin {currentUser.Username} deleting document {documentId}");
        await documentService.DeleteDocument(documentId);
    }
    
    public async Task<IEnumerable<string>> ListDocuments()
    {
        var allDocuments = await documentService.ListDocuments();
        var accessibleDocuments = allDocuments.Where(CanRead);
        
        Console.WriteLine($"Filtered documents for user role: {currentUser.Role}");
        return accessibleDocuments;
    }
    
    private bool CanRead(string documentId)
    {
        if (!documentPermissions.TryGetValue(documentId, out var requiredRole))
            return false;
            
        return currentUser.Role >= requiredRole;
    }
    
    private bool CanWrite(string documentId)
    {
        return currentUser.Role >= UserRole.User && CanRead(documentId);
    }
}
```

## 3. Caching Proxy

```csharp
public interface IDataService
{
    Task<T> GetData<T>(string key) where T : class;
    Task SetData<T>(string key, T data, TimeSpan? expiration = null) where T : class;
    Task<bool> DeleteData(string key);
    Task<IEnumerable<string>> Search(string pattern);
}

// Real subject - expensive remote service
public class RemoteDataService : IDataService
{
    private readonly Dictionary<string, object> remoteData = new()
    {
        ["user:1"] = new { Id = 1, Name = "John Doe", Email = "john@example.com" },
        ["user:2"] = new { Id = 2, Name = "Jane Smith", Email = "jane@example.com" },
        ["config:app"] = new { Theme = "Dark", Language = "en-US", Timeout = 30 }
    };
    
    public async Task<T> GetData<T>(string key) where T : class
    {
        Console.WriteLine($"Making expensive remote call for key: {key}");
        // Simulate network latency
        await Task.Delay(500);
        
        if (remoteData.TryGetValue(key, out var data))
        {
            return (T)data;
        }
        
        throw new KeyNotFoundException($"Key {key} not found");
    }
    
    public async Task SetData<T>(string key, T data, TimeSpan? expiration = null) where T : class
    {
        Console.WriteLine($"Storing data remotely for key: {key}");
        await Task.Delay(200);
        _remoteData[key] = data;
    }
    
    public async Task<bool> DeleteData(string key)
    {
        Console.WriteLine($"Deleting remote data for key: {key}");
        await Task.Delay(100);
        return remoteData.Remove(key);
    }
    
    public async Task<IEnumerable<string>> Search(string pattern)
    {
        Console.WriteLine($"Searching remote data with pattern: {pattern}");
        await Task.Delay(800);
        return remoteData.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

// Caching Proxy
public class CachingDataProxy : IDataService
{
    private readonly IDataService dataService;
    private readonly Dictionary<string, CacheEntry> cache = new();
    private readonly object lock = new();
    
    public CachingDataProxy(IDataService dataService)
    {
        dataService = dataService;
    }
    
    public async Task<T> GetData<T>(string key) where T : class
    {
        lock (lock)
        {
            if (cache.TryGetValue(key, out var cacheEntry) && !cacheEntry.IsExpired)
            {
                Console.WriteLine($"Cache hit for key: {key}");
                return (T)cacheEntry.Data;
            }
        }
        
        Console.WriteLine($"Cache miss for key: {key}");
        var data = await dataService.GetData<T>(key);
        
        lock (lock)
        {
            _cache[key] = new CacheEntry(data, DateTime.UtcNow.AddMinutes(5));
        }
        
        return data;
    }
    
    public async Task SetData<T>(string key, T data, TimeSpan? expiration = null) where T : class
    {
        await dataService.SetData(key, data, expiration);
        
        lock (lock)
        {
            var expirationTime = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(5));
            _cache[key] = new CacheEntry(data, expirationTime);
        }
        
        Console.WriteLine($"Data cached for key: {key}");
    }
    
    public async Task<bool> DeleteData(string key)
    {
        var result = await dataService.DeleteData(key);
        
        lock (lock)
        {
            cache.Remove(key);
        }
        
        Console.WriteLine($"Data removed from cache for key: {key}");
        return result;
    }
    
    public async Task<IEnumerable<string>> Search(string pattern)
    {
        // For search operations, we might implement more sophisticated caching
        var cacheKey = $"search:{pattern}";
        
        lock (lock)
        {
            if (cache.TryGetValue(cacheKey, out var cacheEntry) && !cacheEntry.IsExpired)
            {
                Console.WriteLine($"Search cache hit for pattern: {pattern}");
                return (IEnumerable<string>)cacheEntry.Data;
            }
        }
        
        var results = await dataService.Search(pattern);
        var resultsList = results.ToList();
        
        lock (lock)
        {
            _cache[cacheKey] = new CacheEntry(resultsList, DateTime.UtcNow.AddMinutes(2));
        }
        
        return resultsList;
    }
    
    public void ClearCache()
    {
        lock (lock)
        {
            cache.Clear();
            Console.WriteLine("Cache cleared");
        }
    }
    
    public CacheStatistics GetCacheStatistics()
    {
        lock (lock)
        {
            var totalEntries = cache.Count;
            var expiredEntries = cache.Values.Count(e => e.IsExpired);
            var activeEntries = totalEntries - expiredEntries;
            
            return new CacheStatistics
            {
                TotalEntries = totalEntries,
                ActiveEntries = activeEntries,
                ExpiredEntries = expiredEntries
            };
        }
    }
}

public class CacheEntry
{
    public object Data { get; }
    public DateTime ExpirationTime { get; }
    
    public CacheEntry(object data, DateTime expirationTime)
    {
        Data = data;
        ExpirationTime = expirationTime;
    }
    
    public bool IsExpired => DateTime.UtcNow > ExpirationTime;
}

public class CacheStatistics
{
    public int TotalEntries { get; init; }
    public int ActiveEntries { get; init; }
    public int ExpiredEntries { get; init; }
}
```

## 4. Proxy Factory and Advanced Usage

```csharp
public enum ProxyType
{
    Virtual,
    Protection,
    Caching,
    Logging
}

public static class ProxyFactory
{
    public static IImage CreateImageProxy(string filename, ProxyType type = ProxyType.Virtual)
    {
        return type switch
        {
            ProxyType.Virtual => new ImageProxy(filename),
            ProxyType.Logging => new LoggingImageProxy(new ImageProxy(filename)),
            _ => new ImageProxy(filename)
        };
    }
    
    public static IDocumentService CreateDocumentProxy(User user, ProxyType type = ProxyType.Protection)
    {
        var baseService = new DocumentService();
        
        return type switch
        {
            ProxyType.Protection => new SecureDocumentProxy(baseService, user),
            ProxyType.Logging => new LoggingDocumentProxy(new SecureDocumentProxy(baseService, user)),
            _ => new SecureDocumentProxy(baseService, user)
        };
    }
    
    public static IDataService CreateDataProxy(ProxyType type = ProxyType.Caching)
    {
        var baseService = new RemoteDataService();
        
        return type switch
        {
            ProxyType.Caching => new CachingDataProxy(baseService),
            ProxyType.Logging => new LoggingDataProxy(new CachingDataProxy(baseService)),
            _ => baseService
        };
    }
}

// Logging Proxy (can wrap other proxies)
public class LoggingImageProxy : IImage
{
    private readonly IImage image;
    
    public LoggingImageProxy(IImage image)
    {
        image = image;
    }
    
    public void Display()
    {
        Console.WriteLine($"[LOG] Display() called at {DateTime.Now}");
        image.Display();
    }
    
    public void Resize(int width, int height)
    {
        Console.WriteLine($"[LOG] Resize({width}, {height}) called at {DateTime.Now}");
        image.Resize(width, height);
    }
    
    public string GetMetadata()
    {
        Console.WriteLine($"[LOG] GetMetadata() called at {DateTime.Now}");
        return image.GetMetadata();
    }
}

public class LoggingDocumentProxy : IDocumentService
{
    private readonly IDocumentService service;
    
    public LoggingDocumentProxy(IDocumentService service)
    {
        service = service;
    }
    
    public async Task<string> ReadDocument(string documentId)
    {
        Console.WriteLine($"[LOG] Reading document: {documentId} at {DateTime.Now}");
        try
        {
            var result = await service.ReadDocument(documentId);
            Console.WriteLine($"[LOG] Successfully read document: {documentId}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOG] Failed to read document {documentId}: {ex.Message}");
            throw;
        }
    }
    
    public async Task WriteDocument(string documentId, string content)
    {
        Console.WriteLine($"[LOG] Writing document: {documentId} at {DateTime.Now}");
        await service.WriteDocument(documentId, content);
        Console.WriteLine($"[LOG] Successfully wrote document: {documentId}");
    }
    
    public async Task DeleteDocument(string documentId)
    {
        Console.WriteLine($"[LOG] Deleting document: {documentId} at {DateTime.Now}");
        await service.DeleteDocument(documentId);
        Console.WriteLine($"[LOG] Successfully deleted document: {documentId}");
    }
    
    public async Task<IEnumerable<string>> ListDocuments()
    {
        Console.WriteLine($"[LOG] Listing documents at {DateTime.Now}");
        var result = await service.ListDocuments();
        Console.WriteLine($"[LOG] Found {result.Count()} documents");
        return result;
    }
}

public class LoggingDataProxy : IDataService
{
    private readonly IDataService service;
    
    public LoggingDataProxy(IDataService service)
    {
        service = service;
    }
    
    public async Task<T> GetData<T>(string key) where T : class
    {
        Console.WriteLine($"[LOG] GetData<{typeof(T).Name}>({key}) at {DateTime.Now}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await service.GetData<T>(key);
            Console.WriteLine($"[LOG] GetData completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOG] GetData failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
    
    public async Task SetData<T>(string key, T data, TimeSpan? expiration = null) where T : class
    {
        Console.WriteLine($"[LOG] SetData<{typeof(T).Name}>({key}) at {DateTime.Now}");
        await service.SetData(key, data, expiration);
        Console.WriteLine($"[LOG] SetData completed successfully");
    }
    
    public async Task<bool> DeleteData(string key)
    {
        Console.WriteLine($"[LOG] DeleteData({key}) at {DateTime.Now}");
        var result = await service.DeleteData(key);
        Console.WriteLine($"[LOG] DeleteData result: {result}");
        return result;
    }
    
    public async Task<IEnumerable<string>> Search(string pattern)
    {
        Console.WriteLine($"[LOG] Search({pattern}) at {DateTime.Now}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await service.Search(pattern);
        var resultList = result.ToList();
        Console.WriteLine($"[LOG] Search completed in {stopwatch.ElapsedMilliseconds}ms, found {resultList.Count} results");
        return resultList;
    }
}
```

**Usage**:

```csharp
// 1. Virtual Proxy Example
var imageProxy = new ImageProxy("large-photo.jpg");
// Image is not loaded yet
Console.WriteLine("Proxy created, image not loaded");

// Now the image is loaded when first accessed
imageProxy.Display();
imageProxy.Resize(1920, 1080);

// 2. Protection Proxy Example
var adminUser = new User { Username = "admin", Role = UserRole.Admin };
var regularUser = new User { Username = "john", Role = UserRole.User };

var adminDocumentService = new SecureDocumentProxy(new DocumentService(), adminUser);
var userDocumentService = new SecureDocumentProxy(new DocumentService(), regularUser);

// Admin can access all documents
var adminDocs = await adminDocumentService.ListDocuments();
Console.WriteLine($"Admin sees: {string.Join(", ", adminDocs)}");

// Regular user sees filtered list
var userDocs = await userDocumentService.ListDocuments();
Console.WriteLine($"User sees: {string.Join(", ", userDocs)}");

try
{
    await userDocumentService.ReadDocument("doc3"); // Should throw
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Access denied: {ex.Message}");
}

// 3. Caching Proxy Example
var cachingService = new CachingDataProxy(new RemoteDataService());

// First call - cache miss, goes to remote service
var user1 = await cachingService.GetData<dynamic>("user:1");
Console.WriteLine($"First call: {user1}");

// Second call - cache hit, no remote call
var user1Again = await cachingService.GetData<dynamic>("user:1");
Console.WriteLine($"Second call: {user1Again}");

// Check cache statistics
var stats = cachingService.GetCacheStatistics();
Console.WriteLine($"Cache stats - Total: {stats.TotalEntries}, Active: {stats.ActiveEntries}");

// 4. Composite Proxy Example (Virtual + Protection + Caching + Logging)
var compositeService = new LoggingDataProxy(
    new CachingDataProxy(
        new RemoteDataService()
    )
);

await compositeService.SetData("user:3", new { Id = 3, Name = "Bob Wilson" });
var user3 = await compositeService.GetData<dynamic>("user:3");

// 5. Factory Usage
var virtualImageProxy = ProxyFactory.CreateImageProxy("photo.jpg", ProxyType.Virtual);
var secureDocService = ProxyFactory.CreateDocumentProxy(regularUser, ProxyType.Protection);
var cachedDataService = ProxyFactory.CreateDataProxy(ProxyType.Caching);

// Expected output demonstrates:
// - Virtual Proxy: Lazy loading behavior
// - Protection Proxy: Access control based on user roles
// - Caching Proxy: Performance improvement through caching
// - Logging Proxy: Method call tracking and timing
// - Composite Proxies: Layered functionality
```

**Notes**:

- **Virtual Proxy**: Use for expensive object creation (large images, database connections, remote services)
- **Protection Proxy**: Implement access control, authentication, and authorization
- **Caching Proxy**: Improve performance by caching expensive operations
- **Smart Proxy**: Add reference counting, logging, or transaction management
- **Thread Safety**: Important for caching and virtual proxies in multi-threaded environments
- **Performance**: Proxies add indirection but can improve overall performance through optimization
- **Composition**: Proxies can be chained to combine functionality (security + caching + logging)
- **Memory Management**: Virtual proxies help with lazy loading and memory optimization
- **Transparency**: Clients should be unaware they're using a proxy vs. the real object

**Prerequisites**:

- .NET 6.0 or later
- Understanding of interfaces and polymorphism
- Knowledge of async/await for data service examples
- Familiarity with thread synchronization for caching scenarios

**Related Patterns**:

- **Decorator**: Both add functionality, but Proxy controls access while Decorator enhances behavior
- **Adapter**: Changes interface vs. Proxy which maintains the same interface
- **Facade**: Simplifies interface vs. Proxy which controls access to existing interface
