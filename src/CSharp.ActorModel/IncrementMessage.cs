namespace CSharp.ActorModel;

public record IncrementMessage(int Amount = 1) : ActorMessage;