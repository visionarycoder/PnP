namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document element interface extending basic visitable
/// </summary>
public interface IDocumentElement : IVisitable
{
    string Id { get; }
    string Content { get; set; }
}