using Microsoft.Extensions.Logging;

namespace CSharp.ActorModel;

// Actor context implementation
public class ActorContext : IActorContext
{
    private readonly IActorSystem system;
    private readonly ILogger logger;
    private IActorRef? sender;

    public ActorContext(string actorId, IActorRef self, IActorSystem system, ILogger logger)
    {
        ActorId = actorId;
        Self = self;
        this.system = system;
        this.logger = logger;
    }

    public string ActorId { get; }
    public IActorRef Self { get; }
    public IActorRef? Sender => sender;
    public IActorSystem System => system;
    public ILogger Logger => logger;

    internal void SetSender(IActorRef? senderRef)
    {
        sender = senderRef;
    }

    public Task<IActorRef> ActorOf<T>(string? name = null) where T : ActorBase, new()
    {
        return system.ActorOf<T>(name);
    }

    public Task Tell(IActorRef target, IMessage message)
    {
        return target.Tell(message, Self);
    }

    public Task<TResponse> Ask<TResponse>(IActorRef target, IMessage message, TimeSpan timeout)
    {
        return target.Ask<TResponse>(message, timeout);
    }

    public Task Stop(IActorRef actor)
    {
        return system.Stop(actor);
    }

    public Task Watch(IActorRef actor)
    {
        // Implementation for death watch
        return Task.CompletedTask;
    }

    public Task Unwatch(IActorRef actor)
    {
        // Implementation for death watch
        return Task.CompletedTask;
    }
}