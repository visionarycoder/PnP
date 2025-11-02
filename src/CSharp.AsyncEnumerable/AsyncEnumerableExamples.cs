using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CSharp.AsyncEnumerable;

// Supporting types
public record SensorReading
{
    public DateTime Timestamp { get; init; }
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public double Pressure { get; init; }
}

public class PagedResponse<T>
{
    public List<T>? Items { get; set; }
    public bool HasNextPage { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

// Basic async enumerable implementation
public static class AsyncEnumerableExamples
{
    // 1. Simple async enumerable with yield
    public static async IAsyncEnumerable<int> GenerateNumbersAsync(
        int count, 
        int delayMs = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(delayMs, cancellationToken);
            yield return i;
        }
    }

    // 2. Reading file lines asynchronously
    public static async IAsyncEnumerable<string> ReadLinesAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(filePath);
        
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                yield return line;
            }
        }
    }

    // 3. HTTP API pagination with async enumerable
    public static async IAsyncEnumerable<T> FetchPagedDataAsync<T>(
        HttpClient httpClient,
        string baseUrl,
        int pageSize = 20,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int currentPage = 1;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = $"{baseUrl}?page={currentPage}&size={pageSize}";
            var response = await httpClient.GetStringAsync(url, cancellationToken);
            var pageData = JsonSerializer.Deserialize<PagedResponse<T>>(response);

            if (pageData?.Items != null)
            {
                foreach (var item in pageData.Items)
                {
                    yield return item;
                }

                hasMoreData = pageData.HasNextPage;
                currentPage++;
            }
            else
            {
                hasMoreData = false;
            }
        }
    }

    // 4. Database records streaming
    public static async IAsyncEnumerable<TResult> StreamQueryResultsAsync<TResult>(
        Func<int, int, Task<IEnumerable<TResult>>> queryFunction,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int offset = 0;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await queryFunction(offset, batchSize);
            var items = batch.ToList();

            foreach (var item in items)
            {
                yield return item;
            }

            hasMoreData = items.Count == batchSize;
            offset += batchSize;
        }
    }

    // 5. Real-time data stream simulation
    public static async IAsyncEnumerable<SensorReading> SimulateSensorDataAsync(
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);

            yield return new SensorReading
            {
                Timestamp = DateTime.UtcNow,
                Temperature = 20 + random.NextDouble() * 10, // 20-30°C
                Humidity = 40 + random.NextDouble() * 20,    // 40-60%
                Pressure = 1000 + random.NextDouble() * 50   // 1000-1050 hPa
            };
        }
    }
}