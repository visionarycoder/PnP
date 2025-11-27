using System.Runtime.CompilerServices;

namespace CSharp.ProducerConsumer;

public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield(); // Allow other work to proceed
        }
    }
}