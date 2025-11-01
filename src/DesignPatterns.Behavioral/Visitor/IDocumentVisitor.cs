namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Specialized document visitor interface
/// </summary>
/// <typeparam name="TResult">Return type of document operations</typeparam>
public interface IDocumentVisitor<TResult> : IVisitor<TResult>
{
    TResult Visit(Paragraph paragraph);
    TResult Visit(Header header);
    TResult Visit(Image image);
    TResult Visit(Table table);
    TResult Visit(DocumentList list);
}