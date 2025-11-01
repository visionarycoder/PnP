namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing variable assignments
/// </summary>
public class AssignmentNode : AstNode
{
    public override string NodeType => "Assignment";
    public string VariableName { get; set; } = "";
    public IAstNode Value { get; set; } = null!;

    public AssignmentNode(string variable, IAstNode value)
    {
        VariableName = variable;
        Value = value;
        AddChild(value);
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"Assignment({VariableName} = ...)";
}