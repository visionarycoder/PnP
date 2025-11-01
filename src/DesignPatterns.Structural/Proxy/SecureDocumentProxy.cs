namespace Snippets.DesignPatterns.Structural.Proxy;

public class SecureDocumentProxy(IDocumentService documentService, User currentUser) : IDocumentService
{
    private readonly Dictionary<string, UserRole> documentPermissions = new()
    {
        ["doc1"] = UserRole.Guest, // Anyone can read
        ["doc2"] = UserRole.User, // Users and above
        ["doc3"] = UserRole.Admin // Admin only
    };

    // Define permission levels for documents
    // Anyone can read
    // Users and above
    // Admin only

    public async Task<string> ReadDocument(string documentId)
    {
        if (!HasReadPermission(documentId))
        {
            throw new UnauthorizedAccessException($"User {currentUser.Username} ({currentUser.Role}) " +
                                                  $"does not have permission to read document {documentId}");
        }

        Console.WriteLine($"✅ Access granted for {currentUser.Username} to read {documentId}");
        return await documentService.ReadDocument(documentId);
    }

    public async Task WriteDocument(string documentId, string content)
    {
        if (currentUser.Role < UserRole.User)
        {
            throw new UnauthorizedAccessException($"User {currentUser.Username} ({currentUser.Role}) " +
                                                  $"does not have write permissions");
        }

        Console.WriteLine($"✅ Write access granted for {currentUser.Username}");
        await documentService.WriteDocument(documentId, content);
    }

    public async Task DeleteDocument(string documentId)
    {
        if (currentUser.Role < UserRole.Admin)
        {
            throw new UnauthorizedAccessException($"User {currentUser.Username} ({currentUser.Role}) " +
                                                  $"does not have delete permissions");
        }

        Console.WriteLine($"✅ Delete access granted for {currentUser.Username}");
        await documentService.DeleteDocument(documentId);
    }

    public async Task<IEnumerable<string>> ListDocuments()
    {
        var allDocuments = await documentService.ListDocuments();

        // Filter documents based on user permissions
        var accessibleDocuments = allDocuments.Where(HasReadPermission).ToList();

        Console.WriteLine(
            $"📋 User {currentUser.Username} can access {accessibleDocuments.Count} out of {allDocuments.Count()} documents");
        return accessibleDocuments;
    }

    private bool HasReadPermission(string documentId)
    {
        if (!documentPermissions.TryGetValue(documentId, out var requiredRole))
        {
            return currentUser.Role >= UserRole.User; // Default permission
        }

        return currentUser.Role >= requiredRole;
    }
}