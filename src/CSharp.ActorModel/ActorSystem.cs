using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace CSharp.ActorModel;

// Actor system implementation
public class ActorSystem : IActorSystem, IDisposable
{
    private readonly ConcurrentDictionary<string, IActorRef> actors;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ActorSystem> logger;
    private volatile bool isShuttingDown = false;

    public ActorSystem(string name, IServiceProvider serviceProvider, ILogger<ActorSystem> logger)
    {
        Name = name;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        actors = new();
    }

    public string Name { get; }

    public event EventHandler<ActorSystemEventArgs>? ActorSystemEvent;

    public async Task<IActorRef> ActorOf<T>(string? name = null) where T : ActorBase, new()
    {
        if (isShuttingDown) throw new InvalidOperationException("Actor system is shutting down");

        var actorId = name ?? $"{typeof(T).Name}-{Guid.NewGuid():N}";
        var path = $"/{Name}/user/{actorId}";

        if (actors.ContainsKey(path))
        {
            throw new InvalidOperationException($"Actor with path {path} already exists");
        }

        var actor = new T();
        var mailbox = new BoundedMailbox();
        var actorLogger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<T>();
        
        var actorRef = new ActorRef(actorId, path, actor, mailbox, actorLogger);
        var context = new ActorContext(actorId, actorRef, this, actorLogger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        actor.Initialize(context, mailbox);
        actors[path] = actorRef;

        await actor.Start();

        logger.LogInformation("Created actor {ActorType} with id {ActorId} at path {Path}", 
            typeof(T).Name, actorId, path);

        ActorSystemEvent?.Invoke(this, new ActorSystemEventArgs
        {
            EventType = "ActorCreated",
            ActorId = actorId,
            Message = $"Actor {typeof(T).Name} created"
        });

        return actorRef;
    }

    public IActorRef? GetActor(string path)
    {
        actors.TryGetValue(path, out var actor);
        return actor;
    }

    public async Task Stop(IActorRef actor)
    {
        if (actor != null && actors.TryRemove(actor.Path, out _))
        {
            await actor.Stop();
            
            logger.LogInformation("Stopped actor {ActorId}", actor.ActorId);
            
            ActorSystemEvent?.Invoke(this, new ActorSystemEventArgs
            {
                EventType = "ActorStopped",
                ActorId = actor.ActorId,
                Message = "Actor stopped"
            });
        }
    }

    public async Task Shutdown()
    {
        if (isShuttingDown) return;

        isShuttingDown = true;
        logger.LogInformation("Shutting down actor system {SystemName}", Name);

        var stopTasks = actors.Values.Select(actor => actor.Stop()).ToArray();
        await Task.WhenAll(stopTasks);

        actors.Clear();

        logger.LogInformation("Actor system {SystemName} shutdown complete", Name);
        
        ActorSystemEvent?.Invoke(this, new ActorSystemEventArgs
        {
            EventType = "SystemShutdown",
            Message = "Actor system shutdown complete"
        });
    }

    public void Dispose()
    {
        Shutdown().Wait(TimeSpan.FromSeconds(30));
    }
}