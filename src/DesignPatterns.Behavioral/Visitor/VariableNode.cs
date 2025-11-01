namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing variable references
/// </summary>
public class VariableNode : AstNode
{
    public override string NodeType => "Variable";
    public string Name { get; set; } = "";

    public VariableNode(string name)
    {
        Name = name;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"Variable({Name})";
}