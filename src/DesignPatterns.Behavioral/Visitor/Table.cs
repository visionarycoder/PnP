namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document table element
/// </summary>
public class Table : DocumentElement
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public List<List<string>> Data { get; set; } = [];
    public List<string> Headers { get; set; } = [];

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }

    public override string ToString() => $"Table({Rows}x{Columns} with {Data.Count} data rows)";
}