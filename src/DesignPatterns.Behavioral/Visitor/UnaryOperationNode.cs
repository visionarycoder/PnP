namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing unary operations
/// </summary>
public class UnaryOperationNode : AstNode
{
    public override string NodeType => "UnaryOperation";
    public string Operator { get; set; } = "";
    public IAstNode Operand { get; set; } = null!;

    public UnaryOperationNode(string op, IAstNode operand)
    {
        Operator = op;
        Operand = operand;
        AddChild(operand);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"UnaryOp({Operator})";
}