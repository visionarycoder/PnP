namespace CSharp.ActorModel;

// System message base class

// Base actor implementation
public abstract class ActorBase : IDisposable
{
    private IActorContext? context;
    private IMailbox? mailbox;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? messageLoop;
    private volatile bool isRunning = false;
    private volatile bool isDisposed = false;

    protected IActorContext Context => context ?? throw new InvalidOperationException("Actor not initialized");
    protected ILogger? Logger => context?.Logger;

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
            cancellationTokenSource?.Cancel();
            
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
            while (isRunning && !cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    var envelope = await mailbox!.Receive(cancellationTokenSource.Token);
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
            ((ActorContext)context!).SetSender(envelope.Sender);

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
                if (Context.System.ActorSystemEvent != null)
                {
                    await Task.Run(() => Context.System.ActorSystemEvent.Invoke(Context.System, 
                        new ActorSystemEventArgs 
                        { 
                            EventType = "Exception", 
                            ActorId = Context.ActorId, 
                            Exception = exception 
                        }));
                }
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