using System.Collections.Concurrent;

namespace CSharp.ActorModel;

// Ask message for request-response pattern

// Actor reference implementation
public class ActorRef : IActorRef
{
    private readonly ActorBase actor;
    private readonly IMailbox mailbox;
    private readonly ILogger? logger;
    private volatile bool isTerminated = false;

    public ActorRef(string actorId, string path, ActorBase actor, IMailbox mailbox, ILogger? logger)
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

    public async Task Tell(IMessage message, IActorRef? sender = null)
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