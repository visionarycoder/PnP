namespace Snippets.DesignPatterns.Behavioral.Mediator;

// Colleague interface

// Base colleague implementation
public abstract class BaseColleague(string id) : IColleague
{
    public string Id { get; } = id;
    public IMediator? Mediator { get; set; }

    public abstract Task ReceiveAsync(object message, IColleague sender);

    protected async Task SendAsync(object message)
    {
        if (Mediator != null)
        {
            await Mediator.SendAsync(message, this);
        }
    }
}

// Message types

// User profile

// Chat room mediator

// Chat participants

// UI Dialog Mediator Example

// Dialog mediator

// UI Components