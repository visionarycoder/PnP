namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document header element
/// </summary>
public class Header : DocumentElement
{
    public int Level { get; set; } = 1; // H1, H2, H3, etc.
    public string Anchor { get; set; } = "";

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }

    public override string ToString() => $"Header(Level={Level}, Content='{Content}')";
}