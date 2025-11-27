using System.Collections.Concurrent;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Collects and manages diagnostic information for error analysis.
/// </summary>
public class DiagnosticCollector
{
    private readonly ConcurrentDictionary<string, object> diagnosticData = new();
    private readonly List<DiagnosticEvent> events = new();
    private readonly object eventsLock = new();

    public string CorrelationId { get; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public void AddData(string key, object value)
    {
        diagnosticData.AddOrUpdate(key, value, (k, v) => value);
    }

    public T? GetData<T>(string key)
    {
        if (diagnosticData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddEvent(string eventType, string message, Exception? exception = null)
    {
        var diagnosticEvent = new DiagnosticEvent(eventType, message, exception, DateTime.UtcNow);
        
        lock (eventsLock)
        {
            events.Add(diagnosticEvent);
        }
    }

    public IReadOnlyList<DiagnosticEvent> GetEvents()
    {
        lock (eventsLock)
        {
            return events.ToList();
        }
    }

    public Dictionary<string, object> GetAllData()
    {
        return new Dictionary<string, object>(diagnosticData);
    }

    public DiagnosticSummary CreateSummary()
    {
        return new DiagnosticSummary(
            CorrelationId,
            CreatedAt,
            GetAllData(),
            GetEvents());
    }
}