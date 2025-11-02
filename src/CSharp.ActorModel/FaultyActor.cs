namespace CSharp.ActorModel;

// Demo messages for fault tolerance testing
public record CauseArgumentExceptionMessage() : ActorMessage;
public record CauseInvalidOperationMessage() : ActorMessage;
public record NormalMessage(string Text) : ActorMessage;

/// <summary>
/// Demo actor that demonstrates supervision strategies by throwing various exceptions
/// </summary>
public class FaultyActor : ActorBase
{
    protected override Task OnReceive(IMessage message)
    {
        return message switch
        {
            CauseArgumentExceptionMessage => Task.FromException(new ArgumentException("Simulated argument error")),
            CauseInvalidOperationMessage => Task.FromException(new InvalidOperationException("Simulated operation error")),
            NormalMessage normal => HandleNormal(normal),
            _ => Task.CompletedTask
        };
    }

    private Task HandleNormal(NormalMessage message)
    {
        Logger?.LogInformation("Processed normal message: {Text}", message.Text);
        return Task.CompletedTask;
    }
}