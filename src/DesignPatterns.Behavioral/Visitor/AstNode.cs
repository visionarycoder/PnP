namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Abstract base class for AST nodes
/// </summary>
public abstract class AstNode : IAstNode
{
    public abstract string NodeType { get; }
    public List<IAstNode> Children { get; } = [];
    public Dictionary<string, object> Attributes { get; } = new();

    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);

    protected void AddChild(IAstNode child)
    {
        Children.Add(child);
    }
}