namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document paragraph element
/// </summary>
public class Paragraph : DocumentElement
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public bool IsJustified { get; set; } = false;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }

    public override string ToString() =>
        $"Paragraph({Content[..Math.Min(30, Content.Length)]}{(Content.Length > 30 ? "..." : "")})";
}