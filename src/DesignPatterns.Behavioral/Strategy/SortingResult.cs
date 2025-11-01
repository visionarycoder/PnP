namespace Snippets.DesignPatterns.Behavioral.Strategy;

public record SortingResult<T>(
    string AlgorithmName,
    T[] OriginalArray,
    T[] SortedArray,
    long ElapsedMilliseconds,
    SortingComplexity TimeComplexity,
    SortingComplexity SpaceComplexity,
    bool IsStable) where T : IComparable<T>;