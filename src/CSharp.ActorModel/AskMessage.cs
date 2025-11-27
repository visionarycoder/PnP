namespace CSharp.ActorModel;

public record AskMessage<TResponse>(IMessage OriginalMessage, TaskCompletionSource<TResponse> ResponsePromise) : ActorMessage;