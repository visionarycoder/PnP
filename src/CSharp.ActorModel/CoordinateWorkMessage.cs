namespace CSharp.ActorModel;

public record CoordinateWorkMessage(params string[] Tasks) : ActorMessage;