namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Generic visitor interface defining operations on different element types
/// </summary>
/// <typeparam name="TResult">Return type of visitor operations</typeparam>
public interface IVisitor<TResult>
{
    TResult Visit(ElementA element);
    TResult Visit(ElementB element);
    TResult Visit(ElementC element);
}