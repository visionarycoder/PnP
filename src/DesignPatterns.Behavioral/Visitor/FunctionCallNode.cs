namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node representing function calls
/// </summary>
public class FunctionCallNode : AstNode
{
    public override string NodeType => "FunctionCall";
    public string FunctionName { get; set; } = "";
    public List<IAstNode> Arguments { get; set; } = [];

    public FunctionCallNode(string name, params IAstNode[] args)
    {
        FunctionName = name;
        Arguments.AddRange(args);
        foreach (var arg in args)
        {
            AddChild(arg);
        }
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }

        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }

    public override string ToString() => $"FunctionCall({FunctionName}, {Arguments.Count} args)";
}