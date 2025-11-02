using Microsoft.Extensions.Logging;

namespace CSharp.ActorModel;

// Actor context for message handling
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

// Actor reference interface
public interface IActorRef
{
    string ActorId { get; }
    string Path { get; }
    Task Tell(IMessage message, IActorRef? sender = null);
    Task<TResponse> Ask<TResponse>(IMessage message, TimeSpan timeout);
    Task Stop();
    bool IsTerminated { get; }
}

// Actor system interface
public interface IActorSystem : IDisposable
{
    string Name { get; }
    Task<IActorRef> ActorOf<T>(string? name = null) where T : ActorBase, new();
    IActorRef? GetActor(string path);
    Task Stop(IActorRef actor);
    Task Shutdown();
    event EventHandler<ActorSystemEventArgs>? ActorSystemEvent;
}

// Actor system events
public class ActorSystemEventArgs : EventArgs
{
    public string EventType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}