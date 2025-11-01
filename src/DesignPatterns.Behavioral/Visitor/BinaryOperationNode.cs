namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing binary operations
/// </summary>
public class BinaryOperationNode : AstNode
{
    public override string NodeType => "BinaryOperation";
    public string Operator { get; set; } = "";
    public IAstNode Left { get; set; } = null!;
    public IAstNode Right { get; set; } = null!;

    public BinaryOperationNode(string op, IAstNode left, IAstNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
        AddChild(left);
        AddChild(right);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"BinaryOp({Operator})";
}