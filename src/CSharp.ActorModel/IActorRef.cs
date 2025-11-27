namespace CSharp.ActorModel;

public interface IActorRef
{
    string ActorId { get; }
    string Path { get; }
    Task Tell(IMessage message, IActorRef? sender = null);
    Task<TResponse> Ask<TResponse>(IMessage message, TimeSpan timeout);
    Task Stop();
    bool IsTerminated { get; }
}