namespace Snippets.DesignPatterns.Behavioral.Mediator;

public interface IColleague
{
    string Id { get; }
    IMediator? Mediator { get; set; }
    Task ReceiveAsync(object message, IColleague sender);
}