namespace Snippets.DesignPatterns.Structural.Proxy;

public interface IDocumentService
{
    Task<string> ReadDocument(string documentId);
    Task WriteDocument(string documentId, string content);
    Task DeleteDocument(string documentId);
    Task<IEnumerable<string>> ListDocuments();
}