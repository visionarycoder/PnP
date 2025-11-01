namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document list element
/// </summary>
public class DocumentList : DocumentElement
{
    public ListType Type { get; set; } = ListType.Unordered;
    public List<string> Items { get; set; } = [];
    public int IndentLevel { get; set; } = 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }

    public override string ToString() => $"List({Type}, {Items.Count} items, indent={IndentLevel})";
}