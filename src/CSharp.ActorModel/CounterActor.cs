namespace CSharp.ActorModel;

// Example actor messages

// Example Counter actor implementation
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

    protected override Task OnRestart()
    {
        Logger?.LogInformation("Counter actor {ActorId} restarting, resetting count", Context.ActorId);
        count = 0; // Reset state on restart
        return base.OnRestart();
    }
}