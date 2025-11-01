namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitable interface for elements that can accept visitors
/// </summary>
public interface IVisitable
{
    TResult Accept<TResult>(IVisitor<TResult> visitor);
}