namespace CSharp.ActorModel;

// Base message interface

// Base actor message
public abstract record ActorMessage(string MessageId, DateTime Timestamp, string SenderId) : IMessage
{
    protected ActorMessage() : this(Guid.NewGuid().ToString(), DateTime.UtcNow, string.Empty) { }
}

// System messages for actor lifecycle management