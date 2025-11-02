namespace CSharp.ActorModel;

// Demo message for coordination scenarios
public record CoordinateWorkMessage(params string[] Tasks) : ActorMessage;

/// <summary>
/// Demo actor that demonstrates supervision and coordination patterns
/// </summary>
public class SupervisorActor : ActorBase
{
    protected override Task OnReceive(IMessage message)
    {
        if (message is CoordinateWorkMessage coordinate)
        {
            Logger?.LogInformation("Supervisor coordinating {Count} tasks", coordinate.Tasks.Length);
            foreach (var task in coordinate.Tasks)
            {
                Logger?.LogInformation("Delegating task: {Task}", task);
            }
        }

        return Task.CompletedTask;
    }
}