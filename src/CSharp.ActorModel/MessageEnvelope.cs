namespace CSharp.ActorModel;

// Mailbox implementation

public record MessageEnvelope(IMessage Message, IActorRef? Sender, DateTime ReceivedAt);