namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Concrete element C with collection and date data
/// </summary>
public class ElementC : IVisitable
{
    public List<string> DataC { get; set; } = [];
    public DateTime TimeC { get; set; }

    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"ElementC(DataC=[{string.Join(", ", DataC)}], TimeC={TimeC:yyyy-MM-dd})";
}