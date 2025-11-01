namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class TicketResponse
{
    public bool IsHandled { get; init; }
    public string HandlerName { get; init; } = "";
    public string Message { get; init; } = "";
    public string AssignedTo { get; init; } = "";
    public TimeSpan EstimatedResolution { get; init; }

    public static TicketResponse Handle(string handlerName, string assignedTo, string message,
        TimeSpan estimatedResolution)
    {
        return new TicketResponse
        {
            IsHandled = true,
            HandlerName = handlerName,
            AssignedTo = assignedTo,
            Message = message,
            EstimatedResolution = estimatedResolution
        };
    }

    public static TicketResponse PassThrough()
    {
        return new TicketResponse { IsHandled = false };
    }
}