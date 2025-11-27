namespace CSharp.ActorModel;

public interface IMessage
{
    string MessageId { get; }
    DateTime Timestamp { get; }
    string SenderId { get; }
}