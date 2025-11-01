namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Concrete element B with double and boolean data
/// </summary>
public class ElementB : IVisitable
{
    public double DataB { get; set; }
    public bool FlagB { get; set; }

    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"ElementB(DataB={DataB}, FlagB={FlagB})";
}