# Retry Pattern

**Description**: Implements a retry pattern with exponential backoff for operations that may fail temporarily.

**Language/Technology**: C# / .NET 8.0

**Code**:

```csharp
using System;
using System.Threading.Tasks;

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
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                // Calculate exponential backoff delay
                var delay = delayMilliseconds * (int)Math.Pow(2, attempt);
                Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                Console.WriteLine($"Retrying in {delay}ms...");
                await Task.Delay(delay).ConfigureAwait(false);
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
            await operation().ConfigureAwait(false);
            return true;
        }, maxRetries, delayMilliseconds).ConfigureAwait(false);
    }
}
```

**Usage**:

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Example 1: Retry an HTTP request
        var result = await RetryHelper.RetryAsync(async () => await FetchDataAsync(), maxRetries: 3, delayMilliseconds: 1000);
        Console.WriteLine($"Result: {result}");
        
        // Example 2: Retry a void operation
        await RetryHelper.RetryAsync(async () => await SaveDataAsync(), maxRetries: 5, delayMilliseconds: 500);
    }
    
    static async Task<string> FetchDataAsync()
    {
        using var client = new HttpClient();
        return await client.GetStringAsync("https://api.example.com/data");
    }
    
    static async Task SaveDataAsync()
    {
        // Simulate a database save
        await Task.Delay(100);
        // May throw exception on failure
    }
}
```

## Notes

- Targets .NET 8.0 SDK with modern C# features
- Uses exponential backoff (delay doubles with each retry)
- Works with async/await pattern and includes `ConfigureAwait(false)`
- Uses `var` for obvious types (attempt, delay variables)
- Generic implementation works with any return type
- Includes overload for void operations
- Delay formula: initialDelay × 2^attemptNumber
- Consider adding specific exception type filtering in production
- For production use, consider using Polly library for more advanced scenarios
- Related: [Circuit Breaker Pattern](circuit-breaker.md)
