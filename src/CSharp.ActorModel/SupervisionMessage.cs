namespace CSharp.ActorModel;

public record SupervisionMessage(Exception Exception, string FailedActorId) : ActorMessage;