namespace CSharp.RetryPattern;

/// <summary>
/// Helper class for implementing retry logic with exponential backoff.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes an async function with retry logic and exponential backoff
    /// </summary>
    /// <typeparam name="T">Return type of the function</typeparam>
    /// <param name="operation">The async operation to retry</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="delayMilliseconds">Initial delay between retries in milliseconds</param>
    /// <returns>Result of the operation</returns>
    public static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                // Calculate exponential backoff delay
                int delay = delayMilliseconds * (int)Math.Pow(2, attempt);
                Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                Console.WriteLine($"Retrying in {delay}ms...");
                await Task.Delay(delay);
            }
            catch (Exception lastEx)
            {
                // If we get here, all retries failed - preserve original exception
                throw new AggregateException($"Operation failed after {maxRetries} retries", lastEx);
            }
        }
        // This should never be reached due to the catch block above
        throw new InvalidOperationException("Unexpected code path");
    }
    
    /// <summary>
    /// Executes an action with retry logic (void return)
    /// </summary>
    public static async Task RetryAsync(Func<Task> operation, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        await RetryAsync(async () =>
        {
            await operation();
            return true;
        }, maxRetries, delayMilliseconds);
    }
}