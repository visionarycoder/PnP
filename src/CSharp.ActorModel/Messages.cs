namespace CSharp.ActorModel;

// Base message interface
public interface IMessage
{
    string MessageId { get; }
    DateTime Timestamp { get; }
    string SenderId { get; }
}

// Base actor message
public abstract record ActorMessage(string MessageId, DateTime Timestamp, string SenderId) : IMessage
{
    protected ActorMessage() : this(Guid.NewGuid().ToString(), DateTime.UtcNow, string.Empty) { }
}

// System messages for actor lifecycle management
public record StartMessage() : ActorMessage;
public record StopMessage() : ActorMessage;
public record RestartMessage() : ActorMessage;
public record PoisonPillMessage() : ActorMessage;
public record SupervisionMessage(Exception Exception, string FailedActorId) : ActorMessage;