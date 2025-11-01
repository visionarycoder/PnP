namespace Snippets.DesignPatterns.Structural.Proxy;

public class LoggingDocumentProxy(IDocumentService documentService, User currentUser) : IDocumentService
{
    public async Task<string> ReadDocument(string documentId)
    {
        var startTime = DateTime.Now;

        try
        {
            Console.WriteLine($"📖 [{startTime:HH:mm:ss}] User {currentUser.Username} reading document {documentId}");
            var result = await documentService.ReadDocument(documentId);
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"✅ [{DateTime.Now:HH:mm:ss}] Read operation completed in {duration.TotalMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"❌ [{DateTime.Now:HH:mm:ss}] Read operation failed after {duration.TotalMilliseconds}ms: {ex.Message}");
            throw;
        }
    }

    public async Task WriteDocument(string documentId, string content)
    {
        var startTime = DateTime.Now;

        try
        {
            Console.WriteLine($"✏️ [{startTime:HH:mm:ss}] User {currentUser.Username} writing document {documentId}");
            await documentService.WriteDocument(documentId, content);
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"✅ [{DateTime.Now:HH:mm:ss}] Write operation completed in {duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"❌ [{DateTime.Now:HH:mm:ss}] Write operation failed after {duration.TotalMilliseconds}ms: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteDocument(string documentId)
    {
        var startTime = DateTime.Now;

        try
        {
            Console.WriteLine(
                $"🗑️ [{startTime:HH:mm:ss}] User {currentUser.Username} deleting document {documentId}");
            await documentService.DeleteDocument(documentId);
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"✅ [{DateTime.Now:HH:mm:ss}] Delete operation completed in {duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"❌ [{DateTime.Now:HH:mm:ss}] Delete operation failed after {duration.TotalMilliseconds}ms: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListDocuments()
    {
        var startTime = DateTime.Now;

        try
        {
            Console.WriteLine($"📋 [{startTime:HH:mm:ss}] User {currentUser.Username} listing documents");
            var result = await documentService.ListDocuments();
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"✅ [{DateTime.Now:HH:mm:ss}] List operation completed in {duration.TotalMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            Console.WriteLine(
                $"❌ [{DateTime.Now:HH:mm:ss}] List operation failed after {duration.TotalMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
}