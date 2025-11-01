namespace Snippets.DesignPatterns.Structural.Proxy;

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
        Console.WriteLine($"Writing document: {documentId}");
        documents[documentId] = content;
        return Task.CompletedTask;
    }

    public Task DeleteDocument(string documentId)
    {
        Console.WriteLine($"Deleting document: {documentId}");
        documents.Remove(documentId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListDocuments()
    {
        Console.WriteLine("Listing all documents");
        return Task.FromResult(documents.Keys.AsEnumerable());
    }
}