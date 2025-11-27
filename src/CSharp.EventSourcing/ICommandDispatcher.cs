namespace CSharp.EventSourcing;

/// <summary>
/// Dispatcher interface for commands
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatch a command to its appropriate handler
    /// </summary>
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken token = default) where TCommand : ICommand;
}