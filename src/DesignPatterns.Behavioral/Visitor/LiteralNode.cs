namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing literal values
/// </summary>
public class LiteralNode : AstNode
{
    public override string NodeType => "Literal";
    public object Value { get; set; } = null!;
    public Type ValueType { get; set; } = typeof(object);

    public LiteralNode(object value)
    {
        Value = value;
        ValueType = value?.GetType() ?? typeof(object);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"Literal({Value})";
}