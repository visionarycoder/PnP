namespace CSharp.ActorModel;

public interface IMailbox
{
    Task Post(IMessage message, IActorRef? sender = null);
    Task<MessageEnvelope?> Receive(CancellationToken cancellationToken);
    int Count { get; }
    bool HasMessages { get; }
}