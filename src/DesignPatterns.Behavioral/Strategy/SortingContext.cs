using System.Diagnostics;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Sorting context that uses different sorting strategies
/// </summary>
public class SortingContext<T> where T : IComparable<T>
{
    public SortingResult<T> Sort(T[] array, ISortingStrategy<T> strategy)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));

        var originalArray = (T[])array.Clone();
        var sortedArray = (T[])array.Clone();

        var stopwatch = Stopwatch.StartNew();
        strategy.Sort(sortedArray);
        stopwatch.Stop();

        return new SortingResult<T>(
            strategy.Name,
            originalArray,
            sortedArray,
            stopwatch.ElapsedMilliseconds,
            strategy.TimeComplexity,
            strategy.SpaceComplexity,
            strategy.IsStable);
    }

    public void CompareSortingStrategies(T[] array, params ISortingStrategy<T>[] strategies)
    {
        Console.WriteLine($"\n🔄 Comparing Sorting Algorithms (Array size: {array.Length})");
        Console.WriteLine(new string('=', 90));
        Console.WriteLine(
            $"{"Algorithm",-15} {"Time (ms)",-12} {"Stable",-8} {"Time Complexity",-20} {"Space Complexity",-15}");
        Console.WriteLine(new string('-', 90));

        var results = new List<SortingResult<T>>();

        foreach (var strategy in strategies)
        {
            var result = Sort(array, strategy);
            results.Add(result);

            Console.WriteLine($"{result.AlgorithmName,-15} {result.ElapsedMilliseconds,-12} " +
                              $"{(result.IsStable ? "Yes" : "No"),-8} " +
                              $"{result.TimeComplexity.Average,-20} " +
                              $"{result.SpaceComplexity.Average,-15}");
        }

        // Show fastest algorithm
        var fastest = results.OrderBy(r => r.ElapsedMilliseconds).First();
        Console.WriteLine($"\n🏆 Fastest: {fastest.AlgorithmName} ({fastest.ElapsedMilliseconds}ms)");
    }
}