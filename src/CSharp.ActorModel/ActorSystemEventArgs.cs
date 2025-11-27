namespace CSharp.ActorModel;

// Actor context for message handling

// Actor reference interface

// Actor system interface

// Actor system events
public class ActorSystemEventArgs : EventArgs
{
    public string EventType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}