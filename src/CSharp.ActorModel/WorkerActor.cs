namespace CSharp.ActorModel;

/// <summary>
/// Demo worker actor for coordination scenarios
/// </summary>
public class WorkerActor : ActorBase
{
    protected override Task OnReceive(IMessage message)
    {
        Logger?.LogInformation("Worker {ActorId} processing message", Context.Self.Path);
        return Task.CompletedTask;
    }
}