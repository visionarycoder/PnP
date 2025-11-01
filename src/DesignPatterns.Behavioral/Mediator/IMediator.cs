namespace Snippets.DesignPatterns.Behavioral.Mediator;

/// <summary>
/// Mediator Pattern Implementation
/// Defines how a set of objects interact with each other. Promotes loose coupling 
/// by keeping objects from referring to each other explicitly, and letting you 
/// vary their interaction independently through a mediator object.
/// </summary>

// Mediator interface
public interface IMediator
{
    Task SendAsync(object message, IColleague sender);
    void RegisterColleague(IColleague colleague);
    void RemoveColleague(IColleague colleague);
}