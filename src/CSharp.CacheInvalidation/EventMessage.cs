namespace CSharp.CacheInvalidation;

public class EventMessage
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}