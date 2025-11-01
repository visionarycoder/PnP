# Actor Model

**Description**: Comprehensive actor-based concurrency patterns with message-passing, isolation, fault-tolerance, mailbox implementation, actor lifecycle management, supervision strategies, distributed actor systems, and hierarchical actor supervision for building resilient concurrent applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Base message interface
public interface IMessage
{
    string MessageId { get; }
    DateTime Timestamp { get; }
    string SenderId { get; }
}

// Base actor message
public abstract record ActorMessage(string MessageId, DateTime Timestamp, string SenderId) : IMessage
{
    protected ActorMessage() : this(Guid.NewGuid().ToString(), DateTime.UtcNow, string.Empty) { }
}

// System messages for actor lifecycle management
public record StartMessage() : ActorMessage;
public record StopMessage() : ActorMessage;
public record RestartMessage() : ActorMessage;
public record PoisonPillMessage() : ActorMessage;
public record SupervisionMessage(Exception Exception, string FailedActorId) : ActorMessage;

// Actor context for message handling
public interface IActorContext
{
    string ActorId { get; }
    IActorRef Self { get; }
    IActorRef Sender { get; }
    IActorSystem System { get; }
    ILogger Logger { get; }
    Task<IActorRef> ActorOf&lt;T&gt;(string name = null) where T : ActorBase, new();
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
    Task Tell(IMessage message, IActorRef sender = null);
    Task<TResponse> Ask<TResponse>(IMessage message, TimeSpan timeout);
    Task Stop();
    bool IsTerminated { get; }
}

// Actor system interface
public interface IActorSystem : IDisposable
{
    string Name { get; }
    Task<IActorRef> ActorOf&lt;T&gt;(string name = null) where T : ActorBase, new();
    IActorRef GetActor(string path);
    Task Stop(IActorRef actor);
    Task Shutdown();
    event EventHandler<ActorSystemEventArgs> ActorSystemEvent;
}

// Actor system events
public class ActorSystemEventArgs : EventArgs
{
    public string EventType { get; set; }
    public string ActorId { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
}

// Supervision strategy
public enum SupervisionDirective
{
    Resume,
    Restart,
    Stop,
    Escalate
}

public interface ISupervisionStrategy
{
    SupervisionDirective Decide(Exception exception);
}

public class OneForOneStrategy : ISupervisionStrategy
{
    private readonly Dictionary<Type, SupervisionDirective> exceptionDirectives;
    private readonly SupervisionDirective defaultDirective;

    public OneForOneStrategy(SupervisionDirective defaultDirective = SupervisionDirective.Restart)
    {
        this.defaultDirective = defaultDirective;
        exceptionDirectives = new Dictionary<Type, SupervisionDirective>();
    }

    public OneForOneStrategy Handle<TException>(SupervisionDirective directive) where TException : Exception
    {
        exceptionDirectives[typeof(TException)] = directive;
        return this;
    }

    public SupervisionDirective Decide(Exception exception)
    {
        var exceptionType = exception.GetType();
        
        // Look for exact match first
        if (exceptionDirectives.TryGetValue(exceptionType, out var directive))
        {
            return directive;
        }

        // Look for base type matches
        foreach (var kvp in exceptionDirectives)
        {
            if (kvp.Key.IsAssignableFrom(exceptionType))
            {
                return kvp.Value;
            }
        }

        return defaultDirective;
    }
}

// Mailbox implementation
public interface IMailbox
{
    Task Post(IMessage message, IActorRef sender = null);
    Task<MessageEnvelope> Receive(CancellationToken cancellationToken);
    int Count { get; }
    bool HasMessages { get; }
}

public record MessageEnvelope(IMessage Message, IActorRef Sender, DateTime ReceivedAt);

public class BoundedMailbox : IMailbox, IDisposable
{
    private readonly Channel<MessageEnvelope> channel;
    private readonly ChannelWriter<MessageEnvelope> writer;
    private readonly ChannelReader<MessageEnvelope> reader;
    private volatile bool isDisposed = false;

    public BoundedMailbox(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        channel = Channel.CreateBounded<MessageEnvelope>(options);
        writer = channel.Writer;
        reader = channel.Reader;
    }

    public async Task Post(IMessage message, IActorRef sender = null)
    {
        if (isDisposed) return;

        var envelope = new MessageEnvelope(message, sender, DateTime.UtcNow);
        
        try
        {
            await writer.WriteAsync(envelope);
        }
        catch (ObjectDisposedException)
        {
            // Mailbox has been disposed
        }
    }

    public async Task<MessageEnvelope> Receive(CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public int Count => reader.CanCount ? reader.Count : 0;
    public bool HasMessages => reader.CanCount && reader.Count > 0;

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            writer.TryComplete();
        }
    }
}

// Base actor implementation
public abstract class ActorBase : IDisposable
{
    private IActorContext context;
    private IMailbox mailbox;
    private CancellationTokenSource cancellationTokenSource;
    private Task messageLoop;
    private volatile bool isRunning = false;
    private volatile bool isDisposed = false;

    protected IActorContext Context => context;
    protected ILogger Logger => context?.Logger;

    public virtual ISupervisionStrategy SupervisionStrategy =>
        new OneForOneStrategy()
            .Handle<ArgumentException>(SupervisionDirective.Resume)
            .Handle<InvalidOperationException>(SupervisionDirective.Restart)
            .Handle<TimeoutException>(SupervisionDirective.Resume);

    internal void Initialize(IActorContext actorContext, IMailbox actorMailbox)
    {
        context = actorContext;
        mailbox = actorMailbox;
        cancellationTokenSource = new CancellationTokenSource();
    }

    internal Task Start()
    {
        if (isRunning || isDisposed) return Task.CompletedTask;

        isRunning = true;
        messageLoop = Task.Run(MessageLoop);
        
        // Send start message to self
        _ = Task.Run(() => OnStart());
        
        return Task.CompletedTask;
    }

    internal async Task Stop()
    {
        if (!isRunning || isDisposed) return;

        isRunning = false;
        
        try
        {
            await OnStop();
            cancellationTokenSource.Cancel();
            
            if (messageLoop != null)
            {
                await messageLoop;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error stopping actor {ActorId}", Context.ActorId);
        }
    }

    private async Task MessageLoop()
    {
        try
        {
            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var envelope = await mailbox.Receive(cancellationTokenSource.Token);
                    if (envelope == null) break;

                    await ProcessMessage(envelope);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error in message loop for actor {ActorId}", Context.ActorId);
                    
                    // Apply supervision strategy
                    var directive = SupervisionStrategy.Decide(ex);
                    await HandleSupervisionDirective(directive, ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Fatal error in message loop for actor {ActorId}", Context.ActorId);
        }
    }

    private async Task ProcessMessage(MessageEnvelope envelope)
    {
        try
        {
            // Set sender in context
            ((ActorContext)context).SetSender(envelope.Sender);

            // Handle system messages
            if (envelope.Message is SystemMessage systemMessage)
            {
                await HandleSystemMessage(systemMessage);
                return;
            }

            // Handle user messages
            await OnReceive(envelope.Message);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error processing message {MessageType} in actor {ActorId}", 
                envelope.Message.GetType().Name, Context.ActorId);
            
            var directive = SupervisionStrategy.Decide(ex);
            await HandleSupervisionDirective(directive, ex);
        }
    }

    private async Task HandleSystemMessage(SystemMessage message)
    {
        switch (message)
        {
            case StartMessage:
                await OnStart();
                break;
            case StopMessage:
                await OnStop();
                isRunning = false;
                break;
            case RestartMessage:
                await OnRestart();
                break;
            case PoisonPillMessage:
                await OnStop();
                isRunning = false;
                break;
        }
    }

    private async Task HandleSupervisionDirective(SupervisionDirective directive, Exception exception)
    {
        switch (directive)
        {
            case SupervisionDirective.Resume:
                Logger?.LogWarning("Resuming actor {ActorId} after exception: {Exception}", 
                    Context.ActorId, exception.Message);
                break;
                
            case SupervisionDirective.Restart:
                Logger?.LogWarning("Restarting actor {ActorId} after exception: {Exception}", 
                    Context.ActorId, exception.Message);
                await OnRestart();
                break;
                
            case SupervisionDirective.Stop:
                Logger?.LogError("Stopping actor {ActorId} after exception: {Exception}", 
                    Context.ActorId, exception.Message);
                await Stop();
                break;
                
            case SupervisionDirective.Escalate:
                Logger?.LogError("Escalating exception from actor {ActorId}: {Exception}", 
                    Context.ActorId, exception.Message);
                // Notify parent actor or system
                await Context.System.ActorSystemEvent?.Invoke(Context.System, 
                    new ActorSystemEventArgs 
                    { 
                        EventType = "Exception", 
                        ActorId = Context.ActorId, 
                        Exception = exception 
                    });
                break;
        }
    }

    // Virtual methods for actor lifecycle
    protected virtual Task OnStart() => Task.CompletedTask;
    protected virtual Task OnStop() => Task.CompletedTask;
    protected virtual Task OnRestart()
    {
        // Default restart behavior
        return Task.CompletedTask;
    }

    // Abstract method for handling messages
    protected abstract Task OnReceive(IMessage message);

    public virtual void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            Stop().Wait(TimeSpan.FromSeconds(5));
            cancellationTokenSource?.Dispose();
            mailbox?.Dispose();
        }
    }
}

// System message base class
public abstract record SystemMessage() : ActorMessage;

// Actor context implementation
public class ActorContext : IActorContext
{
    private readonly IActorSystem system;
    private readonly ILogger logger;
    private IActorRef sender;

    public ActorContext(string actorId, IActorRef self, IActorSystem system, ILogger logger)
    {
        ActorId = actorId;
        Self = self;
        this.system = system;
        this.logger = logger;
    }

    public string ActorId { get; }
    public IActorRef Self { get; }
    public IActorRef Sender => sender;
    public IActorSystem System => system;
    public ILogger Logger => logger;

    internal void SetSender(IActorRef senderRef)
    {
        sender = senderRef;
    }

    public Task<IActorRef> ActorOf&lt;T&gt;(string name = null) where T : ActorBase, new()
    {
        return system.ActorOf&lt;T&gt;(name);
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

// Actor reference implementation
public class ActorRef : IActorRef
{
    private readonly ActorBase actor;
    private readonly IMailbox mailbox;
    private readonly ILogger logger;
    private volatile bool isTerminated = false;

    public ActorRef(string actorId, string path, ActorBase actor, IMailbox mailbox, ILogger logger)
    {
        ActorId = actorId;
        Path = path;
        this.actor = actor;
        this.mailbox = mailbox;
        this.logger = logger;
    }

    public string ActorId { get; }
    public string Path { get; }
    public bool IsTerminated => isTerminated;

    public async Task Tell(IMessage message, IActorRef sender = null)
    {
        if (isTerminated)
        {
            logger?.LogWarning("Attempted to send message to terminated actor {ActorId}", ActorId);
            return;
        }

        await mailbox.Post(message, sender);
    }

    public async Task<TResponse> Ask<TResponse>(IMessage message, TimeSpan timeout)
    {
        if (isTerminated)
        {
            throw new InvalidOperationException($"Actor {ActorId} is terminated");
        }

        var responsePromise = new TaskCompletionSource<TResponse>();
        var responseMessage = new AskMessage<TResponse>(message, responsePromise);

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => responsePromise.TrySetCanceled());

        await mailbox.Post(responseMessage);
        return await responsePromise.Task;
    }

    public async Task Stop()
    {
        if (!isTerminated)
        {
            isTerminated = true;
            await mailbox.Post(new StopMessage());
            await actor.Stop();
        }
    }
}

// Ask message for request-response pattern
public record AskMessage<TResponse>(IMessage OriginalMessage, TaskCompletionSource<TResponse> ResponsePromise) : ActorMessage;

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
        actors = new ConcurrentDictionary<string, IActorRef>();
    }

    public string Name { get; }

    public event EventHandler<ActorSystemEventArgs> ActorSystemEvent;

    public async Task<IActorRef> ActorOf&lt;T&gt;(string name = null) where T : ActorBase, new()
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
        var actorLogger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger&lt;T&gt;();
        
        var actorRef = new ActorRef(actorId, path, actor, mailbox, actorLogger);
        var context = new ActorContext(actorId, actorRef, this, actorLogger);

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

    public IActorRef GetActor(string path)
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

// Example actor implementations

// Counter actor
public record IncrementMessage(int Amount = 1) : ActorMessage;
public record GetCountMessage() : ActorMessage;
public record CountResponseMessage(int Count) : ActorMessage;

public class CounterActor : ActorBase
{
    private int count = 0;

    protected override async Task OnReceive(IMessage message)
    {
        switch (message)
        {
            case IncrementMessage increment:
                count += increment.Amount;
                Logger?.LogDebug("Counter incremented by {Amount}, new count: {Count}", 
                    increment.Amount, count);
                break;

            case GetCountMessage:
                var response = new CountResponseMessage(count);
                if (Context.Sender != null)
                {
                    await Context.Sender.Tell(response, Context.Self);
                }
                break;

            case AskMessage<int> askCount when askCount.OriginalMessage is GetCountMessage:
                askCount.ResponsePromise.SetResult(count);
                break;

            default:
                Logger?.LogWarning("Unknown message type: {MessageType}", message.GetType().Name);
                break;
        }
    }

    protected override Task OnStart()
    {
        Logger?.LogInformation("Counter actor {ActorId} started", Context.ActorId);
        return base.OnStart();
    }

    protected override Task OnStop()
    {
        Logger?.LogInformation("Counter actor {ActorId} stopped with final count: {Count}", 
            Context.ActorId, count);
        return base.OnStop();
    }
}

// Worker actor for processing tasks
public record WorkMessage(string WorkId, string Data, TimeSpan ProcessingTime) : ActorMessage;
public record WorkCompleteMessage(string WorkId, string Result) : ActorMessage;
public record WorkFailedMessage(string WorkId, Exception Exception) : ActorMessage;

public class WorkerActor : ActorBase
{
    private readonly Dictionary<string, DateTime> activeWork = new Dictionary<string, DateTime>();

    protected override async Task OnReceive(IMessage message)
    {
        switch (message)
        {
            case WorkMessage work:
                await ProcessWork(work);
                break;

            case AskMessage<string> askWork when askWork.OriginalMessage is WorkMessage workMsg:
                try
                {
                    var result = await ProcessWorkInternal(workMsg);
                    askWork.ResponsePromise.SetResult(result);
                }
                catch (Exception ex)
                {
                    askWork.ResponsePromise.SetException(ex);
                }
                break;

            default:
                Logger?.LogWarning("Unknown message type: {MessageType}", message.GetType().Name);
                break;
        }
    }

    private async Task ProcessWork(WorkMessage work)
    {
        try
        {
            activeWork[work.WorkId] = DateTime.UtcNow;
            Logger?.LogInformation("Starting work {WorkId}", work.WorkId);

            var result = await ProcessWorkInternal(work);

            var completeMessage = new WorkCompleteMessage(work.WorkId, result);
            if (Context.Sender != null)
            {
                await Context.Sender.Tell(completeMessage, Context.Self);
            }

            Logger?.LogInformation("Completed work {WorkId}", work.WorkId);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to process work {WorkId}", work.WorkId);
            
            var failedMessage = new WorkFailedMessage(work.WorkId, ex);
            if (Context.Sender != null)
            {
                await Context.Sender.Tell(failedMessage, Context.Self);
            }
        }
        finally
        {
            activeWork.Remove(work.WorkId);
        }
    }

    private async Task<string> ProcessWorkInternal(WorkMessage work)
    {
        // Simulate work processing
        await Task.Delay(work.ProcessingTime);
        
        // Simulate occasional failures
        if (work.Data.Contains("error"))
        {
            throw new InvalidOperationException("Simulated work failure");
        }

        return $"Processed: {work.Data} (Length: {work.Data.Length})";
    }

    protected override Task OnStart()
    {
        Logger?.LogInformation("Worker actor {ActorId} started", Context.ActorId);
        return base.OnStart();
    }

    protected override Task OnStop()
    {
        Logger?.LogInformation("Worker actor {ActorId} stopped. Active work items: {ActiveCount}", 
            Context.ActorId, activeWork.Count);
        return base.OnStop();
    }
}

// Supervisor actor that manages child actors
public record CreateChildMessage(string ChildName, Type ActorType) : ActorMessage;
public record ForwardToChildMessage(string ChildName, IMessage Message) : ActorMessage;
public record ChildActorTerminatedMessage(string ChildName, IActorRef ChildRef) : ActorMessage;

public class SupervisorActor : ActorBase
{
    private readonly Dictionary<string, IActorRef> children = new Dictionary<string, IActorRef>();

    public override ISupervisionStrategy SupervisionStrategy =>
        new OneForOneStrategy()
            .Handle<ArgumentException>(SupervisionDirective.Resume)
            .Handle<InvalidOperationException>(SupervisionDirective.Restart)
            .Handle<TimeoutException>(SupervisionDirective.Resume)
            .Handle<Exception>(SupervisionDirective.Escalate);

    protected override async Task OnReceive(IMessage message)
    {
        switch (message)
        {
            case CreateChildMessage createChild:
                await CreateChild(createChild.ChildName, createChild.ActorType);
                break;

            case ForwardToChildMessage forwardMessage:
                await ForwardToChild(forwardMessage.ChildName, forwardMessage.Message);
                break;

            case ChildActorTerminatedMessage terminated:
                await HandleChildTerminated(terminated.ChildName, terminated.ChildRef);
                break;

            default:
                Logger?.LogWarning("Unknown message type: {MessageType}", message.GetType().Name);
                break;
        }
    }

    private async Task CreateChild(string childName, Type actorType)
    {
        if (children.ContainsKey(childName))
        {
            Logger?.LogWarning("Child actor {ChildName} already exists", childName);
            return;
        }

        try
        {
            // This is a simplified version - in a real implementation, you'd use reflection
            // or a factory to create actors of different types
            if (actorType == typeof(CounterActor))
            {
                var childRef = await Context.ActorOf<CounterActor>(childName);
                children[childName] = childRef;
                Logger?.LogInformation("Created child actor {ChildName} of type {ActorType}", 
                    childName, actorType.Name);
            }
            else if (actorType == typeof(WorkerActor))
            {
                var childRef = await Context.ActorOf<WorkerActor>(childName);
                children[childName] = childRef;
                Logger?.LogInformation("Created child actor {ChildName} of type {ActorType}", 
                    childName, actorType.Name);
            }
            else
            {
                Logger?.LogError("Unknown actor type: {ActorType}", actorType.Name);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to create child actor {ChildName}", childName);
        }
    }

    private async Task ForwardToChild(string childName, IMessage message)
    {
        if (children.TryGetValue(childName, out var childRef))
        {
            await childRef.Tell(message, Context.Sender ?? Context.Self);
        }
        else
        {
            Logger?.LogWarning("Child actor {ChildName} not found", childName);
        }
    }

    private Task HandleChildTerminated(string childName, IActorRef childRef)
    {
        children.Remove(childName);
        Logger?.LogInformation("Child actor {ChildName} terminated", childName);
        return Task.CompletedTask;
    }

    protected override async Task OnStop()
    {
        // Stop all child actors
        var stopTasks = children.Values.Select(child => child.Stop()).ToArray();
        await Task.WhenAll(stopTasks);
        
        Logger?.LogInformation("Supervisor actor {ActorId} stopped. Stopped {ChildCount} children", 
            Context.ActorId, children.Count);
        
        children.Clear();
        await base.OnStop();
    }
}

// Actor system builder with dependency injection support
public class ActorSystemBuilder
{
    private string systemName = "DefaultSystem";
    private readonly ServiceCollection services = new ServiceCollection();

    public ActorSystemBuilder WithName(string name)
    {
        systemName = name;
        return this;
    }

    public ActorSystemBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        configureServices(services);
        return this;
    }

    public ActorSystem Build()
    {
        // Add default logging if not configured
        if (!services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
        {
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        }

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ActorSystem>>();
        
        return new ActorSystem(systemName, serviceProvider, logger);
    }
}

// Performance monitoring for actor systems
public class ActorSystemMetrics
{
    private readonly ConcurrentDictionary<string, ActorMetrics> actorMetrics = new ConcurrentDictionary<string, ActorMetrics>();

    public void RecordMessage(string actorId, string messageType, TimeSpan processingTime, bool successful)
    {
        var metrics = actorMetrics.GetOrAdd(actorId, _ => new ActorMetrics());
        metrics.RecordMessage(messageType, processingTime, successful);
    }

    public void RecordActorLifecycle(string actorId, string eventType)
    {
        var metrics = actorMetrics.GetOrAdd(actorId, _ => new ActorMetrics());
        metrics.RecordLifecycleEvent(eventType);
    }

    public ActorSystemStats GetSystemStats()
    {
        var allMetrics = actorMetrics.Values.ToList();
        
        return new ActorSystemStats
        {
            TotalActors = actorMetrics.Count,
            TotalMessages = allMetrics.Sum(m => m.TotalMessages),
            AverageProcessingTime = allMetrics.Any() 
                ? TimeSpan.FromTicks((long)allMetrics.Average(m => m.AverageProcessingTime.Ticks))
                : TimeSpan.Zero,
            MessageThroughput = allMetrics.Sum(m => m.MessageThroughput),
            ErrorRate = allMetrics.Any() 
                ? allMetrics.Average(m => m.ErrorRate) 
                : 0.0
        };
    }

    public ActorMetrics GetActorMetrics(string actorId)
    {
        return actorMetrics.TryGetValue(actorId, out var metrics) ? metrics : null;
    }
}

public class ActorMetrics
{
    private readonly ConcurrentDictionary<string, long> messageTypeCounts = new ConcurrentDictionary<string, long>();
    private readonly ConcurrentDictionary<string, long> lifecycleEventCounts = new ConcurrentDictionary<string, long>();
    private volatile long totalMessages = 0;
    private volatile long successfulMessages = 0;
    private volatile long totalProcessingTicks = 0;
    private readonly object lockObject = new object();
    private DateTime lastResetTime = DateTime.UtcNow;

    public long TotalMessages => totalMessages;
    public long SuccessfulMessages => successfulMessages;
    public double ErrorRate => totalMessages > 0 ? 1.0 - (double)successfulMessages / totalMessages : 0.0;
    
    public TimeSpan AverageProcessingTime => totalMessages > 0 
        ? TimeSpan.FromTicks(totalProcessingTicks / totalMessages) 
        : TimeSpan.Zero;
    
    public double MessageThroughput
    {
        get
        {
            var elapsed = DateTime.UtcNow - lastResetTime;
            return elapsed.TotalSeconds > 0 ? totalMessages / elapsed.TotalSeconds : 0.0;
        }
    }

    public void RecordMessage(string messageType, TimeSpan processingTime, bool successful)
    {
        messageTypeCounts.AddOrUpdate(messageType, 1, (_, count) => count + 1);
        
        Interlocked.Increment(ref totalMessages);
        if (successful)
        {
            Interlocked.Increment(ref successfulMessages);
        }
        
        Interlocked.Add(ref totalProcessingTicks, processingTime.Ticks);
    }

    public void RecordLifecycleEvent(string eventType)
    {
        lifecycleEventCounts.AddOrUpdate(eventType, 1, (_, count) => count + 1);
    }

    public Dictionary<string, long> GetMessageTypeCounts()
    {
        return new Dictionary<string, long>(messageTypeCounts);
    }

    public Dictionary<string, long> GetLifecycleEventCounts()
    {
        return new Dictionary<string, long>(lifecycleEventCounts);
    }

    public void Reset()
    {
        lock (lockObject)
        {
            messageTypeCounts.Clear();
            lifecycleEventCounts.Clear();
            totalMessages = 0;
            successfulMessages = 0;
            totalProcessingTicks = 0;
            lastResetTime = DateTime.UtcNow;
        }
    }
}

public class ActorSystemStats
{
    public int TotalActors { get; set; }
    public long TotalMessages { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public double MessageThroughput { get; set; }
    public double ErrorRate { get; set; }
}

// Remote actor system support (simplified)
public interface IRemoteActorSystem : IActorSystem
{
    Task<IActorRef> ActorSelection(string path);
    Task RegisterRemoteSystem(string address, int port);
    Task UnregisterRemoteSystem(string address, int port);
}

public record RemoteMessage(string TargetPath, IMessage Message, string SourceSystemId) : ActorMessage;

// Distributed actor system with clustering support
public class ClusteredActorSystem : ActorSystem, IRemoteActorSystem
{
    private readonly ConcurrentDictionary<string, RemoteSystemInfo> remoteSystems;
    private readonly string localAddress;
    private readonly int localPort;

    public ClusteredActorSystem(string name, string address, int port, IServiceProvider serviceProvider, ILogger<ActorSystem> logger)
        : base(name, serviceProvider, logger)
    {
        localAddress = address;
        localPort = port;
        remoteSystems = new ConcurrentDictionary<string, RemoteSystemInfo>();
    }

    public Task<IActorRef> ActorSelection(string path)
    {
        // Implementation for remote actor selection
        var actor = GetActor(path);
        return Task.FromResult(actor);
    }

    public Task RegisterRemoteSystem(string address, int port)
    {
        var key = $"{address}:{port}";
        var info = new RemoteSystemInfo(address, port, DateTime.UtcNow);
        remoteSystems[key] = info;
        return Task.CompletedTask;
    }

    public Task UnregisterRemoteSystem(string address, int port)
    {
        var key = $"{address}:{port}";
        remoteSystems.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private record RemoteSystemInfo(string Address, int Port, DateTime RegisteredAt);
}
```

**Usage**:

```csharp
// Example 1: Basic Actor System Setup
Console.WriteLine("Basic Actor System Examples:");

var actorSystem = new ActorSystemBuilder()
    .WithName("ExampleSystem")
    .ConfigureServices(services =>
    {
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

// Create counter actors
var counter1 = await actorSystem.ActorOf<CounterActor>("counter1");
var counter2 = await actorSystem.ActorOf<CounterActor>("counter2");

// Send increment messages
await counter1.Tell(new IncrementMessage(5));
await counter1.Tell(new IncrementMessage(3));
await counter2.Tell(new IncrementMessage(10));

// Query counter values using Ask pattern
var count1 = await counter1.Ask<int>(new GetCountMessage(), TimeSpan.FromSeconds(5));
var count2 = await counter2.Ask<int>(new GetCountMessage(), TimeSpan.FromSeconds(5));

Console.WriteLine($"Counter 1: {count1}, Counter 2: {count2}");

// Example 2: Worker Actors with Error Handling
Console.WriteLine("\nWorker Actor Examples:");

var worker1 = await actorSystem.ActorOf<WorkerActor>("worker1");
var worker2 = await actorSystem.ActorOf<WorkerActor>("worker2");

// Send work messages
var workTasks = new List<Task>();

for (int i = 1; i <= 10; i++)
{
    var workId = $"work-{i}";
    var data = i == 5 ? "error-data" : $"data-{i}"; // Simulate error on work item 5
    var processingTime = TimeSpan.FromMilliseconds(100 + (i * 50));
    
    var worker = i % 2 == 0 ? worker1 : worker2;
    
    workTasks.Add(Task.Run(async () =>
    {
        try
        {
            var result = await worker.Ask<string>(new WorkMessage(workId, data, processingTime), 
                TimeSpan.FromSeconds(10));
            Console.WriteLine($"Work {workId} completed: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Work {workId} failed: {ex.Message}");
        }
    }));
}

await Task.WhenAll(workTasks);

// Example 3: Supervisor Actor Hierarchy
Console.WriteLine("\nSupervisor Actor Examples:");

var supervisor = await actorSystem.ActorOf<SupervisorActor>("supervisor");

// Create child actors through supervisor
await supervisor.Tell(new CreateChildMessage("childCounter", typeof(CounterActor)));
await supervisor.Tell(new CreateChildMessage("childWorker", typeof(WorkerActor)));

// Forward messages to children
await supervisor.Tell(new ForwardToChildMessage("childCounter", new IncrementMessage(25)));
await supervisor.Tell(new ForwardToChildMessage("childWorker", 
    new WorkMessage("supervised-work", "important-data", TimeSpan.FromMilliseconds(200))));

// Give time for processing
await Task.Delay(500);

// Query child counter through supervisor
await supervisor.Tell(new ForwardToChildMessage("childCounter", new GetCountMessage()));

// Example 4: Actor System Events and Monitoring
Console.WriteLine("\nActor System Events and Monitoring:");

var metrics = new ActorSystemMetrics();

// Subscribe to actor system events
actorSystem.ActorSystemEvent += (sender, args) =>
{
    Console.WriteLine($"Actor System Event - Type: {args.EventType}, " +
                     $"Actor: {args.ActorId}, Message: {args.Message}");
    
    if (args.Exception != null)
    {
        Console.WriteLine($"Exception: {args.Exception.Message}");
    }
    
    metrics.RecordActorLifecycle(args.ActorId, args.EventType);
};

// Create multiple actors to demonstrate monitoring
var monitoredActors = new List<IActorRef>();

for (int i = 1; i <= 5; i++)
{
    var actor = await actorSystem.ActorOf<CounterActor>($"monitored-{i}");
    monitoredActors.Add(actor);
}

// Send messages and record metrics
var messageStartTime = DateTime.UtcNow;

foreach (var actor in monitoredActors)
{
    var messageStart = Stopwatch.StartNew();
    
    try
    {
        await actor.Tell(new IncrementMessage(i));
        messageStart.Stop();
        
        metrics.RecordMessage(actor.ActorId, nameof(IncrementMessage), 
            messageStart.Elapsed, true);
    }
    catch (Exception ex)
    {
        messageStart.Stop();
        metrics.RecordMessage(actor.ActorId, nameof(IncrementMessage), 
            messageStart.Elapsed, false);
    }
}

// Display system metrics
await Task.Delay(1000); // Let events propagate

var systemStats = metrics.GetSystemStats();
Console.WriteLine($"System Stats - Total Actors: {systemStats.TotalActors}, " +
                 $"Total Messages: {systemStats.TotalMessages}, " +
                 $"Throughput: {systemStats.MessageThroughput:F2} msgs/sec, " +
                 $"Error Rate: {systemStats.ErrorRate:P2}");

// Example 5: Concurrent Message Processing
Console.WriteLine("\nConcurrent Message Processing Examples:");

var concurrentWorkers = new List<IActorRef>();

// Create a pool of worker actors
for (int i = 1; i <= 5; i++)
{
    var worker = await actorSystem.ActorOf<WorkerActor>($"concurrent-worker-{i}");
    concurrentWorkers.Add(worker);
}

// Send concurrent work to all workers
var concurrentTasks = Enumerable.Range(1, 100).Select(async i =>
{
    var worker = concurrentWorkers[i % concurrentWorkers.Count];
    var workId = $"concurrent-work-{i}";
    var data = $"payload-{i}";
    var processingTime = TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200));
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await worker.Ask<string>(new WorkMessage(workId, data, processingTime), 
            TimeSpan.FromSeconds(5));
        
        stopwatch.Stop();
        metrics.RecordMessage(worker.ActorId, nameof(WorkMessage), stopwatch.Elapsed, true);
        
        return new { WorkId = workId, Success = true, Result = result, Duration = stopwatch.Elapsed };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        metrics.RecordMessage(worker.ActorId, nameof(WorkMessage), stopwatch.Elapsed, false);
        
        return new { WorkId = workId, Success = false, Result = ex.Message, Duration = stopwatch.Elapsed };
    }
}).ToArray();

var concurrentResults = await Task.WhenAll(concurrentTasks);

var successfulWork = concurrentResults.Count(r => r.Success);
var failedWork = concurrentResults.Count(r => !r.Success);
var avgDuration = concurrentResults.Average(r => r.Duration.TotalMilliseconds);

Console.WriteLine($"Concurrent Processing Results:");
Console.WriteLine($"  Successful: {successfulWork}, Failed: {failedWork}");
Console.WriteLine($"  Average Duration: {avgDuration:F2}ms");
Console.WriteLine($"  Total Processing Time: {concurrentResults.Sum(r => r.Duration.TotalMilliseconds):F2}ms");

// Example 6: Actor Lifecycle and Supervision
Console.WriteLine("\nActor Lifecycle and Supervision Examples:");

// Create a supervisor with custom supervision strategy
var faultTolerantSupervisor = await actorSystem.ActorOf<SupervisorActor>("fault-supervisor");

// Create children that will experience different types of failures
await faultTolerantSupervisor.Tell(new CreateChildMessage("resilientWorker1", typeof(WorkerActor)));
await faultTolerantSupervisor.Tell(new CreateChildMessage("resilientWorker2", typeof(WorkerActor)));

// Send work that will cause different types of exceptions
var faultWorkTasks = new[]
{
    new WorkMessage("normal-work", "good-data", TimeSpan.FromMilliseconds(100)),
    new WorkMessage("error-work", "error-data", TimeSpan.FromMilliseconds(100)), // Will cause exception
    new WorkMessage("timeout-work", "slow-data", TimeSpan.FromSeconds(10)), // Will timeout
    new WorkMessage("recovery-work", "recovery-data", TimeSpan.FromMilliseconds(100))
};

foreach (var work in faultWorkTasks)
{
    var targetWorker = work.WorkId.Contains("1") ? "resilientWorker1" : "resilientWorker2";
    
    Console.WriteLine($"Sending {work.WorkId} to {targetWorker}");
    await faultTolerantSupervisor.Tell(new ForwardToChildMessage(targetWorker, work));
    
    await Task.Delay(200); // Brief delay between messages
}

// Example 7: Performance Comparison and Load Testing
Console.WriteLine("\nPerformance and Load Testing Examples:");

const int loadTestMessages = 10000;
var loadTestWorkers = new List<IActorRef>();

// Create worker pool
for (int i = 0; i < Environment.ProcessorCount; i++)
{
    var worker = await actorSystem.ActorOf<WorkerActor>($"load-test-worker-{i}");
    loadTestWorkers.Add(worker);
}

var loadTestStopwatch = Stopwatch.StartNew();

// Fire-and-forget load test
var fireAndForgetTasks = Enumerable.Range(1, loadTestMessages).Select(async i =>
{
    var worker = loadTestWorkers[i % loadTestWorkers.Count];
    await worker.Tell(new IncrementMessage(1)); // Use simple message for throughput test
}).ToArray();

await Task.WhenAll(fireAndForgetTasks);
loadTestStopwatch.Stop();

var throughput = loadTestMessages / loadTestStopwatch.Elapsed.TotalSeconds;
Console.WriteLine($"Fire-and-forget throughput: {throughput:F0} messages/second");

// Request-response load test
loadTestStopwatch.Restart();

var requestResponseTasks = Enumerable.Range(1, 1000).Select(async i =>
{
    var worker = loadTestWorkers[i % loadTestWorkers.Count];
    return await worker.Ask<string>(new WorkMessage($"load-{i}", $"data-{i}", 
        TimeSpan.FromMilliseconds(10)), TimeSpan.FromSeconds(5));
}).ToArray();

var loadTestResults = await Task.WhenAll(requestResponseTasks);
loadTestStopwatch.Stop();

var requestResponseThroughput = loadTestResults.Length / loadTestStopwatch.Elapsed.TotalSeconds;
Console.WriteLine($"Request-response throughput: {requestResponseThroughput:F0} messages/second");

// Example 8: Graceful Shutdown
Console.WriteLine("\nGraceful Shutdown Examples:");

// Display final metrics before shutdown
var finalStats = metrics.GetSystemStats();
Console.WriteLine($"Final System Statistics:");
Console.WriteLine($"  Total Actors Created: {finalStats.TotalActors}");
Console.WriteLine($"  Total Messages Processed: {finalStats.TotalMessages}");
Console.WriteLine($"  Average Processing Time: {finalStats.AverageProcessingTime.TotalMilliseconds:F2}ms");
Console.WriteLine($"  Overall Throughput: {finalStats.MessageThroughput:F2} msgs/sec");
Console.WriteLine($"  Error Rate: {finalStats.ErrorRate:P2}");

// Graceful shutdown of actor system
Console.WriteLine("Shutting down actor system...");
var shutdownStopwatch = Stopwatch.StartNew();

await actorSystem.Shutdown();

shutdownStopwatch.Stop();
Console.WriteLine($"Actor system shutdown completed in {shutdownStopwatch.ElapsedMilliseconds}ms");

Console.WriteLine("\nActor model examples completed!");
```

**Notes**:

- Implement comprehensive actor-based concurrency with message-passing isolation and fault-tolerance
- Support actor lifecycle management including start, stop, restart, and supervision strategies  
- Use bounded mailboxes with channels for high-performance message delivery and backpressure handling
- Implement supervision hierarchies with configurable fault-tolerance policies and error escalation
- Support both fire-and-forget (Tell) and request-response (Ask) messaging patterns
- Provide actor system events and comprehensive performance monitoring for operational visibility
- Implement proper resource cleanup and graceful shutdown procedures for production use
- Support concurrent message processing with worker pools and load balancing strategies
- Use dependency injection integration for flexible actor system configuration and testing
- Implement custom supervision strategies for different exception types and recovery scenarios
- Support distributed actor systems with remote actor selection and clustering capabilities
- Provide comprehensive metrics collection including throughput, latency, and error rates

**Prerequisites**:

- Understanding of actor model concepts including message-passing, isolation, and fault-tolerance
- Knowledge of async/await programming and Task-based concurrency patterns
- Familiarity with Channel-based communication and producer-consumer scenarios
- Experience with dependency injection and service provider patterns
- Understanding of supervision strategies and fault-tolerance in distributed systems

**Related Snippets**:

- [Concurrent Collections](concurrent-collections.md) - Thread-safe collections and lock-free data structures
- [Producer-Consumer](producer-consumer.md) - Producer-consumer patterns with channels and bounded queues
- [Message Queue](message-queue.md) - Reliable messaging patterns with persistence and delivery guarantees
