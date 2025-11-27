namespace CSharp.EventSourcing;

/// <summary>
/// Handler interface for processing commands
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handle the specified command
    /// </summary>
    Task HandleAsync(TCommand command, CancellationToken token = default);
}