namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Abstract base class for document elements
/// </summary>
public abstract class DocumentElement : IDocumentElement
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Content { get; set; } = "";

    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);

    protected TResult AcceptDocumentVisitor<TResult>(IDocumentVisitor<TResult> visitor)
    {
        return this switch
        {
            Paragraph p => visitor.Visit(p),
            Header h => visitor.Visit(h),
            Image img => visitor.Visit(img),
            Table t => visitor.Visit(t),
            DocumentList l => visitor.Visit(l),
            _ => throw new NotSupportedException($"Element type {GetType().Name} not supported")
        };
    }
}