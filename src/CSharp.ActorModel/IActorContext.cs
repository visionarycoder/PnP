namespace CSharp.ActorModel;

public interface IActorContext
{
    string ActorId { get; }
    IActorRef Self { get; }
    IActorRef? Sender { get; }
    IActorSystem System { get; }
    ILogger Logger { get; }
    Task<IActorRef> ActorOf<T>(string? name = null) where T : ActorBase, new();
    Task Tell(IActorRef target, IMessage message);
    Task<TResponse> Ask<TResponse>(IActorRef target, IMessage message, TimeSpan timeout);
    Task Stop(IActorRef actor);
    Task Watch(IActorRef actor);
    Task Unwatch(IActorRef actor);
}