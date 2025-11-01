namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST node interface extending visitable
/// </summary>
public interface IAstNode : IVisitable
{
    string NodeType { get; }
    List<IAstNode> Children { get; }
}