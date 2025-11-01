namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document image element
/// </summary>
public class Image : DocumentElement
{
    public string Source { get; set; } = "";
    public string AltText { get; set; } = "";
    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }

    public override string ToString() => $"Image(Source='{Source}', {Width}x{Height})";
}