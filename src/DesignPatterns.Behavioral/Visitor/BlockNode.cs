namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing code blocks
/// </summary>
public class BlockNode : AstNode
{
    public override string NodeType => "Block";
    public List<IAstNode> Statements { get; set; } = [];

    public BlockNode(params IAstNode[] statements)
    {
        Statements.AddRange(statements);
        foreach (var stmt in statements)
        {
            AddChild(stmt);
        }
    }

    public void AddStatement(IAstNode statement)
    {
        Statements.Add(statement);
        AddChild(statement);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"Block({Statements.Count} statements)";
}