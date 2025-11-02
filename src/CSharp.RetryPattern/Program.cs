using CSharp.RetryPattern;
using System.Net.Http;

namespace CSharp.RetryPattern;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Retry Pattern Demo ===\n");

        // Example 1: Simulate a flaky operation that eventually succeeds
        Console.WriteLine("--- Example 1: Flaky Operation ---");
        try
        {
            var result = await RetryHelper.RetryAsync(async () => await SimulateFlaky(), maxRetries: 3, delayMilliseconds: 500);
            Console.WriteLine($"Success! Result: {result}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed after all retries: {ex.Message}\n");
        }

        // Example 2: Operation that always fails
        Console.WriteLine("--- Example 2: Always Failing Operation ---");
        try
        {
            await RetryHelper.RetryAsync(async () => await SimulateAlwaysFails(), maxRetries: 2, delayMilliseconds: 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expected failure: {ex.Message}\n");
        }

        // Example 3: Void operation with retry
        Console.WriteLine("--- Example 3: Void Operation ---");
        try
        {
            await RetryHelper.RetryAsync(async () => await SimulateVoidOperation(), maxRetries: 2, delayMilliseconds: 300);
            Console.WriteLine("Void operation completed successfully!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Void operation failed: {ex.Message}\n");
        }

        // Example 4: HTTP request simulation
        Console.WriteLine("--- Example 4: HTTP Request Simulation ---");
        try
        {
            var httpResult = await RetryHelper.RetryAsync(async () => await SimulateHttpRequest(), maxRetries: 3, delayMilliseconds: 1000);
            Console.WriteLine($"HTTP Result: {httpResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTTP request failed: {ex.Message}");
        }
    }

    private static int attemptCount = 0;
    
    static async Task<string> SimulateFlaky()
    {
        await Task.Delay(100); // Simulate some work
        attemptCount++;
        
        if (attemptCount < 3)
        {
            throw new InvalidOperationException($"Simulated failure on attempt {attemptCount}");
        }
        
        return "Operation succeeded!";
    }

    static async Task<string> SimulateAlwaysFails()
    {
        await Task.Delay(50);
        throw new InvalidOperationException("This operation always fails");
    }

    private static int voidAttempts = 0;
    
    static async Task SimulateVoidOperation()
    {
        await Task.Delay(100);
        voidAttempts++;
        
        if (voidAttempts < 2)
        {
            throw new InvalidOperationException($"Void operation failed on attempt {voidAttempts}");
        }
        
        Console.WriteLine("Void operation internal work completed");
    }

    static async Task<string> SimulateHttpRequest()
    {
        await Task.Delay(200); // Simulate network delay
        
        // Simulate various HTTP failures
        Random random = new Random();
        int outcome = random.Next(1, 4);
        
        return outcome switch
        {
            1 => throw new HttpRequestException("Network timeout"),
            2 => throw new HttpRequestException("Server unavailable"),
            3 => "{ \"data\": \"success\", \"timestamp\": \"" + DateTime.Now + "\" }",
            _ => "Default response"
        };
    }
}