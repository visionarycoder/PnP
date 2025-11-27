using System.Net.Http;

namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides examples of common cancellation patterns for async operations
/// </summary>
public static class CancellationExamples
{
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Fetches data from a URL with timeout and cancellation support
    /// </summary>
    public static async Task<string> FetchDataWithTimeoutAsync(
        string url,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            return await HttpClient.GetStringAsync(url, combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Request to {url} timed out after {timeoutSeconds} seconds");
        }
    }

    /// <summary>
    /// Downloads a file with progress reporting and cancellation support
    /// </summary>
    public static async Task<byte[]> DownloadFileWithProgressAsync(
        string url,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var buffer = new byte[8192];
        var totalDownloaded = 0L;
        var content = new List<byte>();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            content.AddRange(buffer.Take(bytesRead));
            totalDownloaded += bytesRead;

            progress?.Report(new DownloadProgress(totalDownloaded, totalBytes));

            // Check for cancellation periodically
            cancellationToken.ThrowIfCancellationRequested();
        }

        return content.ToArray();
    }

    /// <summary>
    /// Calculates prime numbers up to maxNumber with cooperative cancellation
    /// </summary>
    public static async Task<int> CalculatePrimesAsync(
        int maxNumber,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var primeCount = 0;
            var primes = new bool[maxNumber + 1];
            Array.Fill(primes, true);
            primes[0] = primes[1] = false;

            for (int i = 2; i <= maxNumber; i++)
            {
                // Check for cancellation every 1000 iterations
                if (i % 1000 == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                if (primes[i])
                {
                    primeCount++;
                    
                    // Mark multiples as not prime
                    for (long j = (long)i * i; j <= maxNumber; j += i)
                    {
                        primes[j] = false;
                    }
                }
            }

            return primeCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Processes multiple items in parallel with cancellation
    /// </summary>
    public static async Task<TResult[]> ProcessItemsInParallelAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> processor,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await processor(item, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Simulates work with periodic progress reporting and cancellation checks
    /// </summary>
    public static async Task<string> DoWorkWithProgressAsync(
        int totalSteps,
        TimeSpan stepDelay,
        IProgress<WorkProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < totalSteps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(stepDelay, cancellationToken);
            
            progress?.Report(new WorkProgress(i + 1, totalSteps, $"Completed step {i + 1}"));
        }

        return "Work completed successfully";
    }
}