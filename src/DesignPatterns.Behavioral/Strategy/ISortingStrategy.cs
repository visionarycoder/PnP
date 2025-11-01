namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Sorting strategy interface for different sorting algorithms
/// </summary>
public interface ISortingStrategy<T> where T : IComparable<T>
{
    string Name { get; }
    SortingComplexity TimeComplexity { get; }
    SortingComplexity SpaceComplexity { get; }
    bool IsStable { get; }
    void Sort(T[] array);
}