namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Interface for visitors that can combine results from multiple elements
/// </summary>
/// <typeparam name="TResult">Result type to combine</typeparam>
public interface ICombiningVisitor<TResult>
{
    TResult CombineResults(List<TResult> results);
}