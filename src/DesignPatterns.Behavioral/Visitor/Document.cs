namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Document container that can accept visitors
/// </summary>
public class Document
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<IDocumentElement> Elements { get; set; } = [];

    public void AddElement(IDocumentElement element)
    {
        Elements.Add(element);
    }

    public TResult Accept<TResult>(IDocumentVisitor<TResult> visitor)
    {
        var results = Elements.Select(element => element.Accept(visitor)).ToList();

        // Combine results if visitor supports it
        if (visitor is ICombiningVisitor<TResult> combiningVisitor)
        {
            return combiningVisitor.CombineResults(results);
        }

        return results.LastOrDefault() ?? default(TResult)!;
    }

    public override string ToString() => $"Document('{Title}', {Elements.Count} elements)";
}