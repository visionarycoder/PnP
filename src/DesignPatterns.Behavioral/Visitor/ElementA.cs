namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Concrete element A with string and integer data
/// </summary>
public class ElementA : IVisitable
{
    public string DataA { get; set; } = "";
    public int ValueA { get; set; }

    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"ElementA(DataA='{DataA}', ValueA={ValueA})";
}